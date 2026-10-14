using System;
using System.Collections.Generic;

namespace Kolotopir.Core
{
    /// <summary>
    /// Experiencia, niveles (máximo inicial 30) y atributos: Vigor, Fuerza, Agilidad, Percepción y Resonancia.
    /// La experiencia llega por misiones, exploración, descubrimientos, combate y Cápsulas.
    /// </summary>
    public sealed class Progression
    {
        public const int MaxLevel = 30;
        public const int StartingAttribute = 1;
        public const int MaxAttribute = 20;

        public int Level { get; private set; } = 1;
        public int Xp { get; private set; }
        public int TotalXp { get; private set; }
        public int AttributePoints { get; private set; }
        public int SkillPoints { get; private set; }
        private readonly int[] _attributes = { StartingAttribute, StartingAttribute, StartingAttribute, StartingAttribute, StartingAttribute };

        public event Action<int> LeveledUp;

        public static int XpToNext(int level)
        {
            if (level >= MaxLevel) return 0;
            return 60 + 40 * level + 10 * level * level;
        }

        public int XpForNextLevel => XpToNext(Level);
        public float LevelProgress => Level >= MaxLevel ? 1f : Xp / (float)XpForNextLevel;

        public int Get(AttributeType a) => _attributes[(int)a];

        /// <summary>Suma experiencia y devuelve cuántos niveles se han ganado.</summary>
        public int AddXp(int amount)
        {
            if (amount <= 0 || Level >= MaxLevel) return 0;
            TotalXp += amount;
            Xp += amount;
            int gained = 0;
            while (Level < MaxLevel && Xp >= XpToNext(Level))
            {
                Xp -= XpToNext(Level);
                Level++;
                AttributePoints++;
                SkillPoints++;
                gained++;
                LeveledUp?.Invoke(Level);
            }
            if (Level >= MaxLevel) Xp = 0;
            return gained;
        }

        public bool SpendAttributePoint(AttributeType a)
        {
            if (AttributePoints <= 0 || _attributes[(int)a] >= MaxAttribute) return false;
            AttributePoints--;
            _attributes[(int)a]++;
            return true;
        }

        public bool SpendSkillPoints(int cost)
        {
            if (cost <= 0 || SkillPoints < cost) return false;
            SkillPoints -= cost;
            return true;
        }

        public void Restore(int level, int xp, int totalXp, int attributePoints, int skillPoints, int[] attributes)
        {
            Level = OromMath.Clamp(level, 1, MaxLevel);
            Xp = Math.Max(0, xp);
            TotalXp = Math.Max(0, totalXp);
            AttributePoints = Math.Max(0, attributePoints);
            SkillPoints = Math.Max(0, skillPoints);
            if (attributes != null)
                for (int i = 0; i < 5 && i < attributes.Length; i++)
                    _attributes[i] = OromMath.Clamp(attributes[i], StartingAttribute, MaxAttribute);
        }

        public int[] AttributesCopy() => (int[])_attributes.Clone();
    }

    /// <summary>Habilidades desbloqueadas en los árboles Superviviente, Explorador y Resonancia.</summary>
    public sealed class SkillSet
    {
        private readonly HashSet<string> _unlocked = new HashSet<string>();
        private readonly Dictionary<string, float> _cooldownUntil = new Dictionary<string, float>();

        public IEnumerable<string> Unlocked => _unlocked;
        public bool Has(string id) => _unlocked.Contains(id);

        public int CountInTree(string tree, IReadOnlyDictionary<string, SkillDef> db)
        {
            int c = 0;
            foreach (var id in _unlocked)
                if (db.TryGetValue(id, out var def) && def.Tree == tree) c++;
            return c;
        }

        public static int RequiredInTree(int tier) => tier <= 1 ? 0 : (tier == 2 ? 1 : 2);

        public bool CanUnlock(SkillDef def, Progression progression, IReadOnlyDictionary<string, SkillDef> db, out string reason)
        {
            reason = null;
            if (def == null) { reason = "Habilidad desconocida."; return false; }
            if (Has(def.Id)) { reason = "Ya la conoces."; return false; }
            int required = RequiredInTree(def.Tier);
            if (CountInTree(def.Tree, db) < required)
            {
                reason = $"Necesitas {required} habilidad(es) de {def.Tree}.";
                return false;
            }
            if (progression.SkillPoints < def.Cost) { reason = $"Necesitas {def.Cost} punto(s) de habilidad."; return false; }
            return true;
        }

        public bool Unlock(SkillDef def, Progression progression, IReadOnlyDictionary<string, SkillDef> db)
        {
            if (!CanUnlock(def, progression, db, out _)) return false;
            if (!progression.SpendSkillPoints(def.Cost)) return false;
            _unlocked.Add(def.Id);
            return true;
        }

        public void ForceUnlock(string id) => _unlocked.Add(id);
        public void Clear() { _unlocked.Clear(); _cooldownUntil.Clear(); }

        public bool IsReady(string id, float now) => !_cooldownUntil.TryGetValue(id, out var t) || now >= t;
        public float CooldownRemaining(string id, float now) => _cooldownUntil.TryGetValue(id, out var t) ? Math.Max(0f, t - now) : 0f;
        public void StartCooldown(string id, float now, float seconds) => _cooldownUntil[id] = now + seconds;
    }
}
