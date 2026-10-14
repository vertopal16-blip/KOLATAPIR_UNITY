using Kolotopir.Core;
using UnityEngine;

namespace Kolotopir
{
    /// <summary>
    /// Nilo en primera persona: caminar, correr, saltar, agacharse, nadar, trepar en puntos diseñados,
    /// mantener el equilibrio, sentarse y recuperarse de caídas (doc 13 y actualización RPG).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        public const float StandHeight = 1.62f;
        public const float CrouchHeight = 1.0f;
        public const float Radius = 0.32f;

        public float WalkSpeed = 3.4f;
        public float RunSpeed = 6.1f;
        public float CrouchSpeed = 1.7f;
        public float SwimSpeed = 2.3f;
        public float ClimbSpeed = 1.9f;
        public float JumpHeight = 1.15f;
        public float Gravity = 22f;

        public CharacterController CC { get; private set; }
        public bool Frozen;
        public bool IsGrounded { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsCrouching { get; private set; }
        public bool IsSwimming { get; private set; }
        public bool IsClimbing { get; private set; }
        public bool IsBalancing { get; private set; }
        public bool IsSitting { get; private set; }
        public float Balance { get; private set; }
        public float NoiseRadius { get; private set; }
        public float EyeHeight => (IsSitting ? 1.0f : IsSwimming ? 1.35f : CC.height - 0.12f);
        public Vector3 Velocity;
        public Vector3 LastSafePosition { get; private set; }
        public SurfaceKind Surface { get; private set; }

        private float _coyote;
        private Vector3 _external;
        private float _externalTime;
        private ClimbZone _climb;
        private float _mantle;
        private Vector3 _mantleFrom, _mantleTo;
        private Seat _seat;
        private float _safeTimer;
        private float _airStartY;
        private bool _wasGrounded = true;
        private float _stepDistance;
        private float _swimSplash;
        private float _balanceDrift;
        private float _balanceSeed;
        private Collider _groundCollider;

        private void Awake()
        {
            CC = GetComponent<CharacterController>();
            CC.height = StandHeight;
            CC.radius = Radius;
            CC.center = new Vector3(0f, StandHeight * 0.5f, 0f);
            CC.slopeLimit = 50f;
            CC.stepOffset = 0.42f;
            CC.skinWidth = 0.04f;
            CC.minMoveDistance = 0f;
            _balanceSeed = Random.value * 50f;
        }

        public void Teleport(Vector3 pos, float yaw)
        {
            CC.enabled = false;
            transform.position = pos;
            transform.rotation = K.Yaw(yaw);
            CC.enabled = true;
            Velocity = Vector3.zero;
            _external = Vector3.zero;
            IsClimbing = false;
            IsSitting = false;
            IsSwimming = false;
            _mantle = 0f;
            LastSafePosition = pos;
            _airStartY = pos.y;
            if (Game.Cam != null) Game.Cam.ResetPitch();
        }

        public void AddImpulse(Vector3 velocity, float duration)
        {
            _external = velocity;
            _externalTime = duration;
        }

        public float ExternalMoveMultiplier()
        {
            float m = 1f;
            if (Game.Combat != null) m *= Game.Combat.MoveMultiplier;
            if (Game.Vitals != null) m *= Game.Vitals.MoveMultiplier;
            if (Game.Listener != null) m *= Game.Listener.MoveMultiplier;
            return m;
        }

        private bool Blocked => Frozen || !Game.InGame || (Game.UI != null && Game.UI.BlocksGameplay);

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || CC == null || !CC.enabled) return;
            bool blocked = Blocked;
            Vector2 input = blocked ? Vector2.zero : InputMap.Move();

            if (IsSitting) { UpdateSitting(input, blocked); return; }
            if (_mantle > 0f) { UpdateMantle(dt); return; }
            if (IsClimbing) { UpdateClimb(input, dt, blocked); return; }

            UpdateSwimState();
            if (IsSwimming) UpdateSwim(input, dt, blocked);
            else UpdateGround(input, dt, blocked);

            PostMove(dt);
        }

        // ---------------------------------------------------------------- Suelo

        private void UpdateGround(Vector2 input, float dt, bool blocked)
        {
            var stats = Game.State.Stats;
            if (!blocked && InputMap.Down(InputAction.Agacharse)) SetCrouch(!IsCrouching);

            bool wantsRun = !blocked && InputMap.Held(InputAction.Correr) && input.y > 0.1f && !IsCrouching && !IsBalancing;
            float sprintCost = 13f * stats.SprintCostMult * stats.StaminaCostMult;
            IsRunning = wantsRun && Game.Vitals.TrySpendStaminaContinuous(sprintCost * dt);

            float speed = IsCrouching ? CrouchSpeed : IsRunning ? RunSpeed : WalkSpeed;
            speed *= stats.MoveSpeedMult * ExternalMoveMultiplier();

            Vector3 wish;
            if (IsBalancing)
            {
                // Sobre una pasarela estrecha: A/D corrigen el equilibrio en lugar de desplazar.
                speed = Mathf.Min(speed, 1.5f);
                wish = transform.forward * input.y * speed;
                UpdateBalance(input.x, input.y, dt);
            }
            else
            {
                wish = (transform.right * input.x + transform.forward * input.y) * speed;
                Balance = Mathf.MoveTowards(Balance, 0f, dt * 2f);
            }

            var horiz = new Vector3(Velocity.x, 0f, Velocity.z);
            float accel = IsGrounded ? 14f : 2.5f;
            horiz = Vector3.Lerp(horiz, wish, 1f - Mathf.Exp(-accel * dt));

            if (IsGrounded)
            {
                _coyote = 0.15f;
                if (Velocity.y < 0f) Velocity.y = -2f;
            }
            else _coyote -= dt;

            if (!blocked && InputMap.Down(InputAction.Saltar) && _coyote > 0f && !IsCrouching && !IsBalancing)
            {
                if (Game.Vitals.TrySpendStamina(8f * stats.StaminaCostMult))
                {
                    Velocity.y = Mathf.Sqrt(2f * Gravity * JumpHeight);
                    _coyote = 0f;
                    Game.Audio?.Play2D("salto", 0.5f);
                }
            }
            Velocity.y -= Gravity * dt;
            Velocity = new Vector3(horiz.x, Velocity.y, horiz.z);

            Vector3 motion = Velocity * dt;
            if (_externalTime > 0f)
            {
                motion += _external * dt;
                _externalTime -= dt;
                _external = Vector3.Lerp(_external, Vector3.zero, dt * 6f);
            }
            var flags = CC.Move(motion);
            IsGrounded = CC.isGrounded;
            if ((flags & CollisionFlags.Above) != 0 && Velocity.y > 0f) Velocity.y = 0f;

            DetectGround();

            // Agarrarse a una pared trepable
            if (!blocked && input.y > 0.5f && !IsCrouching)
            {
                foreach (var zone in ClimbZone.All)
                {
                    if (zone != null && zone.CanGrab(transform.position, transform.forward))
                    {
                        StartClimb(zone);
                        break;
                    }
                }
            }

            float moving = horiz.magnitude;
            NoiseRadius = moving < 0.2f ? 0f : IsCrouching ? 2.5f : IsRunning ? 14f : 7f;
            if (IsGrounded && moving > 0.3f)
            {
                _stepDistance += moving * dt;
                float stride = IsRunning ? 2.4f : IsCrouching ? 1.4f : 1.9f;
                if (_stepDistance > stride)
                {
                    _stepDistance = 0f;
                    Footstep(IsCrouching ? 0.25f : IsRunning ? 0.7f : 0.45f);
                }
            }
        }

        private void SetCrouch(bool crouch)
        {
            if (!crouch)
            {
                // ¿Hay espacio para levantarse?
                var p = transform.position + Vector3.up * (CrouchHeight - Radius);
                if (Physics.SphereCast(p, Radius * 0.9f, Vector3.up, out _, StandHeight - CrouchHeight + 0.05f, ~0, QueryTriggerInteraction.Ignore)) return;
            }
            IsCrouching = crouch;
            float h = crouch ? CrouchHeight : StandHeight;
            CC.height = h;
            CC.center = new Vector3(0f, h * 0.5f, 0f);
        }

        private void DetectGround()
        {
            _groundCollider = null;
            IsBalancing = false;
            if (!IsGrounded) return;
            if (Physics.Raycast(transform.position + Vector3.up * 0.3f, Vector3.down, out var hit, 0.8f, ~0, QueryTriggerInteraction.Ignore))
            {
                _groundCollider = hit.collider;
                IsBalancing = hit.collider.GetComponent<BalanceBeam>() != null;
                var tag = hit.collider.GetComponent<SurfaceTag>();
                if (tag != null) Surface = tag.Kind;
                else if (hit.collider is TerrainCollider)
                {
                    var p = transform.position;
                    Surface = WorldShape.DistanceToPath(p.x, p.z, 4f) < 2.6f || P2.Distance(new P2(p.x, p.z), WorldLayout.Plaza) < 13f ? SurfaceKind.Tierra : SurfaceKind.Hierba;
                }
                else Surface = SurfaceKind.Piedra;
            }
        }

        private void UpdateBalance(float lateral, float forward, float dt)
        {
            float t = Time.time + _balanceSeed;
            float drift = (Mathf.PerlinNoise(t * 0.6f, 3.1f) - 0.5f) * 1.6f;
            _balanceDrift = Mathf.Lerp(_balanceDrift, drift, dt * 2f);
            float instability = 0.9f + Mathf.Abs(forward) * 0.6f;
            Balance += (_balanceDrift * instability + Balance * 0.8f) * dt;
            Balance -= lateral * 1.7f * dt;
            if (Mathf.Abs(Balance) >= 1f)
            {
                float side = Mathf.Sign(Balance);
                Balance = 0f;
                IsBalancing = false;
                AddImpulse(transform.right * side * 3.5f + Vector3.up * 0.5f, 0.35f);
                Game.UI?.Toast("¡Has perdido el equilibrio!");
            }
        }

        // ---------------------------------------------------------------- Nado

        private void UpdateSwimState()
        {
            var p = transform.position;
            float level = WaterSystem.LevelAt(p.x, p.z);
            if (float.IsNaN(level)) { IsSwimming = false; return; }
            float depth = level - p.y;
            if (!IsSwimming && depth > 1.15f)
            {
                IsSwimming = true;
                if (IsCrouching) SetCrouch(false);
                Game.Audio?.Play3D("chapoteo", p + Vector3.up, 0.8f);
                Game.Ctx?.Notify("Nadando");
            }
            else if (IsSwimming && depth < 0.95f)
            {
                IsSwimming = false;
            }
        }

        private void UpdateSwim(Vector2 input, float dt, bool blocked)
        {
            var p = transform.position;
            float level = WaterSystem.LevelAt(p.x, p.z);
            if (float.IsNaN(level)) { IsSwimming = false; return; }
            bool fast = !blocked && InputMap.Held(InputAction.Correr) && Game.Vitals.TrySpendStaminaContinuous(8f * dt);
            float speed = SwimSpeed * (fast ? 1.5f : 1f) * Game.State.Stats.MoveSpeedMult;
            var wish = (transform.right * input.x + transform.forward * input.y) * speed;
            var horiz = Vector3.Lerp(new Vector3(Velocity.x, 0f, Velocity.z), wish, 1f - Mathf.Exp(-4f * dt));
            float targetY = level - 1.25f + Mathf.Sin(Time.time * 2f) * 0.04f;
            float vy = (targetY - p.y) * 3f;
            if (!blocked && InputMap.Held(InputAction.Saltar)) vy += 1.2f;
            Velocity = new Vector3(horiz.x, vy, horiz.z);
            CC.Move(Velocity * dt);
            IsGrounded = false;
            IsRunning = false;
            NoiseRadius = horiz.magnitude > 0.3f ? 6f : 0f;
            _swimSplash -= dt;
            if (horiz.magnitude > 0.5f && _swimSplash <= 0f)
            {
                _swimSplash = 0.9f;
                Game.Audio?.Play3D("chapoteo", p + Vector3.up * 1.2f, 0.35f);
            }
        }

        // ---------------------------------------------------------------- Trepar

        private void StartClimb(ClimbZone zone)
        {
            IsClimbing = true;
            _climb = zone;
            Velocity = Vector3.zero;
            if (IsCrouching) SetCrouch(false);
            var local = zone.transform.InverseTransformPoint(transform.position);
            local.x = Mathf.Clamp(local.x, -zone.Width * 0.5f, zone.Width * 0.5f);
            local.z = 0.5f;
            local.y = Mathf.Max(0f, local.y);
            CC.enabled = false;
            transform.position = zone.transform.TransformPoint(local);
            transform.rotation = Quaternion.LookRotation(-zone.transform.forward);
            CC.enabled = true;
            Game.Ctx?.Notify("Trepando · {Saltar} para soltarte");
        }

        private void UpdateClimb(Vector2 input, float dt, bool blocked)
        {
            if (_climb == null) { IsClimbing = false; return; }
            var zone = _climb.transform;
            bool tired = Game.State.Stamina.Current <= 0.5f;
            float vy = blocked ? 0f : input.y * ClimbSpeed;
            if (input.y > 0.1f && !Game.Vitals.TrySpendStaminaContinuous(4f * dt)) tired = true;
            if (tired && vy > 0f) vy = -0.6f;
            float vx = blocked ? 0f : input.x * ClimbSpeed * 0.6f;
            var delta = Vector3.up * vy * dt + zone.right * vx * dt;
            var next = transform.position + delta;
            var local = zone.InverseTransformPoint(next);
            local.x = Mathf.Clamp(local.x, -_climb.Width * 0.5f, _climb.Width * 0.5f);
            local.z = 0.5f;
            next = zone.TransformPoint(local);
            CC.enabled = false;
            transform.position = next;
            CC.enabled = true;
            NoiseRadius = 3f;

            if (!blocked && InputMap.Down(InputAction.Saltar))
            {
                IsClimbing = false;
                AddImpulse(zone.forward * 2.5f, 0.25f);
                Velocity = Vector3.up * 2f;
                return;
            }
            if (transform.position.y >= _climb.TopY - 0.2f && input.y > 0.1f)
            {
                // Subir a la cima
                IsClimbing = false;
                _mantle = 0.45f;
                _mantleFrom = transform.position;
                _mantleTo = transform.position + Vector3.up * 0.55f - zone.forward * 1.05f;
                _mantleTo.y = _climb.TopY + 0.08f;
                return;
            }
            if (local.y <= 0.02f && input.y < -0.1f) IsClimbing = false;
            _stepDistance += Mathf.Abs(vy) * dt;
            if (_stepDistance > 0.8f) { _stepDistance = 0f; Game.Audio?.Play3D("recolectar", transform.position + Vector3.up, 0.25f); }
        }

        private void UpdateMantle(float dt)
        {
            _mantle -= dt;
            float t = 1f - Mathf.Clamp01(_mantle / 0.45f);
            var p = t < 0.5f
                ? Vector3.Lerp(_mantleFrom, new Vector3(_mantleFrom.x, _mantleTo.y, _mantleFrom.z), t * 2f)
                : Vector3.Lerp(new Vector3(_mantleFrom.x, _mantleTo.y, _mantleFrom.z), _mantleTo, (t - 0.5f) * 2f);
            CC.enabled = false;
            transform.position = p;
            CC.enabled = true;
            if (_mantle <= 0f) { Velocity = Vector3.zero; _airStartY = transform.position.y; }
        }

        // ---------------------------------------------------------------- Sentarse

        public void SitAt(Seat seat)
        {
            if (seat == null || IsSitting) return;
            _seat = seat;
            IsSitting = true;
            Velocity = Vector3.zero;
            CC.enabled = false;
            transform.position = seat.transform.position - Vector3.up * 0.55f;
            transform.rotation = Quaternion.Euler(0f, seat.transform.eulerAngles.y, 0f);
            CC.enabled = true;
            Game.Ctx?.Notify("Sentado. Muévete para levantarte.");
        }

        private void UpdateSitting(Vector2 input, bool blocked)
        {
            NoiseRadius = 0f;
            if (blocked) return;
            if (input.sqrMagnitude > 0.1f || InputMap.Down(InputAction.Saltar) || InputMap.Down(InputAction.Interactuar))
            {
                IsSitting = false;
                CC.enabled = false;
                if (_seat != null) transform.position = _seat.transform.position + _seat.transform.forward * 0.7f - Vector3.up * 0.5f;
                CC.enabled = true;
                _seat = null;
            }
        }

        // ---------------------------------------------------------------- Después de moverse

        private void PostMove(float dt)
        {
            var p = transform.position;
            if (IsGrounded && !_wasGrounded)
            {
                float drop = _airStartY - p.y;
                if (drop > 1.2f) Game.Audio?.Play2D("caida", Mathf.Clamp01(drop / 6f));
                if (drop > 6.5f && !IsSwimming)
                {
                    float dmg = (drop - 6.5f) * 5f;
                    Game.Vitals?.FallDamage(dmg);
                    Game.Cam?.Shake(0.4f);
                }
                Game.Cam?.LandDip(Mathf.Clamp01(drop / 4f));
            }
            if (!IsGrounded && _wasGrounded) _airStartY = p.y;
            if (!IsGrounded && Velocity.y > 0f) _airStartY = Mathf.Max(_airStartY, p.y);
            _wasGrounded = IsGrounded;

            _safeTimer -= dt;
            if (_safeTimer <= 0f)
            {
                _safeTimer = 1f;
                if (IsGrounded && !IsBalancing && _groundCollider != null && WorldShape.IsSafeGround(p.x, p.z) && !WorldShape.IsOnBridgeCorridor(p.x, p.z))
                    LastSafePosition = p;
            }
            if (p.y < WorldShape.FallRecoveryY) Game.Root.RecoverFromFall();
        }

        private void Footstep(float vol)
        {
            int v = Random.Range(0, 3);
            string clip;
            switch (Surface)
            {
                case SurfaceKind.Madera: clip = "paso_madera_" + v; break;
                case SurfaceKind.Piedra: clip = "paso_piedra_" + v; break;
                case SurfaceKind.Tierra: clip = "paso_tierra_" + v; break;
                default: clip = "paso_hierba_" + v; break;
            }
            Game.Audio?.Play2D(clip, vol, Random.Range(0.92f, 1.08f));
        }

        public void ForceStand()
        {
            if (IsCrouching) SetCrouch(false);
            IsSitting = false;
            IsClimbing = false;
        }
    }
}
