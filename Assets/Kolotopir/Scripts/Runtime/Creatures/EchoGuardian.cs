using Kolotopir.Core;
using UnityEngine;

namespace Kolotopir
{
    /// <summary>
    /// Guardián de Ecos de la Parada Antigua. Jefe con patrones legibles y ventanas de oportunidad
    /// (doc), pero con una alternativa no violenta: escuchar sus tres ecos y devolverle su recuerdo.
    /// </summary>
    public sealed class EchoGuardian : CreatureBase, IListenable
    {
        private enum S { Dormant, Approach, SlamWindup, Slam, Stuck, SweepWindup, Sweep, Cooldown, Calm, Gone }

        private S _s = S.Dormant;
        private float _t;
        private bool _summon60, _summon30;
        private Material _body, _core;
        private Transform _visual, _armL, _armR;
        private bool _engaged;

        public override bool Hostile => _s != S.Calm && _s != S.Dormant && _s != S.Gone;
        public override Vector3 AimPoint => transform.position + Vector3.up * 2.0f;
        protected override int XpReward => 150;

        public Vector3 ListenPoint => transform.position + Vector3.up * 1.8f;
        public float ListenRadius => 4.5f;
        public string ListenLabel => "Guardián de Ecos";
        public bool IsAvailable => _s == S.Calm;

        protected override void OnEnable() { base.OnEnable(); Listenables.All.Add(this); }
        protected override void OnDisable()
        {
            base.OnDisable();
            Listenables.All.Remove(this);
            if (Game.UI != null) Game.UI.SetBoss(null);
        }

        private void Start()
        {
            Name = "Guardián de Ecos";
            BestiaryEntry = "bes_guardian";
            InitHealth(420f, 6f, 0.2f);
            HoverHeight = 0.3f;
            _visual = K.Child(transform, "Visual");
            _body = MaterialLibrary.TransparentInstance(new Color(0.5f, 0.62f, 1f, 0.5f));
            MaterialLibrary.SetEmission(_body, Pal.Echo * 0.5f);
            _core = MaterialLibrary.LitInstance(new Color(0.75f, 0.8f, 1f), 0.5f);
            MaterialLibrary.SetEmission(_core, Pal.Echo);
            FlashMaterials.Add(_body);
            var mb = new ModelBuilder { SmoothAngle = 70f, Detail = 1.4f };
            // Kolotapir anciano hecho de eco: cuerpo robusto, hocico de tapir, orejas, bastón
            mb.For(_body).Lathe(Vector3.zero, new[] { new Vector2(0.3f, 0f), new Vector2(0.9f, 0.6f), new Vector2(1.05f, 1.4f), new Vector2(0.85f, 2.3f), new Vector2(0.5f, 2.7f), new Vector2(0f, 2.8f) }, 12, Quaternion.identity, true, false);
            mb.For(_body).Sphere(new Vector3(0f, 3.05f, 0.1f), new Vector3(0.48f, 0.45f, 0.5f), 10, 7, Quaternion.identity);
            mb.For(_body).Cylinder(new Vector3(0f, 2.95f, 0.45f), 0.22f, 0.13f, 0.7f, 8, Quaternion.Euler(110f, 0f, 0f));
            mb.For(_body).Sphere(new Vector3(0.38f, 3.35f, 0f), new Vector3(0.12f, 0.22f, 0.06f), 6, 4, Quaternion.Euler(0f, 0f, -25f));
            mb.For(_body).Sphere(new Vector3(-0.38f, 3.35f, 0f), new Vector3(0.12f, 0.22f, 0.06f), 6, 4, Quaternion.Euler(0f, 0f, 25f));
            mb.For(_core).Sphere(new Vector3(0f, 1.6f, 0.2f), Vector3.one * 0.35f, 10, 6, Quaternion.identity);
            var eyes = MaterialLibrary.Emissive(Color.white, new Color(0.85f, 0.9f, 1f), 2.5f);
            mb.For(eyes).Sphere(new Vector3(0.18f, 3.12f, 0.43f), Vector3.one * 0.05f, 5, 3, Quaternion.identity);
            mb.For(eyes).Sphere(new Vector3(-0.18f, 3.12f, 0.43f), Vector3.one * 0.05f, 5, 3, Quaternion.identity);
            mb.Build(_visual, "Guardian", false, false);
            _armR = K.Child(_visual, "BrazoD", new Vector3(0.95f, 2.3f, 0f));
            _armL = K.Child(_visual, "BrazoI", new Vector3(-0.95f, 2.3f, 0f));
            foreach (var arm in new[] { _armR, _armL })
            {
                var ab = new ModelBuilder { SmoothAngle = 70f, Detail = 1.4f };
                ab.For(_body).Cylinder(Vector3.zero, 0.24f, 0.18f, 1.5f, 8, Quaternion.Euler(180f, 0f, 0f));
                ab.For(_body).Sphere(new Vector3(0f, -1.55f, 0f), Vector3.one * 0.3f, 8, 5, Quaternion.identity);
                ab.Build(arm, "Brazo", false, false);
            }
            var staff = new ModelBuilder { SmoothAngle = 70f, Detail = 1.4f };
            staff.For(_body).Cylinder(new Vector3(0f, -1.6f, 0.2f), 0.06f, 0.06f, 3.4f, 6, Quaternion.identity);
            staff.For(_core).Sphere(new Vector3(0f, 1.9f, 0.2f), Vector3.one * 0.2f, 8, 5, Quaternion.identity);
            staff.Build(_armR, "Baston", false, false);
            FX.Motes(_visual, new Vector3(0f, 1.6f, 0f), K.WithA(Pal.Echo, 0.6f), 1.2f, 14f);
            Colliders.Capsule(transform, new Vector3(0f, 1.6f, 0f), 1.0f, 3.2f, "Cuerpo");
            gameObject.AddComponent<Rigidbody>().isKinematic = true;
            AudioManager.AttachLoop(gameObject, "eco_susurro", 0.5f, 25f);
            SnapToGround();
            if (Game.State != null && Game.State.Has("guardian_ecos_completos")) _s = S.Calm;
        }

        private void Update()
        {
            if (Dead || Health == null) return;
            float dt = Time.deltaTime;
            var s = Game.State;
            UpdateFlash(dt, Pal.Echo * 0.5f);
            UpdateObservation(dt);
            float d = DistanceToPlayer();
            bool available = PlayerAvailable;
            float armRaise = 0f, twist = 0f;

            if (s != null && s.Has("guardian_ecos_completos") && _s != S.Calm && _s != S.Gone)
            {
                _s = S.Calm;
                _engaged = false;
                Game.UI?.SetBoss(null);
                Game.Ctx?.Notify("El Guardián baja los brazos. Parece escuchar algo...");
            }

            switch (_s)
            {
                case S.Dormant:
                    if (available && d < 18f)
                    {
                        _s = S.Approach;
                        _engaged = true;
                        Game.Audio?.Play3D("guardian_rugido", AimPoint, 1f, 1f, "El Guardián despierta");
                        Game.UI?.SetBoss(this);
                        Game.Ctx?.AddNotebook("bes_guardian", Knowledge.Observado);
                        Game.Ctx?.AddNotebook("pj_guardian", Knowledge.Observado);
                    }
                    break;
                case S.Approach:
                    if (!available || d > 30f)
                    {
                        _s = S.Dormant; _engaged = false; Game.UI?.SetBoss(null);
                        MoveTowards(Home, 1.6f, dt);
                        break;
                    }
                    if (Stunned) break;
                    if (d > 4f) MoveTowards(PlayerT.position, 1.7f, dt, 3f);
                    else
                    {
                        FacePlayer(dt, 4f);
                        if (Random.value < 0.6f) { _s = S.SlamWindup; _t = 1.0f; Game.Audio?.Play3D("guardian_rugido", AimPoint, 0.8f, 1.3f, "El Guardián alza los brazos"); }
                        else { _s = S.SweepWindup; _t = 0.8f; Game.Audio?.Play3D("guardian_barrido", AimPoint, 0.7f, 0.8f, "El Guardián gira: ¡salta o esquiva!"); }
                    }
                    break;
                case S.SlamWindup:
                    _t -= dt;
                    armRaise = 1f - _t;
                    FacePlayer(dt, 2f);
                    if (_t <= 0f)
                    {
                        _s = S.Slam; _t = 0.15f;
                        var front = transform.position + transform.forward * 2.2f;
                        Game.Audio?.Play3D("guardian_golpe", front, 1f);
                        FX.Ring(front, K.WithA(Pal.Echo, 0.9f), 5f, 0.5f);
                        Game.Cam?.Shake(0.5f);
                        if (available && d < 4.8f && Vector3.Angle(transform.forward, PlayerT.position - transform.position) < 70f)
                            HitPlayer(24f, DamageType.Fisico, 6f, true);
                    }
                    break;
                case S.Slam:
                    _t -= dt;
                    armRaise = -0.4f;
                    if (_t <= 0f) { _s = S.Stuck; _t = 1.6f; StunUntil = Time.time + 1.6f; }
                    break;
                case S.Stuck:
                    _t -= dt;
                    armRaise = -0.5f;
                    if (_t <= 0f) { _s = S.Cooldown; _t = 0.9f; }
                    break;
                case S.SweepWindup:
                    _t -= dt;
                    twist = (0.8f - _t) / 0.8f * -60f;
                    if (_t <= 0f)
                    {
                        _s = S.Sweep; _t = 0.35f;
                        FX.Ring(transform.position, K.WithA(Pal.Resonance, 0.9f), 3.8f, 0.35f);
                        bool airborne = Game.Player != null && !Game.Player.IsGrounded;
                        if (available && d < 3.8f && !airborne) HitPlayer(16f, DamageType.Resonancia, 4f);
                    }
                    break;
                case S.Sweep:
                    _t -= dt;
                    twist = Mathf.Lerp(300f, -60f, _t / 0.35f);
                    if (_t <= 0f) { _s = S.Cooldown; _t = 1.2f; }
                    break;
                case S.Cooldown:
                    _t -= dt;
                    FacePlayer(dt, 3f);
                    if (_t <= 0f) _s = S.Approach;
                    break;
                case S.Calm:
                    armRaise = -0.6f;
                    _visual.localPosition = Vector3.down * 0.5f;
                    MaterialLibrary.SetEmission(_core, new Color(0.6f, 0.75f, 1f) * (1.2f + Mathf.Sin(Time.time * 1.5f) * 0.3f));
                    break;
            }
            if (_armR != null)
            {
                _armR.localRotation = K.Damp(_armR.localRotation, Quaternion.Euler(-armRaise * 150f, 0f, 10f), 10f, dt);
                _armL.localRotation = K.Damp(_armL.localRotation, Quaternion.Euler(-armRaise * 150f, 0f, -10f), 10f, dt);
            }
            if (_s != S.Calm) _visual.localRotation = K.Damp(_visual.localRotation, Quaternion.Euler(0f, twist, 0f), 12f, dt);

            if (_engaged && Health.Normalized < 0.6f && !_summon60) { _summon60 = true; Summon(); }
            if (_engaged && Health.Normalized < 0.3f && !_summon30) { _summon30 = true; Summon(); }
        }

        private void Summon()
        {
            Game.Audio?.Play3D("guardian_rugido", AimPoint, 1f, 1.4f, "El Guardián llama a los ecos");
            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject("EcoInvocado");
                go.transform.SetParent(transform.parent, false);
                var p = transform.position + Quaternion.Euler(0f, i * 180f + 90f, 0f) * Vector3.forward * 3f;
                p.y = Ground.Height(p.x, p.z);
                go.transform.position = p;
                var e = go.AddComponent<EchoEnemy>();
                e.SpawnId = "eco_invocado_" + Random.Range(0, 99999);
                e.Home = p;
            }
        }

        protected override void OnHurt(in DamageInfo info)
        {
            if (_s == S.Dormant) { _s = S.Approach; _engaged = true; Game.UI?.SetBoss(this); }
        }

        public override void OnParried() => StunUntil = Time.time + 1.5f;
        public override void OnPulse(float power) => StunUntil = Time.time + 2f;

        public override void TakeHit(in DamageInfo info)
        {
            if (_s == S.Calm || _s == S.Gone) return;
            base.TakeHit(info);
        }

        protected override void Die()
        {
            Game.UI?.SetBoss(null);
            var ctx = Game.Ctx;
            if (ctx != null)
            {
                ctx.SetFlag("guardian_resuelto");
                ctx.SetFlag("guardian_derrotado");
                ctx.GiveItem("yelmo_parada", 1);
                ctx.AddNotebook("bes_guardian", Knowledge.Sabe);
                ctx.RecordDecision("dec_guardian", "Derrotado", "Venciste al Guardián de la Parada con la espada.", ConsequenceScale.Local);
            }
            Game.Audio?.Play3D("eco_muerte", AimPoint, 1f, 0.6f);
            FX.Burst(AimPoint, Pal.Echo, 60, 6f, 0.3f, 1.4f);
            base.Die();
        }

        public string NextResonance(out string reason)
        {
            reason = null;
            if (_s != S.Calm) { reason = "El Guardián no te escucha."; return null; }
            return "res_guardian_calma";
        }

        public void OnListened(string resonanceId)
        {
            if (!Game.State.Has("guardian_calmado")) return;
            _s = S.Gone;
            Dead = true;
            Game.State.World.MarkDefeated(SpawnId, Game.State.Clock.TotalHours);
            FX.Burst(AimPoint, new Color(0.7f, 0.85f, 1f), 70, 2.5f, 0.25f, 2.5f);
            Game.Audio?.Play2D("descubrir", 0.9f);
            Destroy(gameObject, 0.1f);
        }
    }
}
