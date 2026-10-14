using System.Collections.Generic;
using Kolotopir.Core;
using UnityEngine;

namespace Kolotopir
{
    public struct DamageInfo
    {
        public float Amount;
        public DamageType Type;
        public Vector3 From;
        public float Stagger;
        public float Knockback;
        public bool Heavy;
        public bool FromPlayer;
        public bool Blockable;
        public CreatureBase Attacker;
    }

    public interface IDamageable
    {
        bool IsAlive { get; }
        bool Hostile { get; }
        string DisplayName { get; }
        float HealthFraction { get; }
        Vector3 AimPoint { get; }
        void TakeHit(in DamageInfo info);
    }

    /// <summary>
    /// Combate con espada (doc: ataques rápidos y fuertes, bloqueo, esquiva, interrupciones, aturdimientos
    /// y habilidades especiales). Pocas armas pulidas: en el primer prototipo, solo la espada.
    /// </summary>
    public sealed class PlayerCombat : MonoBehaviour
    {
        private enum State { Idle, Windup, Active, Recover, Charging, HeavyWindup, HeavyActive, HeavyRecover, Stunned }

        private State _state = State.Idle;
        private float _t;
        private int _combo;
        private bool _queued;
        private float _holdT;
        private bool _holding;
        private float _charge;
        private float _blockStart = -10f;
        private float _dodgeUntil = -10f;
        private float _dodgeCooldown;
        private float _counterUntil = -10f;
        private float _noArmHintCooldown;
        private readonly HashSet<IDamageable> _hit = new HashSet<IDamageable>();

        public bool IsBlocking { get; private set; }
        public bool IsAttacking => _state != State.Idle && _state != State.Stunned;
        public bool IsInvulnerable => Time.time < _dodgeUntil;
        public bool PerfectBlockWindow => IsBlocking && Time.time - _blockStart < 0.22f;
        public float SwordGlow { get; private set; }
        public float NoiseRadius { get; private set; }
        public float MoveMultiplier
        {
            get
            {
                if (_state == State.Charging) return 0.45f;
                if (IsAttacking) return 0.7f;
                if (IsBlocking) return 0.55f;
                if (_state == State.Stunned) return 0.3f;
                return 1f;
            }
        }

        private static float Duration(State s)
        {
            switch (s)
            {
                case State.Windup: return 0.12f;
                case State.Active: return 0.12f;
                case State.Recover: return 0.26f;
                case State.HeavyWindup: return 0.14f;
                case State.HeavyActive: return 0.16f;
                case State.HeavyRecover: return 0.45f;
                default: return 0f;
            }
        }

        private bool Blocked => !Game.InGame || (Game.UI != null && Game.UI.BlocksGameplay) || Game.Player == null || Game.Player.Frozen
                                || Game.Player.IsSwimming || Game.Player.IsClimbing || Game.Player.IsSitting || (Game.Vitals != null && Game.Vitals.IsHealing)
                                || (Game.Listener != null && Game.Listener.IsListening) || (Game.Cam != null && Game.Cam.Scoping);

        private void Update()
        {
            float dt = Time.deltaTime;
            var stats = Game.State?.Stats;
            if (stats == null) return;
            _dodgeCooldown -= dt;
            _noArmHintCooldown -= dt;
            NoiseRadius = Mathf.MoveTowards(NoiseRadius, 0f, dt * 10f);
            bool blocked = Blocked;
            bool armed = stats.HasWeapon;
            float speed = stats.AttackSpeed;

            if (blocked)
            {
                IsBlocking = false;
                if (_state == State.Charging) _state = State.Idle;
                _holding = false;
            }

            // --- Bloqueo
            bool wantBlock = !blocked && armed && InputMap.Held(InputAction.Bloquear) && (_state == State.Idle || _state == State.Recover) && Game.State.Stamina.Current > 1f;
            if (wantBlock && !IsBlocking) _blockStart = Time.time;
            IsBlocking = wantBlock;

            // --- Esquiva
            if (!blocked && InputMap.Down(InputAction.Esquivar) && _dodgeCooldown <= 0f && _state != State.Stunned)
            {
                float cost = 18f * stats.DodgeCostMult * stats.StaminaCostMult;
                if (Game.Vitals.TrySpendStamina(cost))
                {
                    var input = InputMap.Move();
                    var t = Game.Player.transform;
                    var dir = input.sqrMagnitude > 0.05f ? (t.right * input.x + t.forward * input.y).normalized : -t.forward;
                    Game.Player.AddImpulse(dir * 13f, 0.22f);
                    _dodgeUntil = Time.time + (Game.State.HasSkill("esquiva_agil") ? 0.3f : 0.2f);
                    _dodgeCooldown = 0.4f;
                    _state = State.Idle;
                    IsBlocking = false;
                    Game.Audio?.Play2D("esquiva", 0.7f);
                }
                else Game.UI?.Toast("Sin aguante");
            }

            // --- Habilidad activa: Pulso de Resonancia
            if (!blocked && InputMap.Down(InputAction.Habilidad)) TryPulse();

            // --- Ataques: pulsar = rápido, mantener = fuerte
            if (!blocked && InputMap.Down(InputAction.Atacar))
            {
                if (!armed)
                {
                    if (_noArmHintCooldown <= 0f) { Game.UI?.Toast("No llevas ninguna arma equipada ({Inventario})."); _noArmHintCooldown = 4f; }
                }
                else if (_state == State.Idle) { _holding = true; _holdT = 0f; }
                else if (_state == State.Recover || _state == State.Active) _queued = true;
            }
            if (_holding)
            {
                _holdT += dt;
                if (_holdT > 0.3f && _state == State.Idle)
                {
                    _state = State.Charging;
                    _charge = 0f;
                }
                if (!InputMap.Held(InputAction.Atacar) || blocked)
                {
                    _holding = false;
                    if (_state == State.Charging) StartHeavy();
                    else if (_state == State.Idle && !blocked) StartLight(0);
                }
            }

            switch (_state)
            {
                case State.Charging:
                    _charge = Mathf.Min(1f, _charge + dt);
                    SwordGlow = _charge;
                    break;
                case State.Windup:
                case State.HeavyWindup:
                case State.Active:
                case State.HeavyActive:
                case State.Recover:
                case State.HeavyRecover:
                    _t += dt * speed;
                    if (_t >= Duration(_state)) Advance();
                    break;
                case State.Stunned:
                    _t -= dt;
                    if (_t <= 0f) _state = State.Idle;
                    break;
            }
            if (_state != State.Charging) SwordGlow = Mathf.MoveTowards(SwordGlow, Time.time < _counterUntil ? 0.6f : 0f, dt * 4f);
        }

        private void StartLight(int combo)
        {
            var stats = Game.State.Stats;
            if (!Game.Vitals.TrySpendStamina(10f * stats.StaminaCostMult))
            {
                Game.UI?.Toast("Sin aguante");
                return;
            }
            _combo = combo;
            _state = State.Windup;
            _t = 0f;
            _queued = false;
            _hit.Clear();
            IsBlocking = false;
        }

        private void StartHeavy()
        {
            var stats = Game.State.Stats;
            if (!Game.Vitals.TrySpendStamina(22f * stats.HeavyCostMult * stats.StaminaCostMult))
            {
                Game.UI?.Toast("Sin aguante");
                _state = State.Idle;
                return;
            }
            _state = State.HeavyWindup;
            _t = 0f;
            _hit.Clear();
            Game.Audio?.Play2D("tajo_fuerte", 0.8f);
        }

        private void Advance()
        {
            _t = 0f;
            switch (_state)
            {
                case State.Windup:
                    _state = State.Active;
                    Game.Audio?.Play2D("tajo", 0.75f, 1f + _combo * 0.06f);
                    Strike(false);
                    break;
                case State.Active:
                    _state = State.Recover;
                    break;
                case State.Recover:
                    if (_queued && _combo < 2) StartLight(_combo + 1);
                    else { _state = State.Idle; _combo = 0; }
                    break;
                case State.HeavyWindup:
                    _state = State.HeavyActive;
                    Strike(true);
                    break;
                case State.HeavyActive:
                    _state = State.HeavyRecover;
                    break;
                case State.HeavyRecover:
                    _state = State.Idle;
                    _charge = 0f;
                    break;
            }
        }

        private void Strike(bool heavy)
        {
            var cam = Game.Cam.Cam.transform;
            var stats = Game.State.Stats;
            float mult = heavy ? 2f + _charge * 0.5f : (_combo == 2 ? 1.35f : _combo == 1 ? 1.1f : 1f);
            if (Time.time < _counterUntil) { mult *= 1.5f; _counterUntil = -10f; }
            NoiseRadius = 16f;
            CreatureBase.AlertNearby(Game.Player.transform.position, 16f);
            Game.Npcs?.OnPlayerSwing(Game.Player.transform.position);

            var center = cam.position + cam.forward * 1.3f;
            var cols = Physics.OverlapSphere(center, 1.35f, ~0, QueryTriggerInteraction.Collide);
            bool any = false;
            foreach (var c in cols)
            {
                var d = c.GetComponentInParent<IDamageable>();
                if (d == null || !d.IsAlive || _hit.Contains(d)) continue;
                var to = d.AimPoint - cam.position;
                var flat = new Vector3(to.x, 0f, to.z);
                var fwd = new Vector3(cam.forward.x, 0f, cam.forward.z);
                if (flat.magnitude > 2.9f || Vector3.Angle(fwd, flat) > 75f) continue;
                _hit.Add(d);
                any = true;
                var info = new DamageInfo
                {
                    Amount = stats.Attack * mult,
                    Type = stats.AttackType,
                    From = Game.Player.transform.position,
                    Stagger = heavy ? 0.8f : 0.2f,
                    Knockback = heavy ? 4f : 1.4f,
                    Heavy = heavy,
                    FromPlayer = true,
                };
                d.TakeHit(info);
                FX.Burst(d.AimPoint, d is EchoEnemy || d is EchoGuardian ? Pal.Echo : new Color(1f, 0.85f, 0.6f), heavy ? 26 : 14, heavy ? 4f : 3f);
                Game.Audio?.Play3D(d is EchoEnemy || d is EchoGuardian ? "impacto_eco" : "impacto", d.AimPoint, 0.9f);
                Game.UI?.ShowTarget(d);
            }
            if (any)
            {
                Game.Cam.Shake(heavy ? 0.45f : 0.2f);
                Game.Root.HitStop(heavy ? 0.07f : 0.035f);
            }
        }

        private void TryPulse()
        {
            var s = Game.State;
            if (!s.HasSkill("pulso_resonancia"))
            {
                Game.UI?.Toast("Aún no conoces ninguna habilidad activa (árbol de Resonancia).");
                return;
            }
            var def = GameDb.Skill("pulso_resonancia");
            if (!s.Skills.IsReady(def.Id, Time.time))
            {
                Game.UI?.Toast($"Pulso de Resonancia: {s.Skills.CooldownRemaining(def.Id, Time.time):0} s");
                return;
            }
            if (!Game.Vitals.TrySpendStamina(15f)) { Game.UI?.Toast("Sin aguante"); return; }
            s.Skills.StartCooldown(def.Id, Time.time, def.Cooldown);
            var p = Game.Player.transform.position;
            FX.Ring(p, K.WithA(Pal.Resonance, 0.9f), 7f, 0.6f);
            Game.Audio?.Play2D("pulso", 0.9f);
            float power = 1f + 0.1f * (s.Attr(AttributeType.Resonancia) - 1);
            foreach (var c in CreatureBase.All.ToArray())
            {
                if (c == null || !c.IsAlive) continue;
                if (Vector3.Distance(c.transform.position, p) > 7f) continue;
                c.OnPulse(power);
            }
        }

        public void Stun(float seconds)
        {
            _state = State.Stunned;
            _t = seconds;
            IsBlocking = false;
            Game.UI?.Toast("¡Guardia rota!");
        }

        public void NotifyParry()
        {
            if (Game.State.HasSkill("contraataque")) _counterUntil = Time.time + 1.5f;
            Game.Cam?.Shake(0.15f);
        }

        /// <summary>Postura de los brazos según el estado del combate (la usa <see cref="ViewModel"/>).</summary>
        public void GetSwordPose(ref Vector3 rp, ref Vector3 re, ref Vector3 lp, ref Vector3 le, ref bool showLeft)
        {
            float u = Duration(_state) > 0f ? Mathf.Clamp01(_t / Duration(_state)) : 0f;
            if (IsBlocking)
            {
                rp = new Vector3(0.14f, -0.16f, 0.4f); re = new Vector3(-15f, -75f, 70f);
                lp = new Vector3(-0.12f, -0.2f, 0.42f); le = new Vector3(-10f, 40f, 0f);
                showLeft = true;
                return;
            }
            Vector3 windPos, windRot, endPos, endRot;
            switch (_combo)
            {
                case 1: windPos = new Vector3(-0.05f, -0.14f, 0.3f); windRot = new Vector3(-20f, -60f, 50f); endPos = new Vector3(0.42f, -0.3f, 0.45f); endRot = new Vector3(15f, 55f, 40f); break;
                case 2: windPos = new Vector3(0.22f, 0.02f, 0.22f); windRot = new Vector3(-75f, 0f, 0f); endPos = new Vector3(0.12f, -0.38f, 0.58f); endRot = new Vector3(40f, -5f, 0f); break;
                default: windPos = new Vector3(0.38f, -0.12f, 0.26f); windRot = new Vector3(-20f, 60f, -50f); endPos = new Vector3(-0.12f, -0.3f, 0.48f); endRot = new Vector3(15f, -60f, -40f); break;
            }
            switch (_state)
            {
                case State.Windup: rp = Vector3.Lerp(rp, windPos, u); re = Vector3.Lerp(re, windRot, u); break;
                case State.Active: rp = Vector3.Lerp(windPos, endPos, u); re = Vector3.Lerp(windRot, endRot, u); break;
                case State.Recover: rp = Vector3.Lerp(endPos, rp, u); re = Vector3.Lerp(endRot, re, u); break;
                case State.Charging:
                    var jitter = Random.insideUnitSphere * 0.004f * _charge;
                    rp = new Vector3(0.36f, 0.04f, 0.12f) + jitter; re = new Vector3(-80f, 20f, -20f);
                    break;
                case State.HeavyWindup: rp = new Vector3(0.36f, 0.08f, 0.12f); re = new Vector3(-85f, 20f, -20f); break;
                case State.HeavyActive: rp = Vector3.Lerp(new Vector3(0.36f, 0.08f, 0.12f), new Vector3(0.04f, -0.45f, 0.6f), u); re = Vector3.Lerp(new Vector3(-85f, 20f, -20f), new Vector3(55f, -15f, 10f), u); break;
                case State.HeavyRecover: rp = Vector3.Lerp(new Vector3(0.04f, -0.45f, 0.6f), rp, u); re = Vector3.Lerp(new Vector3(55f, -15f, 10f), re, u); break;
                case State.Stunned: rp = new Vector3(0.3f, -0.45f, 0.25f); re = new Vector3(40f, 10f, -30f); break;
            }
        }
    }
}
