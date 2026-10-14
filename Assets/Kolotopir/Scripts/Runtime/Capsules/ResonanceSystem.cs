using System;
using System.Collections;
using System.Collections.Generic;
using Kolotopir.Core;
using UnityEngine;

namespace Kolotopir
{
    /// <summary>Punto del mundo con una Resonancia: señales visuales y sonoras legibles (doc 11).</summary>
    public sealed class ResonanceSpot : MonoBehaviour, IListenable
    {
        public ResonanceSpotDef Def;
        private ParticleSystem _motes;
        private AudioSource _chime;
        private Transform _ring;
        private float _check;
        private bool _available;

        public Vector3 ListenPoint => transform.position;
        public float ListenRadius => Def.Radius;
        public string ListenLabel => GameDb.Resonance(Def.ResonanceId)?.Title ?? "Resonancia";
        public bool IsAvailable => _available;

        private void Start()
        {
            _motes = FX.Motes(transform, Vector3.zero, K.WithA(Pal.ResonanceSoft, 0.8f), 0.5f, 6f);
            _ring = K.Child(transform, "Anillo", Vector3.down * (Def.YOffset - 0.05f));
            var b = new MeshBuilder();
            int seg = 32;
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * Mathf.PI * 2f / seg, a1 = (i + 1) * Mathf.PI * 2f / seg;
                Vector3 i0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * 0.9f, i1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * 0.9f;
                b.Quad(i0, i1, i1 / 0.9f, i0 / 0.9f, Vector3.up);
            }
            _ring.gameObject.AddComponent<MeshFilter>().sharedMesh = b.ToMesh("AnilloResonancia");
            var mr = _ring.gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = MaterialLibrary.UnlitTransparent(K.WithA(Pal.ResonanceSoft, 0.35f));
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _ring.localScale = Vector3.one * Mathf.Max(0.8f, Def.Radius * 0.45f);
            _chime = AudioManager.AttachLoop(gameObject, "res_campanilla", 0.45f, 14f);
            Refresh();
        }

        private void OnEnable() => Listenables.All.Add(this);
        private void OnDisable() => Listenables.All.Remove(this);

        private void Update()
        {
            _check -= Time.deltaTime;
            if (_check <= 0f) { _check = 0.5f; Refresh(); }
            if (_ring != null) _ring.Rotate(0f, Time.deltaTime * 12f, 0f);
        }

        private void Refresh()
        {
            var s = Game.State;
            if (s == null) return;
            _available = Def.Active == null || Def.Active(s);
            bool heard = s.Heard(Def.ResonanceId);
            bool show = _available;
            if (_motes != null)
            {
                var em = _motes.emission;
                em.rateOverTime = show ? (heard ? 1.5f : 6f) : 0f;
            }
            if (_ring != null) _ring.gameObject.SetActive(show && !heard);
            if (_chime != null) _chime.mute = !show || heard;
        }

        public string NextResonance(out string reason)
        {
            reason = null;
            var def = GameDb.Resonance(Def.ResonanceId);
            if (def == null) return null;
            if (def.Requirement != null && !def.Requirement(Game.State))
            {
                reason = def.RequirementText;
                return null;
            }
            return def.Id;
        }

        public void OnListened(string resonanceId) => Refresh();
    }

    /// <summary>
    /// Reproduce una Resonancia: fragmentos breves y evocadores (no una secuencia explicativa larga),
    /// registra la nota en el cuaderno y aplica sus efectos. Respeta la privacidad y el consentimiento.
    /// </summary>
    public sealed class ResonanceSystem : MonoBehaviour
    {
        public bool Playing { get; private set; }
        private bool _skip;

        public void Request(string id, Vector3 at, Action onFinished = null)
        {
            var def = GameDb.Resonance(id);
            var s = Game.State;
            if (def == null || Playing) return;
            if (def.Requirement != null && !def.Requirement(s))
            {
                Game.UI?.Toast(def.RequirementText);
                return;
            }
            if (def.IsPrivate && !s.Has(def.ConsentFlag) && !s.Heard(id))
            {
                var owner = GameDb.Npc(def.OwnerNpc);
                string name = owner != null ? owner.Name : "alguien";
                Game.UI.Confirm("Memoria privada",
                    $"Este recuerdo pertenece a {name}. No te ha dado permiso para escucharlo.\n\nAcceder a recuerdos personales sin consentimiento puede tener consecuencias.",
                    "Escuchar igualmente", "Respetar su privacidad",
                    ok =>
                    {
                        if (ok) StartCoroutine(Play(def, at, onFinished));
                        else
                        {
                            Game.State.Flags.Set("respeto_privacidad_" + id);
                            Game.UI?.Toast("Decides no escucharlo.");
                        }
                    });
                return;
            }
            StartCoroutine(Play(def, at, onFinished));
        }

        public void Skip() => _skip = true;

        private IEnumerator Play(ResonanceDef def, Vector3 at, Action onFinished)
        {
            Playing = true;
            _skip = false;
            var s = Game.State;
            bool first = !s.Heard(def.Id);
            Game.Audio?.Play2D("res_fragmento", 0.8f);
            var ghosts = GhostFigures.Spawn(def.Id, at);
            Game.UI?.ResonanceBegin(def.Title, Names.Resonance(def.Type));
            yield return new WaitForSeconds(0.7f);
            foreach (var line in def.Lines)
            {
                _skip = false;
                Game.UI?.ResonanceLine(line.Speaker, line.Text);
                if (!string.IsNullOrEmpty(line.Speaker) && line.Speaker != "Nilo")
                {
                    var sp = line.Speaker == "Guardián" || line.Speaker.Contains("kolotapir") || line.Speaker.StartsWith("Boro") ? Species.Kolotapir : Species.Humano;
                    Game.Audio?.Voice(sp, sp == Species.Kolotapir ? 0.7f : 0.85f, Mathf.Clamp(line.Text.Length / 12, 2, 7));
                }
                ghosts?.Talk(line.Speaker);
                float wait = 1.8f + line.Text.Length * 0.045f;
                float t = 0f;
                while (t < wait && !_skip)
                {
                    t += Time.deltaTime;
                    if (t > 0.6f && InputMap.Down(InputAction.Interactuar)) _skip = true;
                    yield return null;
                }
            }
            Game.UI?.ResonanceEnd();
            ghosts?.Fade();

            if (first)
            {
                var ctx = Game.Ctx;
                s.Flags.Set("heard_" + def.Id);
                ctx.AddNotebook(GameDb.ResonanceEntryId(def.Id), Knowledge.Observado);
                foreach (var e in def.Evidence) ctx.AddEvidence(e);
                if (def.Xp > 0) ctx.AddXp(def.Xp, "Resonancia");
                def.OnHeard?.Invoke(ctx);
                if (!string.IsNullOrEmpty(def.Note)) Game.UI?.Toast("Nota de Nilo: " + def.Note);
                Game.MarkDirty();
            }
            Playing = false;
            onFinished?.Invoke();
        }
    }

    /// <summary>Figuras translúcidas que representan a quienes aparecen en un recuerdo.</summary>
    public sealed class GhostFigures : MonoBehaviour
    {
        private readonly List<HumanoidRig> _rigs = new List<HumanoidRig>();
        private readonly List<string> _names = new List<string>();
        private Material _mat;
        private float _alpha;
        private bool _fading;
        private string _speaking;

        private static readonly Dictionary<string, (string name, Species sp, Appearance look, Vector3 offset)[]> Casts = new Dictionary<string, (string, Species, Appearance, Vector3)[]>
        {
            { "res_recuerdo_daren", new[]
                {
                    ("Daren", Species.Humano, new Appearance { Height = 1.8f, Build = 1.1f, HairStyle = "corto", Beard = true }, new Vector3(-0.7f, 0f, 0.6f)),
                    ("Eira", Species.Humano, new Appearance { Height = 1.68f, HairStyle = "largo" }, new Vector3(0.7f, 0f, 0.6f)),
                } },
            { "res_recuerdo_custodio", new[] { ("Voz grave", Species.Humano, new Appearance { Height = 2.05f, HairStyle = "capucha", Shawl = true }, new Vector3(0f, 0f, 1.6f)) } },
            { "res_guardian_1", new[] { ("Guardián", Species.Kolotapir, new Appearance { Height = 2.2f, Build = 1.3f, Staff = true }, new Vector3(0f, 0f, 1.2f)) } },
            { "res_guardian_2", new[] { ("Guardián", Species.Kolotapir, new Appearance { Height = 2.2f, Build = 1.3f, Staff = true }, new Vector3(0f, 0f, 1.2f)) } },
            { "res_guardian_3", new[] { ("Guardián", Species.Kolotapir, new Appearance { Height = 2.2f, Build = 1.3f, Staff = true }, new Vector3(0f, 0f, 1.2f)) } },
            { "res_boro_cuenta", new[]
                {
                    ("Boro, joven", Species.Kolotapir, new Appearance { Height = 1.7f }, new Vector3(-0.6f, 0f, 1f)),
                    ("Una kolotapir joven", Species.Kolotapir, new Appearance { Height = 1.6f }, new Vector3(0.6f, 0f, 1f)),
                } },
            { "res_talla_refugio", new[]
                {
                    ("Daren", Species.Humano, new Appearance { Height = 1.8f, Build = 1.1f, HairStyle = "corto", Beard = true }, new Vector3(0f, 0f, 0.8f)),
                    ("Eira", Species.Humano, new Appearance { Height = 1.68f, HairStyle = "largo" }, new Vector3(1f, 0f, 1.2f)),
                } },
            { "res_mirador", new[] { ("Eira", Species.Humano, new Appearance { Height = 1.68f, HairStyle = "largo" }, new Vector3(0f, -1f, 1f)) } },
        };

        public static GhostFigures Spawn(string resonanceId, Vector3 at)
        {
            if (!Casts.TryGetValue(resonanceId, out var cast)) return null;
            var go = new GameObject("Recuerdo_" + resonanceId);
            var gf = go.AddComponent<GhostFigures>();
            gf._mat = MaterialLibrary.TransparentInstance(K.WithA(Pal.ResonanceSoft, 0f));
            MaterialLibrary.SetEmission(gf._mat, Pal.Resonance * 0.6f);
            var player = Game.Player != null ? Game.Player.transform : null;
            var basePos = new Vector3(at.x, Ground.Height(at.x, at.z), at.z);
            var face = player != null ? Quaternion.LookRotation(Vector3.ProjectOnPlane(player.position - basePos, Vector3.up).normalized + Vector3.forward * 0.0001f) : Quaternion.identity;
            foreach (var c in cast)
            {
                var rig = HumanoidBuilder.Build(go.transform, c.sp, c.look, gf._mat);
                var pos = basePos + face * c.offset;
                pos.y = Ground.Height(pos.x, pos.z);
                rig.Root.position = pos;
                rig.Root.rotation = face * Quaternion.Euler(0f, 180f, 0f);
                gf._rigs.Add(rig);
                gf._names.Add(c.name);
            }
            return gf;
        }

        public void Talk(string speaker) => _speaking = speaker;
        public void Fade() => _fading = true;

        private void Update()
        {
            _alpha = Mathf.MoveTowards(_alpha, _fading ? 0f : 0.42f, Time.deltaTime * (_fading ? 0.5f : 0.8f));
            MaterialLibrary.SetColor(_mat, K.WithA(Pal.ResonanceSoft, _alpha));
            for (int i = 0; i < _rigs.Count; i++)
            {
                var rig = _rigs[i];
                bool talking = _names[i] == _speaking;
                HumanoidAnimator.Pose(rig, Time.time + i, 0f, talking ? Activity.Conversar : Activity.Vigilar, Emotion.Neutral, talking, null);
                rig.Root.position += Vector3.up * Mathf.Sin(Time.time * 1.3f + i) * 0.0015f;
            }
            if (_fading && _alpha <= 0.001f)
            {
                Destroy(_mat);
                Destroy(gameObject);
            }
        }
    }
}
