using Kolotopir.Core;
using UnityEngine;

namespace Kolotopir
{
    /// <summary>
    /// Brazos de Nilo en primera persona: mangas de la túnica morada (siempre visibles), brazales cuando
    /// se equipan, espada en la mano derecha, catalejo y vendas en la izquierda.
    /// </summary>
    public sealed class ViewModel : MonoBehaviour
    {
        private Transform _right, _left, _sword, _bracerR, _bracerL, _spyglass, _bandage, _glow;
        private Material _swordGlow;
        private float _swayT;
        private Vector3 _rPos, _lPos;
        private Quaternion _rRot, _lRot;
        private float _hitJolt;

        public static ViewModel Create(Transform camera)
        {
            var root = K.Child(camera, "Brazos");
            var vm = root.gameObject.AddComponent<ViewModel>();
            vm.Build(root);
            return vm;
        }

        private void Build(Transform root)
        {
            var sleeve = MaterialLibrary.Lit(Pal.Tunic, 0.15f);
            var sleeveDark = MaterialLibrary.Lit(K.Mul(Pal.Tunic, 0.75f), 0.15f);
            var skin = MaterialLibrary.Lit(Pal.NiloSkin, 0.25f);
            var leather = MaterialLibrary.Lit(new Color(0.42f, 0.29f, 0.2f), 0.2f);
            _right = K.Child(root, "BrazoDerecho");
            _left = K.Child(root, "BrazoIzquierdo");
            foreach (var arm in new[] { _right, _left })
            {
                var mb = new ModelBuilder();
                mb.For(sleeve).Cylinder(new Vector3(0f, 0f, -0.5f), 0.085f, 0.07f, 0.42f, 8, Quaternion.Euler(90f, 0f, 0f));
                mb.For(sleeveDark).Cylinder(new Vector3(0f, 0f, -0.1f), 0.075f, 0.078f, 0.05f, 8, Quaternion.Euler(90f, 0f, 0f));
                mb.For(skin).Cylinder(new Vector3(0f, 0f, -0.08f), 0.05f, 0.048f, 0.1f, 7, Quaternion.Euler(90f, 0f, 0f));
                mb.For(skin).Sphere(new Vector3(0f, -0.005f, 0.05f), new Vector3(0.055f, 0.045f, 0.07f), 7, 5, Quaternion.identity);
                mb.For(skin).Sphere(new Vector3(arm == _right ? -0.04f : 0.04f, 0.01f, 0.04f), new Vector3(0.02f, 0.02f, 0.045f), 5, 3, Quaternion.Euler(0f, arm == _right ? -30f : 30f, 0f));
                foreach (var r in mb.Build(arm, "Brazo", false, false)) r.gameObject.layer = 0;
                var bracer = K.Child(arm, "Brazal");
                var bmb = new ModelBuilder();
                bmb.For(leather).Cylinder(new Vector3(0f, 0f, -0.3f), 0.088f, 0.08f, 0.17f, 8, Quaternion.Euler(90f, 0f, 0f));
                bmb.For(MaterialLibrary.Lit(new Color(0.3f, 0.2f, 0.14f), 0.2f)).Cylinder(new Vector3(0f, 0f, -0.26f), 0.09f, 0.09f, 0.02f, 8, Quaternion.Euler(90f, 0f, 0f));
                bmb.Build(bracer, "Brazal", false, false);
                if (arm == _right) _bracerR = bracer; else _bracerL = bracer;
            }
            // Espada corta del Reverso
            _sword = K.Child(_right, "Espada", new Vector3(0f, 0f, 0.05f), Quaternion.Euler(60f, 0f, 0f));
            var smb = new ModelBuilder();
            smb.For(MaterialLibrary.Lit(new Color(0.25f, 0.18f, 0.13f), 0.2f)).Cylinder(new Vector3(0f, -0.08f, 0f), 0.018f, 0.018f, 0.16f, 6, Quaternion.identity);
            smb.For(MaterialLibrary.Lit(new Color(0.55f, 0.45f, 0.3f), 0.5f, 0.6f)).Box(new Vector3(0f, 0.09f, 0f), new Vector3(0.15f, 0.025f, 0.035f), Quaternion.identity);
            smb.For(MaterialLibrary.Lit(new Color(0.7f, 0.73f, 0.77f), 0.75f, 0.85f)).Box(new Vector3(0f, 0.43f, 0f), new Vector3(0.045f, 0.62f, 0.012f), Quaternion.identity);
            smb.For(MaterialLibrary.Lit(new Color(0.7f, 0.73f, 0.77f), 0.75f, 0.85f)).Cone(new Vector3(0f, 0.74f, 0f), 0.026f, 0.08f, 4, Quaternion.Euler(0f, 45f, 0f), false);
            smb.Build(_sword, "Espada", false, false);
            _glow = K.Child(_sword, "Brillo", new Vector3(0f, 0.43f, 0f));
            _swordGlow = MaterialLibrary.UnlitTransparent(new Color(1f, 0.85f, 0.5f, 0f));
            var gmb = new MeshBuilder();
            gmb.Box(Vector3.zero, new Vector3(0.08f, 0.66f, 0.03f), Quaternion.identity);
            var ggo = _glow.gameObject;
            ggo.AddComponent<MeshFilter>().sharedMesh = gmb.ToMesh("Brillo");
            var gmr = ggo.AddComponent<MeshRenderer>();
            gmr.sharedMaterial = _swordGlow;
            gmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // Catalejo y venda en la mano izquierda
            _spyglass = K.Child(_left, "Catalejo", new Vector3(0f, 0.02f, 0.12f));
            var cmb = new ModelBuilder();
            cmb.For(MaterialLibrary.Lit(new Color(0.69f, 0.55f, 0.34f), 0.6f, 0.7f)).Cylinder(Vector3.zero, 0.03f, 0.024f, 0.32f, 8, Quaternion.Euler(90f, 0f, 0f));
            cmb.Build(_spyglass, "Catalejo", false, false);
            _bandage = K.Child(_left, "Venda", new Vector3(0f, 0.04f, 0.1f));
            var vmb = new ModelBuilder();
            vmb.For(MaterialLibrary.Lit(new Color(0.92f, 0.9f, 0.84f), 0.1f)).Cylinder(Vector3.zero, 0.045f, 0.045f, 0.08f, 8, Quaternion.Euler(0f, 0f, 90f));
            vmb.Build(_bandage, "Venda", false, false);
            _rPos = new Vector3(0.3f, -0.6f, 0.3f);
            _lPos = new Vector3(-0.3f, -0.6f, 0.3f);
            _rRot = _lRot = Quaternion.identity;
        }

        public void HitJolt() => _hitJolt = 1f;

        private void LateUpdate()
        {
            var player = Game.Player;
            var combat = Game.Combat;
            var state = Game.State;
            if (player == null || combat == null || state == null) return;
            float dt = Time.deltaTime;
            bool armed = state.Stats.HasWeapon;
            bool scoping = Game.Cam != null && Game.Cam.Scoping;
            bool healing = Game.Vitals != null && Game.Vitals.IsHealing;
            bool listening = Game.Listener != null && Game.Listener.IsListening;
            bool hidden = Game.UI != null && Game.UI.HideViewModel;

            _bracerR.gameObject.SetActive(state.Equipment.Get(EquipSlot.Brazos) != null);
            _bracerL.gameObject.SetActive(state.Equipment.Get(EquipSlot.Brazos) != null);
            _sword.gameObject.SetActive(armed);
            _spyglass.gameObject.SetActive(scoping);
            _bandage.gameObject.SetActive(healing);

            float speed = new Vector2(player.Velocity.x, player.Velocity.z).magnitude;
            _swayT += dt * (1f + speed * 1.6f);
            var sway = new Vector3(Mathf.Cos(_swayT * 0.9f) * 0.006f, Mathf.Sin(_swayT * 1.8f) * 0.008f * (0.4f + speed * 0.3f), 0f);

            // Postura base
            Vector3 rp = new Vector3(0.27f, -0.33f, 0.36f), lp = new Vector3(-0.29f, -0.62f, 0.25f);
            Vector3 re = new Vector3(8f, -6f, -8f), le = new Vector3(10f, 8f, 8f);
            bool showLeft = false, showRight = armed;

            if (player.IsClimbing)
            {
                float c = Mathf.Sin(Time.time * 6f) * (Mathf.Abs(player.Velocity.y) > 0.01f || InputMap.Move().y != 0f ? 1f : 0f);
                rp = new Vector3(0.22f, 0.05f + c * 0.08f, 0.38f); re = new Vector3(-70f, 0f, 0f);
                lp = new Vector3(-0.22f, 0.05f - c * 0.08f, 0.38f); le = new Vector3(-70f, 0f, 0f);
                showLeft = showRight = true;
            }
            else if (player.IsSwimming)
            {
                float c = Time.time * 3f;
                rp = new Vector3(0.25f + Mathf.Cos(c) * 0.08f, -0.35f + Mathf.Sin(c) * 0.05f, 0.35f); re = new Vector3(10f, -20f, 0f);
                lp = new Vector3(-0.25f - Mathf.Cos(c) * 0.08f, -0.35f + Mathf.Sin(c) * 0.05f, 0.35f); le = new Vector3(10f, 20f, 0f);
                showLeft = showRight = true;
            }
            else if (scoping)
            {
                lp = new Vector3(-0.05f, -0.06f, 0.18f); le = new Vector3(0f, 0f, 0f);
                showLeft = true;
                rp = new Vector3(0.3f, -0.55f, 0.3f);
            }
            else if (healing)
            {
                lp = new Vector3(-0.08f, -0.3f, 0.38f); le = new Vector3(20f, 40f, 0f);
                rp = new Vector3(0.12f, -0.32f, 0.4f); re = new Vector3(20f, -50f, 0f);
                showLeft = true; showRight = true;
            }
            else if (listening)
            {
                float p = Mathf.Sin(Time.time * 2f) * 0.01f;
                lp = new Vector3(-0.14f, -0.24f + p, 0.42f); le = new Vector3(-20f, 25f, 0f);
                rp = new Vector3(0.14f, -0.24f - p, 0.42f); re = new Vector3(-20f, -25f, 0f);
                showLeft = showRight = true;
            }
            else if (armed)
            {
                combat.GetSwordPose(ref rp, ref re, ref lp, ref le, ref showLeft);
            }

            if (hidden || !showRight) rp += new Vector3(0f, -0.6f, -0.1f);
            if (hidden || !showLeft) lp += new Vector3(0f, -0.6f, -0.1f);
            _hitJolt = Mathf.MoveTowards(_hitJolt, 0f, dt * 5f);
            var jolt = new Vector3(0f, -0.04f, -0.06f) * _hitJolt;

            float k = 1f - Mathf.Exp(-18f * dt);
            _rPos = Vector3.Lerp(_rPos, rp + sway + jolt, k);
            _lPos = Vector3.Lerp(_lPos, lp + sway + jolt, k);
            _rRot = Quaternion.Slerp(_rRot, Quaternion.Euler(re), k);
            _lRot = Quaternion.Slerp(_lRot, Quaternion.Euler(le), k);
            _right.localPosition = _rPos;
            _right.localRotation = _rRot;
            _left.localPosition = _lPos;
            _left.localRotation = _lRot;

            float glow = combat.SwordGlow;
            MaterialLibrary.SetColor(_swordGlow, new Color(1f, 0.85f, 0.5f, glow * (Game.Settings != null && Game.Settings.Data.ReduceFlashes ? 0.25f : 0.45f)));
        }
    }
}
