using System;
using System.Collections.Generic;

namespace Kolotopir.Core
{
    /// <summary>Banderas de historia y variables numéricas.</summary>
    public sealed class FlagStore
    {
        private readonly HashSet<string> _flags = new HashSet<string>();
        private readonly Dictionary<string, int> _vars = new Dictionary<string, int>();
        public event Action<string> FlagSet;

        public bool Has(string flag) => !string.IsNullOrEmpty(flag) && _flags.Contains(flag);

        public bool Set(string flag)
        {
            if (string.IsNullOrEmpty(flag) || !_flags.Add(flag)) return false;
            FlagSet?.Invoke(flag);
            return true;
        }

        public bool Clear(string flag) => _flags.Remove(flag);
        public int Get(string key) => _vars.TryGetValue(key, out var v) ? v : 0;
        public void SetVar(string key, int value) => _vars[key] = value;
        public int Increment(string key, int delta = 1) { int v = Get(key) + delta; _vars[key] = v; return v; }
        public IEnumerable<string> All => _flags;
        public IEnumerable<KeyValuePair<string, int>> Vars => _vars;
        public void ClearAll() { _flags.Clear(); _vars.Clear(); }
    }

    public sealed class Relationship
    {
        public int Affinity;
        public readonly List<string> Memories = new List<string>();
    }

    /// <summary>
    /// Relaciones con los habitantes. El sistema recuerda acciones significativas (ayuda, promesas rotas,
    /// compartir pruebas, invadir la privacidad) en vez de cada movimiento irrelevante.
    /// </summary>
    public sealed class RelationshipStore
    {
        private readonly Dictionary<string, Relationship> _rel = new Dictionary<string, Relationship>();
        public event Action<string, int, string> Changed;

        public Relationship Get(string npcId)
        {
            if (!_rel.TryGetValue(npcId, out var r)) { r = new Relationship(); _rel[npcId] = r; }
            return r;
        }

        public int Affinity(string npcId) => _rel.TryGetValue(npcId, out var r) ? r.Affinity : 0;
        public bool Remembers(string npcId, string memory) => _rel.TryGetValue(npcId, out var r) && r.Memories.Contains(memory);

        public void Change(string npcId, int delta, string memory = null)
        {
            var r = Get(npcId);
            r.Affinity = OromMath.Clamp(r.Affinity + delta, -100, 100);
            if (!string.IsNullOrEmpty(memory) && !r.Memories.Contains(memory)) r.Memories.Add(memory);
            Changed?.Invoke(npcId, delta, memory);
        }

        public IEnumerable<KeyValuePair<string, Relationship>> All => _rel;
        public void ClearAll() => _rel.Clear();
    }

    /// <summary>Reloj del mundo: el día y la noche modifican rutinas, sonidos, accesos y eventos.</summary>
    public sealed class GameClock
    {
        public int Day { get; private set; } = 1;
        public float Hour { get; private set; } = 7.5f;
        public double TotalHours => (Day - 1) * 24.0 + Hour;

        public event Action<int> NewDay;

        public void Set(int day, float hour)
        {
            Day = Math.Max(1, day);
            Hour = ((hour % 24f) + 24f) % 24f;
        }

        public void Advance(float hours)
        {
            if (hours <= 0f) return;
            Hour += hours;
            while (Hour >= 24f)
            {
                Hour -= 24f;
                Day++;
                NewDay?.Invoke(Day);
            }
        }

        /// <summary>Avanza hasta la próxima vez que el reloj marque la hora indicada.</summary>
        public float AdvanceTo(float targetHour)
        {
            float delta = targetHour - Hour;
            if (delta <= 0.01f) delta += 24f;
            Advance(delta);
            return delta;
        }

        public bool IsNight => Hour < 6f || Hour >= 20.5f;
        public string TimeString => $"{(int)Hour:00}:{(int)((Hour - (int)Hour) * 60f):00}";

        public string Phase
        {
            get
            {
                if (Hour < 5f) return "Noche cerrada";
                if (Hour < 7.5f) return "Amanecer";
                if (Hour < 12f) return "Mañana";
                if (Hour < 14.5f) return "Mediodía";
                if (Hour < 18.5f) return "Tarde";
                if (Hour < 20.5f) return "Atardecer";
                return "Noche";
            }
        }
    }

    /// <summary>Cambios persistentes del mundo: cofres, objetos recogidos, criaturas, Cápsulas y sectores visitados.</summary>
    public sealed class WorldPersistence
    {
        public readonly HashSet<string> Taken = new HashSet<string>();
        public readonly Dictionary<string, double> DefeatedAt = new Dictionary<string, double>();
        public readonly Dictionary<string, CapsuleState> Capsules = new Dictionary<string, CapsuleState>();
        public readonly HashSet<int> VisitedSectors = new HashSet<int>();

        public bool IsTaken(string id) => Taken.Contains(id);
        public void MarkTaken(string id) { if (!string.IsNullOrEmpty(id)) Taken.Add(id); }

        public void MarkDefeated(string id, double totalHours) => DefeatedAt[id] = totalHours;

        /// <summary>Las criaturas comunes reaparecen tras un día; las únicas, nunca.</summary>
        public bool IsDefeated(string id, double nowHours, bool unique, double respawnHours = 24.0)
        {
            if (!DefeatedAt.TryGetValue(id, out var t)) return false;
            return unique || nowHours - t < respawnHours;
        }

        public CapsuleState CapsuleOr(string id, CapsuleState fallback) => Capsules.TryGetValue(id, out var s) ? s : fallback;

        public void ClearAll()
        {
            Taken.Clear(); DefeatedAt.Clear(); Capsules.Clear(); VisitedSectors.Clear();
        }
    }
}
