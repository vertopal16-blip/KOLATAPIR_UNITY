using System.Collections.Generic;
using Kolotopir.Core;
using UnityEngine;

namespace Kolotopir
{
    /// <summary>
    /// Salud (float, 100 al inicio), aguante, curación y derrota. Curarse lleva tiempo y deja vulnerable
    /// (doc: "el jugador debe decidir si gasta recursos ahora o los reserva").
    /// </summary>
    public sealed class PlayerVitals : MonoBehaviour
    {
        private sealed class Regen { public float PerSecond; public float Left; public float StaminaBuff; }

        private readonly List<Regen> _regens = new List<Regen>();
        private float _staminaDelay;
        private float _healPerSecond;
        private float _healLeft;
        private float _eatTimer;
        public int QuickIndex;

        public bool IsHealing => _healLeft > 0f;
        public float MoveMultiplier => IsHealing && !Game.State.HasSkill("vendaje_rapido") ? 0.5f : 1f;
        public string HealingItem { get; private set; }

        public bool TrySpendStamina(float amount)
        {
            var st = Game.State.Stamina;
            if (amount <= 0f) return true;
            if (st.Current < amount * 0.6f) return false;
            st.Reduce(amount);
            _staminaDelay = 0.9f;
            return true;
        }

        public bool TrySpendStaminaContinuous(float amount)
        {
            var st = Game.State.Stamina;
            if (st.Current <= 0.5f) return false;
            st.Reduce(amount);
            _staminaDelay = 0.6f;
            return true;
        }

        private bool Blocked => !Game.InGame || (Game.UI != null && Game.UI.BlocksGameplay) || Game.Player == null || Game.Player.Frozen;

        private void Update()
        {
            var s = Game.State;
            if (s == null || !Game.InGame) return;
            float dt = Time.deltaTime;
            var stats = s.Stats;

            // Aguante
            _staminaDelay -= dt;
            if (_staminaDelay <= 0f && !(Game.Combat != null && Game.Combat.IsBlocking) && !(Game.Player != null && Game.Player.IsRunning))
            {
                float regen = stats.StaminaRegen;
                foreach (var r in _regens) regen *= 1f + r.StaminaBuff;
                if (Game.Player != null && Game.Player.IsSitting) regen *= 2f;
                s.Stamina.Restore(regen * dt);
            }

            // Comida: recuperación gradual
            for (int i = _regens.Count - 1; i >= 0; i--)
            {
                var r = _regens[i];
                float step = Mathf.Min(r.Left, dt);
                s.Health.Restore(r.PerSecond * step);
                r.Left -= dt;
                if (r.Left <= 0f) _regens.RemoveAt(i);
            }

            // Vendas y ungüentos: curación canalizada
            if (_healLeft > 0f)
            {
                float step = Mathf.Min(_healLeft, _healPerSecond * dt);
                s.Health.Restore(step);
                _healLeft -= step;
                if (_healLeft <= 0.01f) { _healLeft = 0f; HealingItem = null; }
            }
            _eatTimer -= dt;

            if (Blocked) return;
            float scroll = InputMap.Scroll();
            if (InputMap.Down(InputAction.SiguienteObjeto) || Mathf.Abs(scroll) > 0.01f) CycleQuick(scroll < 0f ? -1 : 1);
            for (int i = 0; i < 4; i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i)) { QuickIndex = i; Game.Audio?.Play2D("ui_clic", 0.4f); }
            if (InputMap.Down(InputAction.UsarObjeto)) UseQuick();
        }

        public string QuickItem => Game.State.QuickSlots[Mathf.Clamp(QuickIndex, 0, Game.State.QuickSlots.Length - 1)];

        private void CycleQuick(int dir)
        {
            var slots = Game.State.QuickSlots;
            for (int i = 1; i <= slots.Length; i++)
            {
                int idx = ((QuickIndex + dir * i) % slots.Length + slots.Length) % slots.Length;
                if (Game.State.Inventory.Count(slots[idx]) > 0 || i == slots.Length) { QuickIndex = idx; break; }
            }
            Game.Audio?.Play2D("ui_clic", 0.4f);
        }

        public void UseQuick() => UseItem(QuickItem);

        public bool UseItem(string itemId)
        {
            var s = Game.State;
            var def = GameDb.Item(itemId);
            if (def == null || !def.IsConsumable) return false;
            if (s.Inventory.Count(itemId) <= 0) { Game.UI?.Toast($"No te quedan: {def.Name}"); return false; }
            if (IsHealing || _eatTimer > 0f) return false;

            // Ofrecer bayas a un jabalí inquieto (puzle de criaturas)
            if (itemId == "bayas")
            {
                var boar = MossBoar.NearestAgitated(Game.Player.transform.position, 7f);
                if (boar != null)
                {
                    s.Inventory.Remove("bayas", 1);
                    boar.Calm();
                    return true;
                }
            }

            s.Inventory.Remove(itemId, 1);
            if (def.HealAmount > 0f)
            {
                float duration = def.HealDuration / s.Stats.HealSpeedMult;
                _healLeft = def.HealAmount;
                _healPerSecond = def.HealAmount / Mathf.Max(0.1f, duration);
                HealingItem = def.Name;
                Game.Audio?.Play2D("curar", 0.7f);
            }
            if (def.IsFood)
            {
                _regens.Add(new Regen { PerSecond = def.RegenPerSecond, Left = def.RegenDuration, StaminaBuff = def.StaminaRegenBuff });
                _eatTimer = 0.8f;
                Game.Audio?.Play2D("comer", 0.7f);
                Game.UI?.Toast($"{def.Name}: recuperación gradual");
            }
            Game.MarkDirty();
            return true;
        }

        public void InterruptHealing()
        {
            if (_healLeft <= 0f) return;
            _healLeft = 0f;
            HealingItem = null;
            Game.UI?.Toast("¡Curación interrumpida!");
        }

        /// <summary>Recibir un golpe: esquiva, bloqueo (perfecto o no), defensa y resistencias.</summary>
        public void TakeDamage(DamageInfo info)
        {
            var s = Game.State;
            if (s == null || !Game.InGame || s.Health.IsEmpty) return;
            var combat = Game.Combat;
            if (combat != null && combat.IsInvulnerable)
            {
                Game.UI?.Toast("¡Esquivado!");
                return;
            }
            var stats = s.Stats;
            float dmg = DamageModel.Compute(info.Amount, info.Type, stats.Defense, stats.ResistPhysical, stats.ResistResonance);
            var toAttacker = info.From - Game.Player.transform.position;
            toAttacker.y = 0f;
            bool frontal = Vector3.Angle(Game.Player.transform.forward, toAttacker) < 75f;
            if (combat != null && combat.IsBlocking && frontal && info.Blockable)
            {
                bool perfect = combat.PerfectBlockWindow;
                if (perfect)
                {
                    Game.Audio?.Play2D("parada", 0.9f);
                    if (info.Attacker != null) info.Attacker.OnParried();
                    combat.NotifyParry();
                    Game.UI?.Toast("¡Bloqueo perfecto!");
                    return;
                }
                float blocked = DamageModel.Blocked(dmg, false, 0.7f);
                float cost = (dmg - blocked) * 1.2f + 4f;
                if (s.Stamina.Current >= cost)
                {
                    s.Stamina.Reduce(cost);
                    dmg = blocked;
                    Game.Audio?.Play2D("bloqueo", 0.8f);
                    Game.Cam?.Shake(0.15f);
                }
                else
                {
                    s.Stamina.Reduce(s.Stamina.Current);
                    combat.Stun(0.8f);
                }
            }
            InterruptHealing();
            if (dmg <= 0f) return;
            s.Health.Reduce(dmg);
            Game.Audio?.Play2D("herida", 0.8f);
            Game.Cam?.Shake(info.Heavy ? 0.5f : 0.25f);
            Game.UI?.DamageFlash(Mathf.Clamp01(dmg / 25f), info.From);
            if (info.Knockback > 0f) Game.Player.AddImpulse(-toAttacker.normalized * info.Knockback * 1.6f, 0.2f);
            if (s.Health.IsEmpty)
            {
                var skill = GameDb.Skill("segundo_aliento");
                if (s.HasSkill("segundo_aliento") && s.Skills.IsReady("segundo_aliento", Time.time))
                {
                    s.Health.SetCurrent(1f);
                    s.Skills.StartCooldown("segundo_aliento", Time.time, skill.Cooldown);
                    Game.UI?.Toast("Segundo aliento: sigues en pie.");
                    return;
                }
                Game.Root.PlayerDefeated();
            }
        }

        /// <summary>Daño por caída: tolerante, nunca mortal.</summary>
        public void FallDamage(float dmg)
        {
            var h = Game.State.Health;
            float applied = Mathf.Min(dmg, h.Current - 1f);
            if (applied > 0f)
            {
                h.Reduce(applied);
                Game.Audio?.Play2D("herida", 0.6f);
                Game.UI?.DamageFlash(0.4f, Game.Player.transform.position);
            }
        }

        public void ClearEffects()
        {
            _regens.Clear();
            _healLeft = 0f;
            HealingItem = null;
        }
    }
}
