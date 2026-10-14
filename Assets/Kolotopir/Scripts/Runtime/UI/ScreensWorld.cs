using System;
using System.Collections.Generic;
using Kolotopir.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Kolotopir
{
    /// <summary>Artesanía y mejoras exclusivas (doc 10: crafteo basado en conocimiento y mejoras con elección).</summary>
    public sealed class CraftingScreen : UIScreen
    {
        public bool AtWorkbench;
        private RectTransform _list, _detail;
        private Text _where;
        private RecipeDef _recipe;
        private ItemStack _upgradeTarget;
        private EquipSlot _upgradeSlot;

        public override void Build(RectTransform parent)
        {
            var w = Window(parent, "Artesania", new Vector2(1400f, 880f), "Artesanía");
            _where = U.Txt(w, "Lugar", "", 18, U.Dim, TextAnchor.MiddleRight);
            U.Place(_where.rectTransform, U.TopRight, new Vector2(-80f, -26f), new Vector2(700f, 34f));
            var lp = U.PanelImg(w, "Recetas", new Color(0f, 0f, 0f, 0.25f));
            U.Place(lp.rectTransform, U.TopLeft, new Vector2(24f, -84f), new Vector2(520f, 772f));
            _list = U.Scroll(lp.transform, "Lista", out _, 6f, 10);
            U.Stretch((RectTransform)_list.parent);
            var dp = U.PanelImg(w, "Detalle", new Color(0f, 0f, 0f, 0.3f));
            U.Place(dp.rectTransform, U.TopLeft, new Vector2(560f, -84f), new Vector2(816f, 772f));
            _detail = U.Scroll(dp.transform, "Texto", out _, 10f, 20);
            U.Stretch((RectTransform)_detail.parent);
        }

        public override void OnOpen()
        {
            _recipe = null;
            _upgradeTarget = null;
            Refresh();
        }

        private void Refresh()
        {
            var s = Game.State;
            _where.text = AtWorkbench ? "Mesa de trabajo: recetas completas y mejoras de equipo." : "Sin mesa: solo recetas de campo.";
            U.Clear(_list);
            U.Clear(_detail);
            U.Header(_list, "Recetas", 22);
            foreach (var r in GameDb.Recipes)
            {
                bool known = Crafting.IsKnown(r, s);
                bool can = Crafting.CanCraft(r, s, AtWorkbench, out _);
                var recipe = r;
                string label = known ? r.Name + (r.Station == "mesa" ? U.Colored("  (mesa)", U.Dim) : "") : U.Colored("Receta desconocida", U.Dim);
                var b = U.Btn(_list, (can ? U.Colored("● ", U.Good) : "") + label, () => { _recipe = recipe; _upgradeTarget = null; Refresh(); }, 18,
                    _recipe == r ? U.ButtonHover : new Color(0.13f, 0.1f, 0.15f, 0.9f));
                U.Label(b).alignment = TextAnchor.MiddleLeft;
                U.Size(b, -1f, 46f);
            }
            if (AtWorkbench)
            {
                U.Separator(_list);
                U.Header(_list, "Mejoras de equipo", 22);
                foreach (var slot in Equipment.AllSlots)
                {
                    var st = s.Equipment.Get(slot);
                    if (st == null) continue;
                    var def = GameDb.Item(st.ItemId);
                    if (def == null || def.Upgrades.Count == 0) continue;
                    var stack = st;
                    var sl = slot;
                    var b = U.Btn(_list, def.Name + (st.HasUpgrade ? U.Colored("  ★ mejorada", Pal.Gold) : ""), () => { _upgradeTarget = stack; _upgradeSlot = sl; _recipe = null; Refresh(); }, 18,
                        _upgradeTarget == st ? U.ButtonHover : new Color(0.13f, 0.1f, 0.15f, 0.9f));
                    U.Label(b).alignment = TextAnchor.MiddleLeft;
                    U.Size(b, -1f, 46f);
                }
            }
            if (_recipe != null) ShowRecipe(_recipe);
            else if (_upgradeTarget != null) ShowUpgrades(_upgradeTarget);
            else U.Para(_detail, "Las recetas se aprenden escuchando a la gente y explorando: la progresión depende de lo que Nilo sabe.\n\nEn la mesa de trabajo puedes además aplicar una mejora a cada pieza equipada. Cada pieza admite una sola mejora: elige bien.", 19, U.Dim);
        }

        private void ShowRecipe(RecipeDef r)
        {
            var s = Game.State;
            if (!Crafting.IsKnown(r, s))
            {
                U.Header(_detail, "Receta desconocida", 26);
                U.Para(_detail, "Todavía no sabes hacer esto. Alguien en la tribu (o fuera de ella) podría enseñártelo.", 19, U.Dim);
                return;
            }
            var outDef = GameDb.Item(r.Output.ItemId);
            U.Header(_detail, r.Name, 28);
            if (!string.IsNullOrEmpty(r.Description)) U.Para(_detail, r.Description, 18, U.Dim);
            if (outDef != null) U.Para(_detail, U.Colored("Resultado: ", U.Accent) + outDef.Name + (r.Output.Count > 1 ? " ×" + r.Output.Count : "") + "\n" + outDef.Description, 19);
            U.Para(_detail, U.Colored("Materiales:", U.Accent), 19);
            foreach (var a in r.Inputs)
            {
                var d = GameDb.Item(a.ItemId);
                int have = s.Inventory.Count(a.ItemId);
                U.Para(_detail, "  • " + (d != null ? d.Name : a.ItemId) + "  " + U.Colored(have + "/" + a.Count, have >= a.Count ? U.Good : U.Bad), 19);
            }
            bool can = Crafting.CanCraft(r, s, AtWorkbench, out var reason);
            var b = U.Btn(_detail, "Fabricar", () =>
            {
                if (Crafting.Craft(r, s, AtWorkbench))
                {
                    Game.Audio?.Play2D("cortar", 0.6f);
                    Game.Ctx.Notify("Fabricado: " + (outDef != null ? outDef.Name : r.Name));
                    Game.Ctx.SetFlag("primer_crafteo");
                    if (outDef != null && outDef.Category == ItemCategory.Armadura) Game.Ctx.SetFlag("armadura_fabricada");
                    Game.MarkDirty();
                }
                Refresh();
            }, 21);
            U.Size(b, 280f, 50f);
            b.interactable = can;
            if (!can && reason != null) U.Para(_detail, reason, 17, U.Bad);
        }

        private void ShowUpgrades(ItemStack st)
        {
            var s = Game.State;
            var def = GameDb.Item(st.ItemId);
            U.Header(_detail, def.Name, 28);
            U.Para(_detail, "Cada pieza solo admite una mejora. La elección es definitiva.", 18, U.Dim);
            foreach (var up in def.Upgrades)
            {
                U.Separator(_detail);
                bool chosen = st.UpgradeId == up.Id;
                U.Para(_detail, U.Colored(up.Name, chosen ? Pal.Gold : U.Accent) + (chosen ? "  ★" : "") + "\n" + up.Description + "\n" + U.Colored(up.Mods.Describe(), U.Good), 19);
                var cost = new List<string>();
                foreach (var a in up.Cost)
                {
                    var d = GameDb.Item(a.ItemId);
                    int have = s.Inventory.Count(a.ItemId);
                    cost.Add((d != null ? d.Name : a.ItemId) + " " + U.Colored(have + "/" + a.Count, have >= a.Count ? U.Good : U.Bad));
                }
                U.Para(_detail, "Coste: " + string.Join(", ", cost), 17);
                if (st.HasUpgrade) continue;
                bool can = Crafting.CanUpgrade(st, up, s, out var reason);
                var upgrade = up;
                var b = U.Btn(_detail, "Aplicar esta mejora", () =>
                    Game.UI.Confirm("Mejora definitiva", "¿Aplicar «" + upgrade.Name + "» a " + def.Name + "? No podrás elegir la otra mejora.", "Aplicar", "Cancelar", ok =>
                    {
                        if (!ok) return;
                        if (Crafting.ApplyUpgrade(st, upgrade, s))
                        {
                            s.RecomputeStats();
                            Game.Audio?.Play2D("nivel", 0.5f, 1.3f);
                            Game.Ctx.Notify("Mejora aplicada: " + upgrade.Name);
                            Game.Ctx.SetFlag("primer_crafteo");
                            Game.MarkDirty();
                        }
                        Refresh();
                    }), 19);
                U.Size(b, 300f, 46f);
                b.interactable = can;
                if (!can && reason != null) U.Para(_detail, reason, 16, U.Bad);
            }
        }
    }

    /// <summary>Trueque (doc 10: economía ligera de trueque y favores; los kolotapires no usan moneda).</summary>
    public sealed class BarterScreen : UIScreen
    {
        public MerchantDef Merchant;
        private Text _greeting, _balance;
        private RectTransform _offers, _mine;
        private TradeOffer _selected;
        private readonly Dictionary<string, int> _offered = new Dictionary<string, int>();
        private Button _trade;

        public override void Build(RectTransform parent)
        {
            var w = Window(parent, "Trueque", new Vector2(1500f, 900f), "Trueque");
            _greeting = U.Txt(w, "Saludo", "", 20, U.TextColor, TextAnchor.UpperLeft, FontStyle.Italic);
            U.Place(_greeting.rectTransform, U.TopLeft, new Vector2(28f, -70f), new Vector2(1440f, 60f));
            var lp = U.PanelImg(w, "Ofertas", new Color(0f, 0f, 0f, 0.25f));
            U.Place(lp.rectTransform, U.TopLeft, new Vector2(24f, -140f), new Vector2(700f, 620f));
            _offers = U.Scroll(lp.transform, "Lista", out _, 6f, 10);
            U.Stretch((RectTransform)_offers.parent);
            var rp = U.PanelImg(w, "Mio", new Color(0f, 0f, 0f, 0.25f));
            U.Place(rp.rectTransform, U.TopLeft, new Vector2(744f, -140f), new Vector2(732f, 620f));
            _mine = U.Scroll(rp.transform, "Lista", out _, 6f, 10);
            U.Stretch((RectTransform)_mine.parent);
            _balance = U.Txt(w, "Balance", "", 20, U.TextColor, TextAnchor.MiddleLeft);
            U.Place(_balance.rectTransform, U.BottomLeft, new Vector2(28f, 30f), new Vector2(1060f, 80f));
            _trade = U.Btn(w, "Intercambiar", DoTrade, 22);
            U.Place((RectTransform)_trade.transform, U.BottomRight, new Vector2(-28f, 36f), new Vector2(320f, 58f));
        }

        public override void OnOpen()
        {
            _selected = null;
            _offered.Clear();
            Refresh();
        }

        private string SoldKey(TradeOffer o) => "vendido_" + Merchant.NpcId + "_" + o.ItemId;
        private int Stock(TradeOffer o) => Mathf.Max(0, o.Stock - Game.State.Flags.Get(SoldKey(o)));

        private List<ItemAmount> Offered()
        {
            var list = new List<ItemAmount>();
            foreach (var kv in _offered) if (kv.Value > 0) list.Add(new ItemAmount(kv.Key, kv.Value));
            return list;
        }

        private void Refresh()
        {
            var s = Game.State;
            if (Merchant == null) return;
            _greeting.text = U.Colored(Merchant.Name + ": ", U.Accent) + "«" + Merchant.Greeting + "»";
            U.Clear(_offers);
            U.Clear(_mine);
            U.Header(_offers, "Lo que ofrece " + Merchant.Name, 22);
            int affinity = s.Affinity(Merchant.NpcId);
            foreach (var o in Merchant.Offers)
            {
                var def = GameDb.Item(o.ItemId);
                if (def == null) continue;
                int stock = Stock(o);
                var offer = o;
                float ask = Barter.AskValue(o, GameDb.Item, affinity);
                var b = U.Btn(_offers, def.Name + (o.Count > 1 ? " ×" + o.Count : "") + U.Colored($"   valor {ask:0.#} · quedan {stock}", U.Dim), () => { _selected = offer; Refresh(); }, 18,
                    _selected == o ? U.ButtonHover : new Color(0.13f, 0.1f, 0.15f, 0.9f));
                U.Label(b).alignment = TextAnchor.MiddleLeft;
                U.Size(b, -1f, 48f);
                b.interactable = stock > 0;
                if (_selected == o) U.Para(_offers, "   " + def.Description, 16, U.Dim);
            }

            U.Header(_mine, "Lo que puedes ofrecer", 22);
            var apprec = new List<string>();
            foreach (var kv in Merchant.Appreciation) if (kv.Value > 1.01f) apprec.Add(Names.Category(kv.Key).ToLowerInvariant());
            if (apprec.Count > 0) U.Para(_mine, Merchant.Name + " valora especialmente: " + string.Join(", ", apprec) + ".", 16, Pal.ResonanceSoft);
            foreach (var st in s.Inventory.Stacks)
            {
                var def = GameDb.Item(st.ItemId);
                if (def == null || def.Unique || def.Category == ItemCategory.Narrativo) continue;
                int offered = _offered.TryGetValue(def.Id, out var c) ? c : 0;
                int have = s.Inventory.Count(def.Id);
                float mult = Merchant.Appreciation.TryGetValue(def.Category, out var m) ? m : 1f;
                var row = U.Rect(_mine, "Fila");
                U.Size(row, -1f, 46f);
                U.HLayout(row, 8f, 0);
                var name = U.Txt(row, "Nombre", def.Name + U.Colored($"  (tienes {have} · valor {def.Value * mult:0.#})", U.Dim), 18, U.TextColor, TextAnchor.MiddleLeft);
                U.Size(name, -1f, 42f, 1f);
                string id = def.Id;
                var minus = U.Btn(row, "−", () => { _offered[id] = Mathf.Max(0, offered - 1); Refresh(); }, 22);
                U.Size(minus, 44f, 40f);
                var count = U.Txt(row, "Cantidad", offered.ToString(), 20, offered > 0 ? U.Accent : U.Dim, TextAnchor.MiddleCenter);
                U.Size(count, 50f, 40f);
                var plus = U.Btn(row, "+", () => { _offered[id] = Mathf.Min(have, offered + 1); Refresh(); }, 22);
                U.Size(plus, 44f, 40f);
            }

            float value = Barter.OfferValue(Offered(), GameDb.Item, Merchant);
            if (_selected != null)
            {
                float ask = Barter.AskValue(_selected, GameDb.Item, affinity);
                var copy = new TradeOffer { ItemId = _selected.ItemId, Count = _selected.Count, Stock = Stock(_selected), PriceMultiplier = _selected.PriceMultiplier };
                bool ok = Barter.CanTrade(Offered(), copy, s, Merchant);
                _balance.text = $"Ofreces: {U.Colored(value.ToString("0.#"), value + 0.001f >= ask ? U.Good : U.Bad)}  ·  Pide: {ask:0.#}" + (affinity > 0 ? U.Colored($"  (descuento por amistad {affinity * 0.3f:0} %)", U.Dim) : "");
                _trade.interactable = ok;
            }
            else
            {
                _balance.text = "Elige lo que quieres y ofrece objetos de valor parecido.";
                _trade.interactable = false;
            }
        }

        private void DoTrade()
        {
            var s = Game.State;
            if (_selected == null || Merchant == null) return;
            var copy = new TradeOffer { ItemId = _selected.ItemId, Count = _selected.Count, Stock = Stock(_selected), PriceMultiplier = _selected.PriceMultiplier };
            var offered = Offered();
            if (Barter.Trade(offered, copy, s, Merchant))
            {
                s.Flags.Increment(SoldKey(_selected));
                s.Flags.Increment("trueques_" + Merchant.NpcId);
                Game.Ctx.ChangeAffinity(Merchant.NpcId, 2);
                var def = GameDb.Item(_selected.ItemId);
                Game.Ctx.Notify("Trueque con " + Merchant.Name + ": " + (def != null ? def.Name : _selected.ItemId));
                Game.Audio?.Play2D("recoger", 0.6f);
                if (def != null && (def.Category == ItemCategory.Cultural || def.Category == ItemCategory.Herramienta)) Game.Ctx.AddNotebook("obj_" + def.Id, Knowledge.Observado);
                Game.MarkDirty();
                _offered.Clear();
            }
            Refresh();
        }
    }

    /// <summary>
    /// Interacción con una Cápsula (doc 6): examinar, escuchar, reparar, reconstruir un recuerdo
    /// fragmentado (puzle con pistas graduales) y decidir qué hacer con él.
    /// </summary>
    public sealed class CapsuleScreen : UIScreen
    {
        public CapsuleController Capsule;
        private Text _title, _state;
        private RectTransform _body;
        private enum Mode { Main, Puzzle, Decision }
        private Mode _mode;
        private readonly List<string> _order = new List<string>();
        private int _hintLevel;
        private CapsuleDecision _pending;

        public override void Build(RectTransform parent)
        {
            var w = Window(parent, "Capsula", new Vector2(1180f, 860f), " ");
            _title = U.Txt(w, "Nombre", "", 32, Pal.ResonanceSoft, TextAnchor.UpperLeft, FontStyle.Bold);
            U.Place(_title.rectTransform, U.TopLeft, new Vector2(28f, -18f), new Vector2(980f, 46f));
            _state = U.Txt(w, "Estado", "", 18, U.Dim, TextAnchor.UpperLeft);
            U.Place(_state.rectTransform, U.TopLeft, new Vector2(28f, -66f), new Vector2(1100f, 30f));
            var bp = U.PanelImg(w, "Cuerpo", new Color(0f, 0f, 0f, 0.3f));
            U.Place(bp.rectTransform, U.TopLeft, new Vector2(24f, -104f), new Vector2(1132f, 732f));
            _body = U.Scroll(bp.transform, "Contenido", out _, 10f, 22);
            U.Stretch((RectTransform)_body.parent);
        }

        public override void OnOpen()
        {
            _mode = Mode.Main;
            _order.Clear();
            _hintLevel = 0;
            _pending = null;
            Refresh();
        }

        public override bool Back()
        {
            if (_mode != Mode.Main) { _mode = Mode.Main; Refresh(); return true; }
            return false;
        }

        private void Refresh()
        {
            if (Capsule == null) return;
            var def = Capsule.Def;
            var st = Capsule.State;
            _title.text = def.Name;
            _state.text = "Estado: " + Names.Capsule(st) + " — " + Names.CapsuleReading(st);
            U.Clear(_body);
            switch (_mode)
            {
                case Mode.Main: Main(); break;
                case Mode.Puzzle: Puzzle(); break;
                case Mode.Decision: Decision(); break;
            }
        }

        private Button Act(string label, Action a, bool enabled = true, string reason = null)
        {
            var b = U.Btn(_body, label, a, 21);
            U.Size(b, -1f, 52f);
            b.interactable = enabled;
            if (!enabled && !string.IsNullOrEmpty(reason)) U.Para(_body, "   " + reason, 16, U.Dim);
            return b;
        }

        private void Main()
        {
            var c = Capsule;
            U.Para(_body, c.ExamineText(), 20);
            U.Separator(_body);
            Act("Examinar con atención", () =>
            {
                c.Examine();
                Game.Audio?.Play2D("examinar", 0.5f);
                Refresh();
            });
            string res = c.NextResonance(out var why);
            Act("Escuchar (" + InputMap.KeyName(InputAction.Escuchar) + ")", () =>
            {
                Game.UI.Close(this);
                Game.Resonance.Request(res, c.ListenPoint, () => c.OnListened(res));
            }, res != null && !Game.Resonance.Playing, why);
            if (c.RepairPending)
            {
                bool can = c.CanRepair(out var reason);
                var costs = new List<string>();
                foreach (var a in c.Def.RepairCost)
                {
                    var d = GameDb.Item(a.ItemId);
                    costs.Add((d != null ? d.Name : a.ItemId) + " ×" + a.Count);
                }
                if (!string.IsNullOrEmpty(c.Def.RepairText)) U.Para(_body, c.Def.RepairText, 18, U.Dim);
                Act("Encajar un fragmento (" + string.Join(", ", costs) + ")", () => { c.Repair(); Refresh(); }, can, reason);
            }
            if (c.PuzzlePending)
            {
                bool avail = c.PuzzleAvailable;
                Act("Reconstruir el recuerdo", () => { _mode = Mode.Puzzle; _order.Clear(); Refresh(); }, avail, avail ? null : c.Def.PuzzleLockedText);
            }
            if (c.DecisionPending)
                Act("Decidir qué hacer con el recuerdo", () => { _mode = Mode.Decision; _pending = null; Refresh(); });
            U.Separator(_body);
            U.Para(_body, "Las Cápsulas responden a la Escucha. Algunas están dañadas y solo muestran fragmentos; repararlas o reconstruir su recuerdo puede revelar más. Recuerda: acceder a memorias ajenas tiene consecuencias.", 16, U.Dim);
        }

        private void Puzzle()
        {
            var def = Capsule.Def;
            U.Header(_body, "Reconstruir el recuerdo", 26);
            if (!string.IsNullOrEmpty(def.PuzzleIntro)) U.Para(_body, def.PuzzleIntro, 19);
            U.Para(_body, "Ordena los fragmentos del principio al final. Pulsa un fragmento para añadirlo a la secuencia.", 17, U.Dim);
            var cards = U.Rect(_body, "Fragmentos");
            U.Size(cards, -1f, 200f);
            U.Grid(cards, new Vector2(255f, 190f), new Vector2(12f, 12f), 4);
            foreach (var f in def.Fragments)
            {
                bool used = _order.Contains(f.Id);
                var frag = f;
                var b = U.Btn(cards, "", () =>
                {
                    if (_order.Contains(frag.Id) || _order.Count >= def.CorrectOrder.Length) return;
                    _order.Add(frag.Id);
                    Game.Audio?.Play2D("res_campanilla", 0.3f, 0.9f + _order.Count * 0.1f);
                    Refresh();
                }, 16, used ? new Color(0.1f, 0.08f, 0.12f, 0.6f) : new Color(0.22f, 0.15f, 0.3f, 0.95f));
                var sym = U.Txt(b.transform, "Simbolo", f.Symbol, 46, used ? U.Dim : Pal.ResonanceSoft, TextAnchor.UpperCenter);
                U.Place(sym.rectTransform, U.Top, new Vector2(0f, -8f), new Vector2(240f, 60f));
                var txt = U.Txt(b.transform, "Texto", f.Text, 16, used ? U.Dim : U.TextColor, TextAnchor.UpperCenter, FontStyle.Italic);
                U.Place(txt.rectTransform, U.Top, new Vector2(0f, -70f), new Vector2(236f, 116f));
                b.interactable = !used;
            }
            var seq = new List<string>();
            foreach (var id in _order)
            {
                foreach (var f in def.Fragments) if (f.Id == id) seq.Add(f.Symbol);
            }
            U.Para(_body, U.Colored("Secuencia: ", U.Accent) + (seq.Count > 0 ? string.Join("  →  ", seq) : "(vacía)"), 22);
            var row = U.Rect(_body, "Botones");
            U.Size(row, -1f, 52f);
            U.HLayout(row, 10f, 0, TextAnchor.MiddleLeft, true);
            var undo = U.Btn(row, "Deshacer", () => { if (_order.Count > 0) _order.RemoveAt(_order.Count - 1); Refresh(); }, 20);
            U.Size(undo, -1f, 50f);
            var check = U.Btn(row, "Comprobar", Check, 20);
            U.Size(check, -1f, 50f);
            check.interactable = _order.Count == def.CorrectOrder.Length;
            var hint = U.Btn(row, "Pista (" + Mathf.Min(_hintLevel, def.PuzzleHints.Length) + "/" + def.PuzzleHints.Length + ")", () => { _hintLevel = Mathf.Min(_hintLevel + 1, def.PuzzleHints.Length); Refresh(); }, 20);
            U.Size(hint, -1f, 50f);
            hint.interactable = Game.Settings == null || Game.Settings.Data.Hints;
            for (int i = 0; i < _hintLevel && i < def.PuzzleHints.Length; i++)
                U.Para(_body, U.Colored("Pista " + (i + 1) + ": ", Pal.ResonanceSoft) + Tokens.Apply(def.PuzzleHints[i]), 18);
        }

        private void Check()
        {
            var c = Capsule;
            if (c.SolvePuzzle(_order))
            {
                Game.Audio?.Play2D("res_fragmento", 0.9f);
                FX.Burst(c.transform.position + Vector3.up * 1.4f, Pal.Resonance, 50, 3f);
                Game.Ctx.Notify("El recuerdo se recompone.");
                _mode = Mode.Main;
                Game.UI.Close(this);
                // La Cápsula reproduce el recuerdo reconstruido.
                string res = c.NextResonance(out _);
                if (res != null && !Game.State.Heard(res)) Game.Resonance.Request(res, c.ListenPoint, () => c.OnListened(res));
                return;
            }
            Game.Audio?.Play2D("aviso", 0.5f);
            Game.UI.Toast("Los fragmentos chirrían y se separan. Ese no es el orden.");
            Game.Cam?.Shake(0.15f);
            _order.Clear();
            Refresh();
        }

        private void Decision()
        {
            var def = Capsule.Def;
            U.Header(_body, "¿Qué haces con el recuerdo?", 26);
            U.Para(_body, "No hay respuesta correcta. Cada opción tiene un alcance distinto y cambiará cómo te recuerdan y cómo se recuerda.", 18, U.Dim);
            foreach (var d in def.Decisions)
            {
                U.Separator(_body);
                var dec = d;
                U.Para(_body, U.Colored(d.Label, _pending == d ? U.Accent : U.TextColor) + "  " + U.Colored("· alcance " + Names.Scale(d.Scale).ToLowerInvariant(), Pal.ResonanceSoft) + "\n" + d.Description + "\n" + U.Colored(JournalScreen.ScaleHelp(d.Scale), U.Dim), 19);
                var b = U.Btn(_body, _pending == d ? "Elegida" : "Elegir", () => { _pending = dec; Refresh(); }, 19, _pending == d ? U.ButtonHover : U.ButtonColor);
                U.Size(b, 220f, 44f);
            }
            U.Separator(_body);
            var confirm = U.Btn(_body, "Confirmar decisión", () =>
            {
                var d = _pending;
                if (d == null) return;
                Game.UI.Confirm("Decisión definitiva", "«" + d.Label + "»\n\n" + d.Description + "\n\nNo podrás cambiarla.", "Decidir", "Pensarlo más", ok =>
                {
                    if (!ok) return;
                    Capsule.Decide(d);
                    Game.Audio?.Play2D("res_fragmento", 0.8f, 0.8f);
                    _mode = Mode.Main;
                    Game.UI.Close(this);
                });
            }, 21);
            U.Size(confirm, 340f, 52f);
            confirm.interactable = _pending != null;
        }
    }
}
