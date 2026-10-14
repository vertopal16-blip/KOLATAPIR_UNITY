using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kolotopir.Core;
using UnityEngine;

namespace Kolotopir
{
    /// <summary>
    /// Reproduce efectos 2D/3D, ambientes por región (doc: "cada región tiene un paisaje sonoro reconocible"),
    /// voces y música selectiva. Los sonidos importantes generan señales visuales opcionales (accesibilidad).
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        private readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        private readonly List<AudioSource> _pool = new List<AudioSource>();
        private static readonly List<KeyValuePair<AudioSource, string>> PendingLoops = new List<KeyValuePair<AudioSource, string>>();
        private static readonly List<AudioSource> WorldLoops = new List<AudioSource>();
        private AudioSource _ui, _voice, _musicA, _musicB;
        private AudioSource[] _amb;
        private readonly float[] _ambTarget = new float[5];
        private readonly string[] _ambClips = { "viento", "pradera", "grillos", "campanillas", "ruinas" };
        private float _birdTimer = 3f;
        private bool _musicOnA = true;
        private float _musicTarget;
        private Coroutine _voiceCo;

        public bool Ready { get; private set; }
        public float Master = 1f, MusicVolume = 0.6f, SfxVolume = 0.9f, AmbienceVolume = 0.7f, VoiceVolume = 0.8f;

        public IEnumerator Init()
        {
            _ui = Source2D("UI");
            _voice = Source2D("Voz");
            _musicA = Source2D("MusicaA"); _musicA.loop = true;
            _musicB = Source2D("MusicaB"); _musicB.loop = true;
            _amb = new AudioSource[_ambClips.Length];
            for (int i = 0; i < _amb.Length; i++) { _amb[i] = Source2D("Ambiente_" + _ambClips[i]); _amb[i].loop = true; }
            for (int i = 0; i < 20; i++)
            {
                var s = new GameObject("Sfx3D").AddComponent<AudioSource>();
                s.transform.SetParent(transform, false);
                s.spatialBlend = 1f;
                s.rolloffMode = AudioRolloffMode.Linear;
                s.playOnAwake = false;
                s.dopplerLevel = 0f;
                _pool.Add(s);
            }

            List<AudioSynth.ClipData> data = null;
            var task = Task.Run(() => data = AudioSynth.GenerateAll());
            while (!task.IsCompleted) yield return null;
            if (task.Exception != null) { Debug.LogException(task.Exception); yield break; }
            int n = 0;
            foreach (var d in data)
            {
                var clip = AudioClip.Create(d.Name, d.Samples.Length, 1, AudioSynth.Rate, false);
                clip.SetData(d.Samples, 0);
                _clips[d.Name] = clip;
                if (++n % 12 == 0) yield return null;
            }
            for (int i = 0; i < _amb.Length; i++)
            {
                _amb[i].clip = Clip(_ambClips[i]);
                _amb[i].volume = 0f;
                if (_amb[i].clip != null) _amb[i].Play();
            }
            Ready = true;
            foreach (var kv in PendingLoops)
                if (kv.Key != null) { kv.Key.clip = Clip(kv.Value); if (kv.Key.clip != null) kv.Key.Play(); }
            PendingLoops.Clear();
        }

        private AudioSource Source2D(string name)
        {
            var s = new GameObject(name).AddComponent<AudioSource>();
            s.transform.SetParent(transform, false);
            s.spatialBlend = 0f;
            s.playOnAwake = false;
            return s;
        }

        public AudioClip Clip(string name) => name != null && _clips.TryGetValue(name, out var c) ? c : null;

        public void Play2D(string clip, float vol = 1f, float pitch = 1f)
        {
            var c = Clip(clip);
            if (c == null) return;
            _ui.pitch = pitch;
            _ui.PlayOneShot(c, vol * SfxVolume * Master);
        }

        public void Play3D(string clip, Vector3 pos, float vol = 1f, float pitch = 1f, string cue = null, float maxDistance = 32f)
        {
            var c = Clip(clip);
            if (c != null)
            {
                AudioSource src = null;
                foreach (var s in _pool) if (!s.isPlaying) { src = s; break; }
                if (src == null) src = _pool[0];
                src.transform.position = pos;
                src.clip = c;
                src.volume = vol * SfxVolume * Master;
                src.pitch = pitch * Random.Range(0.96f, 1.04f);
                src.maxDistance = maxDistance;
                src.minDistance = 1.5f;
                src.Play();
            }
            if (!string.IsNullOrEmpty(cue) && Game.UI != null) Game.UI.SoundCue(pos, cue);
        }

        /// <summary>Bucle 3D pegado a un objeto (fuego, agua, Cápsulas, ruinas).</summary>
        public static AudioSource AttachLoop(GameObject go, string clip, float vol, float maxDistance)
        {
            var s = go.AddComponent<AudioSource>();
            s.loop = true;
            s.spatialBlend = 1f;
            s.rolloffMode = AudioRolloffMode.Linear;
            s.minDistance = 2f;
            s.maxDistance = maxDistance;
            s.dopplerLevel = 0f;
            s.playOnAwake = false;
            var mgr = Game.Audio;
            float master = mgr != null ? mgr.Master * mgr.AmbienceVolume : 0.7f;
            s.volume = vol * master;
            var loop = go.AddComponent<WorldLoop>();
            loop.BaseVolume = vol;
            if (mgr != null && mgr.Ready)
            {
                s.clip = mgr.Clip(clip);
                if (s.clip != null) s.Play();
            }
            else PendingLoops.Add(new KeyValuePair<AudioSource, string>(s, clip));
            return s;
        }

        /// <summary>Balbuceo de voz al hablar: sílabas sintetizadas con el tono del personaje.</summary>
        public void Voice(Species species, float pitch, int syllables, Vector3? at = null)
        {
            if (_voiceCo != null) StopCoroutine(_voiceCo);
            _voiceCo = StartCoroutine(VoiceRoutine(species, pitch, Mathf.Clamp(syllables, 1, 9), at));
        }

        private IEnumerator VoiceRoutine(Species species, float pitch, int syllables, Vector3? at)
        {
            string prefix = species == Species.Kolotapir ? "voz_kolotapir_" : "voz_humano_";
            for (int i = 0; i < syllables; i++)
            {
                var c = Clip(prefix + Random.Range(0, 6));
                if (c != null)
                {
                    _voice.pitch = pitch * Random.Range(0.92f, 1.08f);
                    _voice.PlayOneShot(c, 0.55f * VoiceVolume * Master);
                }
                yield return new WaitForSecondsRealtime(Random.Range(0.1f, 0.16f));
            }
        }

        public void PlayMusic(string clip, bool loop = false, float volume = 1f)
        {
            var c = Clip(clip);
            if (c == null) return;
            _musicOnA = !_musicOnA;
            var src = _musicOnA ? _musicA : _musicB;
            src.clip = c;
            src.loop = loop;
            src.volume = 0f;
            src.Play();
            _musicTarget = volume;
        }

        public void StopMusic() => _musicTarget = 0f;

        /// <summary>Capas de ambiente según región y momento del día.</summary>
        public void SetAmbience(Region region, float night, bool listening)
        {
            bool tribe = region == Region.TribuDelReverso;
            bool ruins = region == Region.ParadaAntigua;
            _ambTarget[0] = region == Region.Abismo || region == Region.BordeDelMundo ? 1f : 0.55f;
            _ambTarget[1] = !tribe && !ruins ? (1f - night * 0.5f) * 0.8f : 0.15f;
            _ambTarget[2] = night * 0.7f;
            _ambTarget[3] = tribe ? 0.6f * (1f - night * 0.3f) : 0f;
            _ambTarget[4] = ruins ? 0.8f : 0f;
            if (listening) for (int i = 0; i < _ambTarget.Length; i++) _ambTarget[i] *= 0.3f;
        }

        private void Update()
        {
            if (!Ready) return;
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < _amb.Length; i++)
                _amb[i].volume = Mathf.MoveTowards(_amb[i].volume, _ambTarget[i] * AmbienceVolume * Master, dt * 0.4f);
            var cur = _musicOnA ? _musicA : _musicB;
            var other = _musicOnA ? _musicB : _musicA;
            cur.volume = Mathf.MoveTowards(cur.volume, _musicTarget * MusicVolume * Master, dt * 0.3f);
            other.volume = Mathf.MoveTowards(other.volume, 0f, dt * 0.4f);
            if (other.volume <= 0.001f && other.isPlaying) other.Stop();
            if (!cur.loop && cur.isPlaying && cur.clip != null && cur.time > cur.clip.length - 2f) _musicTarget = 0f;

            // Pájaros durante el día cerca de Nilo
            _birdTimer -= Time.deltaTime;
            if (_birdTimer <= 0f && Game.Sky != null && Game.Player != null)
            {
                _birdTimer = Random.Range(3f, 9f);
                if (Game.Sky.Night < 0.4f)
                {
                    var p = Game.Player.transform.position + new Vector3(Random.Range(-20f, 20f), Random.Range(4f, 10f), Random.Range(-20f, 20f));
                    Play3D("pajaro_" + Random.Range(0, 4), p, 0.35f * AmbienceVolume, Random.Range(0.9f, 1.15f), null, 40f);
                }
            }
        }

        public void ApplyVolumes()
        {
            AudioListener.volume = 1f;
            foreach (var l in WorldLoops)
                if (l != null) { var w = l.GetComponent<WorldLoop>(); if (w != null) l.volume = w.BaseVolume * AmbienceVolume * Master; }
        }

        public static void RegisterLoop(AudioSource s) { if (!WorldLoops.Contains(s)) WorldLoops.Add(s); }
        public static void UnregisterLoop(AudioSource s) => WorldLoops.Remove(s);
    }

    /// <summary>Registra un bucle de ambiente para que respete el volumen de los ajustes.</summary>
    public sealed class WorldLoop : MonoBehaviour
    {
        public float BaseVolume = 1f;
        private AudioSource _src;
        private void OnEnable() { _src = GetComponent<AudioSource>(); if (_src != null) AudioManager.RegisterLoop(_src); }
        private void OnDisable() { if (_src != null) AudioManager.UnregisterLoop(_src); }
    }
}
