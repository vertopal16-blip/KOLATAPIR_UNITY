using System;
using System.Collections.Generic;

namespace Kolotopir.Core
{
    /// <summary>Puente entre el contenido narrativo (datos) y el juego en ejecución.</summary>
    public interface IGameContext
    {
        GameState State { get; }
        void SetFlag(string flag);
        void ClearFlag(string flag);
        void GiveItem(string itemId, int count = 1);
        bool TakeItem(string itemId, int count = 1);
        void AddXp(int amount, string reason);
        void StartQuest(string questId);
        void AddNotebook(string entryId, Knowledge knowledge = Knowledge.Observado);
        void AddEvidence(string evidenceId);
        void ChangeAffinity(string npcId, int delta, string memory = null);
        void SetCapsuleState(string capsuleId, CapsuleState state);
        void RecordDecision(string id, string option, string summary, ConsequenceScale scale);
        void Notify(string text);
        void Command(string command);
    }

    /// <summary>Estado completo de una partida. No depende de Unity: se puede probar y serializar.</summary>
    public sealed class GameState
    {
        public readonly Inventory Inventory;
        public readonly Equipment Equipment = new Equipment();
        public readonly Progression Progression = new Progression();
        public readonly SkillSet Skills = new SkillSet();
        public readonly QuestLog Quests = new QuestLog();
        public readonly Notebook Notebook = new Notebook();
        public readonly FlagStore Flags = new FlagStore();
        public readonly RelationshipStore Relationships = new RelationshipStore();
        public readonly GameClock Clock = new GameClock();
        public readonly WorldPersistence World = new WorldPersistence();
        public readonly Vital Health = new Vital(StatCalculator.BaseHealth);
        public readonly Vital Stamina = new Vital(StatCalculator.BaseStamina);
        public readonly List<DecisionRecord> Decisions = new List<DecisionRecord>();
        public readonly string[] QuickSlots = { "venda", "unguento", "pan_raiz", "bayas" };
        public readonly StatMods Temporary = new StatMods();

        public string LastSafePoint = "r_casa_gina";
        public float PlaytimeSeconds;
        public int Chapter = 1;

        public StatBlock Stats { get; private set; }

        public event Action<int, string> XpGained;
        public event Action<int> LeveledUp;
        public event Action StatsChanged;

        public GameState()
        {
            Inventory = new Inventory(GameDb.Item);
            Progression.LeveledUp += lvl => LeveledUp?.Invoke(lvl);
            Equipment.Changed += RecomputeStats;
            RecomputeStats();
        }

        public ItemDef Items(string id) => GameDb.Item(id);

        public void RecomputeStats()
        {
            Stats = StatCalculator.Compute(Progression, Equipment, Skills, GameDb.Item, Temporary);
            Health.SetMax(Stats.MaxHealth);
            Stamina.SetMax(Stats.MaxStamina);
            StatsChanged?.Invoke();
        }

        public int AddXp(int amount, string reason)
        {
            if (amount <= 0) return 0;
            int levels = Progression.AddXp(amount);
            XpGained?.Invoke(amount, reason);
            if (levels > 0)
            {
                RecomputeStats();
                Health.Fill();
            }
            return levels;
        }

        // ---------------------------------------------------------------- Consultas cómodas para el contenido

        public bool Has(string flag) => Flags.Has(flag);
        public bool HasItem(string id, int count = 1) => Inventory.Count(id) + (Equipment.IsEquipped(id) ? 1 : 0) >= count;
        public bool Heard(string resonanceId) => Flags.Has("heard_" + resonanceId);
        public bool QuestActive(string id) => Quests.IsActive(id);
        public bool QuestDone(string id) => Quests.IsCompleted(id);
        public bool AtStep(string questId, string stepId)
        {
            var step = Quests.CurrentStep(questId);
            return step != null && step.Id == stepId;
        }
        public bool PastStep(string questId, int index) => Quests.IsCompleted(questId) || Quests.StepIndex(questId) > index;
        public int Affinity(string npc) => Relationships.Affinity(npc);
        public bool Remembers(string npc, string memory) => Relationships.Remembers(npc, memory);
        public bool HasEvidence(string id) => Notebook.HasEvidence(id);
        public string Hypothesis(string mystery) => Notebook.Chosen(mystery);
        public bool Knows(string entry) => Notebook.HasEntry(entry);
        public int Attr(AttributeType a) => Stats.Attr(a);
        public bool HasSkill(string id) => Skills.Has(id);

        public CapsuleState CapsuleOf(string id)
        {
            var def = GameDb.Capsule(id);
            return World.CapsuleOr(id, def != null ? def.InitialState : CapsuleState.Inactiva);
        }

        public string DecisionOption(string id)
        {
            foreach (var d in Decisions) if (d.Id == id) return d.Option;
            return null;
        }

        // ---------------------------------------------------------------- Partida nueva

        public void NewGame()
        {
            Inventory.Clear();
            Equipment.Clear();
            Skills.Clear();
            Quests.Clear();
            Notebook.Clear();
            Flags.ClearAll();
            Relationships.ClearAll();
            World.ClearAll();
            Decisions.Clear();
            Progression.Restore(1, 0, 0, 0, 0, new[] { 1, 1, 1, 1, 1 });
            Clock.Set(1, 7.4f);
            LastSafePoint = "r_casa_gina";
            PlaytimeSeconds = 0f;
            Chapter = 1;

            // Nilo empieza con su cuaderno, el amuleto familiar, la navaja y el catalejo (lámina de Nilo).
            Inventory.Add("cuaderno", 1);
            Inventory.Add("navaja", 1);
            Inventory.Add("catalejo", 1);
            Inventory.Add("amuleto_familiar", 1);
            Inventory.Add("venda", 2);
            Inventory.Add("pan_raiz", 1);
            var amulet = Inventory.FirstStack("amuleto_familiar");
            Equipment.Equip(amulet, Inventory, GameDb.Item, EquipSlot.Accesorio1);

            Notebook.AddEntry("pj_nilo", Knowledge.Sabe);
            Notebook.AddEntry("pj_gina", Knowledge.Sabe);
            Notebook.AddEntry("cul_reverso", Knowledge.Observado);
            Notebook.AddEntry("loc_reverso", Knowledge.Sabe);
            Notebook.AddEntry("obj_tunica", Knowledge.Sabe);
            Notebook.AddEntry("pj_eira", Knowledge.Sospecha);
            Notebook.AddEntry("pj_daren", Knowledge.Sospecha);
            foreach (var id in new List<string>(Notebook.AllEntries)) Notebook.MarkRead(id);

            Quests.Start("mq_silencio");
            RecomputeStats();
            Health.Fill();
            Stamina.Fill();
        }
    }

    /// <summary>
    /// Implementación base de <see cref="IGameContext"/>: aplica cambios al estado y avisa.
    /// El juego en Unity la extiende para mostrar notificaciones y ejecutar comandos.
    /// </summary>
    public class CoreGameContext : IGameContext
    {
        public GameState State { get; }
        public readonly List<string> Log = new List<string>();

        public CoreGameContext(GameState state) { State = state; }

        public virtual void SetFlag(string flag) => State.Flags.Set(flag);
        public virtual void ClearFlag(string flag) => State.Flags.Clear(flag);

        public virtual void GiveItem(string itemId, int count = 1)
        {
            int added = State.Inventory.Add(itemId, count);
            var def = State.Items(itemId);
            if (added > 0 && def != null)
            {
                Notify(added > 1 ? $"Obtenido: {def.Name} ×{added}" : $"Obtenido: {def.Name}");
                if (def.Category == ItemCategory.Narrativo || def.Category == ItemCategory.Cultural || def.Category == ItemCategory.Herramienta)
                    AddNotebook("obj_" + itemId, Knowledge.Observado);
            }
        }

        public virtual bool TakeItem(string itemId, int count = 1) => State.Inventory.Remove(itemId, count);

        public virtual void AddXp(int amount, string reason) => State.AddXp(amount, reason);

        public virtual void StartQuest(string questId)
        {
            if (State.Quests.Start(questId))
            {
                var q = GameDb.Quest(questId);
                if (q != null) Notify((q.Kind == QuestKind.Principal ? "Nueva misión principal: " : "Nueva misión secundaria: ") + q.Title);
            }
        }

        public virtual void AddNotebook(string entryId, Knowledge knowledge = Knowledge.Observado)
        {
            var def = GameDb.Entry(entryId);
            if (def == null) return;
            bool isNew = !State.Notebook.HasEntry(entryId);
            if (!State.Notebook.AddEntry(entryId, knowledge)) return;
            if (isNew)
            {
                Notify($"Cuaderno · {Names.Notebook(def.Category)}: {def.Title}");
                if (def.Xp > 0) AddXp(def.Xp, "Descubrimiento");
            }
            else Notify($"Cuaderno actualizado: {def.Title} ({Names.Knowledge(knowledge)})");
        }

        public virtual void AddEvidence(string evidenceId)
        {
            var mystery = State.Notebook.AddEvidence(evidenceId, out bool isNew, out bool resolved, out bool matched);
            if (mystery == null || !isNew) return;
            Notify("Nueva evidencia: " + mystery.Question);
            AddXp(10, "Evidencia");
            if (resolved)
            {
                Notify(matched ? "Tu hipótesis se confirma." : "Las pruebas te obligan a revisar tu hipótesis.");
                AddXp(matched ? mystery.Xp + 20 : mystery.Xp, "Misterio resuelto");
            }
        }

        public virtual void ChangeAffinity(string npcId, int delta, string memory = null) => State.Relationships.Change(npcId, delta, memory);

        public virtual void SetCapsuleState(string capsuleId, CapsuleState state) => State.World.Capsules[capsuleId] = state;

        public virtual void RecordDecision(string id, string option, string summary, ConsequenceScale scale)
        {
            State.Decisions.RemoveAll(d => d.Id == id);
            State.Decisions.Add(new DecisionRecord { Id = id, Option = option, Summary = summary, Scale = scale });
        }

        public virtual void Notify(string text) => Log.Add(text);
        public virtual void Command(string command) => Log.Add("cmd:" + command);
    }
}
