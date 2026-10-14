using System;
using System.Collections.Generic;

namespace Kolotopir.Core
{
    [Serializable] public sealed class KeyInt { public string Key; public int Value; public KeyInt() { } public KeyInt(string k, int v) { Key = k; Value = v; } }
    [Serializable] public sealed class KeyString { public string Key; public string Value; public KeyString() { } public KeyString(string k, string v) { Key = k; Value = v; } }
    [Serializable] public sealed class KeyDouble { public string Key; public double Value; public KeyDouble() { } public KeyDouble(string k, double v) { Key = k; Value = v; } }

    [Serializable] public sealed class SlotSave { public int Slot; public string ItemId; public string UpgradeId; }
    [Serializable] public sealed class QuestSave { public string Id; public int Status; public int Step; public int Order; }
    [Serializable] public sealed class RelationSave { public string Npc; public int Affinity; public List<string> Memories = new List<string>(); }
    [Serializable] public sealed class DecisionSave { public string Id; public string Option; public string Summary; public int Scale; }

    /// <summary>
    /// Formato de guardado (JSON). Persiste misiones, inventario, equipo, progresión, cuaderno, relaciones
    /// y los cambios relevantes del mundo (doc: guardado automático en hitos y manual en lugares seguros).
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public string SavedAt = "";
        public string Label = "";
        public string RegionName = "";
        public float Playtime;
        public int Chapter = 1;

        public float PosX, PosY, PosZ, RotY;
        public float Health = 100f, Stamina = 100f;
        public string LastSafePoint = "r_casa_gina";

        public int Level = 1, Xp, TotalXp, AttributePoints, SkillPoints;
        public int[] Attributes = { 1, 1, 1, 1, 1 };
        public List<string> Skills = new List<string>();

        public List<ItemStack> Inventory = new List<ItemStack>();
        public List<SlotSave> Equipment = new List<SlotSave>();
        public List<string> QuickSlots = new List<string>();

        public List<QuestSave> Quests = new List<QuestSave>();
        public List<string> Flags = new List<string>();
        public List<KeyInt> Vars = new List<KeyInt>();

        public List<KeyInt> Notebook = new List<KeyInt>();
        public List<string> Evidence = new List<string>();
        public List<KeyString> Hypotheses = new List<KeyString>();
        public List<string> Resolved = new List<string>();

        public List<RelationSave> Relationships = new List<RelationSave>();
        public List<DecisionSave> Decisions = new List<DecisionSave>();

        public List<string> Taken = new List<string>();
        public List<KeyDouble> Defeated = new List<KeyDouble>();
        public List<KeyInt> Capsules = new List<KeyInt>();
        public List<int> VisitedSectors = new List<int>();

        public int Day = 1;
        public float Hour = 7.5f;
    }

    public static class SaveConverter
    {
        public static SaveData Capture(GameState s, float x, float y, float z, float rotY, string regionName, string label)
        {
            var d = new SaveData
            {
                SavedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                Label = label ?? "",
                RegionName = regionName ?? "",
                Playtime = s.PlaytimeSeconds,
                Chapter = s.Chapter,
                PosX = x, PosY = y, PosZ = z, RotY = rotY,
                Health = s.Health.Current,
                Stamina = s.Stamina.Current,
                LastSafePoint = s.LastSafePoint,
                Level = s.Progression.Level,
                Xp = s.Progression.Xp,
                TotalXp = s.Progression.TotalXp,
                AttributePoints = s.Progression.AttributePoints,
                SkillPoints = s.Progression.SkillPoints,
                Attributes = s.Progression.AttributesCopy(),
                Day = s.Clock.Day,
                Hour = s.Clock.Hour,
            };
            d.Skills.AddRange(s.Skills.Unlocked);
            foreach (var st in s.Inventory.Stacks) d.Inventory.Add(new ItemStack(st.ItemId, st.Count, st.UpgradeId));
            foreach (var kv in s.Equipment.All)
                if (kv.Value != null) d.Equipment.Add(new SlotSave { Slot = (int)kv.Key, ItemId = kv.Value.ItemId, UpgradeId = kv.Value.UpgradeId });
            d.QuickSlots.AddRange(s.QuickSlots);
            foreach (var q in s.Quests.All) d.Quests.Add(new QuestSave { Id = q.Id, Status = (int)q.Status, Step = q.Step, Order = q.Order });
            d.Flags.AddRange(s.Flags.All);
            foreach (var kv in s.Flags.Vars) d.Vars.Add(new KeyInt(kv.Key, kv.Value));
            foreach (var kv in s.Notebook.EntryStates) d.Notebook.Add(new KeyInt(kv.Key, (int)kv.Value));
            d.Evidence.AddRange(s.Notebook.Evidence);
            foreach (var kv in s.Notebook.Choices) d.Hypotheses.Add(new KeyString(kv.Key, kv.Value));
            d.Resolved.AddRange(s.Notebook.Resolved);
            foreach (var kv in s.Relationships.All)
            {
                var r = new RelationSave { Npc = kv.Key, Affinity = kv.Value.Affinity };
                r.Memories.AddRange(kv.Value.Memories);
                d.Relationships.Add(r);
            }
            foreach (var dec in s.Decisions) d.Decisions.Add(new DecisionSave { Id = dec.Id, Option = dec.Option, Summary = dec.Summary, Scale = (int)dec.Scale });
            d.Taken.AddRange(s.World.Taken);
            foreach (var kv in s.World.DefeatedAt) d.Defeated.Add(new KeyDouble(kv.Key, kv.Value));
            foreach (var kv in s.World.Capsules) d.Capsules.Add(new KeyInt(kv.Key, (int)kv.Value));
            d.VisitedSectors.AddRange(s.World.VisitedSectors);
            return d;
        }

        public static void Apply(SaveData d, GameState s)
        {
            if (d == null) throw new ArgumentNullException(nameof(d));
            s.Inventory.Clear();
            s.Equipment.Clear();
            s.Skills.Clear();
            s.Quests.Clear();
            s.Flags.ClearAll();
            s.Relationships.ClearAll();
            s.World.ClearAll();
            s.Decisions.Clear();

            s.Progression.Restore(d.Level, d.Xp, d.TotalXp, d.AttributePoints, d.SkillPoints, d.Attributes);
            if (d.Skills != null) foreach (var sk in d.Skills) s.Skills.ForceUnlock(sk);
            if (d.Inventory != null)
                foreach (var st in d.Inventory)
                    if (st != null && GameDb.Item(st.ItemId) != null) s.Inventory.Insert(new ItemStack(st.ItemId, st.Count, st.UpgradeId));
            if (d.Equipment != null)
                foreach (var e in d.Equipment)
                    if (e != null && GameDb.Item(e.ItemId) != null) s.Equipment.SetDirect((EquipSlot)e.Slot, new ItemStack(e.ItemId, 1, e.UpgradeId));
            if (d.QuickSlots != null)
                for (int i = 0; i < s.QuickSlots.Length && i < d.QuickSlots.Count; i++) s.QuickSlots[i] = d.QuickSlots[i];
            if (d.Quests != null)
                foreach (var q in d.Quests)
                    if (GameDb.Quest(q.Id) != null) s.Quests.Restore(q.Id, (QuestStatus)q.Status, q.Step, q.Order);
            if (d.Flags != null) foreach (var f in d.Flags) s.Flags.Set(f);
            if (d.Vars != null) foreach (var v in d.Vars) s.Flags.SetVar(v.Key, v.Value);

            var entries = new List<KeyValuePair<string, Knowledge>>();
            if (d.Notebook != null) foreach (var kv in d.Notebook) entries.Add(new KeyValuePair<string, Knowledge>(kv.Key, (Knowledge)kv.Value));
            var chosen = new List<KeyValuePair<string, string>>();
            if (d.Hypotheses != null) foreach (var kv in d.Hypotheses) chosen.Add(new KeyValuePair<string, string>(kv.Key, kv.Value));
            s.Notebook.Restore(entries, d.Evidence ?? new List<string>(), chosen, d.Resolved ?? new List<string>());

            if (d.Relationships != null)
                foreach (var r in d.Relationships)
                {
                    var rel = s.Relationships.Get(r.Npc);
                    rel.Affinity = r.Affinity;
                    if (r.Memories != null) rel.Memories.AddRange(r.Memories);
                }
            if (d.Decisions != null)
                foreach (var dec in d.Decisions)
                    s.Decisions.Add(new DecisionRecord { Id = dec.Id, Option = dec.Option, Summary = dec.Summary, Scale = (ConsequenceScale)dec.Scale });
            if (d.Taken != null) foreach (var t in d.Taken) s.World.Taken.Add(t);
            if (d.Defeated != null) foreach (var kv in d.Defeated) s.World.DefeatedAt[kv.Key] = kv.Value;
            if (d.Capsules != null) foreach (var kv in d.Capsules) s.World.Capsules[kv.Key] = (CapsuleState)kv.Value;
            if (d.VisitedSectors != null) foreach (var v in d.VisitedSectors) s.World.VisitedSectors.Add(v);

            s.Clock.Set(d.Day, d.Hour);
            s.LastSafePoint = string.IsNullOrEmpty(d.LastSafePoint) ? "r_casa_gina" : d.LastSafePoint;
            s.PlaytimeSeconds = d.Playtime;
            s.Chapter = d.Chapter;
            s.RecomputeStats();
            s.Health.SetCurrent(d.Health <= 0f ? s.Health.Max : d.Health);
            s.Stamina.SetCurrent(d.Stamina);
        }
    }
}
