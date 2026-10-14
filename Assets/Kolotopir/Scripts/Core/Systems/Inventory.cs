using System;
using System.Collections.Generic;

namespace Kolotopir.Core
{
    [Serializable]
    public sealed class ItemStack
    {
        public string ItemId;
        public int Count;
        /// <summary>Mejora elegida (las mejoras son excluyentes: cada una ofrece una elección reconocible).</summary>
        public string UpgradeId = "";

        public ItemStack() { }
        public ItemStack(string id, int count, string upgrade = "") { ItemId = id; Count = count; UpgradeId = upgrade ?? ""; }
        public bool HasUpgrade => !string.IsNullOrEmpty(UpgradeId);
    }

    /// <summary>Inventario pequeño y comprensible (doc 13): pilas de objetos sin combinaciones interminables.</summary>
    public sealed class Inventory
    {
        private readonly List<ItemStack> _stacks = new List<ItemStack>();
        private readonly Func<string, ItemDef> _lookup;

        public event Action<string, int> ItemAdded;
        public event Action<string, int> ItemRemoved;

        public Inventory(Func<string, ItemDef> lookup) { _lookup = lookup; }

        public IReadOnlyList<ItemStack> Stacks => _stacks;

        public int Count(string itemId)
        {
            int c = 0;
            for (int i = 0; i < _stacks.Count; i++) if (_stacks[i].ItemId == itemId) c += _stacks[i].Count;
            return c;
        }

        public bool Has(string itemId, int count = 1) => Count(itemId) >= count;

        public bool HasAll(IEnumerable<ItemAmount> amounts)
        {
            foreach (var a in amounts) if (!Has(a.ItemId, a.Count)) return false;
            return true;
        }

        public int Add(string itemId, int count = 1, string upgrade = "")
        {
            if (count <= 0) return 0;
            var def = _lookup(itemId);
            if (def == null) return 0;
            int added = 0;
            if (def.Stackable)
            {
                int remaining = count;
                for (int i = 0; i < _stacks.Count && remaining > 0; i++)
                {
                    var s = _stacks[i];
                    if (s.ItemId != itemId || s.Count >= def.MaxStack) continue;
                    int put = Math.Min(remaining, def.MaxStack - s.Count);
                    s.Count += put; remaining -= put; added += put;
                }
                while (remaining > 0)
                {
                    int put = Math.Min(remaining, def.MaxStack);
                    _stacks.Add(new ItemStack(itemId, put));
                    remaining -= put; added += put;
                }
            }
            else
            {
                if (def.Unique && Has(itemId)) return 0;
                for (int i = 0; i < count; i++) { _stacks.Add(new ItemStack(itemId, 1, upgrade)); added++; if (def.Unique) break; }
            }
            if (added > 0) ItemAdded?.Invoke(itemId, added);
            return added;
        }

        public bool Remove(string itemId, int count = 1)
        {
            if (count <= 0) return true;
            if (!Has(itemId, count)) return false;
            int remaining = count;
            for (int i = _stacks.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var s = _stacks[i];
                if (s.ItemId != itemId) continue;
                int take = Math.Min(remaining, s.Count);
                s.Count -= take; remaining -= take;
                if (s.Count <= 0) _stacks.RemoveAt(i);
            }
            ItemRemoved?.Invoke(itemId, count);
            return true;
        }

        public bool RemoveAll(IEnumerable<ItemAmount> amounts)
        {
            var list = new List<ItemAmount>(amounts);
            foreach (var a in list) if (!Has(a.ItemId, a.Count)) return false;
            foreach (var a in list) Remove(a.ItemId, a.Count);
            return true;
        }

        /// <summary>Extrae una pila concreta (para equiparla, conservando su mejora).</summary>
        public bool Take(ItemStack stack)
        {
            int i = _stacks.IndexOf(stack);
            if (i < 0) return false;
            if (stack.Count > 1)
            {
                stack.Count--;
                ItemRemoved?.Invoke(stack.ItemId, 1);
                return true;
            }
            _stacks.RemoveAt(i);
            ItemRemoved?.Invoke(stack.ItemId, 1);
            return true;
        }

        public void Insert(ItemStack stack)
        {
            if (stack == null) return;
            var def = _lookup(stack.ItemId);
            if (def != null && def.Stackable) { Add(stack.ItemId, stack.Count); return; }
            _stacks.Add(stack);
            ItemAdded?.Invoke(stack.ItemId, stack.Count);
        }

        public ItemStack FirstStack(string itemId)
        {
            for (int i = 0; i < _stacks.Count; i++) if (_stacks[i].ItemId == itemId) return _stacks[i];
            return null;
        }

        public void Clear() => _stacks.Clear();
    }

    /// <summary>Ranuras: arma, cabeza, torso, brazos, piernas y dos accesorios. La túnica morada no ocupa ranura: siempre se ve.</summary>
    public sealed class Equipment
    {
        public static readonly EquipSlot[] AllSlots =
        {
            EquipSlot.Arma, EquipSlot.Cabeza, EquipSlot.Torso, EquipSlot.Brazos, EquipSlot.Piernas, EquipSlot.Accesorio1, EquipSlot.Accesorio2
        };

        private readonly Dictionary<EquipSlot, ItemStack> _slots = new Dictionary<EquipSlot, ItemStack>();
        public event Action Changed;

        public ItemStack Get(EquipSlot slot) => _slots.TryGetValue(slot, out var s) ? s : null;
        public bool IsEquipped(string itemId)
        {
            foreach (var kv in _slots) if (kv.Value != null && kv.Value.ItemId == itemId) return true;
            return false;
        }

        public static bool SlotAccepts(EquipSlot slot, ItemDef def)
        {
            if (def == null || !def.IsEquippable) return false;
            if (def.Slot == EquipSlot.Accesorio1 || def.Slot == EquipSlot.Accesorio2)
                return slot == EquipSlot.Accesorio1 || slot == EquipSlot.Accesorio2;
            return def.Slot == slot;
        }

        /// <summary>Elige la ranura destino: para accesorios, la primera libre.</summary>
        public EquipSlot ChooseSlot(ItemDef def)
        {
            if (def.Slot == EquipSlot.Accesorio1 || def.Slot == EquipSlot.Accesorio2)
            {
                if (Get(EquipSlot.Accesorio1) == null) return EquipSlot.Accesorio1;
                if (Get(EquipSlot.Accesorio2) == null) return EquipSlot.Accesorio2;
                return EquipSlot.Accesorio1;
            }
            return def.Slot;
        }

        /// <summary>Equipa desde el inventario; lo que hubiera en la ranura vuelve al inventario.</summary>
        public bool Equip(ItemStack stack, Inventory inventory, Func<string, ItemDef> lookup, EquipSlot preferred = EquipSlot.Ninguna)
        {
            if (stack == null) return false;
            var def = lookup(stack.ItemId);
            if (def == null || !def.IsEquippable) return false;
            EquipSlot slot = preferred != EquipSlot.Ninguna && SlotAccepts(preferred, def) ? preferred : ChooseSlot(def);
            bool split = stack.Count > 1;
            if (!inventory.Take(stack)) return false;
            var equippedStack = split ? new ItemStack(stack.ItemId, 1, stack.UpgradeId) : stack;
            var previous = Get(slot);
            _slots[slot] = equippedStack;
            if (previous != null) inventory.Insert(previous);
            Changed?.Invoke();
            return true;
        }

        public bool Unequip(EquipSlot slot, Inventory inventory)
        {
            var current = Get(slot);
            if (current == null) return false;
            _slots.Remove(slot);
            inventory.Insert(current);
            Changed?.Invoke();
            return true;
        }

        public void SetDirect(EquipSlot slot, ItemStack stack)
        {
            if (stack == null) _slots.Remove(slot); else _slots[slot] = stack;
            Changed?.Invoke();
        }

        public IEnumerable<KeyValuePair<EquipSlot, ItemStack>> All => _slots;
        public void Clear() { _slots.Clear(); Changed?.Invoke(); }
    }

    /// <summary>Estadísticas derivadas de nivel, atributos, equipo, habilidades y efectos temporales.</summary>
    public sealed class StatBlock
    {
        public int Level;
        public readonly int[] Attributes = new int[5];
        public float MaxHealth;
        public float MaxStamina;
        public float StaminaRegen;
        public float Attack;
        public DamageType AttackType = DamageType.Fisico;
        public float AttackSpeed;
        public float Defense;
        public float ResistPhysical;
        public float ResistResonance;
        public float MoveSpeedMult;
        public float StaminaCostMult;
        public float SprintCostMult;
        public float DodgeCostMult;
        public float DetectionMult;
        public float ResonanceRangeMult;
        public float HealSpeedMult;
        public float HeavyCostMult;
        public bool HasWeapon;

        public int Attr(AttributeType a) => Attributes[(int)a];
    }

    public static class StatCalculator
    {
        public const float BaseHealth = 100f;
        public const float BaseStamina = 100f;
        public const float UnarmedAttack = 4f;

        public static StatBlock Compute(Progression p, Equipment eq, SkillSet skills, Func<string, ItemDef> lookup, StatMods temporary = null)
        {
            var mods = new StatMods();
            float weaponAttack = UnarmedAttack;
            float weaponSpeed = 1f;
            bool hasWeapon = false;
            var weaponType = DamageType.Fisico;

            foreach (var kv in eq.All)
            {
                if (kv.Value == null) continue;
                var def = lookup(kv.Value.ItemId);
                if (def == null) continue;
                if (kv.Key == EquipSlot.Arma)
                {
                    hasWeapon = true;
                    weaponAttack = def.Mods.Attack;
                    weaponSpeed = 1f + def.Mods.AttackSpeed;
                    weaponType = def.WeaponDamageType;
                    var wm = new StatMods();
                    def.Mods.AddTo(wm);
                    wm.Attack = 0f; wm.AttackSpeed = 0f;
                    wm.AddTo(mods);
                }
                else def.Mods.AddTo(mods);

                if (kv.Value.HasUpgrade)
                {
                    foreach (var up in def.Upgrades)
                    {
                        if (up.Id != kv.Value.UpgradeId) continue;
                        if (kv.Key == EquipSlot.Arma)
                        {
                            weaponAttack += up.Mods.Attack;
                            weaponSpeed += up.Mods.AttackSpeed;
                            var um = new StatMods();
                            up.Mods.AddTo(um);
                            um.Attack = 0f; um.AttackSpeed = 0f;
                            um.AddTo(mods);
                        }
                        else up.Mods.AddTo(mods);
                    }
                }
            }
            temporary?.AddTo(mods);

            var s = new StatBlock { Level = p.Level, HasWeapon = hasWeapon, AttackType = weaponType };
            for (int i = 0; i < 5; i++) s.Attributes[i] = Math.Max(1, p.Get((AttributeType)i) + mods.Attributes[i]);
            int vig = s.Attr(AttributeType.Vigor), fue = s.Attr(AttributeType.Fuerza), agi = s.Attr(AttributeType.Agilidad);
            int per = s.Attr(AttributeType.Percepcion), res = s.Attr(AttributeType.Resonancia);

            s.MaxHealth = BaseHealth + 10f * (vig - 1) + 2f * (p.Level - 1) + mods.MaxHealth;
            s.MaxStamina = BaseStamina + 6f * (agi - 1);
            s.StaminaRegen = 20f * (1f + 0.04f * (agi - 1) + mods.StaminaRegen);
            s.Attack = weaponAttack * (1f + 0.08f * (fue - 1));
            s.AttackSpeed = Math.Max(0.5f, weaponSpeed * (1f + 0.03f * (agi - 1)));
            s.Defense = mods.Defense * (skills.Has("piel_curtida") ? 1.15f : 1f);
            s.ResistPhysical = OromMath.Clamp(0.02f * (vig - 1) + mods.ResistPhysical + (skills.Has("piel_curtida") ? 0.05f : 0f), 0f, 0.6f);
            s.ResistResonance = OromMath.Clamp(0.03f * (res - 1) + mods.ResistResonance + (skills.Has("eco_protector") ? 0.25f : 0f), 0f, 0.75f);
            s.MoveSpeedMult = Math.Max(0.6f, 1f + 0.015f * (agi - 1) + mods.MoveSpeed);
            s.StaminaCostMult = Math.Max(0.4f, 1f + mods.StaminaCost);
            s.SprintCostMult = skills.Has("paso_ligero") ? 0.7f : 1f;
            s.DodgeCostMult = skills.Has("esquiva_agil") ? 0.7f : 1f;
            s.HeavyCostMult = Math.Max(0.5f, 1f - 0.03f * (fue - 1));
            s.DetectionMult = (1f + 0.1f * (per - 1)) * (skills.Has("ojo_rastreador") ? 2f : 1f);
            s.ResonanceRangeMult = 1f + 0.12f * (res - 1) + (skills.Has("oido_fino") ? 0.5f : 0f);
            s.HealSpeedMult = skills.Has("vendaje_rapido") ? 1.4f : 1f;
            return s;
        }
    }

    /// <summary>Fabricación sencilla con recetas claras y mejoras excluyentes.</summary>
    public static class Crafting
    {
        public static bool IsKnown(RecipeDef recipe, GameState state) => string.IsNullOrEmpty(recipe.RequiredFlag) || state.Flags.Has(recipe.RequiredFlag);

        public static bool CanCraft(RecipeDef recipe, GameState state, bool atWorkbench, out string reason)
        {
            reason = null;
            if (!IsKnown(recipe, state)) { reason = "Todavía no conoces esta receta."; return false; }
            if (recipe.Station == "mesa" && !atWorkbench) { reason = "Necesitas una mesa de trabajo."; return false; }
            if (!state.Inventory.HasAll(recipe.Inputs)) { reason = "Te faltan materiales."; return false; }
            var outDef = state.Items(recipe.Output.ItemId);
            if (outDef != null && outDef.Unique && (state.Inventory.Has(outDef.Id) || state.Equipment.IsEquipped(outDef.Id)))
            {
                reason = "Ya tienes esta pieza.";
                return false;
            }
            return true;
        }

        public static bool Craft(RecipeDef recipe, GameState state, bool atWorkbench)
        {
            if (!CanCraft(recipe, state, atWorkbench, out _)) return false;
            if (!state.Inventory.RemoveAll(recipe.Inputs)) return false;
            state.Inventory.Add(recipe.Output.ItemId, recipe.Output.Count);
            return true;
        }

        public static bool CanUpgrade(ItemStack stack, UpgradeDef up, GameState state, out string reason)
        {
            reason = null;
            if (stack == null || up == null) { reason = "Nada que mejorar."; return false; }
            if (stack.HasUpgrade) { reason = "Esta pieza ya tiene una mejora: la elección es definitiva."; return false; }
            if (!state.Inventory.HasAll(up.Cost)) { reason = "Te faltan materiales."; return false; }
            return true;
        }

        public static bool ApplyUpgrade(ItemStack stack, UpgradeDef up, GameState state)
        {
            if (!CanUpgrade(stack, up, state, out _)) return false;
            if (!state.Inventory.RemoveAll(up.Cost)) return false;
            stack.UpgradeId = up.Id;
            return true;
        }
    }

    /// <summary>Trueque ligero: el valor de lo ofrecido debe cubrir lo pedido (doc: economía ligera, trueque y favores).</summary>
    public static class Barter
    {
        public static float OfferValue(IEnumerable<ItemAmount> offered, Func<string, ItemDef> lookup, MerchantDef merchant)
        {
            float v = 0f;
            foreach (var a in offered)
            {
                var def = lookup(a.ItemId);
                if (def == null) continue;
                float mult = 1f;
                if (merchant != null && merchant.Appreciation.TryGetValue(def.Category, out var m)) mult = m;
                v += def.Value * a.Count * mult;
            }
            return v;
        }

        public static float AskValue(TradeOffer offer, Func<string, ItemDef> lookup, int affinity)
        {
            var def = lookup(offer.ItemId);
            if (def == null) return float.MaxValue;
            float discount = 1f - OromMath.Clamp(affinity, 0, 100) * 0.003f;
            return def.Value * offer.Count * offer.PriceMultiplier * discount;
        }

        public static bool CanTrade(IEnumerable<ItemAmount> offered, TradeOffer offer, GameState state, MerchantDef merchant)
        {
            if (offer.Stock <= 0) return false;
            if (!state.Inventory.HasAll(offered)) return false;
            foreach (var a in offered)
            {
                if (a.Count <= 0) continue;
                var def = state.Items(a.ItemId);
                if (def == null || def.Category == ItemCategory.Narrativo || def.Unique) return false;
            }
            int affinity = state.Relationships.Affinity(merchant.NpcId);
            return OfferValue(offered, state.Items, merchant) + 0.001f >= AskValue(offer, state.Items, affinity);
        }

        public static bool Trade(List<ItemAmount> offered, TradeOffer offer, GameState state, MerchantDef merchant)
        {
            if (!CanTrade(offered, offer, state, merchant)) return false;
            if (!state.Inventory.RemoveAll(offered)) return false;
            state.Inventory.Add(offer.ItemId, offer.Count);
            offer.Stock--;
            return true;
        }
    }
}
