using System.Collections.Generic;
using Kolotopir.Core;
using UnityEngine;

namespace Kolotopir
{
    /// <summary>Elige el objeto interactivo al que mira Nilo y muestra un aviso discreto.</summary>
    public sealed class PlayerInteractor : MonoBehaviour
    {
        public Interactable Current { get; private set; }

        private void Update()
        {
            Current = null;
            bool blocked = !Game.InGame || (Game.UI != null && Game.UI.BlocksGameplay) || Game.Player == null || Game.Player.Frozen || Game.Player.IsClimbing;
            if (!blocked && Game.Cam != null && !Game.Cam.Scoping) Current = Find();
            if (Game.UI != null) Game.UI.SetPrompt(Current != null ? "[" + InputMap.KeyName(InputAction.Interactuar) + "]  " + Tokens.Apply(Current.Prompt) : null);
            if (!blocked && Current != null && InputMap.Down(InputAction.Interactuar)) Current.Interact();
        }

        private Interactable Find()
        {
            var cam = Game.Cam.Cam.transform;
            Vector3 eye = cam.position, fwd = cam.forward;
            Interactable best = null;
            float bestScore = float.MaxValue;
            foreach (var it in Interactable.All)
            {
                if (it == null || !it.isActiveAndEnabled || !it.CanInteract) continue;
                var focus = it.FocusPoint;
                var to = focus - eye;
                float d = to.magnitude;
                if (d > it.Range + 0.4f) continue;
                float angle = d < 0.01f ? 0f : Vector3.Angle(fwd, to);
                float maxAngle = it.NeedsAim ? Mathf.Lerp(55f, 28f, d / (it.Range + 0.4f)) : 80f;
                if (angle > maxAngle) continue;
                if (it.NeedsAim && d > 0.6f && Blocked(eye, focus, it)) continue;
                float score = angle + d * 6f;
                if (score < bestScore) { bestScore = score; best = it; }
            }
            return best;
        }

        private static readonly RaycastHit[] Hits = new RaycastHit[8];

        /// <summary>¿Hay una pared entre los ojos y el objeto? (ignora colisionadores del propio objeto y de Nilo).</summary>
        private static bool Blocked(Vector3 eye, Vector3 focus, Interactable it)
        {
            var dir = focus - eye;
            float d = dir.magnitude - 0.35f;
            if (d <= 0.05f) return false;
            int n = Physics.RaycastNonAlloc(eye, dir.normalized, Hits, d, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var c = Hits[i].collider;
                if (c == null) continue;
                if (c.transform.IsChildOf(it.transform) || it.transform.IsChildOf(c.transform.parent != null ? c.transform.parent : c.transform)) continue;
                if (Game.Player != null && c.transform.IsChildOf(Game.Player.transform)) continue;
                if (c.GetComponentInParent<NpcAgent>() != null) continue;
                return true;
            }
            return false;
        }
    }

    /// <summary>Algo que se puede escuchar con la Escucha de Resonancias (puntos, Cápsulas, el Guardián).</summary>
    public interface IListenable
    {
        Vector3 ListenPoint { get; }
        float ListenRadius { get; }
        string ListenLabel { get; }
        bool IsAvailable { get; }
        /// <summary>Resonancia que sonará; null y <paramref name="reason"/> si no se puede escuchar.</summary>
        string NextResonance(out string reason);
        void OnListened(string resonanceId);
    }

    public static class Listenables
    {
        public static readonly List<IListenable> All = new List<IListenable>();
    }

    /// <summary>
    /// Escucha de Resonancias: mantener la tecla cerca de una señal reproduce un fragmento breve.
    /// Además revela rastros ocultos y atrae a los ecos (riesgo y coste del uso).
    /// </summary>
    public sealed class ResonanceListener : MonoBehaviour
    {
        public bool IsListening { get; private set; }
        public float Reveal { get; private set; }
        public float Progress { get; private set; }
        public IListenable Target { get; private set; }
        public float MoveMultiplier => IsListening ? 0.45f : 1f;

        private float _linger;
        private bool _needsRelease;
        private float _alertTimer;
        private string _lastReason;

        private void Update()
        {
            var s = Game.State;
            if (s == null) return;
            float dt = Time.deltaTime;
            bool blocked = !Game.InGame || (Game.UI != null && Game.UI.BlocksGameplay) || Game.Player == null || Game.Player.Frozen;
            bool held = !blocked && InputMap.Held(InputAction.Escuchar) && !Game.Player.IsSwimming && !(Game.Combat != null && Game.Combat.IsAttacking);
            if (!held) _needsRelease = false;
            IsListening = held;
            if (held) _linger = 4f; else _linger -= dt;
            float targetReveal = held ? 1f : (_linger > 0f ? 0.6f : 0f);
            Reveal = Mathf.MoveTowards(Reveal, targetReveal, dt * (held ? 2.5f : 0.6f));
            if (Game.Sky != null) Game.Sky.ResonanceTint = held ? Mathf.Min(1f, Reveal) : Reveal * 0.3f;
            PostFX.SetResonance(held ? Reveal : Reveal * 0.25f);

            // Rastros ocultos
            foreach (var trail in FootprintTrail.All)
            {
                if (trail == null) continue;
                bool allowed = trail.TrailIndex == 0 ? s.Has("huellas_reveladas") : s.Has("cruzo_puente");
                trail.SetReveal(allowed ? Reveal : 0f);
            }

            // Escuchar atrae a los ecos cercanos
            if (held)
            {
                _alertTimer -= dt;
                if (_alertTimer <= 0f)
                {
                    _alertTimer = 0.6f;
                    CreatureBase.AlertNearby(Game.Player.transform.position, 25f, true);
                }
            }

            Target = blocked ? null : FindTarget(s);
            if (held && Target != null && !_needsRelease && !(Game.Resonance != null && Game.Resonance.Playing))
            {
                string id = Target.NextResonance(out string reason);
                if (id == null)
                {
                    if (reason != _lastReason) { _lastReason = reason; if (!string.IsNullOrEmpty(reason)) Game.UI?.Toast(reason); }
                    Progress = 0f;
                }
                else
                {
                    float listenTime = s.HasSkill("oido_fino") ? 1.15f : 1.6f;
                    Progress += dt / listenTime;
                    if (Progress >= 1f)
                    {
                        Progress = 0f;
                        _needsRelease = true;
                        _lastReason = null;
                        var target = Target;
                        Game.Resonance.Request(id, target.ListenPoint, () => target.OnListened(id));
                    }
                }
            }
            else Progress = Mathf.MoveTowards(Progress, 0f, dt * 2f);
            if (!held) _lastReason = null;
        }

        private IListenable FindTarget(GameState s)
        {
            var eye = Game.Cam.Cam.transform;
            float rangeMult = s.Stats.ResonanceRangeMult;
            IListenable best = null;
            float bestScore = float.MaxValue;
            foreach (var l in Listenables.All)
            {
                if (l == null || !l.IsAvailable) continue;
                var to = l.ListenPoint - eye.position;
                float d = to.magnitude;
                float r = l.ListenRadius * rangeMult;
                if (d > r + 1.2f) continue;
                float angle = Vector3.Angle(eye.forward, to);
                if (d > 1.8f && angle > 70f) continue;
                float score = d + angle * 0.03f;
                if (score < bestScore) { bestScore = score; best = l; }
            }
            return best;
        }

        /// <summary>Señal más cercana (para el indicador sutil del HUD aunque no esté a tiro).</summary>
        public float NearestSignal(out Vector3 where)
        {
            where = Vector3.zero;
            if (Game.Player == null) return 0f;
            float best = 0f;
            var p = Game.Player.transform.position;
            float range = 16f * (Game.State != null ? Game.State.Stats.DetectionMult : 1f);
            foreach (var l in Listenables.All)
            {
                if (l == null || !l.IsAvailable) continue;
                float d = Vector3.Distance(p, l.ListenPoint);
                float k = 1f - d / range;
                if (k > best) { best = k; where = l.ListenPoint; }
            }
            return best;
        }
    }
}
