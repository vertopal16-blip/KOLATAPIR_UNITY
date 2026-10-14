using System;
using System.Collections.Generic;

namespace Kolotopir.Core
{
    public enum AttributeType { Vigor = 0, Fuerza = 1, Agilidad = 2, Percepcion = 3, Resonancia = 4 }
    public enum DamageType { Fisico, Resonancia }
    public enum ItemCategory { Consumible, Material, Arma, Armadura, Accesorio, Herramienta, Narrativo, Cultural }
    public enum EquipSlot { Ninguna, Arma, Cabeza, Torso, Brazos, Piernas, Accesorio1, Accesorio2 }
    public enum QuestStatus { Inactiva, Activa, Completada }
    public enum QuestKind { Principal, Secundaria }

    /// <summary>Grado de certeza de Nilo (doc: distinguir lo que sabe, lo que ha observado y lo que solo sospecha).</summary>
    public enum Knowledge { Sospecha = 0, Observado = 1, Sabe = 2 }

    public enum NotebookCategory { Atlas, Bestiario, Culturas, Personajes, Resonancias, Misterios, Objetos }
    public enum ResonanceType { Ambiental, Personal, Material, Contradictoria, Profunda }
    public enum CapsuleState { Estable, Inactiva, Danada, Sellada, Anomala }
    public enum Emotion { Neutral, Alegre, Triste, Curioso, Sorprendido, Determinado, Preocupado, Enfadado }
    public enum Species { Humano, Kolotapir }
    public enum ConsequenceScale { Local, Comunitaria, Global }

    public enum Activity
    {
        Dormir, Casa, Trabajar, Recolectar, Cocinar, Comer, Patrullar, Conversar,
        Sentarse, Jugar, Pasear, Meditar, Contar, Vigilar, Mirar
    }

    public static class Names
    {
        public static string Attribute(AttributeType a)
        {
            switch (a)
            {
                case AttributeType.Vigor: return "Vigor";
                case AttributeType.Fuerza: return "Fuerza";
                case AttributeType.Agilidad: return "Agilidad";
                case AttributeType.Percepcion: return "Percepción";
                default: return "Resonancia";
            }
        }

        public static string AttributeHelp(AttributeType a)
        {
            switch (a)
            {
                case AttributeType.Vigor: return "Salud máxima (+10) y resistencia al daño físico.";
                case AttributeType.Fuerza: return "Daño cuerpo a cuerpo (+8 %) y menor coste del ataque fuerte.";
                case AttributeType.Agilidad: return "Aguante, movilidad, esquiva y velocidad de ataque.";
                case AttributeType.Percepcion: return "Detectar rastros, recursos, amenazas y detalles ocultos.";
                default: return "Alcance de la Escucha, Resonancias profundas y resistencia a los ecos.";
            }
        }

        public static string Slot(EquipSlot s)
        {
            switch (s)
            {
                case EquipSlot.Arma: return "Arma";
                case EquipSlot.Cabeza: return "Cabeza";
                case EquipSlot.Torso: return "Torso";
                case EquipSlot.Brazos: return "Brazos";
                case EquipSlot.Piernas: return "Piernas";
                case EquipSlot.Accesorio1: return "Accesorio I";
                case EquipSlot.Accesorio2: return "Accesorio II";
                default: return "—";
            }
        }

        public static string Category(ItemCategory c)
        {
            switch (c)
            {
                case ItemCategory.Consumible: return "Consumible";
                case ItemCategory.Material: return "Material";
                case ItemCategory.Arma: return "Arma";
                case ItemCategory.Armadura: return "Armadura";
                case ItemCategory.Accesorio: return "Accesorio";
                case ItemCategory.Herramienta: return "Herramienta";
                case ItemCategory.Narrativo: return "Objeto narrativo";
                default: return "Objeto cultural";
            }
        }

        public static string Knowledge(Knowledge k)
        {
            switch (k)
            {
                case Core.Knowledge.Sabe: return "Sabe";
                case Core.Knowledge.Observado: return "Observado";
                default: return "Sospecha";
            }
        }

        public static string Notebook(NotebookCategory c)
        {
            switch (c)
            {
                case NotebookCategory.Atlas: return "Atlas";
                case NotebookCategory.Bestiario: return "Bestiario";
                case NotebookCategory.Culturas: return "Culturas";
                case NotebookCategory.Personajes: return "Personajes";
                case NotebookCategory.Resonancias: return "Resonancias";
                case NotebookCategory.Misterios: return "Misterios";
                default: return "Objetos";
            }
        }

        public static string Resonance(ResonanceType t)
        {
            switch (t)
            {
                case ResonanceType.Ambiental: return "Ambiental";
                case ResonanceType.Personal: return "Personal";
                case ResonanceType.Material: return "Material";
                case ResonanceType.Contradictoria: return "Contradictoria";
                default: return "Profunda";
            }
        }

        public static string ResonanceRisk(ResonanceType t)
        {
            switch (t)
            {
                case ResonanceType.Ambiental: return "Puede mostrar solo una parte de lo ocurrido.";
                case ResonanceType.Personal: return "Refleja la perspectiva y el estado emocional de una persona.";
                case ResonanceType.Material: return "El objeto puede haber pasado por varias manos.";
                case ResonanceType.Contradictoria: return "La contradicción puede deberse a mentira, error, perspectiva o manipulación.";
                default: return "Difícil de interpretar; puede tener consecuencias emocionales o narrativas.";
            }
        }

        public static string Capsule(CapsuleState s)
        {
            switch (s)
            {
                case CapsuleState.Estable: return "Estable";
                case CapsuleState.Inactiva: return "Inactiva";
                case CapsuleState.Danada: return "Dañada";
                case CapsuleState.Sellada: return "Sellada";
                default: return "Anómala";
            }
        }

        public static string CapsuleReading(CapsuleState s)
        {
            switch (s)
            {
                case CapsuleState.Estable: return "Funciona con normalidad y puede ofrecer recuerdos o servicios permitidos.";
                case CapsuleState.Inactiva: return "No responde a la interacción habitual; requiere energía, contexto o una acción específica.";
                case CapsuleState.Danada: return "Emite señales incompletas, distorsionadas o contradictorias.";
                case CapsuleState.Sellada: return "El acceso está restringido por una razón cultural, técnica o personal.";
                default: return "La resonancia es irregular y puede conectar experiencias que no deberían mezclarse.";
            }
        }

        public static string Scale(ConsequenceScale s)
        {
            switch (s)
            {
                case ConsequenceScale.Local: return "Local";
                case ConsequenceScale.Comunitaria: return "Comunitaria";
                default: return "Global";
            }
        }
    }

    [Serializable]
    public struct ItemAmount
    {
        public string ItemId;
        public int Count;
        public ItemAmount(string id, int count) { ItemId = id; Count = count; }
    }

    /// <summary>Modificadores de estadísticas que aportan equipo, mejoras o efectos.</summary>
    public sealed class StatMods
    {
        public float Attack;
        public float Defense;
        public float AttackSpeed;
        public float ResistPhysical;
        public float ResistResonance;
        public float MoveSpeed;
        public float StaminaRegen;
        public float MaxHealth;
        public float StaminaCost;
        public readonly int[] Attributes = new int[5];

        public StatMods Attr(AttributeType a, int v) { Attributes[(int)a] += v; return this; }

        public void AddTo(StatMods other)
        {
            other.Attack += Attack; other.Defense += Defense; other.AttackSpeed += AttackSpeed;
            other.ResistPhysical += ResistPhysical; other.ResistResonance += ResistResonance;
            other.MoveSpeed += MoveSpeed; other.StaminaRegen += StaminaRegen; other.MaxHealth += MaxHealth;
            other.StaminaCost += StaminaCost;
            for (int i = 0; i < 5; i++) other.Attributes[i] += Attributes[i];
        }

        public string Describe()
        {
            var parts = new List<string>();
            if (Attack != 0) parts.Add($"Ataque {Signed(Attack)}");
            if (Defense != 0) parts.Add($"Defensa {Signed(Defense)}");
            if (AttackSpeed != 0) parts.Add($"Vel. de ataque {Pct(AttackSpeed)}");
            if (ResistPhysical != 0) parts.Add($"Res. física {Pct(ResistPhysical)}");
            if (ResistResonance != 0) parts.Add($"Res. a la Resonancia {Pct(ResistResonance)}");
            if (MoveSpeed != 0) parts.Add($"Movilidad {Pct(MoveSpeed)}");
            if (StaminaRegen != 0) parts.Add($"Recuperación de aguante {Pct(StaminaRegen)}");
            if (MaxHealth != 0) parts.Add($"Salud máx. {Signed(MaxHealth)}");
            if (StaminaCost != 0) parts.Add($"Coste de aguante {Pct(StaminaCost)}");
            for (int i = 0; i < 5; i++)
                if (Attributes[i] != 0) parts.Add($"{Names.Attribute((AttributeType)i)} {Signed(Attributes[i])}");
            return string.Join(" · ", parts);
        }

        private static string Signed(float v) => (v > 0 ? "+" : "") + v.ToString("0.#");
        private static string Pct(float v) => (v > 0 ? "+" : "") + (v * 100f).ToString("0") + " %";
    }

    public sealed class UpgradeDef
    {
        public string Id;
        public string Name;
        public string Description;
        public List<ItemAmount> Cost = new List<ItemAmount>();
        public StatMods Mods = new StatMods();
    }

    public sealed class ItemDef
    {
        public string Id;
        public string Name;
        public string Description;
        public string Origin = "Desconocida";
        public string Function = "";
        public ItemCategory Category;
        public EquipSlot Slot = EquipSlot.Ninguna;
        public int Value = 1;
        public int MaxStack = 1;
        public bool Unique;
        public StatMods Mods = new StatMods();
        public DamageType WeaponDamageType = DamageType.Fisico;

        // Consumibles
        public float HealAmount;
        public float HealDuration;
        public bool IsFood;
        public float RegenPerSecond;
        public float RegenDuration;
        public float StaminaRegenBuff;

        /// <summary>Color de referencia (hex) para iconos y vista previa.</summary>
        public string Color = "#C9B79C";
        public string Glyph = "•";
        public List<UpgradeDef> Upgrades = new List<UpgradeDef>();

        public bool IsEquippable => Slot != EquipSlot.Ninguna;
        public bool IsConsumable => Category == ItemCategory.Consumible;
        public bool Stackable => MaxStack > 1;
    }

    public sealed class RecipeDef
    {
        public string Id;
        public string Name;
        /// <summary>"mesa" (mesa de trabajo) o "campo" (en cualquier lugar).</summary>
        public string Station = "mesa";
        public List<ItemAmount> Inputs = new List<ItemAmount>();
        public ItemAmount Output;
        /// <summary>Bandera de conocimiento necesaria (progresión basada en conocimiento). Vacía = conocida.</summary>
        public string RequiredFlag = "";
        public string Description = "";
    }

    public sealed class SkillDef
    {
        public string Id;
        public string Name;
        public string Tree;
        public int Tier = 1;
        public int Cost = 1;
        public string Description;
        public bool Active;
        public float Cooldown;
    }

    public sealed class TradeOffer
    {
        public string ItemId;
        public int Count = 1;
        public int Stock = 1;
        /// <summary>Si el comerciante solo acepta ciertos objetos a cambio (p. ej. Boro valora lo "inútil").</summary>
        public float PriceMultiplier = 1f;
    }

    public sealed class MerchantDef
    {
        public string NpcId;
        public string Name;
        public string Greeting;
        public List<TradeOffer> Offers = new List<TradeOffer>();
        /// <summary>Multiplicador del valor de lo que Nilo ofrece, por categoría.</summary>
        public Dictionary<ItemCategory, float> Appreciation = new Dictionary<ItemCategory, float>();
    }

    public sealed class DecisionRecord
    {
        public string Id;
        public string Option;
        public string Summary;
        public ConsequenceScale Scale;
    }
}
