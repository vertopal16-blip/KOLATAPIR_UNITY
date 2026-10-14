using System.Collections.Generic;
using Kolotopir.Core;
using UnityEngine;

namespace Kolotopir
{
    /// <summary>
    /// Cápsula: estructura orgánica que conserva sueños y recuerdos. Su aspecto y lo que ofrece dependen
    /// de su estado (estable, inactiva, dañada, sellada, anómala). Se examina, se escucha, se repara,
    /// se reconstruye su recuerdo y se decide qué hacer con él.
    /// </summary>
    public sealed class CapsuleController : Interactable, IListenable
    {
        public string CapsuleId { get; private set; }
        public CapsuleDef Def { get; private set; }
        private Material _core, _shell;
        private Transform _sealWrap, _cracks, _patch;
        private LightSource _light;
        private ParticleSystem _motes;
        private AudioSource _hum, _anomaly;
        private CapsuleState _shown = (CapsuleState)(-1);
        private float _seed;

        public CapsuleState State => Game.State != null ? Game.State.CapsuleOf(CapsuleId) : Def.InitialState;

        public override string Prompt => "Acercarse a la " + Def.Name.ToLowerInvariant() + " (" + Names.Capsule(State).ToLowerInvariant() + ")";
        public override float Range => 3.4f;
        public override Vector3 FocusPoint => transform.position + Vector3.up * 1.2f;

        public Vector3 ListenPoint => transform.position + Vector3.up * 1.2f;
        public float ListenRadius => 3.4f;
        public string ListenLabel => Def.Name;
        public bool IsAvailable => true;

        public void Init(string id, bool sealedWraps)
        {
            CapsuleId = id;
            Def = GameDb.Capsule(id);
            _seed = Random.value * 10f;
            Build(sealedWraps);
        }

        protected override void OnEnable() { base.OnEnable(); Listenables.All.Add(this); }
        protected override void OnDisable() { base.OnDisable(); Listenables.All.Remove(this); }

        private void Build(bool tribal)
        {
            _shell = MaterialLibrary.LitInstance(tribal ? new Color(0.55f, 0.48f, 0.5f) : new Color(0.72f, 0.62f, 0.52f), 0.35f);
            _core = MaterialLibrary.LitInstance(new Color(0.8f, 0.7f, 1f), 0.6f);
            var mb = new ModelBuilder();
            // Vaina orgánica: bulbo con costillas
            var profile = new[] { new Vector2(0.55f, 0f), new Vector2(1.05f, 0.35f), new Vector2(1.2f, 1.0f), new Vector2(1.0f, 1.7f), new Vector2(0.55f, 2.2f), new Vector2(0.18f, 2.5f), new Vector2(0f, 2.55f) };
            mb.For(_shell).Lathe(Vector3.zero, profile, 12, Quaternion.identity, true, false);
            for (int i = 0; i < 8; i++)
            {
                var rot = Quaternion.Euler(0f, i * 45f + 22f, 0f);
                mb.For(_shell).Box(rot * new Vector3(0f, 1.15f, 1.18f), new Vector3(0.14f, 1.9f, 0.12f), rot * Quaternion.Euler(-6f, 0f, 0f));
            }
            // Núcleo luminoso visible entre las costillas
            mb.For(_core).Sphere(new Vector3(0f, 1.15f, 0f), new Vector3(0.95f, 1.05f, 0.95f), 12, 8, Quaternion.identity);
            // Raíces que la anclan al suelo
            for (int i = 0; i < 5; i++)
            {
                var rot = Quaternion.Euler(0f, i * 72f, 0f);
                mb.For(Pal.Bark).Cylinder(rot * new Vector3(0f, 0.15f, 0.8f), 0.16f, 0.04f, 1.6f, 5, rot * Quaternion.Euler(70f, 0f, 0f));
            }
            mb.Build(transform, "Capsula");
            Colliders.Capsule(K.Child(transform, "Col"), new Vector3(0f, 1.2f, 0f), 1.1f, 2.6f);

            // Envoltura de cuerdas y amuletos (sellada por la tribu)
            _sealWrap = K.Child(transform, "Sello");
            var sw = new ModelBuilder();
            for (int i = 0; i < 6; i++)
            {
                float y = 0.4f + i * 0.33f;
                float r = Mathf.Lerp(1.08f, 1.24f, Mathf.Sin(Mathf.PI * (y / 2.5f)));
                sw.For(Pal.Rope).Lathe(new Vector3(0f, y, 0f), new[] { new Vector2(r, 0f), new Vector2(r, 0.06f) }, 14, Quaternion.Euler(0f, i * 13f, 0f), false, false);
            }
            for (int i = 0; i < 6; i++)
            {
                var rot = Quaternion.Euler(0f, i * 60f, 0f);
                sw.For(Pal.Gold).Box(rot * new Vector3(0f, 1.45f, 1.28f), new Vector3(0.14f, 0.14f, 0.03f), rot * Quaternion.Euler(0f, 0f, 45f));
                sw.For(new Color(0.42f, 0.18f, 0.55f)).Box(rot * new Vector3(0f, 1.15f, 1.27f), new Vector3(0.06f, 0.35f, 0.02f), rot);
            }
            sw.Build(_sealWrap, "Sello", false);

            // Grietas (dañada) y parche de fragmento (reparada)
            _cracks = K.Child(transform, "Grietas");
            var cr = new ModelBuilder();
            var crackMat = MaterialLibrary.Emissive(new Color(0.9f, 0.85f, 1f), Pal.Resonance, 1.6f);
            for (int i = 0; i < 7; i++)
            {
                var rot = Quaternion.Euler(0f, 20f + i * 17f, 0f);
                cr.For(crackMat).Box(rot * new Vector3(0f, 1.0f + (i % 3) * 0.3f, 1.2f), new Vector3(0.05f, 0.5f, 0.03f), rot * Quaternion.Euler(0f, 0f, (i % 2 == 0 ? 30f : -25f)));
            }
            cr.Build(_cracks, "Grietas", false);
            _patch = K.Child(transform, "Parche");
            var pm = new ModelBuilder();
            pm.For(MaterialLibrary.Emissive(new Color(0.75f, 0.65f, 0.95f), Pal.Resonance, 0.8f)).Box(Quaternion.Euler(0f, 70f, 0f) * new Vector3(0f, 1.25f, 1.22f), new Vector3(0.45f, 0.6f, 0.05f), Quaternion.Euler(0f, 70f, 0f));
            pm.Build(_patch, "Parche", false);

            _light = LightSource.Create(K.Child(transform, "Luz", new Vector3(0f, 1.3f, 0f)), Pal.Resonance, 9f, 1.6f, false, 4);
            _motes = FX.Motes(transform, new Vector3(0f, 1.3f, 0f), K.WithA(Pal.ResonanceSoft, 0.8f), 1.6f, 6f);
            _hum = AudioManager.AttachLoop(gameObject, "capsula_zumbido", 0.5f, 22f);
            var anomalyGo = K.Child(transform, "Anomalia").gameObject;
            _anomaly = AudioManager.AttachLoop(anomalyGo, "capsula_anomala", 0.6f, 30f);
            ApplyState(true);
        }

        private void Update()
        {
            ApplyState(false);
            var st = State;
            float t = Time.time + _seed;
            float glow;
            Color c;
            switch (st)
            {
                case CapsuleState.Anomala:
                    glow = 0.6f + 0.9f * Mathf.Abs(Mathf.Sin(t * 1.7f) * Mathf.Sin(t * 0.63f + 1f)) + (Mathf.PerlinNoise(t * 3f, 0f) > 0.8f ? 0.6f : 0f);
                    c = Color.Lerp(Pal.Resonance, new Color(0.4f, 0.6f, 1f), Mathf.PerlinNoise(t, 5f));
                    break;
                case CapsuleState.Danada:
                    glow = Mathf.PerlinNoise(t * 6f, 2f) > 0.55f ? 1.1f : 0.25f;
                    c = new Color(0.85f, 0.75f, 1f);
                    break;
                case CapsuleState.Estable:
                    glow = 1.0f + Mathf.Sin(t * 0.8f) * 0.15f;
                    c = new Color(1f, 0.82f, 0.6f);
                    break;
                case CapsuleState.Sellada:
                    glow = 0.08f;
                    c = new Color(0.5f, 0.45f, 0.6f);
                    break;
                default:
                    glow = 0f;
                    c = Color.black;
                    break;
            }
            if (Game.Settings != null && Game.Settings.Data.ReduceFlashes) glow = Mathf.Min(glow, 1f);
            MaterialLibrary.SetEmission(_core, c * glow * 1.4f);
            MaterialLibrary.SetColor(_core, Color.Lerp(new Color(0.3f, 0.28f, 0.32f), c, Mathf.Clamp01(glow)));
            if (_light != null) _light.Boost = glow;
        }

        private void ApplyState(bool force)
        {
            var st = State;
            if (!force && st == _shown) return;
            _shown = st;
            bool tribal = CapsuleId == "cap_sellada";
            _sealWrap.gameObject.SetActive(st == CapsuleState.Sellada || (tribal && st != CapsuleState.Estable));
            _cracks.gameObject.SetActive(st == CapsuleState.Danada || (CapsuleId == "cap_parada" && !Game.State.Has("cap_parada_reparada")));
            _patch.gameObject.SetActive(CapsuleId == "cap_parada" && Game.State != null && Game.State.Has("cap_parada_reparada"));
            var em = _motes.emission;
            em.rateOverTime = st == CapsuleState.Anomala ? 14f : st == CapsuleState.Estable ? 5f : st == CapsuleState.Danada ? 3f : 0f;
            if (_hum != null) _hum.mute = st == CapsuleState.Inactiva || st == CapsuleState.Sellada;
            if (_anomaly != null) _anomaly.mute = st != CapsuleState.Anomala;
        }

        public override void Interact() => Game.UI.OpenCapsule(this);

        // ---------------------------------------------------------------- IListenable

        public string NextResonance(out string reason)
        {
            reason = null;
            var st = State;
            if (st == CapsuleState.Inactiva || st == CapsuleState.Sellada)
            {
                reason = "La Cápsula no responde.";
                return null;
            }
            string last = null;
            foreach (var id in Def.ListenSequence)
            {
                var r = GameDb.Resonance(id);
                if (r == null) continue;
                bool ok = r.Requirement == null || r.Requirement(Game.State);
                if (!ok) continue;
                last = id;
                if (!Game.State.Heard(id)) return id;
            }
            if (last != null) return last;
            reason = "La Cápsula no deja oír nada todavía.";
            return null;
        }

        public void OnListened(string resonanceId) { }

        // ---------------------------------------------------------------- Acciones

        public string ExamineText()
        {
            var st = State;
            string text = Def.ExamineText.TryGetValue(st, out var t) ? t : Def.Description;
            return text + "\n\nEstado: " + Names.Capsule(st) + ". " + Names.CapsuleReading(st);
        }

        public void Examine()
        {
            Def.OnExamine?.Invoke(Game.Ctx);
            Game.Ctx.AddNotebook("cul_capsulas", Knowledge.Observado);
            Game.MarkDirty();
        }

        public bool CanRepair(out string reason)
        {
            reason = null;
            var s = Game.State;
            if (string.IsNullOrEmpty(Def.RepairFlag) || s.Has(Def.RepairFlag)) return false;
            if (Def.RepairAvailable != null && !Def.RepairAvailable(s)) { reason = "El Guardián no te deja acercarte lo suficiente."; return false; }
            if (!s.Inventory.HasAll(Def.RepairCost)) { reason = "Necesitas un fragmento de cápsula."; return false; }
            return true;
        }

        public bool RepairPending => !string.IsNullOrEmpty(Def.RepairFlag) && !Game.State.Has(Def.RepairFlag);

        public void Repair()
        {
            if (!CanRepair(out _)) return;
            Game.State.Inventory.RemoveAll(Def.RepairCost);
            Game.Ctx.SetFlag(Def.RepairFlag);
            Game.Ctx.AddXp(25, "Cápsula reparada");
            Game.Ctx.Notify("Encajas el fragmento en la grieta. La luz se estabiliza.");
            Game.Audio?.Play3D("res_fragmento", transform.position + Vector3.up, 0.8f);
            FX.Burst(transform.position + Vector3.up * 1.2f, Pal.ResonanceSoft, 30, 2f);
            _shown = (CapsuleState)(-1);
            Game.MarkDirty();
        }

        public bool PuzzlePending => Def.Fragments.Count > 0 && !Game.State.Has(Def.PuzzleSolvedFlag);
        public bool PuzzleAvailable => PuzzlePending && (Def.PuzzleAvailable == null || Def.PuzzleAvailable(Game.State));

        public bool SolvePuzzle(IList<string> order)
        {
            if (order.Count != Def.CorrectOrder.Length) return false;
            for (int i = 0; i < order.Count; i++) if (order[i] != Def.CorrectOrder[i]) return false;
            Def.OnPuzzleSolved?.Invoke(Game.Ctx);
            Game.Ctx.SetFlag(Def.PuzzleSolvedFlag);
            Game.MarkDirty();
            return true;
        }

        public bool DecisionPending => Def.Decisions.Count > 0 && !Game.State.Has(Def.DecisionFlag) && (Def.DecisionAvailable == null || Def.DecisionAvailable(Game.State));

        public void Decide(CapsuleDecision d)
        {
            var ctx = Game.Ctx;
            d.Effect?.Invoke(ctx);
            ctx.SetCapsuleState(CapsuleId, d.ResultState);
            ctx.RecordDecision("dec_recuerdo", d.Label, d.Description, d.Scale);
            ctx.SetFlag(Def.DecisionFlag);
            ctx.AddXp(30, "Decisión");
            ctx.Notify("Decisión tomada · alcance " + Names.Scale(d.Scale).ToLowerInvariant());
            _shown = (CapsuleState)(-1);
            Game.MarkDirty();
            Game.Root.OnCapsuleDecision(d);
        }
    }
}
