using System.Collections;
using System.Collections.Generic;
using Kolotopir.Core;
using UnityEngine;

namespace Kolotopir
{
    /// <summary>
    /// Base de criaturas: salud, percepción (vista, sonido, Resonancia), movimiento sobre el terreno,
    /// golpes, aturdimiento y muerte. Las criaturas no son hostiles por defecto (doc).
    /// </summary>
    public abstract class CreatureBase : MonoBehaviour, IDamageable
    {
        public static readonly List<CreatureBase> All = new List<CreatureBase>();

        public string SpawnId;
        public bool Unique;
        public CreatureSpawner Spawner;
        public Vector3 Home;
        public string Name = "Criatura";
        public string BestiaryEntry;

        protected Vital Health;
        protected float Defense;
        protected float ResistResonance;
        protected float HoverHeight;
        protected float StunUntil;
        protected Vector3 Knock;
        protected bool Dead;
        protected readonly List<Material> FlashMaterials = new List<Material>();
        protected float Flash;
        private float _observeTimer;

        public bool IsAlive => !Dead && Health != null && !Health.IsEmpty;
        public virtual bool Hostile => false;
        public string DisplayName => Name;
        public float HealthFraction => Health != null ? Health.Normalized : 0f;
        public virtual Vector3 AimPoint => transform.position + Vector3.up * 0.9f;
        public bool Stunned => Time.time < StunUntil;
        public float LastHitTime { get; private set; } = -100f;

        protected virtual void OnEnable() => All.Add(this);
        protected virtual void OnDisable() => All.Remove(this);

        protected void InitHealth(float max, float defense, float resistRes)
        {
            Health = new Vital(max);
            Defense = defense;
            ResistResonance = resistRes;
        }

        protected Transform PlayerT => Game.Player != null ? Game.Player.transform : null;

        protected float DistanceToPlayer()
        {
            var p = PlayerT;
            return p == null ? float.MaxValue : K.FlatDistance(transform.position, p.position);
        }

        protected bool PlayerAvailable => Game.InGame && Game.State != null && !Game.State.Health.IsEmpty && !(Game.Dialogue != null && Game.Dialogue.Active);

        protected bool CanSeePlayer(float range, float fov)
        {
            var p = PlayerT;
            if (p == null || !PlayerAvailable) return false;
            var to = p.position - transform.position;
            if (to.magnitude > range) return false;
            if (Vector3.Angle(transform.forward, new Vector3(to.x, 0f, to.z)) > fov * 0.5f) return false;
            var eye = transform.position + Vector3.up * 1.2f;
            var target = p.position + Vector3.up * 1.2f;
            return !Physics.Linecast(eye, target, out var hit, ~0, QueryTriggerInteraction.Ignore) || hit.collider.transform.IsChildOf(p);
        }

        protected void SnapToGround()
        {
            var p = transform.position;
            p.y = Ground.Height(p.x, p.z) + HoverHeight;
            transform.position = p;
        }

        /// <summary>Movimiento con evitación sencilla de obstáculos.</summary>
        protected void MoveTowards(Vector3 target, float speed, float dt, float turnRate = 6f)
        {
            var p = transform.position;
            var to = target - p;
            to.y = 0f;
            if (to.sqrMagnitude < 0.01f) return;
            var dir = to.normalized;
            var origin = p + Vector3.up * 0.6f;
            if (Physics.Raycast(origin, dir, out var hit, 1.6f, ~0, QueryTriggerInteraction.Ignore) && !(hit.collider is TerrainCollider) && !hit.collider.transform.IsChildOf(transform)
                && (PlayerT == null || !hit.collider.transform.IsChildOf(PlayerT)))
            {
                var left = Quaternion.Euler(0f, -55f, 0f) * dir;
                var right = Quaternion.Euler(0f, 55f, 0f) * dir;
                bool leftFree = !Physics.Raycast(origin, left, 1.6f, ~0, QueryTriggerInteraction.Ignore);
                dir = leftFree ? left : right;
            }
            var next = p + dir * speed * dt;
            // No caer por acantilados ni entrar en el agua profunda
            float h = WorldShape.Height(next.x, next.z);
            if (h < 1f || WorldShape.IsUnderwater(next.x, next.z) || Mathf.Abs(h - WorldShape.Height(p.x, p.z)) > speed * dt * 2.5f + 0.4f) return;
            transform.position = next;
            transform.rotation = K.Damp(transform.rotation, Quaternion.LookRotation(dir), turnRate, dt);
            SnapToGround();
        }

        protected void FacePlayer(float dt, float rate = 8f)
        {
            var p = PlayerT;
            if (p == null) return;
            var to = p.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.01f) transform.rotation = K.Damp(transform.rotation, Quaternion.LookRotation(to), rate, dt);
        }

        protected void ApplyKnock(float dt)
        {
            if (Knock.sqrMagnitude < 0.01f) return;
            var next = transform.position + Knock * dt;
            if (WorldShape.Height(next.x, next.z) > 1f && !WorldShape.IsUnderwater(next.x, next.z)) transform.position = next;
            Knock = Vector3.Lerp(Knock, Vector3.zero, dt * 6f);
            SnapToGround();
        }

        protected void UpdateFlash(float dt, Color baseEmission)
        {
            Flash = Mathf.MoveTowards(Flash, 0f, dt * 5f);
            foreach (var m in FlashMaterials) if (m != null) MaterialLibrary.SetEmission(m, baseEmission + Color.white * Flash * 0.8f);
        }

        /// <summary>Observar una criatura un rato (cerca o con el catalejo) la registra en el Bestiario.</summary>
        protected void UpdateObservation(float dt)
        {
            if (string.IsNullOrEmpty(BestiaryEntry) || Game.State == null || Game.State.Knows(BestiaryEntry)) return;
            var cam = Game.Cam != null ? Game.Cam.Cam : null;
            if (cam == null) return;
            var to = AimPoint - cam.transform.position;
            bool scoping = Game.Cam.Scoping;
            float range = scoping ? 120f : 14f * Game.State.Stats.DetectionMult;
            if (to.magnitude < range && Vector3.Angle(cam.transform.forward, to) < (scoping ? 6f : 20f))
            {
                _observeTimer += dt;
                if (_observeTimer > (scoping ? 1.2f : 2.5f)) Game.Ctx.AddNotebook(BestiaryEntry, Knowledge.Observado);
            }
            else _observeTimer = Mathf.Max(0f, _observeTimer - dt);
        }

        public virtual void TakeHit(in DamageInfo info)
        {
            if (!IsAlive) return;
            float dmg = DamageModel.Compute(info.Amount, info.Type, Defense, 0f, ResistResonance);
            if (Stunned) dmg *= 1.25f;
            Health.Reduce(dmg);
            LastHitTime = Time.time;
            Flash = 1f;
            var away = transform.position - info.From;
            away.y = 0f;
            Knock = away.normalized * info.Knockback;
            OnHurt(info);
            if (Health.IsEmpty) Die();
        }

        protected virtual void OnHurt(in DamageInfo info) { }

        public virtual void OnParried() => StunUntil = Time.time + 1.2f;
        public virtual void OnPulse(float power) { }
        public virtual void OnNoise(Vector3 pos, bool resonance) { }

        public static void AlertNearby(Vector3 pos, float radius, bool resonance = false)
        {
            for (int i = All.Count - 1; i >= 0; i--)
            {
                var c = All[i];
                if (c == null || !c.IsAlive) continue;
                if (K.FlatDistance(c.transform.position, pos) <= radius) c.OnNoise(pos, resonance);
            }
        }

        protected virtual int XpReward => 15;
        protected virtual void DropLoot() { }

        protected virtual void Die()
        {
            if (Dead) return;
            Dead = true;
            var s = Game.State;
            if (s != null)
            {
                s.World.MarkDefeated(SpawnId, s.Clock.TotalHours);
                if (this is EchoEnemy) s.Flags.Increment("ecos_derrotados");
                Game.Ctx.AddXp(XpReward, "Combate");
                if (!string.IsNullOrEmpty(BestiaryEntry)) Game.Ctx.AddNotebook(BestiaryEntry, Knowledge.Observado);
                DropLoot();
                Game.MarkDirty();
            }
            StartCoroutine(Dissolve());
        }

        private IEnumerator Dissolve()
        {
            float t = 0f;
            var start = transform.localScale;
            while (t < 1f)
            {
                t += Time.deltaTime * 1.8f;
                transform.localScale = start * (1f - t * 0.9f);
                transform.position += Vector3.down * Time.deltaTime * 0.3f;
                yield return null;
            }
            foreach (var m in FlashMaterials) if (m != null) Destroy(m);
            Destroy(gameObject);
        }

        protected void HitPlayer(float amount, DamageType type, float knockback, bool heavy = false, bool blockable = true)
        {
            Game.Vitals?.TakeDamage(new DamageInfo
            {
                Amount = amount, Type = type, From = transform.position, Knockback = knockback, Heavy = heavy, Blockable = blockable, Attacker = this,
            });
        }
    }

    /// <summary>Hace aparecer criaturas de un punto de aparición cuando su sector está cargado.</summary>
    public sealed class CreatureSpawner : MonoBehaviour
    {
        public SpawnDef Def;
        private readonly List<CreatureBase> _alive = new List<CreatureBase>();
        private float _check;

        private void Update()
        {
            _check -= Time.deltaTime;
            if (_check > 0f) return;
            _check = 2f;
            var s = Game.State;
            if (s == null || Game.Sky == null) return;
            _alive.RemoveAll(c => c == null);
            bool night = Game.Sky.Night > 0.5f;
            bool allowed = !Def.NightOnly || night;
            // Consecuencia global: si se selló la Cápsula de la Parada, los ecos de la Parada se calman.
            if (Def.Kind == "eco" && Def.Id.StartsWith("eco_parada") && s.Has("dec_inaccesible")) allowed = false;
            if (Def.Kind == "eco" && Def.Id.StartsWith("eco_parada") && s.Has("guardian_calmado") && Def.Id.EndsWith("c")) allowed = false;
            if (!allowed)
            {
                // Los ecos nocturnos se deshacen al amanecer.
                foreach (var c in _alive) if (c != null) Destroy(c.gameObject);
                _alive.Clear();
                return;
            }
            if (_alive.Count > 0) return;
            if (Game.Player != null && K.FlatDistance(Game.Player.transform.position, transform.position) < 25f) return;
            for (int i = 0; i < Def.Count; i++)
            {
                string id = Def.Count > 1 ? Def.Id + "_" + i : Def.Id;
                if (s.World.IsDefeated(id, s.Clock.TotalHours, Def.Unique)) continue;
                var c = Spawn(id, i);
                if (c != null) _alive.Add(c);
            }
        }

        private CreatureBase Spawn(string id, int index)
        {
            float a = index * 2.4f;
            var offset = Def.Count > 1 ? new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * Def.Radius * 0.6f : Vector3.zero;
            var pos = transform.position + offset;
            pos.y = Ground.Height(pos.x, pos.z);
            var go = new GameObject("Criatura_" + id);
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            CreatureBase c;
            switch (Def.Kind)
            {
                case "eco": c = go.AddComponent<EchoEnemy>(); break;
                case "jabali": c = go.AddComponent<MossBoar>(); break;
                case "corzo": c = go.AddComponent<MistDeer>(); break;
                case "guardian":
                    if (Game.State.Has("guardian_resuelto")) { Destroy(go); return null; }
                    c = go.AddComponent<EchoGuardian>();
                    break;
                default: Destroy(go); return null;
            }
            c.SpawnId = id;
            c.Unique = Def.Unique;
            c.Spawner = this;
            c.Home = pos;
            return c;
        }
    }
}
