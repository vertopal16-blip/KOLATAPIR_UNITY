using Kolotopir.Core;
using UnityEngine;

namespace Kolotopir
{
    /// <summary>Muñeco de práctica de Haro: tres golpes (uno fuerte) y bloquear su contragolpe.</summary>
    public sealed class TrainingDummy : MonoBehaviour, IDamageable
    {
        private Transform _arm;
        private Material _glow;
        private float _counterAt = -1f;
        private float _spin;
        private bool _swinging;

        public bool IsAlive => true;
        public bool Hostile => false;
        public string DisplayName => "Muñeco de práctica";
        public float HealthFraction => 1f;
        public Vector3 AimPoint => transform.position + Vector3.up * 1.3f;

        public static void Create(Transform t)
        {
            var mb = new ModelBuilder();
            mb.For(Pal.WoodDark).Cylinder(Vector3.zero, 0.09f, 0.08f, 1.9f, 6, Quaternion.identity);
            mb.For(Pal.Thatch).Lathe(new Vector3(0f, 0.7f, 0f), new[] { new Vector2(0.2f, 0f), new Vector2(0.32f, 0.3f), new Vector2(0.28f, 0.75f), new Vector2(0.12f, 0.85f) }, 9, Quaternion.identity);
            mb.For(new Color(0.75f, 0.68f, 0.5f)).Sphere(new Vector3(0f, 1.75f, 0f), Vector3.one * 0.2f, 8, 5, Quaternion.identity);
            mb.For(Pal.Rope).Cylinder(new Vector3(0f, 1.0f, 0f), 0.33f, 0.33f, 0.05f, 9, Quaternion.identity);
            mb.Build(t, "Muneco");
            Colliders.Capsule(t, new Vector3(0f, 1f, 0f), 0.35f, 2f, "Cuerpo");
            var arm = K.Child(t, "Brazo", new Vector3(0f, 1.35f, 0f));
            var am = new ModelBuilder();
            am.For(Pal.Wood).Box(new Vector3(0.55f, 0f, 0f), new Vector3(1.1f, 0.08f, 0.08f), Quaternion.identity);
            am.Build(arm, "Brazo");
            var dummy = t.gameObject.AddComponent<TrainingDummy>();
            dummy._arm = arm;
            dummy._glow = MaterialLibrary.UnlitTransparent(new Color(1f, 0.8f, 0.4f, 0f));
            var glow = new MeshBuilder();
            glow.Box(new Vector3(0.55f, 0f, 0f), new Vector3(1.2f, 0.14f, 0.14f), Quaternion.identity);
            var g = K.Child(arm, "Brillo");
            g.gameObject.AddComponent<MeshFilter>().sharedMesh = glow.ToMesh("Brillo");
            var mr = g.gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = dummy._glow;
        }

        public void TakeHit(in DamageInfo info)
        {
            _spin = info.Heavy ? 720f : 360f;
            Game.Audio?.Play3D("muneco", AimPoint, 0.9f);
            var s = Game.State;
            if (s == null) return;
            bool training = s.AtStep("mq_silencio", "entrenar");
            if (training)
            {
                s.Flags.Increment("dummy_hits");
                if (info.Heavy) s.Flags.Increment("dummy_heavy");
                CheckDone();
            }
            if (_counterAt < 0f && Game.Player != null && K.FlatDistance(Game.Player.transform.position, transform.position) < 3.2f)
                _counterAt = Time.time + 0.9f;
        }

        private void Update()
        {
            if (_spin > 0f)
            {
                float step = Mathf.Min(_spin, Time.deltaTime * 900f);
                _arm.Rotate(0f, step, 0f, Space.World);
                _spin -= step;
            }
            float glow = 0f;
            if (_counterAt > 0f)
            {
                float left = _counterAt - Time.time;
                glow = Mathf.Clamp01(1f - left / 0.9f);
                if (!_swinging && Game.Player != null)
                {
                    // El brazo gira hacia Nilo y brilla antes de golpear.
                    var to = Game.Player.transform.position - transform.position;
                    to.y = 0f;
                    if (to.sqrMagnitude > 0.01f)
                    {
                        var targetRot = Quaternion.LookRotation(to) * Quaternion.Euler(0f, -90f, 0f);
                        _arm.rotation = Quaternion.Slerp(_arm.rotation, targetRot, Time.deltaTime * 6f);
                    }
                }
                if (left <= 0f)
                {
                    _counterAt = -1f;
                    Counter();
                }
            }
            MaterialLibrary.SetColor(_glow, new Color(1f, 0.8f, 0.4f, glow * 0.7f));
        }

        private void Counter()
        {
            var p = Game.Player;
            if (p == null || K.FlatDistance(p.transform.position, transform.position) > 2.9f) return;
            _spin = 200f;
            var s = Game.State;
            bool training = s.AtStep("mq_silencio", "entrenar");
            if (Game.Combat != null && Game.Combat.IsBlocking)
            {
                Game.Audio?.Play2D(Game.Combat.PerfectBlockWindow ? "parada" : "bloqueo", 0.8f);
                if (training) { s.Flags.Increment("dummy_blocks"); CheckDone(); }
                Game.UI?.Subtitle("Haro", Game.Combat.PerfectBlockWindow ? "¡Justo a tiempo! Eso es un bloqueo perfecto." : "¡Bien bloqueado!", 2.5f);
            }
            else
            {
                Game.Vitals?.FallDamage(2f);
                Game.Cam?.Shake(0.2f);
                if (training) Game.UI?.Subtitle("Haro", "¡Bloquea cuando el brazo brille! [" + InputMap.KeyName(InputAction.Bloquear) + "]", 3f);
            }
            Game.MarkDirty();
        }

        private static void CheckDone()
        {
            var s = Game.State;
            if (s.Has("entrenamiento_hecho")) return;
            if (s.Flags.Get("dummy_hits") >= 3 && s.Flags.Get("dummy_heavy") >= 1 && s.Flags.Get("dummy_blocks") >= 1)
            {
                Game.Ctx.SetFlag("entrenamiento_hecho");
                Game.UI?.Subtitle("Haro", "¡Eso es! Ya sabes defenderte... más o menos.", 3.5f);
                Game.MarkDirty();
            }
        }
    }

    /// <summary>
    /// Miga: una polilla de papel negra surgida de los fragmentos descartados de Cápsulas antiguas.
    /// Acompaña a Nilo, aporta humor y extrañeza, y revolotea hacia los restos de recuerdos: pistas graduales.
    /// </summary>
    public sealed class Miga : MonoBehaviour
    {
        private Transform _wingL, _wingR, _body;
        private Vector3 _vel;
        private float _hintUntil;
        private Vector3 _hintTarget;
        private float _quipTimer = 120f;
        private int _hintLevel;
        private string _hintStep;
        private float _hintLevelReset;
        private bool _visible;

        private static readonly string[] Quips =
        {
            "Miga ha mordisqueado una esquina de tu cuaderno.",
            "Miga se posa en tu hombro y se queda muy quieta, como si escuchara algo.",
            "Miga intenta posarse en la espada. Resbala.",
            "Miga revolotea alrededor de tu cabeza tres veces. Nadie sabe por qué.",
            "Miga se esconde dentro de tu capucha. Hace cosquillas.",
            "Miga persigue una mota de polvo. La mota gana.",
        };

        public static Miga Create(Transform parent)
        {
            var root = new GameObject("Miga");
            root.transform.SetParent(parent, false);
            var m = root.AddComponent<Miga>();
            m.Build();
            return m;
        }

        private void Build()
        {
            var paper = MaterialLibrary.Lit(new Color(0.06f, 0.05f, 0.07f), 0.3f);
            var edge = MaterialLibrary.Emissive(new Color(0.4f, 0.3f, 0.6f), Pal.Resonance, 0.6f);
            _body = K.Child(transform, "Cuerpo");
            var bm = new ModelBuilder();
            bm.For(paper).Sphere(Vector3.zero, new Vector3(0.025f, 0.025f, 0.07f), 6, 4, Quaternion.identity);
            bm.For(edge).Sphere(new Vector3(0.012f, 0.012f, 0.06f), Vector3.one * 0.008f, 4, 3, Quaternion.identity);
            bm.For(edge).Sphere(new Vector3(-0.012f, 0.012f, 0.06f), Vector3.one * 0.008f, 4, 3, Quaternion.identity);
            bm.Build(_body, "Miga", false, false);
            _wingL = K.Child(_body, "AlaI");
            _wingR = K.Child(_body, "AlaD");
            foreach (var w in new[] { _wingL, _wingR })
            {
                float side = w == _wingL ? -1f : 1f;
                var wm = new ModelBuilder();
                wm.For(paper).DoubleTri(Vector3.zero, new Vector3(side * 0.14f, 0f, 0.05f), new Vector3(side * 0.11f, 0f, -0.08f));
                wm.For(edge).DoubleTri(new Vector3(side * 0.1f, 0.001f, 0.035f), new Vector3(side * 0.14f, 0.001f, 0.05f), new Vector3(side * 0.12f, 0.001f, -0.02f));
                wm.Build(w, "Ala", false, false);
            }
            FX.Motes(_body, Vector3.zero, K.WithA(Pal.ResonanceSoft, 0.5f), 0.05f, 2f);
            SetVisible(false);
        }

        private void SetVisible(bool v)
        {
            if (_visible == v) return;
            _visible = v;
            _body.gameObject.SetActive(v);
        }

        /// <summary>Pista gradual: cada vez que se pide para el mismo paso, es más explícita.</summary>
        public void RequestHint()
        {
            var tracker = Game.Quests;
            if (tracker == null) return;
            if (Game.Settings != null && !Game.Settings.Data.Hints) { Game.UI?.Toast("Las pistas están desactivadas en Ajustes."); return; }
            var step = tracker.HintStep(out var quest);
            if (step == null) { Game.UI?.Toast(_visible ? "Miga revolotea sin rumbo: ahora mismo no hay nada urgente." : "No hay ninguna pista disponible ahora mismo."); return; }
            if (_hintStep != step.Id || Time.time > _hintLevelReset) { _hintLevel = 0; _hintStep = step.Id; }
            int level = Mathf.Min(_hintLevel, step.Hints.Length - 1);
            _hintLevel++;
            _hintLevelReset = Time.time + 120f;
            if (level >= 0 && step.Hints.Length > 0) Game.UI?.ShowHint(quest.Title, Tokens.Apply(step.Hints[level]), level + 1, step.Hints.Length);
            if (_visible && tracker.TryGetTargetPosition(step.HintTarget, out var target))
            {
                var p = Game.Player.transform.position;
                var dir = target - p;
                dir.y = 0f;
                _hintTarget = dir.magnitude < 25f ? target + Vector3.up * 1.5f : p + dir.normalized * 10f + Vector3.up * 2f;
                _hintUntil = Time.time + 4f;
                Game.Audio?.Play3D("miga", transform.position, 0.6f);
            }
        }

        private void Update()
        {
            var s = Game.State;
            var player = Game.Player;
            if (s == null || player == null || !Game.InGame) { SetVisible(false); return; }
            SetVisible(s.Has("miga_aparece"));
            if (!_visible) return;
            float dt = Time.deltaTime;
            var cam = Game.Cam.Cam.transform;
            Vector3 target;
            bool scared = false;
            foreach (var c in CreatureBase.All)
                if (c is EchoEnemy && c.IsAlive && Vector3.Distance(c.transform.position, player.transform.position) < 8f) { scared = true; break; }
            if (Time.time < _hintUntil) target = _hintTarget;
            else if (scared) target = player.transform.position - player.transform.forward * 0.6f + Vector3.up * 1.2f;
            else target = cam.position + cam.right * 0.55f + cam.forward * 0.9f + Vector3.up * (0.15f + Mathf.Sin(Time.time * 2.3f) * 0.08f);
            target += new Vector3(Mathf.Sin(Time.time * 3.1f), Mathf.Sin(Time.time * 4.3f) * 0.5f, Mathf.Cos(Time.time * 2.7f)) * 0.05f;
            transform.position = Vector3.SmoothDamp(transform.position, target, ref _vel, Time.time < _hintUntil ? 0.6f : 0.25f, 30f);
            if (_vel.sqrMagnitude > 0.01f) transform.rotation = K.Damp(transform.rotation, Quaternion.LookRotation(_vel), 8f, dt);
            float flap = Mathf.Sin(Time.time * (scared ? 45f : 28f)) * 55f;
            _wingL.localRotation = Quaternion.Euler(0f, 0f, -flap);
            _wingR.localRotation = Quaternion.Euler(0f, 0f, flap);
            if ((transform.position - cam.position).sqrMagnitude > 2500f) transform.position = cam.position + cam.forward;

            _quipTimer -= dt;
            if (_quipTimer <= 0f)
            {
                _quipTimer = Random.Range(150f, 260f);
                Game.UI?.Toast(Quips[Random.Range(0, Quips.Length)]);
            }
        }
    }
}
