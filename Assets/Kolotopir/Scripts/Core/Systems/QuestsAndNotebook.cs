using System;
using System.Collections.Generic;

namespace Kolotopir.Core
{
    public sealed class QuestProgress
    {
        public string Id;
        public QuestStatus Status;
        public int Step;
        public int Order;
    }

    /// <summary>Registro de misiones: objetivos, condiciones, recompensas y registro en el diario.</summary>
    public sealed class QuestLog
    {
        private readonly Dictionary<string, QuestProgress> _progress = new Dictionary<string, QuestProgress>();
        private int _order;

        public event Action<QuestDef> Started;
        public event Action<QuestDef, QuestStep> StepCompleted;
        public event Action<QuestDef> Completed;

        public QuestProgress Get(string id) => _progress.TryGetValue(id, out var p) ? p : null;
        public bool IsActive(string id) => Get(id)?.Status == QuestStatus.Activa;
        public bool IsCompleted(string id) => Get(id)?.Status == QuestStatus.Completada;
        public bool IsKnown(string id) => Get(id) != null;
        public int StepIndex(string id) => Get(id)?.Step ?? -1;

        public IEnumerable<QuestProgress> All => _progress.Values;

        public bool Start(string id)
        {
            var def = GameDb.Quest(id);
            if (def == null || _progress.ContainsKey(id)) return false;
            _progress[id] = new QuestProgress { Id = id, Status = QuestStatus.Activa, Step = 0, Order = ++_order };
            Started?.Invoke(def);
            return true;
        }

        public QuestStep CurrentStep(string id)
        {
            var p = Get(id);
            var def = GameDb.Quest(id);
            if (p == null || def == null || p.Status != QuestStatus.Activa || p.Step >= def.Steps.Count) return null;
            return def.Steps[p.Step];
        }

        /// <summary>
        /// Avanza todos los pasos cuyas condiciones ya se cumplen (en cadena). Devuelve si hubo progreso.
        /// Permite resolver objetivos en otro orden: si el jugador ya hizo algo antes, el paso se completa al llegar.
        /// </summary>
        public bool Evaluate(GameState state, IGameContext ctx)
        {
            bool any = false;
            for (int guard = 0; guard < 64; guard++)
            {
                bool progressed = false;
                var active = new List<QuestProgress>();
                foreach (var p in _progress.Values) if (p.Status == QuestStatus.Activa) active.Add(p);
                active.Sort((a, b) => a.Order.CompareTo(b.Order));
                foreach (var p in active)
                {
                    var def = GameDb.Quest(p.Id);
                    if (def == null || p.Step >= def.Steps.Count) continue;
                    var step = def.Steps[p.Step];
                    bool done;
                    try { done = step.Done != null && step.Done(state); }
                    catch (Exception) { done = false; }
                    if (!done) continue;
                    p.Step++;
                    progressed = any = true;
                    if (step.Xp > 0) ctx.AddXp(step.Xp, "Objetivo cumplido");
                    step.OnComplete?.Invoke(ctx);
                    StepCompleted?.Invoke(def, step);
                    if (p.Step >= def.Steps.Count)
                    {
                        p.Status = QuestStatus.Completada;
                        if (def.CompletionXp > 0) ctx.AddXp(def.CompletionXp, "Misión completada: " + def.Title);
                        def.OnComplete?.Invoke(ctx);
                        Completed?.Invoke(def);
                    }
                }
                if (!progressed) break;
            }
            return any;
        }

        public void Restore(string id, QuestStatus status, int step, int order)
        {
            _progress[id] = new QuestProgress { Id = id, Status = status, Step = Math.Max(0, step), Order = order };
            _order = Math.Max(_order, order);
        }

        public void Clear() { _progress.Clear(); _order = 0; }
    }

    /// <summary>
    /// Cuaderno de campo: Atlas, Bestiario, Culturas, Personajes, Resonancias, Misterios y Objetos.
    /// Ayuda a recordar sin resolver automáticamente los misterios.
    /// </summary>
    public sealed class Notebook
    {
        private readonly Dictionary<string, Knowledge> _entries = new Dictionary<string, Knowledge>();
        private readonly List<string> _order = new List<string>();
        private readonly HashSet<string> _evidence = new HashSet<string>();
        private readonly Dictionary<string, string> _chosen = new Dictionary<string, string>();
        private readonly HashSet<string> _resolved = new HashSet<string>();
        private readonly HashSet<string> _unread = new HashSet<string>();

        public event Action<string, bool> EntryUpdated;
        public event Action<string> EvidenceAdded;
        public event Action<string, bool> MysteryResolved;

        public bool HasEntry(string id) => _entries.ContainsKey(id);
        public Knowledge Status(string id) => _entries.TryGetValue(id, out var k) ? k : Knowledge.Sospecha;
        public bool IsUnread(string id) => _unread.Contains(id);
        public void MarkRead(string id) => _unread.Remove(id);
        public int UnreadCount => _unread.Count;

        /// <summary>Añade o eleva la certeza de una entrada. Devuelve true si es nueva o sube de nivel.</summary>
        public bool AddEntry(string id, Knowledge k)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (_entries.TryGetValue(id, out var current))
            {
                if (k <= current) return false;
                _entries[id] = k;
                _unread.Add(id);
                EntryUpdated?.Invoke(id, false);
                return true;
            }
            _entries[id] = k;
            _order.Insert(0, id);
            _unread.Add(id);
            EntryUpdated?.Invoke(id, true);
            return true;
        }

        public IEnumerable<string> EntriesIn(NotebookCategory cat)
        {
            foreach (var id in _order)
            {
                var def = GameDb.Entry(id);
                if (def != null && def.Category == cat) yield return id;
            }
        }

        public IEnumerable<string> AllEntries => _order;

        public bool HasEvidence(string id) => _evidence.Contains(id);
        public IEnumerable<string> Evidence => _evidence;

        /// <summary>Registra una evidencia. Devuelve el misterio afectado (o null).</summary>
        public MysteryDef AddEvidence(string evidenceId, out bool isNew, out bool resolvedNow, out bool matched)
        {
            isNew = resolvedNow = matched = false;
            var mystery = GameDb.MysteryForEvidence(evidenceId);
            if (mystery == null) return null;
            if (!_evidence.Add(evidenceId)) return mystery;
            isNew = true;
            AddEntry(mystery.Id, Knowledge.Sospecha);
            _unread.Add(mystery.Id);
            EvidenceAdded?.Invoke(evidenceId);
            if (mystery.ResolvingEvidence == evidenceId && !_resolved.Contains(mystery.Id))
            {
                _resolved.Add(mystery.Id);
                matched = Chosen(mystery.Id) == mystery.TrueHypothesis;
                resolvedNow = true;
                _entries[mystery.Id] = Knowledge.Sabe;
                MysteryResolved?.Invoke(mystery.Id, matched);
            }
            return mystery;
        }

        public string Chosen(string mysteryId) => _chosen.TryGetValue(mysteryId, out var h) ? h : null;

        public bool ChooseHypothesis(string mysteryId, string hypothesisId)
        {
            var m = GameDb.Mystery(mysteryId);
            if (m == null || _resolved.Contains(mysteryId)) return false;
            bool exists = false;
            foreach (var h in m.Hypotheses) if (h.Id == hypothesisId) exists = true;
            if (!exists) return false;
            _chosen[mysteryId] = hypothesisId;
            EntryUpdated?.Invoke(mysteryId, false);
            return true;
        }

        public bool IsResolved(string mysteryId) => _resolved.Contains(mysteryId);

        /// <summary>Estado de una hipótesis según las evidencias reunidas: +1 apoyada, -1 refutada, 0 abierta.</summary>
        public int HypothesisSupport(HypothesisDef h)
        {
            foreach (var r in h.RefutedBy) if (_evidence.Contains(r)) return -1;
            foreach (var s in h.SupportedBy) if (_evidence.Contains(s)) return 1;
            return 0;
        }

        public IEnumerable<KeyValuePair<string, Knowledge>> EntryStates => _entries;
        public IEnumerable<KeyValuePair<string, string>> Choices => _chosen;
        public IEnumerable<string> Resolved => _resolved;

        public void Restore(IEnumerable<KeyValuePair<string, Knowledge>> entries, IEnumerable<string> evidence,
            IEnumerable<KeyValuePair<string, string>> chosen, IEnumerable<string> resolved)
        {
            Clear();
            foreach (var kv in entries) { _entries[kv.Key] = kv.Value; _order.Add(kv.Key); }
            foreach (var e in evidence) _evidence.Add(e);
            foreach (var kv in chosen) _chosen[kv.Key] = kv.Value;
            foreach (var r in resolved) _resolved.Add(r);
        }

        public void Clear()
        {
            _entries.Clear(); _order.Clear(); _evidence.Clear(); _chosen.Clear(); _resolved.Clear(); _unread.Clear();
        }
    }

    /// <summary>Recorre un diálogo ramificado aplicando condiciones y efectos.</summary>
    public sealed class DialogueRunner
    {
        public DialogueDef Def { get; private set; }
        public DialogueNode Current { get; private set; }
        public bool Finished => Current == null;
        private IGameContext _ctx;

        public bool Start(DialogueDef def, IGameContext ctx)
        {
            Def = def;
            _ctx = ctx;
            Current = null;
            if (def == null) return false;
            Go(def.Start);
            return Current != null;
        }

        public bool IsLocked(DialogueChoice c)
        {
            if (c.Condition == null) return false;
            try { return !c.Condition(_ctx.State); }
            catch (Exception) { return true; }
        }

        public List<DialogueChoice> VisibleChoices()
        {
            var list = new List<DialogueChoice>();
            if (Current == null) return list;
            foreach (var c in Current.Choices)
                if (!IsLocked(c) || !c.HideWhenLocked) list.Add(c);
            return list;
        }

        public void Continue()
        {
            if (Current == null) return;
            if (VisibleChoices().Count > 0) return;
            Go(Current.Next);
        }

        public bool Choose(DialogueChoice c)
        {
            if (Current == null || c == null || IsLocked(c)) return false;
            c.Effect?.Invoke(_ctx);
            Go(c.Next);
            return true;
        }

        public void Stop() => Current = null;

        private void Go(string id)
        {
            if (string.IsNullOrEmpty(id) || id == "end" || Def == null || !Def.Nodes.TryGetValue(id, out var node))
            {
                Current = null;
                return;
            }
            Current = node;
            node.OnEnter?.Invoke(_ctx);
        }
    }
}
