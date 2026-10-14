using System.Collections.Generic;
using Kolotopir.Core;
using UnityEngine;

namespace Kolotopir
{
    /// <summary>Eco errante: restos de recuerdos sin dueño. Atacan en grupo, reaccionan al ruido y a la Resonancia, temen el fuego.</summary>
    public sealed class EchoEnemy : CreatureBase
    {
        private enum S { Wander, Investigate, Chase, Circle, Windup, Lunge, Recover, Return, Flee }
        private static int _attackers;

        private S _s = S.Wander;
        private float _t;
        private Vector3 _target;
        private bool _token;
        private Vector3 _lungeDir;
        private bool _lungeHit;
        private float _circle;
        private Material _body;
        private Transform _visual;
        private float _noiseCheck;

        public override bool Hostile => true;
        public override Vector3 AimPoint => transform.position + Vector3.up * 1.0f;
        protected override int XpReward => 18;

        private void Start()
        {
            Name = "Eco errante";
            BestiaryEntry = "bes_eco";
            InitHealth(38f, 0f, 0f);
            HoverHeight = 0.45f;
            _circle = Random.value * Mathf.PI * 2f;
            _visual = K.Child(transform, "Visual");
            _body = MaterialLibrary.TransparentInstance(new Color(0.55f, 0.65f, 1f, 0.55f));
            MaterialLibrary.SetEmission(_body, Pal.Echo * 0.6f);
            FlashMaterials.Add(_body);
            var mb = new ModelBuilder();
            mb.For(_body).Sphere(new Vector3(0f, 0.75f, 0f), new Vector3(0.38f, 0.7f, 0.32f), 10, 7, Quaternion.identity);
            mb.For(_body).Sphere(new Vector3(0f, 1.55f, 0.05f), new Vector3(0.26f, 0.28f, 0.26f), 9, 6, Quaternion.identity);
            mb.For(_body).Cone(new Vector3(0f, 0.1f, 0f), 0.3f, -0.6f, 8, Quaternion.identity, false);
            mb.For(_body).Cylinder(new Vector3(0.35f, 1.05f, 0f), 0.08f, 0.03f, 0.6f, 6, Quaternion.Euler(0f, 0f, -150f));
            mb.For(_body).Cylinder(new Vector3(-0.35f, 1.05f, 0f), 0.08f, 0.03f, 0.6f, 6, Quaternion.Euler(0f, 0f, 150f));
            var eyes = MaterialLibrary.Emissive(Color.white, new Color(0.8f, 0.9f, 1f), 2f);
            mb.For(eyes).Sphere(new Vector3(0.09f, 1.58f, 0.24f), Vector3.one * 0.04f, 5, 3, Quaternion.identity);
            mb.For(eyes).Sphere(new Vector3(-0.09f, 1.58f, 0.24f), Vector3.one * 0.04f, 5, 3, Quaternion.identity);
            mb.Build(_visual, "Eco", false, false);
            FX.Motes(_visual, new Vector3(0f, 0.6f, 0f), K.WithA(Pal.Echo, 0.6f), 0.3f, 8f);
            var col = Colliders.Capsule(transform, new Vector3(0f, 0.9f, 0f), 0.35f, 1.6f, "Cuerpo");
            col.isTrigger = true;
            AudioManager.AttachLoop(gameObject, "eco_susurro", 0.35f, 14f);
            SnapToGround();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ReleaseToken();
        }

        private void ReleaseToken()
        {
            if (_token) { _token = false; _attackers = Mathf.Max(0, _attackers - 1); }
        }

        private float Speed => (Game.Sky != null && Game.Sky.Night > 0.5f ? 1.15f : 1f) * 3.3f;

        private void Update()
        {
            if (Dead || Health == null) return;
            float dt = Time.deltaTime;
            UpdateFlash(dt, Pal.Echo * (0.6f + (_s == S.Windup ? _t * 2.5f : 0f)));
            UpdateObservation(dt);
            ApplyKnock(dt);
            _visual.localPosition = new Vector3(0f, Mathf.Sin(Time.time * 2.2f + _circle) * 0.12f, 0f);
            if (Stunned)
            {
                _visual.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 20f) * 8f, 0f, 0f);
                return;
            }
            _visual.localRotation = Quaternion.identity;

            // La luz del fuego los ahuyenta
            if (FirePoint.Near(transform.position, 2f, out var fire))
            {
                ReleaseToken();
                _s = S.Flee;
                var away = transform.position + (transform.position - fire).normalized * 6f;
                MoveTowards(away, Speed * 1.2f, dt);
                return;
            }
            if (_s == S.Flee) _s = S.Return;

            float d = DistanceToPlayer();
            bool available = PlayerAvailable;
            switch (_s)
            {
                case S.Wander:
                    _t -= dt;
                    if (_t <= 0f) { _t = Random.Range(3f, 5f); _target = Home + new Vector3(Random.Range(-6f, 6f), 0f, Random.Range(-6f, 6f)); }
                    MoveTowards(_target, 1.1f, dt);
                    if (available && (CanSeePlayer(14f, 150f) || HeardPlayer(d))) _s = S.Chase;
                    break;
                case S.Investigate:
                    MoveTowards(_target, 2.4f, dt);
                    if (K.FlatDistance(transform.position, _target) < 1.5f) { _t -= dt; if (_t <= 0f) _s = S.Wander; }
                    if (available && (CanSeePlayer(16f, 160f) || d < 6f)) _s = S.Chase;
                    break;
                case S.Chase:
                    if (!available || K.FlatDistance(transform.position, Home) > 32f) { _s = S.Return; ReleaseToken(); break; }
                    if (d < 3.4f)
                    {
                        if (_token || _attackers < 2)
                        {
                            if (!_token) { _token = true; _attackers++; }
                            _s = S.Windup; _t = 0f;
                            Game.Audio?.Play3D("eco_aviso", transform.position, 0.8f, 1f, "Eco a punto de atacar");
                        }
                        else _s = S.Circle;
                    }
                    else MoveTowards(PlayerT.position, Speed, dt);
                    break;
                case S.Circle:
                    if (!available) { _s = S.Return; break; }
                    _circle += dt * 0.7f;
                    var orbit = PlayerT.position + new Vector3(Mathf.Cos(_circle), 0f, Mathf.Sin(_circle)) * 4.6f;
                    MoveTowards(orbit, Speed * 0.8f, dt);
                    FacePlayer(dt);
                    if (_attackers < 2) _s = S.Chase;
                    break;
                case S.Windup:
                    _t += dt;
                    FacePlayer(dt, 10f);
                    if (_t >= 0.65f)
                    {
                        _s = S.Lunge; _t = 0f; _lungeHit = false;
                        _lungeDir = PlayerT != null ? (PlayerT.position - transform.position).normalized : transform.forward;
                        _lungeDir.y = 0f;
                        Game.Audio?.Play3D("eco_ataque", transform.position, 0.8f);
                    }
                    break;
                case S.Lunge:
                    _t += dt;
                    MoveTowards(transform.position + _lungeDir * 3f, 8f, dt, 2f);
                    if (!_lungeHit && available && DistanceToPlayer() < 1.35f)
                    {
                        _lungeHit = true;
                        HitPlayer(9f * (Game.Sky != null && Game.Sky.Night > 0.5f ? 1.15f : 1f), DamageType.Resonancia, 2.5f);
                    }
                    if (_t >= 0.3f) { _s = S.Recover; _t = 0f; }
                    break;
                case S.Recover:
                    _t += dt;
                    if (_t >= 0.7f) { ReleaseToken(); _s = S.Chase; }
                    break;
                case S.Return:
                    MoveTowards(Home, 2.5f, dt);
                    Health.Restore(5f * dt);
                    if (K.FlatDistance(transform.position, Home) < 2f) _s = S.Wander;
                    break;
            }
        }

        private bool HeardPlayer(float d)
        {
            _noiseCheck -= Time.deltaTime;
            if (_noiseCheck > 0f) return false;
            _noiseCheck = 0.3f;
            float noise = Mathf.Max(Game.Player != null ? Game.Player.NoiseRadius : 0f, Game.Combat != null ? Game.Combat.NoiseRadius : 0f);
            return d < noise;
        }

        public override void OnNoise(Vector3 pos, bool resonance)
        {
            if (_s == S.Wander || _s == S.Return)
            {
                _s = S.Investigate;
                _target = pos;
                _t = 2f;
            }
        }

        protected override void OnHurt(in DamageInfo info)
        {
            if (_s == S.Wander || _s == S.Investigate || _s == S.Return) _s = S.Chase;
            if (info.Heavy) StunUntil = Time.time + 0.5f;
        }

        public override void OnPulse(float power)
        {
            StunUntil = Time.time + 2.5f;
            TakeHit(new DamageInfo { Amount = 8f * power, Type = DamageType.Resonancia, From = Game.Player.transform.position, Knockback = 3f, FromPlayer = true });
        }

        protected override void DropLoot()
        {
            int n = Random.Range(1, 3);
            Game.Ctx.GiveItem("polvo_eco", n);
            if (Random.value < 0.35f) Game.Ctx.GiveItem("fragmento", 1);
        }

        protected override void Die()
        {
            ReleaseToken();
            Game.Audio?.Play3D("eco_muerte", AimPoint, 0.9f);
            FX.Burst(AimPoint, Pal.Echo, 30, 3.5f, 0.2f, 0.9f);
            base.Die();
        }
    }

    /// <summary>Jabalí de musgo: defiende su territorio, avisa antes de embestir y se calma con bayas.</summary>
    public sealed class MossBoar : CreatureBase
    {
        private enum S { Graze, Warn, Windup, Charge, Recover, Flee, Calm }
        private static readonly List<MossBoar> Boars = new List<MossBoar>();
        private const float Territory = 11f;

        private S _s = S.Graze;
        private float _t;
        private Vector3 _target;
        private Vector3 _chargeDir;
        private bool _hit;
        private float _calmUntil;
        private Transform _visual, _head;
        private readonly Transform[] _legs = new Transform[4];
        private float _walk;
        private Material _skin;

        public override bool Hostile => _s == S.Warn || _s == S.Windup || _s == S.Charge || _s == S.Recover;
        public override Vector3 AimPoint => transform.position + Vector3.up * 0.7f;
        protected override int XpReward => 25;

        public static MossBoar NearestAgitated(Vector3 pos, float radius)
        {
            foreach (var b in Boars)
                if (b != null && b.IsAlive && b._s != S.Calm && K.FlatDistance(b.transform.position, pos) < radius) return b;
            return null;
        }

        protected override void OnEnable() { base.OnEnable(); Boars.Add(this); }
        protected override void OnDisable() { base.OnDisable(); Boars.Remove(this); }

        private void Start()
        {
            Name = "Jabalí de musgo";
            BestiaryEntry = "bes_jabali";
            InitHealth(85f, 4f, 0f);
            HoverHeight = 0f;
            _visual = K.Child(transform, "Visual");
            _skin = MaterialLibrary.LitInstance(new Color(0.36f, 0.27f, 0.2f), 0.1f);
            FlashMaterials.Add(_skin);
            var mb = new ModelBuilder();
            mb.For(_skin).Sphere(new Vector3(0f, 0.72f, 0f), new Vector3(0.55f, 0.48f, 0.85f), 10, 7, Quaternion.identity);
            var moss = MaterialLibrary.Lit(Pal.Moss, 0.02f);
            for (int i = 0; i < 4; i++) mb.For(moss).Rock(new Vector3(0f, 1.02f, -0.45f + i * 0.3f), new Vector3(0.6f, 0.3f, 0.45f), Quaternion.identity, 2000 + i, 0.3f, false);
            mb.Build(_visual, "Cuerpo");
            _head = K.Child(_visual, "Cabeza", new Vector3(0f, 0.72f, 0.75f));
            var hb = new ModelBuilder();
            hb.For(_skin).Sphere(new Vector3(0f, 0f, 0.15f), new Vector3(0.3f, 0.3f, 0.38f), 8, 6, Quaternion.identity);
            hb.For(MaterialLibrary.Lit(new Color(0.42f, 0.3f, 0.26f))).Cylinder(new Vector3(0f, -0.06f, 0.45f), 0.12f, 0.11f, 0.12f, 8, Quaternion.Euler(90f, 0f, 0f));
            var tusk = MaterialLibrary.Lit(new Color(0.92f, 0.88f, 0.78f), 0.4f);
            hb.For(tusk).Cone(new Vector3(0.1f, -0.1f, 0.42f), 0.035f, 0.2f, 5, Quaternion.Euler(-40f, 0f, -20f));
            hb.For(tusk).Cone(new Vector3(-0.1f, -0.1f, 0.42f), 0.035f, 0.2f, 5, Quaternion.Euler(-40f, 0f, 20f));
            var eye = MaterialLibrary.Lit(new Color(0.05f, 0.04f, 0.03f), 0.6f);
            hb.For(eye).Sphere(new Vector3(0.14f, 0.08f, 0.32f), Vector3.one * 0.035f, 5, 3, Quaternion.identity);
            hb.For(eye).Sphere(new Vector3(-0.14f, 0.08f, 0.32f), Vector3.one * 0.035f, 5, 3, Quaternion.identity);
            hb.Build(_head, "Cabeza");
            for (int i = 0; i < 4; i++)
            {
                var leg = K.Child(_visual, "Pata", new Vector3(i % 2 == 0 ? 0.28f : -0.28f, 0.45f, i < 2 ? 0.45f : -0.45f));
                var lb = new ModelBuilder();
                lb.For(_skin).Cylinder(new Vector3(0f, -0.45f, 0f), 0.09f, 0.11f, 0.45f, 6, Quaternion.identity);
                lb.Build(leg, "Pata");
                _legs[i] = leg;
            }
            Colliders.Box(transform, new Vector3(0f, 0.7f, 0f), new Vector3(0.9f, 0.9f, 1.8f)).isTrigger = false;
            gameObject.AddComponent<Rigidbody>().isKinematic = true;
            SnapToGround();
        }

        private void Update()
        {
            if (Dead || Health == null) return;
            float dt = Time.deltaTime;
            UpdateFlash(dt, Color.black);
            UpdateObservation(dt);
            ApplyKnock(dt);
            float speedNow = 0f;
            float d = DistanceToPlayer();
            float fromHome = PlayerT != null ? K.FlatDistance(PlayerT.position, Home) : 999f;
            bool available = PlayerAvailable;
            float headDown = 0f;
            if (Stunned && _s != S.Recover) { Animate(0f, 0.3f, dt); return; }

            switch (_s)
            {
                case S.Graze:
                    _t -= dt;
                    if (_t <= 0f) { _t = Random.Range(4f, 8f); _target = Home + new Vector3(Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f)); }
                    if (K.FlatDistance(transform.position, _target) > 0.8f) { MoveTowards(_target, 1.1f, dt); speedNow = 1.1f; }
                    else headDown = 0.6f;
                    if (available && fromHome < Territory)
                    {
                        _s = S.Warn; _t = 2.2f;
                        Game.Audio?.Play3D("jabali_resoplido", transform.position, 1f, 1f, "Jabalí resopla: te avisa");
                        Game.Ctx?.AddNotebook("bes_jabali", Knowledge.Observado);
                    }
                    break;
                case S.Warn:
                    FacePlayer(dt);
                    _t -= dt;
                    headDown = 0.3f + Mathf.Abs(Mathf.Sin(Time.time * 8f)) * 0.2f;
                    if (!available || fromHome > Territory + 2f) { _s = S.Graze; _t = 0f; break; }
                    if (_t <= 0f) { _s = S.Windup; _t = 0.6f; }
                    break;
                case S.Windup:
                    FacePlayer(dt, 12f);
                    headDown = 0.8f;
                    _t -= dt;
                    if (_t <= 0f)
                    {
                        _s = S.Charge; _t = 1.3f; _hit = false;
                        _chargeDir = PlayerT != null ? (PlayerT.position - transform.position) : transform.forward;
                        _chargeDir.y = 0f; _chargeDir.Normalize();
                        Game.Audio?.Play3D("jabali_carga", transform.position, 1f, 1f, "¡Embestida!");
                    }
                    break;
                case S.Charge:
                    _t -= dt;
                    var before = transform.position;
                    MoveTowards(transform.position + _chargeDir * 3f, 9f, dt, 1.5f);
                    speedNow = 9f;
                    headDown = 0.7f;
                    if (!_hit && available && d < 1.6f)
                    {
                        _hit = true;
                        HitPlayer(15f, DamageType.Fisico, 5f, true);
                    }
                    if (_t <= 0f || (before - transform.position).sqrMagnitude < 0.0001f) { _s = S.Recover; _t = 1.4f; StunUntil = Time.time + 1.4f; }
                    break;
                case S.Recover:
                    _t -= dt;
                    headDown = 0.2f;
                    if (_t <= 0f)
                    {
                        if (available && fromHome < Territory + 3f) { _s = S.Warn; _t = 1f; }
                        else _s = S.Graze;
                    }
                    break;
                case S.Flee:
                    _t -= dt;
                    var away = PlayerT != null ? transform.position + (transform.position - PlayerT.position).normalized * 5f : Home;
                    MoveTowards(away, 5f, dt);
                    speedNow = 5f;
                    if (_t <= 0f) { _s = S.Calm; _calmUntil = Time.time + 25f; }
                    break;
                case S.Calm:
                    headDown = 0.7f;
                    if (Time.time > _calmUntil) _s = S.Graze;
                    break;
            }
            Animate(speedNow, headDown, dt);
        }

        private void Animate(float speed, float headDown, float dt)
        {
            _walk += dt * speed * 4f;
            for (int i = 0; i < 4; i++)
            {
                float phase = _walk + (i == 0 || i == 3 ? 0f : Mathf.PI);
                _legs[i].localRotation = Quaternion.Euler(speed > 0.1f ? Mathf.Sin(phase) * 30f : 0f, 0f, 0f);
            }
            _head.localRotation = K.Damp(_head.localRotation, Quaternion.Euler(headDown * 35f, 0f, 0f), 8f, dt);
        }

        public void Calm()
        {
            _s = S.Calm;
            _calmUntil = Time.time + 120f;
            Game.Ctx.AddNotebook("bes_jabali", Knowledge.Sabe);
            Game.Ctx.Notify("El jabalí acepta las bayas y se calma.");
            Game.Ctx.AddXp(15, "Criatura calmada");
            Game.Audio?.Play3D("comer", transform.position, 0.8f);
        }

        protected override void OnHurt(in DamageInfo info)
        {
            Game.Audio?.Play3D("jabali_herido", transform.position, 0.9f);
            if (Health.Normalized < 0.25f) { _s = S.Flee; _t = 6f; return; }
            if (_s == S.Graze || _s == S.Calm || _s == S.Warn) { _s = S.Windup; _t = 0.5f; }
        }

        public override void OnPulse(float power) => StunUntil = Time.time + 1.5f;

        protected override void DropLoot()
        {
            Game.Ctx.GiveItem("cuero", 2);
            Game.Ctx.GiveItem("musgo", Random.Range(1, 3));
        }
    }

    /// <summary>Corzo de bruma: herbívoro que huye si lo asustas. Al amanecer la manada baja a beber al lago.</summary>
    public sealed class MistDeer : CreatureBase
    {
        private Vector3 _target;
        private float _t;
        private float _flee;
        private Vector3 _fleeDir;
        private Transform _visual;
        private readonly Transform[] _legs = new Transform[4];
        private float _walk;
        private Vector3 _offset;
        private float _closeTimer;

        public override Vector3 AimPoint => transform.position + Vector3.up * 1.0f;

        private void Start()
        {
            Name = "Corzo de bruma";
            BestiaryEntry = "bes_corzo";
            InitHealth(30f, 0f, 0f);
            _offset = new Vector3(Random.Range(-6f, 6f), 0f, Random.Range(-6f, 6f));
            _visual = K.Child(transform, "Visual");
            var fur = MaterialLibrary.Lit(new Color(0.72f, 0.66f, 0.6f), 0.1f);
            var mb = new ModelBuilder();
            mb.For(fur).Sphere(new Vector3(0f, 1.0f, 0f), new Vector3(0.3f, 0.32f, 0.7f), 9, 6, Quaternion.identity);
            mb.For(fur).Cylinder(new Vector3(0f, 1.15f, 0.5f), 0.12f, 0.09f, 0.5f, 6, Quaternion.Euler(35f, 0f, 0f));
            mb.For(fur).Sphere(new Vector3(0f, 1.5f, 0.82f), new Vector3(0.13f, 0.14f, 0.22f), 7, 5, Quaternion.identity);
            var antler = MaterialLibrary.Lit(new Color(0.85f, 0.82f, 0.78f), 0.2f);
            mb.For(antler).Cylinder(new Vector3(0.06f, 1.6f, 0.78f), 0.02f, 0.01f, 0.35f, 4, Quaternion.Euler(-20f, 0f, -25f));
            mb.For(antler).Cylinder(new Vector3(-0.06f, 1.6f, 0.78f), 0.02f, 0.01f, 0.35f, 4, Quaternion.Euler(-20f, 0f, 25f));
            mb.For(MaterialLibrary.Lit(new Color(0.95f, 0.95f, 0.93f))).Sphere(new Vector3(0f, 1.05f, -0.68f), Vector3.one * 0.08f, 5, 3, Quaternion.identity);
            mb.Build(_visual, "Corzo");
            for (int i = 0; i < 4; i++)
            {
                var leg = K.Child(_visual, "Pata", new Vector3(i % 2 == 0 ? 0.15f : -0.15f, 0.82f, i < 2 ? 0.45f : -0.45f));
                var lb = new ModelBuilder();
                lb.For(fur).Cylinder(new Vector3(0f, -0.82f, 0f), 0.03f, 0.05f, 0.82f, 5, Quaternion.identity);
                lb.Build(leg, "Pata");
                _legs[i] = leg;
            }
            FX.Motes(_visual, new Vector3(0f, 0.8f, 0f), new Color(0.9f, 0.95f, 1f, 0.3f), 0.5f, 2f);
            SnapToGround();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            UpdateObservation(dt);
            float speed = 0f;
            var center = HerdCenter();
            var p = PlayerT;
            float d = DistanceToPlayer();
            float noise = Game.Player != null ? Mathf.Max(Game.Player.NoiseRadius * 1.15f, Game.Player.IsCrouching ? 3.5f : 8f) : 0f;
            if (Game.Combat != null) noise = Mathf.Max(noise, Game.Combat.NoiseRadius);
            if (p != null && d < noise && _flee <= 0f && Game.InGame)
            {
                _flee = 3f;
                _fleeDir = (transform.position - p.position).normalized;
            }
            if (_flee > 0f)
            {
                _flee -= dt;
                MoveTowards(transform.position + _fleeDir * 4f, 6.5f, dt, 4f);
                speed = 6.5f;
            }
            else
            {
                _t -= dt;
                if (_t <= 0f) { _t = Random.Range(3f, 7f); _target = center + _offset + new Vector3(Random.Range(-3f, 3f), 0f, Random.Range(-3f, 3f)); }
                if (K.FlatDistance(transform.position, _target) > 1f) { MoveTowards(_target, 1.4f, dt); speed = 1.4f; }
            }
            // Acercarse agachado sin asustarlos enseña su comportamiento
            if (p != null && d < 5f && Game.Player.IsCrouching && _flee <= 0f)
            {
                _closeTimer += dt;
                if (_closeTimer > 2f && Game.State != null && Game.State.Notebook.Status("bes_corzo") < Knowledge.Sabe) Game.Ctx.AddNotebook("bes_corzo", Knowledge.Sabe);
            }
            _walk += dt * speed * 5f;
            for (int i = 0; i < 4; i++)
                _legs[i].localRotation = Quaternion.Euler(speed > 0.1f ? Mathf.Sin(_walk + (i == 0 || i == 3 ? 0f : Mathf.PI)) * 28f : 0f, 0f, 0f);
        }

        /// <summary>Migración diaria: al amanecer la manada baja al lago (evento del mundo).</summary>
        private Vector3 HerdCenter()
        {
            float h = Game.State != null ? Game.State.Clock.Hour : 12f;
            if (h > 5.5f && h < 9f) return new Vector3(575f, 0f, 520f);
            return Home;
        }

        public override void TakeHit(in DamageInfo info)
        {
            _flee = 4f;
            _fleeDir = (transform.position - info.From).normalized;
            Game.UI?.Toast("El corzo huye. No hace falta hacerle daño.");
        }
    }
}
