using System;
using System.Collections.Generic;

namespace Kolotopir.Core
{
    /// <summary>
    /// Base de datos del contenido del prototipo. El contenido se declara en código (archivos parciales
    /// en Content/) para que el compilador y las pruebas detecten referencias rotas.
    /// </summary>
    public static partial class GameDb
    {
        private static readonly Dictionary<string, ItemDef> _items = new Dictionary<string, ItemDef>();
        private static readonly Dictionary<string, SkillDef> _skills = new Dictionary<string, SkillDef>();
        private static readonly List<RecipeDef> _recipes = new List<RecipeDef>();
        private static readonly Dictionary<string, QuestDef> _quests = new Dictionary<string, QuestDef>();
        private static readonly Dictionary<string, NotebookEntryDef> _entries = new Dictionary<string, NotebookEntryDef>();
        private static readonly Dictionary<string, MysteryDef> _mysteries = new Dictionary<string, MysteryDef>();
        private static readonly Dictionary<string, string> _evidenceToMystery = new Dictionary<string, string>();
        private static readonly Dictionary<string, ResonanceDef> _resonances = new Dictionary<string, ResonanceDef>();
        private static readonly Dictionary<string, CapsuleDef> _capsules = new Dictionary<string, CapsuleDef>();
        private static readonly Dictionary<string, NpcDef> _npcs = new Dictionary<string, NpcDef>();
        private static readonly Dictionary<string, DialogueDef> _dialogues = new Dictionary<string, DialogueDef>();
        private static readonly Dictionary<string, MerchantDef> _merchants = new Dictionary<string, MerchantDef>();
        private static readonly Dictionary<string, ExamineDef> _examines = new Dictionary<string, ExamineDef>();
        private static readonly List<AmbientConversation> _conversations = new List<AmbientConversation>();

        public static IReadOnlyDictionary<string, ItemDef> Items => _items;
        public static IReadOnlyDictionary<string, SkillDef> Skills => _skills;
        public static IReadOnlyList<RecipeDef> Recipes => _recipes;
        public static IReadOnlyDictionary<string, QuestDef> Quests => _quests;
        public static IReadOnlyDictionary<string, NotebookEntryDef> Entries => _entries;
        public static IReadOnlyDictionary<string, MysteryDef> Mysteries => _mysteries;
        public static IReadOnlyDictionary<string, ResonanceDef> Resonances => _resonances;
        public static IReadOnlyDictionary<string, CapsuleDef> Capsules => _capsules;
        public static IReadOnlyDictionary<string, NpcDef> Npcs => _npcs;
        public static IReadOnlyDictionary<string, DialogueDef> Dialogues => _dialogues;
        public static IReadOnlyDictionary<string, MerchantDef> Merchants => _merchants;
        public static IReadOnlyDictionary<string, ExamineDef> Examines => _examines;
        public static IReadOnlyList<AmbientConversation> Conversations => _conversations;

        static GameDb()
        {
            AddItems();
            AddSkills();
            AddRecipes();
            AddMerchants();
            AddNotebook();
            AddResonances();
            AddCapsules();
            AddQuests();
            AddNpcs();
            AddTribeDialogues();
            AddPradosDialogues();
            AddExamines();
            AddAutoEntries();
        }

        public static ItemDef Item(string id) => id != null && _items.TryGetValue(id, out var d) ? d : null;
        public static SkillDef Skill(string id) => id != null && _skills.TryGetValue(id, out var d) ? d : null;
        public static QuestDef Quest(string id) => id != null && _quests.TryGetValue(id, out var d) ? d : null;
        public static NotebookEntryDef Entry(string id) => id != null && _entries.TryGetValue(id, out var d) ? d : null;
        public static MysteryDef Mystery(string id) => id != null && _mysteries.TryGetValue(id, out var d) ? d : null;
        public static ResonanceDef Resonance(string id) => id != null && _resonances.TryGetValue(id, out var d) ? d : null;
        public static CapsuleDef Capsule(string id) => id != null && _capsules.TryGetValue(id, out var d) ? d : null;
        public static NpcDef Npc(string id) => id != null && _npcs.TryGetValue(id, out var d) ? d : null;
        public static DialogueDef Dialogue(string id) => id != null && _dialogues.TryGetValue(id, out var d) ? d : null;
        public static MerchantDef Merchant(string id) => id != null && _merchants.TryGetValue(id, out var d) ? d : null;
        public static ExamineDef Examine(string id) => id != null && _examines.TryGetValue(id, out var d) ? d : null;

        public static MysteryDef MysteryForEvidence(string evidenceId)
        {
            return evidenceId != null && _evidenceToMystery.TryGetValue(evidenceId, out var m) ? Mystery(m) : null;
        }

        public static EvidenceDef Evidence(string evidenceId)
        {
            var m = MysteryForEvidence(evidenceId);
            if (m == null) return null;
            foreach (var e in m.Evidence) if (e.Id == evidenceId) return e;
            return null;
        }

        public static LocationDef Location(string id)
        {
            foreach (var l in WorldLayout.Locations) if (l.Id == id) return l;
            return null;
        }

        public static VistaDef Vista(string id)
        {
            foreach (var v in WorldLayout.Vistas) if (v.Id == id) return v;
            return null;
        }

        // ---------------------------------------------------------------- Registro

        private static ItemDef Reg(ItemDef d) { _items[d.Id] = d; return d; }
        private static void Reg(SkillDef d) => _skills[d.Id] = d;
        private static void Reg(RecipeDef d) => _recipes.Add(d);
        private static void Reg(MerchantDef d) => _merchants[d.NpcId] = d;
        private static void Reg(NotebookEntryDef d) => _entries[d.Id] = d;
        private static void Reg(ResonanceDef d) => _resonances[d.Id] = d;
        private static void Reg(CapsuleDef d) => _capsules[d.Id] = d;
        private static void Reg(QuestDef d) => _quests[d.Id] = d;
        private static void Reg(NpcDef d) => _npcs[d.Id] = d;
        private static void Reg(DialogueDef d) => _dialogues[d.Id] = d;
        private static void Reg(ExamineDef d) => _examines[d.Id] = d;
        private static void Reg(AmbientConversation c) => _conversations.Add(c);

        private static void Reg(MysteryDef d)
        {
            _mysteries[d.Id] = d;
            foreach (var e in d.Evidence) _evidenceToMystery[e.Id] = d.Id;
            _entries[d.Id] = new NotebookEntryDef { Id = d.Id, Category = NotebookCategory.Misterios, Title = d.Question, Text = d.Question, Xp = 0 };
        }

        /// <summary>Entradas automáticas del cuaderno: lugares, vistas, Resonancias y objetos singulares.</summary>
        private static void AddAutoEntries()
        {
            foreach (var l in WorldLayout.Locations)
                Reg(new NotebookEntryDef { Id = l.Id, Category = NotebookCategory.Atlas, Title = l.Name, Text = l.Text, Xp = 0 });
            foreach (var v in WorldLayout.Vistas)
                Reg(new NotebookEntryDef { Id = v.Id, Category = NotebookCategory.Atlas, Title = v.Name + " (avistado)", Text = v.Text, Xp = 15 });
            foreach (var r in _resonances.Values)
            {
                var lines = new List<string>();
                foreach (var l in r.Lines) lines.Add(string.IsNullOrEmpty(l.Speaker) ? l.Text : $"{l.Speaker}: «{l.Text}»");
                string text = $"Resonancia {Names.Resonance(r.Type).ToLowerInvariant()}.\n\n" + string.Join("\n", lines)
                              + (string.IsNullOrEmpty(r.Note) ? "" : "\n\nNota de Nilo: " + r.Note)
                              + "\n\nRiesgo de interpretación: " + Names.ResonanceRisk(r.Type);
                Reg(new NotebookEntryDef { Id = "res_" + StripPrefix(r.Id), Category = NotebookCategory.Resonancias, Title = r.Title, Text = text, Xp = 0 });
            }
            foreach (var it in _items.Values)
            {
                if (it.Category != ItemCategory.Narrativo && it.Category != ItemCategory.Cultural && it.Category != ItemCategory.Herramienta) continue;
                Reg(new NotebookEntryDef
                {
                    Id = "obj_" + it.Id,
                    Category = NotebookCategory.Objetos,
                    Title = it.Name,
                    Text = it.Description + "\n\nProcedencia: " + it.Origin + (string.IsNullOrEmpty(it.Function) ? "" : "\nFunción conocida: " + it.Function),
                    Xp = 5,
                });
            }
        }

        public static string ResonanceEntryId(string resonanceId) => "res_" + StripPrefix(resonanceId);
        private static string StripPrefix(string id) => id.StartsWith("res_") ? id.Substring(4) : id;

        /// <summary>Comprueba referencias cruzadas. Devuelve la lista de problemas (vacía si todo está bien).</summary>
        public static List<string> Validate()
        {
            var problems = new List<string>();
            foreach (var r in _recipes)
            {
                foreach (var i in r.Inputs) if (Item(i.ItemId) == null) problems.Add($"Receta {r.Id}: material desconocido {i.ItemId}");
                if (Item(r.Output.ItemId) == null) problems.Add($"Receta {r.Id}: resultado desconocido {r.Output.ItemId}");
            }
            foreach (var m in _merchants.Values)
                foreach (var o in m.Offers) if (Item(o.ItemId) == null) problems.Add($"Comerciante {m.NpcId}: objeto desconocido {o.ItemId}");
            foreach (var it in _items.Values)
                foreach (var up in it.Upgrades)
                    foreach (var c in up.Cost) if (Item(c.ItemId) == null) problems.Add($"Mejora {up.Id}: material desconocido {c.ItemId}");
            foreach (var d in _dialogues.Values)
            {
                if (!d.Nodes.ContainsKey(d.Start)) problems.Add($"Diálogo {d.Id}: nodo inicial inexistente {d.Start}");
                foreach (var n in d.Nodes.Values)
                {
                    if (!string.IsNullOrEmpty(n.Next) && n.Next != "end" && !d.Nodes.ContainsKey(n.Next)) problems.Add($"Diálogo {d.Id}.{n.Id}: siguiente inexistente {n.Next}");
                    foreach (var c in n.Choices)
                        if (!string.IsNullOrEmpty(c.Next) && c.Next != "end" && !d.Nodes.ContainsKey(c.Next)) problems.Add($"Diálogo {d.Id}.{n.Id}: opción hacia nodo inexistente {c.Next}");
                }
            }
            foreach (var n in _npcs.Values)
            {
                foreach (var s in n.Schedule)
                {
                    if (WorldLayout.FindNode(s.Node) == null) problems.Add($"NPC {n.Id}: nodo de horario inexistente {s.Node}");
                    foreach (var r in s.Route) if (WorldLayout.FindNode(r) == null) problems.Add($"NPC {n.Id}: nodo de ruta inexistente {r}");
                }
                if (!string.IsNullOrEmpty(n.MerchantId) && Merchant(n.MerchantId) == null) problems.Add($"NPC {n.Id}: comerciante inexistente");
                if (!string.IsNullOrEmpty(n.NotebookEntry) && Entry(n.NotebookEntry) == null) problems.Add($"NPC {n.Id}: entrada inexistente {n.NotebookEntry}");
            }
            foreach (var c in _capsules.Values)
            {
                foreach (var kv in c.ListenResonance) if (Resonance(kv.Value) == null) problems.Add($"Cápsula {c.Id}: resonancia inexistente {kv.Value}");
                foreach (var id in c.CorrectOrder)
                {
                    bool found = false;
                    foreach (var f in c.Fragments) if (f.Id == id) found = true;
                    if (!found) problems.Add($"Cápsula {c.Id}: fragmento inexistente {id}");
                }
            }
            foreach (var r in _resonances.Values)
                foreach (var e in r.Evidence) if (MysteryForEvidence(e) == null) problems.Add($"Resonancia {r.Id}: evidencia sin misterio {e}");
            foreach (var spot in WorldLayout.ResonanceSpots)
                if (Resonance(spot.ResonanceId) == null) problems.Add($"Punto de resonancia: inexistente {spot.ResonanceId}");
            foreach (var p in WorldLayout.Pickups) if (Item(p.ItemId) == null) problems.Add($"Objeto en el mundo desconocido {p.ItemId}");
            foreach (var ch in WorldLayout.Chests)
                foreach (var it in ch.Contents) if (Item(it.ItemId) == null) problems.Add($"Cofre {ch.Id}: objeto desconocido {it.ItemId}");
            foreach (var m in _mysteries.Values)
            {
                if (!string.IsNullOrEmpty(m.TrueHypothesis) && !m.Hypotheses.Exists(h => h.Id == m.TrueHypothesis)) problems.Add($"Misterio {m.Id}: hipótesis verdadera inexistente");
                foreach (var h in m.Hypotheses)
                {
                    foreach (var e in h.SupportedBy) if (MysteryForEvidence(e) == null) problems.Add($"Hipótesis {h.Id}: evidencia inexistente {e}");
                    foreach (var e in h.RefutedBy) if (MysteryForEvidence(e) == null) problems.Add($"Hipótesis {h.Id}: evidencia inexistente {e}");
                }
            }
            for (int i = 0; i < WorldLayout.Edges.Count; i++)
            {
                var e = WorldLayout.Edges[i];
                if (WorldLayout.FindNode(e.Key) == null || WorldLayout.FindNode(e.Value) == null) problems.Add($"Arista con nodo inexistente {e.Key}-{e.Value}");
            }
            return problems;
        }
    }

    /// <summary>Constructor fluido de diálogos: las líneas consecutivas se enlazan solas.</summary>
    public sealed class DialogueBuilder
    {
        private readonly DialogueDef _def;
        private DialogueNode _last;
        private bool _lastTerminal;

        public DialogueBuilder(string id, string start = "start")
        {
            _def = new DialogueDef { Id = id, Start = start };
        }

        public DialogueBuilder Say(string id, string speaker, string text, Emotion emotion = Emotion.Neutral)
        {
            var node = new DialogueNode { Id = id, Speaker = speaker, Text = text, Emotion = emotion };
            if (_last != null && !_lastTerminal && string.IsNullOrEmpty(_last.Next) && _last.Choices.Count == 0) _last.Next = id;
            _def.Nodes[id] = node;
            _last = node;
            _lastTerminal = false;
            return this;
        }

        public DialogueBuilder Enter(Action<IGameContext> action)
        {
            var prev = _last.OnEnter;
            _last.OnEnter = prev == null ? action : (ctx => { prev(ctx); action(ctx); });
            return this;
        }

        public DialogueBuilder Next(string next) { _last.Next = next; _lastTerminal = true; return this; }
        public DialogueBuilder End() { _last.Next = "end"; _lastTerminal = true; return this; }

        public DialogueBuilder Choice(string text, string next, Func<GameState, bool> condition = null, Action<IGameContext> effect = null, string lockedReason = null)
        {
            _last.Choices.Add(new DialogueChoice
            {
                Text = text, Next = next, Condition = condition, Effect = effect,
                LockedReason = lockedReason, HideWhenLocked = string.IsNullOrEmpty(lockedReason),
            });
            _lastTerminal = true;
            return this;
        }

        public DialogueDef Build() => _def;
    }
}
