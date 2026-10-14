using Kolotopir.Core;
using UnityEngine;

namespace Kolotopir
{
    /// <summary>
    /// Cámara en primera persona: mirar con el ratón, balanceo opcional, temblor, catalejo y encuadres
    /// controlados para escenas narrativas (doc: "sin abusar de interrupciones").
    /// </summary>
    public sealed class PlayerCamera : MonoBehaviour
    {
        public Camera Cam { get; private set; }
        public Transform Head { get; private set; }
        public float BridgeSway;
        public bool Scoping { get; private set; }
        public float Pitch => _pitch;

        private PlayerController _player;
        private float _pitch;
        private float _bobPhase;
        private float _shake;
        private float _landDip;
        private float _eye = 1.5f;
        private float _baseFov = 75f;
        private bool _headBob = true;
        private float _shakeScale = 1f;
        private bool _reduceMotion;
        private float _scopeT;

        // Encuadre controlado
        private Transform _focus;
        private Vector3 _focusPoint;
        private bool _hasFocusPoint;
        private bool _shotActive;
        private Vector3 _shotPos, _shotLook;
        private float _shotBlend;

        public static PlayerCamera Create(PlayerController player)
        {
            var head = K.Child(player.transform, "Cabeza", new Vector3(0f, 1.5f, 0f));
            var camGo = new GameObject("Camara");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(head, false);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 3200f;
            cam.fieldOfView = 75f;
            camGo.AddComponent<AudioListener>();
            var pc = player.gameObject.AddComponent<PlayerCamera>();
            pc.Cam = cam;
            pc.Head = head;
            pc._player = player;
            PostFX.SetupCamera(cam);
            return pc;
        }

        public void ApplySettings(SettingsData d)
        {
            _baseFov = d.Fov;
            _headBob = d.HeadBob && !d.ReduceMotion;
            _shakeScale = d.ReduceMotion ? d.CameraShake * 0.3f : d.CameraShake;
            _reduceMotion = d.ReduceMotion;
        }

        public void ResetPitch() => _pitch = 0f;
        public void Shake(float amount) => _shake = Mathf.Max(_shake, amount * _shakeScale);
        public void LandDip(float amount) { if (!_reduceMotion) _landDip = Mathf.Max(_landDip, amount * 0.12f); }

        /// <summary>Durante un diálogo la cámara gira suavemente hacia quien habla.</summary>
        public void SetFocus(Transform target) { _focus = target; _hasFocusPoint = false; }
        public void SetFocus(Vector3 point) { _focus = null; _focusPoint = point; _hasFocusPoint = true; }
        public void ClearFocus() { _focus = null; _hasFocusPoint = false; }

        /// <summary>Plano cinematográfico breve: la cámara se separa de Nilo y mira algo.</summary>
        public void BeginShot(Vector3 pos, Vector3 look) { _shotActive = true; _shotPos = pos; _shotLook = look; }
        public void EndShot() => _shotActive = false;

        private void LateUpdate()
        {
            if (Cam == null || _player == null) return;
            float dt = Time.deltaTime;
            bool blocked = !Game.InGame || (Game.UI != null && Game.UI.BlocksGameplay) || _player.Frozen;

            // Catalejo
            bool wantScope = !blocked && Game.State != null && Game.State.HasItem("catalejo") && InputMap.Held(InputAction.Catalejo) && !_player.IsSwimming && !_player.IsClimbing;
            Scoping = wantScope;
            _scopeT = Mathf.MoveTowards(_scopeT, wantScope ? 1f : 0f, Time.unscaledDeltaTime * 4f);

            if (!blocked && !_shotActive)
            {
                var look = InputMap.Look();
                float sens = Mathf.Lerp(1f, 0.25f, _scopeT);
                _player.transform.Rotate(0f, look.x * sens, 0f);
                _pitch = Mathf.Clamp(_pitch - look.y * sens, -85f, 85f);
            }

            var target = _focus != null ? _focus.position : _focusPoint;
            if ((_focus != null || _hasFocusPoint) && !_shotActive)
            {
                var flat = target - _player.transform.position;
                flat.y = 0f;
                if (flat.sqrMagnitude > 0.01f)
                    _player.transform.rotation = K.Damp(_player.transform.rotation, Quaternion.LookRotation(flat), 5f, dt);
                var fromEye = target - Head.position;
                float desiredPitch = -Mathf.Atan2(fromEye.y, new Vector2(fromEye.x, fromEye.z).magnitude) * Mathf.Rad2Deg;
                _pitch = K.Damp(_pitch, Mathf.Clamp(desiredPitch, -40f, 40f), 5f, dt);
            }

            // Altura de los ojos, balanceo al caminar y temblores
            _eye = K.Damp(_eye, _player.EyeHeight, 10f, dt);
            float speed = new Vector2(_player.Velocity.x, _player.Velocity.z).magnitude;
            Vector3 bob = Vector3.zero;
            if (_headBob && _player.IsGrounded && speed > 0.5f)
            {
                _bobPhase += dt * speed * 2.1f;
                bob = new Vector3(Mathf.Cos(_bobPhase * 0.5f) * 0.025f, Mathf.Sin(_bobPhase) * 0.032f, 0f);
            }
            _landDip = Mathf.MoveTowards(_landDip, 0f, dt * 0.6f);
            Vector3 shake = Vector3.zero;
            if (_shake > 0.001f)
            {
                shake = Random.insideUnitSphere * _shake * 0.08f;
                _shake = Mathf.MoveTowards(_shake, 0f, dt * 2.2f);
            }
            Head.localPosition = new Vector3(0f, _eye - _landDip, 0f);
            float roll = 0f;
            if (!_reduceMotion)
            {
                roll += _player.Balance * 7f;
                roll += BridgeSway * Mathf.Sin(Time.time * 1.3f) * 1.1f;
            }
            Head.localRotation = Quaternion.Euler(_pitch, 0f, roll);

            if (_shotActive) _shotBlend = Mathf.MoveTowards(_shotBlend, 1f, Time.unscaledDeltaTime * 0.8f);
            else _shotBlend = Mathf.MoveTowards(_shotBlend, 0f, Time.unscaledDeltaTime * 1.5f);
            var ct = Cam.transform;
            if (_shotBlend > 0.001f)
            {
                float k = Mathf.SmoothStep(0f, 1f, _shotBlend);
                var fpPos = Head.TransformPoint(bob + shake);
                var fpRot = Head.rotation;
                var shotRot = Quaternion.LookRotation((_shotLook - _shotPos).normalized);
                ct.position = Vector3.Lerp(fpPos, _shotPos, k);
                ct.rotation = Quaternion.Slerp(fpRot, shotRot, k);
            }
            else
            {
                ct.localPosition = bob + shake;
                ct.localRotation = Quaternion.identity;
            }

            float fov = _baseFov;
            if (_player.IsRunning && !_reduceMotion) fov += 4f;
            fov = Mathf.Lerp(fov, 16f, _scopeT);
            Cam.fieldOfView = K.Damp(Cam.fieldOfView, fov, 10f, Time.unscaledDeltaTime);
        }
    }
}
