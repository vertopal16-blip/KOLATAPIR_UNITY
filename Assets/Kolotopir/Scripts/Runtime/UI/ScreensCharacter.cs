using System;
using System.Collections.Generic;
using Kolotopir.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kolotopir
{
    /// <summary>Caja de diálogo: nombre, texto con revelado progresivo y opciones (ratón o teclas 1–9).</summary>
    public sealed class DialogueView
    {
        private RectTransform _root;
        private Text _speaker, _role, _text, _continue;
        private RectTransform _choices;
        private readonly List<Button> _choiceButtons = new List<Button>();
        private bool _choicesBuilt;

        public void Build(RectTransform parent)
        {
            var p = U.PanelImg(parent, "Dialogo", new Color(0.07f, 0.05f, 0.09f, 0.93f));
            _root = p.rectTransform;
            U.Place(_root, U.Bottom, new Vector2(0f, 36f), new Vector2(1320f, 330f));
            _speaker = U.Txt(_root, "Nombre", "", 26, U.Accent, TextAnchor.UpperLeft, FontStyle.Bold);
            U.Place(_speaker.rectTransform, U.TopLeft, new Vector2(30f, -18f), new Vector2(700f, 36f));
            _role = U.Txt(_root, "Rol", "", 17, U.Dim, TextAnchor.UpperRight, FontStyle.Italic);
            U.Place(_role.rectTransform, U.TopRight, new Vector2(-30f, -24f), new Vector2(560f, 28f));
            _text = U.Txt(_root, "Texto", "", 23, U.TextColor);
            U.Place(_text.rectTransform, U.TopLeft, new Vector2(30f, -58f), new Vector2(1260f, 120f));
            _choices = U.Rect(_root, "Opciones");
            U.Place(_choices, U.TopLeft, new Vector2(30f, -180f), new Vector2(1260f, 140f));
            U.VLayout(_choices, 6f);
            _continue = U.Txt(_root, "Seguir", "", 17, U.Dim, TextAnchor.LowerRight);
            U.Place(_continue.rectTransform, U.BottomRight, new Vector2(-24f, 14f), new Vector2(500f, 26f));
            var clickArea = p.gameObject.AddComponent<Button>();
            clickArea.transition = Selectable.Transition.None;
            var nav = clickArea.navigation; nav.mode = Navigation.Mode.None; clickArea.navigation = nav;
            clickArea.onClick.AddListener(() =>
            {
                var d = Game.Dialogue;
                if (d != null && d.Active && (d.Runner.VisibleChoices().Count == 0 || !d.FullyRevealed)) d.Advance();
            });
            _root.gameObject.SetActive(false);
        }

        public void Show(bool show)
        {
            _root.gameObject.SetActive(show);
            if (show) NodeChanged();
        }

        public void NodeChanged()
        {
            _choicesBuilt = false;
            U.Clear(_choices);
            _choiceButtons.Clear();
            var d = Game.Dialogue;
            if (d == null || d.Current == null) return;
            _speaker.text = d.Current.Speaker;
            string role = "";
            if (d.Npc != null && d.Npc.Def.Name == d.Current.Speaker)
            {
                role = d.Npc.Def.Role ?? "";
                int aff = Game.State.Affinity(d.Npc.Def.Id);
                if (aff != 0) role += (role.Length > 0 ? " · " : "") + "afinidad " + (aff > 0 ? "+" : "") + aff;
            }
            _role.text = role;
        }

        private void BuildChoices()
        {
            _choicesBuilt = true;
            var d = Game.Dialogue;
            var list = d.Runner.VisibleChoices();
            float height = 0f;
            for (int i = 0; i < list.Count; i++)
            {
                var c = list[i];
                bool locked = d.Runner.IsLocked(c);
                string label = (i + 1) + ".  " + Tokens.Apply(c.Text) + (locked && !string.IsNullOrEmpty(c.LockedReason) ? "  " + U.Colored("(" + c.LockedReason + ")", U.Bad) : "");
                var b = U.Btn(_choices, label, () => d.Choose(c), 21, locked ? new Color(0.14f, 0.12f, 0.14f, 0.9f) : new Color(0.22f, 0.16f, 0.26f, 0.95f));
                var t = U.Label(b);
                t.alignment = TextAnchor.MiddleLeft;
                t.color = locked ? U.Dim : U.TextColor;
                U.Size(b, -1f, 40f);
                _choiceButtons.Add(b);
                height += 46f;
            }
            float total = 200f + Mathf.Max(0f, height - 10f);
            _root.sizeDelta = new Vector2(1320f, Mathf.Max(330f, total + 20f));
            _choices.sizeDelta = new Vector2(1260f, height);
        }

        public void Tick(float dt)
        {
            var d = Game.Dialogue;
            if (d == null || !d.Active || !_root.gameObject.activeSelf) return;
            string full = d.CurrentText;
            int n = Mathf.Clamp(Mathf.FloorToInt(d.Reveal), 0, full.Length);
            _text.text = n >= full.Length ? full : full.Substring(0, n) + "<color=#00000000>" + full.Substring(n) + "</color>";
            bool revealed = d.FullyRevealed;
            int choices = d.Runner.VisibleChoices().Count;
            if (revealed && choices > 0 && !_choicesBuilt) BuildChoices();
            if (!revealed && _choicesBuilt) { U.Clear(_choices); _choicesBuilt = false; }
            if (choices == 0) _root.sizeDelta = new Vector2(1320f, 220f);
            _continue.text = !revealed ? "" : choices > 0 ? "Elige con el ratón o con las teclas 1–" + Mathf.Min(9, choices) : "▶  [" + InputMap.KeyName(InputAction.Interactuar) + "] / clic para continuar";
        }
    }

    // ====================================================================== Vista previa de Nilo

    /// <summary>Muñeco de Nilo para el inventario: refleja el equipo que lleva puesto (renderizado aparte).</summary>
    public sealed class PaperDoll : MonoBehaviour
    {
        public const int Layer = 31;
        public RenderTexture Texture { get; private set; }
        private Camera _cam;
        private HumanoidRig _rig;
        private Transform _pivot;
        private readonly Dictionary<EquipSlot, List<GameObject>> _pieces = new Dictionary<EquipSlot, List<GameObject>>();
        private readonly Dictionary<EquipSlot, string> _shown = new Dictionary<EquipSlot, string>();
        private float _yaw = 200f;
        private float _time;

        public static readonly Appearance NiloLook = new Appearance
        {
            Height = 1.52f, Build = 0.92f, Skin = "#C99A78", Hair = "#0B0A0E", Eyes = "#7FD3F0",
            Cloth = "#6B2E8C", Cloth2 = "#4A1F63", Accent = "#C9A14A", Pants = "#3E3A4A", HairStyle = "despeinado",
        };

        public static PaperDoll Create()
        {
            var go = new GameObject("MunecoNilo");
            DontDestroyOnLoad(go);
            go.transform.position = new Vector3(-3000f, 1000f, -3000f);
            var d = go.AddComponent<PaperDoll>();
            d.Build();
            return d;
        }

        private void Build()
        {
            _pivot = K.Child(transform, "Pivote");
            _rig = HumanoidBuilder.Build(_pivot, Species.Humano, NiloLook, null, true);
            K.SetLayerRecursive(_pivot.gameObject, Layer);
            Texture = new RenderTexture(512, 720, 24, RenderTextureFormat.ARGB32) { name = "Nilo", antiAliasing = 2 };
            var camGo = new GameObject("CamaraMuneco");
            camGo.transform.SetParent(transform, false);
            camGo.transform.localPosition = new Vector3(0f, 0.95f, 2.6f);
            camGo.transform.localRotation = Quaternion.LookRotation(new Vector3(0f, -0.08f, -1f));
            _cam = camGo.AddComponent<Camera>();
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            _cam.cullingMask = 1 << Layer;
            _cam.fieldOfView = 30f;
            _cam.nearClipPlane = 0.1f;
            _cam.farClipPlane = 10f;
            _cam.targetTexture = Texture;
            _cam.enabled = false;
            var key = new GameObject("Luz").AddComponent<Light>();
            key.transform.SetParent(transform, false);
            key.transform.localPosition = new Vector3(1.2f, 2.2f, 2f);
            key.type = LightType.Point;
            key.range = 7f;
            key.intensity = 2.2f;
            key.color = new Color(1f, 0.92f, 0.82f);
            var rim = new GameObject("Contraluz").AddComponent<Light>();
            rim.transform.SetParent(transform, false);
            rim.transform.localPosition = new Vector3(-1.4f, 1.8f, -1.6f);
            rim.type = LightType.Point;
            rim.range = 6f;
            rim.intensity = 1.6f;
            rim.color = Pal.ResonanceSoft;
            foreach (var c in Camera.allCameras) if (c != _cam) c.cullingMask &= ~(1 << Layer);
        }

        public void SetActive(bool on)
        {
            _cam.enabled = on;
            if (on) Refresh();
        }

        public void Rotate(float delta) => _yaw += delta;

        private void Update()
        {
            if (!_cam.enabled) return;
            _time += Time.unscaledDeltaTime;
            _pivot.localRotation = Quaternion.Euler(0f, _yaw, 0f);
            HumanoidAnimator.Pose(_rig, _time, 0f, Activity.Casa, Emotion.Neutral, false, null, Time.unscaledDeltaTime);
        }

        public void Refresh()
        {
            var s = Game.State;
            if (s == null) return;
            foreach (var slot in Equipment.AllSlots)
            {
                var st = s.Equipment.Get(slot);
                string id = st != null ? st.ItemId + "|" + st.UpgradeId : null;
                _shown.TryGetValue(slot, out var cur);
                if (cur == id) continue;
                _shown[slot] = id;
                if (_pieces.TryGetValue(slot, out var old)) foreach (var o in old) if (o != null) Destroy(o);
                _pieces.Remove(slot);
                if (st == null) continue;
                var def = GameDb.Item(st.ItemId);
                if (def == null) continue;
                var list = new List<GameObject>();
                BuildPiece(slot, def, st.HasUpgrade, list);
                foreach (var go in list) K.SetLayerRecursive(go, Layer);
                _pieces[slot] = list;
            }
        }

        private static GameObject Piece(Transform parent, string name, ModelBuilder mb, List<GameObject> output)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            mb.Build(go.transform, name, false, false);
            output.Add(go);
            return go;
        }

        private void BuildPiece(EquipSlot slot, ItemDef def, bool upgraded, List<GameObject> output)
        {
            float H = NiloLook.Height;
            var col = MaterialLibrary.Lit(K.Hex(def.Color), upgraded ? 0.5f : 0.25f, def.Category == ItemCategory.Arma ? 0.6f : 0f);
            var dark = MaterialLibrary.Lit(K.Mul(K.Hex(def.Color), 0.6f), 0.2f);
            switch (slot)
            {
                case EquipSlot.Cabeza:
                {
                    float headR = 0.068f * H;
                    var mb = new ModelBuilder();
                    mb.For(col).Sphere(new Vector3(0f, headR + 0.045f * H, -0.004f * H), new Vector3(headR * 1.18f, headR * 0.95f, headR * 1.2f), 10, 5, Quaternion.identity);
                    mb.For(dark).Cylinder(new Vector3(0f, headR + 0.02f * H, 0f), headR * 1.22f, headR * 1.22f, 0.012f * H, 10, Quaternion.identity);
                    Piece(_rig.Head, "Casco", mb, output);
                    break;
                }
                case EquipSlot.Torso:
                {
                    float w = 0.12f * H * NiloLook.Build;
                    var mb = new ModelBuilder();
                    mb.For(col).Lathe(new Vector3(0f, 0.02f * H, 0f), new[]
                    {
                        new Vector2(w * 1.12f, 0f), new Vector2(w * 1.02f, 0.14f * H), new Vector2(w * 1.1f, 0.28f * H), new Vector2(w * 0.6f, 0.31f * H),
                    }, 10, Quaternion.identity, false, false);
                    mb.For(dark).Box(new Vector3(0f, 0.12f * H, w * 1.08f), new Vector3(0.012f * H, 0.22f * H, 0.01f * H), Quaternion.identity);
                    Piece(_rig.Torso, "Peto", mb, output);
                    break;
                }
                case EquipSlot.Brazos:
                    foreach (var arm in new[] { _rig.ArmL, _rig.ArmR })
                    {
                        float a = 0.37f * H;
                        var mb = new ModelBuilder();
                        mb.For(col).Cylinder(new Vector3(0f, -a * 0.9f, 0f), 0.034f * H, 0.038f * H, a * 0.3f, 7, Quaternion.identity);
                        Piece(arm, "Brazal", mb, output);
                    }
                    break;
                case EquipSlot.Piernas:
                    foreach (var leg in new[] { _rig.LegL, _rig.LegR })
                    {
                        float l = 0.47f * H;
                        var mb = new ModelBuilder();
                        mb.For(col).Cylinder(new Vector3(0f, -l * 0.86f, 0.004f * H), 0.045f * H, 0.05f * H, l * 0.36f, 7, Quaternion.identity);
                        Piece(leg, "Greba", mb, output);
                    }
                    break;
                case EquipSlot.Arma:
                {
                    var mb = new ModelBuilder();
                    mb.For(MaterialLibrary.Lit(new Color(0.78f, 0.8f, 0.84f), 0.75f, 0.7f)).Box(new Vector3(0f, 0.42f, 0f), new Vector3(0.05f, 0.62f, 0.012f), Quaternion.identity);
                    mb.For(dark).Box(new Vector3(0f, 0.1f, 0f), new Vector3(0.16f, 0.03f, 0.03f), Quaternion.identity);
                    mb.For(col).Cylinder(new Vector3(0f, -0.06f, 0f), 0.018f, 0.018f, 0.16f, 6, Quaternion.identity);
                    var go = Piece(_rig.ArmR, "Espada", mb, output);
                    go.transform.localPosition = new Vector3(0f, -0.37f * H, 0.02f);
                    go.transform.localRotation = Quaternion.Euler(80f, 0f, 0f);
                    break;
                }
                case EquipSlot.Accesorio1:
                case EquipSlot.Accesorio2:
                {
                    float side = slot == EquipSlot.Accesorio1 ? 0.03f : -0.03f;
                    var mb = new ModelBuilder();
                    mb.For(MaterialLibrary.Emissive(K.Hex(def.Color), K.Hex(def.Color), 0.4f)).Sphere(new Vector3(side * H, 0.2f * H, 0.13f * H * NiloLook.Build), Vector3.one * 0.016f * H, 6, 4, Quaternion.identity);
                    Piece(_rig.Torso, "Accesorio", mb, output);
                    break;
                }
            }
        }
    }

    /// <summary>Zona arrastrable que gira el muñeco.</summary>
    public sealed class DollDrag : MonoBehaviour, IDragHandler
    {
        public void OnDrag(PointerEventData e)
        {
            if (Game.Root != null && Game.Root.Doll != null) Game.Root.Doll.Rotate(-e.delta.x * 0.5f);
        }
    }

    // ====================================================================== Inventario y personaje

    public sealed class InventoryScreen : UIScreen
    {
        private enum Tab { Objetos, Personaje }
        private Tab _tab = Tab.Objetos;
        private RectTransform _itemsPage, _charPage;
        private RectTransform _grid, _slots, _details, _attrs, _skills, _filters;
        private Text _summary;
        private ItemCategory? _filter;
        private ItemStack _selected;
        private EquipSlot _selectedSlot = EquipSlot.Ninguna;
        private Button _tabItems, _tabChar;
        private RawImage _doll, _doll2;

        private static readonly Dictionary<string, string> ItemResonances = new Dictionary<string, string> { { "cuenta_boro", "res_boro_cuenta" } };

        public override void Build(RectTransform parent)
        {
            var w = Window(parent, "Inventario", new Vector2(1720f, 940f), "Inventario");
            _tabItems = U.Btn(w, "Objetos", () => { _tab = Tab.Objetos; Refresh(); }, 22);
            U.Place((RectTransform)_tabItems.transform, U.TopLeft, new Vector2(330f, -20f), new Vector2(200f, 46f));
            _tabChar = U.Btn(w, "Personaje", () => { _tab = Tab.Personaje; Refresh(); }, 22);
            U.Place((RectTransform)_tabChar.transform, U.TopLeft, new Vector2(540f, -20f), new Vector2(200f, 46f));
            _summary = U.Txt(w, "Resumen", "", 18, U.Dim, TextAnchor.MiddleRight);
            U.Place(_summary.rectTransform, U.TopRight, new Vector2(-80f, -26f), new Vector2(820f, 34f));

            // --- Página de objetos
            _itemsPage = U.Rect(w, "PaginaObjetos");
            U.Stretch(_itemsPage, 24f, 24f, 84f, 24f);
            var left = U.PanelImg(_itemsPage, "Nilo", new Color(0f, 0f, 0f, 0.25f));
            U.Place(left.rectTransform, U.TopLeft, Vector2.zero, new Vector2(400f, 832f));
            _doll = BuildDoll(left.rectTransform, new Vector2(380f, 470f));
            _slots = U.Rect(left.rectTransform, "Ranuras");
            U.Place(_slots, U.TopLeft, new Vector2(10f, -480f), new Vector2(380f, 340f));
            U.VLayout(_slots, 4f);

            _filters = U.Rect(_itemsPage, "Filtros");
            U.Place(_filters, U.TopLeft, new Vector2(420f, 0f), new Vector2(700f, 92f));
            U.Grid(_filters, new Vector2(170f, 40f), new Vector2(6f, 6f), 4);
            AddFilter("Todo", null);
            AddFilter("Consumibles", ItemCategory.Consumible);
            AddFilter("Materiales", ItemCategory.Material);
            AddFilter("Armas", ItemCategory.Arma);
            AddFilter("Armaduras", ItemCategory.Armadura);
            AddFilter("Accesorios", ItemCategory.Accesorio);
            AddFilter("Herramientas", ItemCategory.Herramienta);
            AddFilter("Recuerdos", ItemCategory.Narrativo);

            var gridHolder = U.PanelImg(_itemsPage, "Objetos", new Color(0f, 0f, 0f, 0.25f));
            U.Place(gridHolder.rectTransform, U.TopLeft, new Vector2(420f, -100f), new Vector2(700f, 732f));
            _grid = U.Scroll(gridHolder.transform, "Lista", out _, 6f, 8);
            U.Stretch((RectTransform)_grid.parent);
            UnityEngine.Object.Destroy(_grid.GetComponent<VerticalLayoutGroup>());
            U.Grid(_grid, new Vector2(126f, 136f), new Vector2(8f, 8f), 5);
            _grid.GetComponent<GridLayoutGroup>().padding = new RectOffset(10, 10, 10, 10);

            var det = U.PanelImg(_itemsPage, "Detalles", new Color(0f, 0f, 0f, 0.3f));
            U.Place(det.rectTransform, U.TopLeft, new Vector2(1140f, 0f), new Vector2(532f, 832f));
            _details = U.Scroll(det.transform, "Detalle", out _, 10f, 18);
            U.Stretch((RectTransform)_details.parent);

            // --- Página de personaje
            _charPage = U.Rect(w, "PaginaPersonaje");
            U.Stretch(_charPage, 24f, 24f, 84f, 24f);
            var left2 = U.PanelImg(_charPage, "Nilo", new Color(0f, 0f, 0f, 0.25f));
            U.Place(left2.rectTransform, U.TopLeft, Vector2.zero, new Vector2(400f, 832f));
            _doll2 = BuildDoll(left2.rectTransform, new Vector2(380f, 600f));
            var bio = U.Txt(left2.transform, "Bio", "Nilo · 12 años · Tribu del Reverso\nCurioso, valiente, despistado, empático e impulsivo. La túnica morada se la regaló la abuela Gina.", 17, U.Dim);
            U.Place(bio.rectTransform, U.TopLeft, new Vector2(14f, -620f), new Vector2(372f, 200f));
            var attrPanel = U.PanelImg(_charPage, "Atributos", new Color(0f, 0f, 0f, 0.3f));
            U.Place(attrPanel.rectTransform, U.TopLeft, new Vector2(420f, 0f), new Vector2(520f, 832f));
            _attrs = U.Scroll(attrPanel.transform, "Lista", out _, 8f, 16);
            U.Stretch((RectTransform)_attrs.parent);
            var skillPanel = U.PanelImg(_charPage, "Habilidades", new Color(0f, 0f, 0f, 0.3f));
            U.Place(skillPanel.rectTransform, U.TopLeft, new Vector2(960f, 0f), new Vector2(712f, 832f));
            _skills = U.Scroll(skillPanel.transform, "Lista", out _, 8f, 16);
            U.Stretch((RectTransform)_skills.parent);
        }

        private RawImage BuildDoll(RectTransform parent, Vector2 size)
        {
            var r = U.Rect(parent, "Vista");
            U.Place(r, U.Top, new Vector2(0f, -10f), size);
            var img = r.gameObject.AddComponent<RawImage>();
            img.raycastTarget = true;
            r.gameObject.AddComponent<DollDrag>();
            var hint = U.Txt(r, "Arrastra", "Arrastra para girar", 14, U.Dim, TextAnchor.LowerCenter);
            U.Place(hint.rectTransform, U.Bottom, Vector2.zero, new Vector2(size.x, 22f));
            return img;
        }

        private void AddFilter(string label, ItemCategory? cat)
        {
            U.Btn(_filters, label, () => { _filter = cat; Refresh(); }, 18);
        }

        public override void OnOpen()
        {
            var doll = Game.Root.Doll;
            if (doll != null) { doll.SetActive(true); _doll.texture = doll.Texture; _doll2.texture = doll.Texture; }
            Game.Quests?.Tutorial("inventario");
            Refresh();
        }

        public override void OnClose()
        {
            Game.Root.Doll?.SetActive(false);
            _selected = null;
        }

        public void Refresh()
        {
            if (Game.State == null) return;
            _itemsPage.gameObject.SetActive(_tab == Tab.Objetos);
            _charPage.gameObject.SetActive(_tab == Tab.Personaje);
            _tabItems.image.color = _tab == Tab.Objetos ? U.ButtonHover : U.ButtonColor;
            _tabChar.image.color = _tab == Tab.Personaje ? U.ButtonHover : U.ButtonColor;
            var s = Game.State;
            _summary.text = $"Nivel {s.Progression.Level} · Salud {Mathf.CeilToInt(s.Health.Current)}/{Mathf.RoundToInt(s.Health.Max)} · Ataque {s.Stats.Attack:0.#} · Defensa {s.Stats.Defense:0.#}";
            Game.Root.Doll?.Refresh();
            if (_tab == Tab.Objetos) { RefreshSlots(); RefreshGrid(); RefreshDetails(); }
            else { RefreshAttributes(); RefreshSkills(); }
        }

        // ------------------------------------------------------------------ Objetos

        private void RefreshSlots()
        {
            U.Clear(_slots);
            var s = Game.State;
            foreach (var slot in Equipment.AllSlots)
            {
                var st = s.Equipment.Get(slot);
                var def = st != null ? GameDb.Item(st.ItemId) : null;
                var slotCopy = slot;
                string label = Names.Slot(slot) + ": " + (def != null ? U.Colored(def.Name + (st.HasUpgrade ? " ★" : ""), U.TextColor) : U.Colored("vacío", U.Dim));
                var b = U.Btn(_slots, label, () => { _selected = st; _selectedSlot = slotCopy; RefreshDetails(); }, 17,
                    _selectedSlot == slot && _selected == st && st != null ? U.ButtonHover : new Color(0.12f, 0.1f, 0.14f, 0.9f));
                U.Label(b).alignment = TextAnchor.MiddleLeft;
                U.Size(b, -1f, 42f);
            }
        }

        private void RefreshGrid()
        {
            U.Clear(_grid);
            var s = Game.State;
            foreach (var st in s.Inventory.Stacks)
            {
                var def = GameDb.Item(st.ItemId);
                if (def == null) continue;
                if (_filter.HasValue)
                {
                    var f = _filter.Value;
                    bool match = def.Category == f || (f == ItemCategory.Narrativo && def.Category == ItemCategory.Cultural);
                    if (!match) continue;
                }
                var stack = st;
                var cell = U.Btn(_grid, "", () => { _selected = stack; _selectedSlot = EquipSlot.Ninguna; RefreshDetails(); }, 14,
                    _selected == st ? U.ButtonHover : new Color(0.15f, 0.12f, 0.17f, 0.95f));
                var icon = U.Img(cell.transform, "Icono", Color.white, ItemIcons.Get(def));
                U.Place(icon.rectTransform, U.Top, new Vector2(0f, -10f), new Vector2(72f, 72f));
                var name = U.Txt(cell.transform, "Nombre", def.Name, 14, U.TextColor, TextAnchor.UpperCenter);
                U.Place(name.rectTransform, U.Bottom, new Vector2(0f, 4f), new Vector2(118f, 44f));
                if (st.Count > 1)
                {
                    var c = U.Outlined(U.Txt(cell.transform, "Cantidad", "×" + st.Count, 16, Color.white, TextAnchor.UpperRight, FontStyle.Bold));
                    U.Place(c.rectTransform, U.TopRight, new Vector2(-6f, -4f), new Vector2(60f, 22f));
                }
                if (st.HasUpgrade)
                {
                    var c = U.Txt(cell.transform, "Mejora", "★", 18, Pal.Gold, TextAnchor.UpperLeft);
                    U.Place(c.rectTransform, U.TopLeft, new Vector2(6f, -4f), new Vector2(30f, 22f));
                }
                int quick = Array.IndexOf(s.QuickSlots, def.Id);
                if (quick >= 0)
                {
                    var q = U.Txt(cell.transform, "Rapido", (quick + 1).ToString(), 15, Pal.ResonanceSoft, TextAnchor.UpperLeft, FontStyle.Bold);
                    U.Place(q.rectTransform, U.TopLeft, new Vector2(8f, -4f), new Vector2(30f, 22f));
                }
            }
            if (s.Inventory.Stacks.Count == 0) U.Para(_grid, "La bolsa está vacía.", 18, U.Dim);
        }

        private void RefreshDetails()
        {
            U.Clear(_details);
            var s = Game.State;
            if (_selected == null)
            {
                U.Para(_details, "Selecciona un objeto para ver su descripción, su origen y su función.\n\nLos objetos culturales y los recuerdos se registran también en el cuaderno.", 19, U.Dim);
                return;
            }
            var def = GameDb.Item(_selected.ItemId);
            if (def == null) return;
            bool equipped = _selectedSlot != EquipSlot.Ninguna;

            var head = U.Rect(_details, "Cabecera");
            U.Size(head, -1f, 96f);
            var icon = U.Img(head, "Icono", Color.white, ItemIcons.Get(def));
            U.Place(icon.rectTransform, U.TopLeft, Vector2.zero, new Vector2(90f, 90f));
            var nm = U.Txt(head, "Nombre", def.Name + (_selected.HasUpgrade ? " ★" : ""), 26, U.Accent, TextAnchor.UpperLeft, FontStyle.Bold);
            U.Place(nm.rectTransform, U.TopLeft, new Vector2(104f, 0f), new Vector2(390f, 60f));
            var cat = U.Txt(head, "Categoria", Names.Category(def.Category) + (def.IsEquippable ? " · " + Names.Slot(def.Slot) : "") + (def.Unique ? " · único" : ""), 17, U.Dim);
            U.Place(cat.rectTransform, U.TopLeft, new Vector2(104f, -62f), new Vector2(390f, 30f));

            U.Para(_details, def.Description, 19);
            if (!string.IsNullOrEmpty(def.Origin)) U.Para(_details, U.Colored("Origen: ", U.Accent) + def.Origin, 17);
            if (!string.IsNullOrEmpty(def.Function)) U.Para(_details, U.Colored("Función: ", U.Accent) + def.Function, 17);
            string mods = def.Mods.Describe();
            if (!string.IsNullOrEmpty(mods)) U.Para(_details, U.Colored("Efectos: ", U.Accent) + mods, 17);
            if (def.IsConsumable)
            {
                var parts = new List<string>();
                if (def.HealAmount > 0) parts.Add($"Cura {def.HealAmount:0} en {def.HealDuration:0.#} s");
                if (def.RegenPerSecond > 0) parts.Add($"Regenera {def.RegenPerSecond:0.#}/s durante {def.RegenDuration:0} s");
                if (def.StaminaRegenBuff > 0) parts.Add($"Aguante +{def.StaminaRegenBuff * 100f:0} %");
                if (parts.Count > 0) U.Para(_details, U.Colored("Consumo: ", U.Accent) + string.Join(" · ", parts), 17);
            }
            if (_selected.HasUpgrade)
            {
                foreach (var up in def.Upgrades)
                    if (up.Id == _selected.UpgradeId) U.Para(_details, U.Colored("Mejora: ", Pal.Gold) + up.Name + " — " + up.Mods.Describe(), 17);
            }
            else if (def.Upgrades.Count > 0) U.Para(_details, U.Colored("Mejoras posibles (exclusivas): ", U.Dim) + string.Join(", ", def.Upgrades.ConvertAll(u => u.Name)) + ". Se aplican en una mesa de trabajo.", 16, U.Dim);

            // Comparación con lo equipado
            if (def.IsEquippable && !equipped)
            {
                var slot = s.Equipment.ChooseSlot(def);
                var cur = s.Equipment.Get(slot);
                var cdef = cur != null ? GameDb.Item(cur.ItemId) : null;
                U.Para(_details, U.Colored("Comparado con lo equipado (" + (cdef != null ? cdef.Name : "nada") + "): ", U.Accent) + Compare(def, cdef), 17);
            }

            U.Separator(_details);
            var actions = U.Rect(_details, "Acciones");
            U.VLayout(actions, 8f);
            if (equipped)
            {
                Act(actions, "Quitar", () =>
                {
                    if (s.Equipment.Unequip(_selectedSlot, s.Inventory)) { s.RecomputeStats(); Game.Audio?.Play2D("recoger", 0.5f); }
                    _selected = null; _selectedSlot = EquipSlot.Ninguna;
                    Refresh();
                });
            }
            else
            {
                if (def.IsEquippable)
                    Act(actions, "Equipar", () =>
                    {
                        if (s.Equipment.Equip(_selected, s.Inventory, GameDb.Item))
                        {
                            s.RecomputeStats();
                            Game.Audio?.Play2D("recoger", 0.6f, 0.8f);
                            if (def.Slot == EquipSlot.Arma) Game.Ctx.SetFlag("espada_equipada");
                            Game.MarkDirty();
                        }
                        _selected = null;
                        Refresh();
                    });
                if (def.IsConsumable)
                {
                    Act(actions, "Usar", () =>
                    {
                        Game.UI.Close(this);
                        Game.Vitals.UseItem(def.Id);
                    });
                    var quick = U.Rect(actions, "Rapido");
                    U.Size(quick, -1f, 44f);
                    U.HLayout(quick, 6f, 0, TextAnchor.MiddleLeft, true);
                    var lbl = U.Txt(quick, "Etiqueta", "Ranura rápida:", 17, U.Dim, TextAnchor.MiddleLeft);
                    U.Size(lbl, 140f, 40f);
                    for (int i = 0; i < s.QuickSlots.Length; i++)
                    {
                        int idx = i;
                        var b = U.Btn(quick, (i + 1).ToString(), () => { s.QuickSlots[idx] = def.Id; Game.Vitals.QuickIndex = idx; Refresh(); }, 18,
                            s.QuickSlots[i] == def.Id ? U.ButtonHover : U.ButtonColor);
                        U.Size(b, -1f, 40f);
                    }
                }
                if (ItemResonances.TryGetValue(def.Id, out var res))
                    Act(actions, "Escuchar", () =>
                    {
                        Game.UI.Close(this);
                        var p = Game.Player.transform.position + Game.Player.transform.forward * 1.2f + Vector3.up * 1.2f;
                        Game.Resonance.Request(res, p, null);
                    });
            }
        }

        private static string Compare(ItemDef a, ItemDef b)
        {
            var parts = new List<string>();
            void Cmp(string name, float va, float vb, bool pct)
            {
                float d = va - vb;
                if (Mathf.Abs(d) < 0.001f) return;
                string v = pct ? (d * 100f).ToString("+0;-0") + " %" : d.ToString("+0.#;-0.#");
                parts.Add(U.Colored(name + " " + v, d > 0 ? U.Good : U.Bad));
            }
            var ma = a.Mods;
            var mb = b != null ? b.Mods : new StatMods();
            Cmp("Ataque", ma.Attack, mb.Attack, false);
            Cmp("Defensa", ma.Defense, mb.Defense, false);
            Cmp("Vel. ataque", ma.AttackSpeed, mb.AttackSpeed, true);
            Cmp("Res. física", ma.ResistPhysical, mb.ResistPhysical, true);
            Cmp("Res. Resonancia", ma.ResistResonance, mb.ResistResonance, true);
            Cmp("Movilidad", ma.MoveSpeed, mb.MoveSpeed, true);
            Cmp("Salud máx.", ma.MaxHealth, mb.MaxHealth, false);
            for (int i = 0; i < 5; i++) Cmp(Names.Attribute((AttributeType)i), ma.Attributes[i], mb.Attributes[i], false);
            return parts.Count > 0 ? string.Join(" · ", parts) : "sin diferencias";
        }

        private static void Act(RectTransform parent, string label, Action a) => U.Size(U.Btn(parent, label, a, 20), -1f, 46f);

        // ------------------------------------------------------------------ Personaje

        private void RefreshAttributes()
        {
            U.Clear(_attrs);
            var s = Game.State;
            var p = s.Progression;
            U.Header(_attrs, "Atributos");
            U.Para(_attrs, $"Nivel {p.Level} · {p.Xp}/{p.XpForNextLevel} XP · Puntos de atributo: {U.Colored(p.AttributePoints.ToString(), p.AttributePoints > 0 ? Pal.ResonanceSoft : U.Dim)}", 18);
            foreach (AttributeType a in Enum.GetValues(typeof(AttributeType)))
            {
                var row = U.Rect(_attrs, "Atributo");
                U.Size(row, -1f, 76f);
                var name = U.Txt(row, "Nombre", Names.Attribute(a) + "  " + U.Colored(s.Stats.Attr(a).ToString(), U.Accent) + (s.Stats.Attr(a) != p.Get(a) ? U.Colored("  (base " + p.Get(a) + ")", U.Dim) : ""), 21, U.TextColor, TextAnchor.UpperLeft, FontStyle.Bold);
                U.Place(name.rectTransform, U.TopLeft, Vector2.zero, new Vector2(380f, 30f));
                var help = U.Txt(row, "Ayuda", Names.AttributeHelp(a), 15, U.Dim);
                U.Place(help.rectTransform, U.TopLeft, new Vector2(0f, -30f), new Vector2(390f, 46f));
                var attr = a;
                var b = U.Btn(row, "+", () =>
                {
                    if (p.SpendAttributePoint(attr)) { s.RecomputeStats(); Game.Audio?.Play2D("nivel", 0.4f, 1.4f); }
                    Refresh();
                }, 26);
                U.Place((RectTransform)b.transform, U.TopRight, Vector2.zero, new Vector2(52f, 52f));
                b.interactable = p.AttributePoints > 0 && p.Get(a) < Progression.MaxAttribute;
            }
            U.Separator(_attrs);
            U.Header(_attrs, "Estadísticas");
            var st = s.Stats;
            U.Para(_attrs,
                $"Salud máxima: {st.MaxHealth:0}\nAguante máximo: {st.MaxStamina:0} (recuperación {st.StaminaRegen:0.#}/s)\n" +
                $"Ataque: {st.Attack:0.#} ({(st.AttackType == DamageType.Resonancia ? "Resonancia" : "físico")}) · velocidad ×{st.AttackSpeed:0.##}\n" +
                $"Defensa: {st.Defense:0.#} · Res. física {st.ResistPhysical * 100f:0} % · Res. Resonancia {st.ResistResonance * 100f:0} %\n" +
                $"Movilidad ×{st.MoveSpeedMult:0.##} · Detección ×{st.DetectionMult:0.##} · Alcance de la Escucha ×{st.ResonanceRangeMult:0.##}", 17);
        }

        private void RefreshSkills()
        {
            U.Clear(_skills);
            var s = Game.State;
            var p = s.Progression;
            U.Header(_skills, "Habilidades");
            U.Para(_skills, "Puntos de habilidad: " + U.Colored(p.SkillPoints.ToString(), p.SkillPoints > 0 ? Pal.ResonanceSoft : U.Dim) + ". Cada árbol tiene tres niveles: para el nivel 2 necesitas 1 habilidad del árbol y para el 3, dos.", 17, U.Dim);
            foreach (var tree in new[] { "Explorador", "Superviviente", "Resonancia" })
            {
                U.Separator(_skills);
                U.Header(_skills, tree, 22);
                var list = new List<SkillDef>();
                foreach (var sd in GameDb.Skills.Values) if (sd.Tree == tree) list.Add(sd);
                list.Sort((x, y) => x.Tier != y.Tier ? x.Tier.CompareTo(y.Tier) : string.Compare(x.Name, y.Name, StringComparison.Ordinal));
                foreach (var sd in list)
                {
                    bool has = s.Skills.Has(sd.Id);
                    bool can = s.Skills.CanUnlock(sd, p, GameDb.Skills, out var reason);
                    var row = U.Rect(_skills, "Habilidad");
                    U.Size(row, -1f, 78f);
                    var title = U.Txt(row, "Nombre", $"{sd.Name}  " + U.Colored($"Nv. {sd.Tier} · coste {sd.Cost}{(sd.Active ? " · activa" : "")}", U.Dim), 19,
                        has ? Pal.ResonanceSoft : U.TextColor, TextAnchor.UpperLeft, FontStyle.Bold);
                    U.Place(title.rectTransform, U.TopLeft, Vector2.zero, new Vector2(500f, 28f));
                    var desc = U.Txt(row, "Descripcion", sd.Description, 15, U.Dim);
                    U.Place(desc.rectTransform, U.TopLeft, new Vector2(0f, -28f), new Vector2(500f, 50f));
                    var skill = sd;
                    var b = U.Btn(row, has ? "Aprendida" : can ? "Aprender" : "Bloqueada", () =>
                    {
                        if (s.Skills.Unlock(skill, p, GameDb.Skills)) { s.RecomputeStats(); Game.Audio?.Play2D("nivel", 0.5f, 1.2f); Game.Ctx.Notify("Habilidad aprendida: " + skill.Name); }
                        Refresh();
                    }, 17, has ? new Color(0.3f, 0.22f, 0.45f, 0.95f) : U.ButtonColor);
                    U.Place((RectTransform)b.transform, U.TopRight, Vector2.zero, new Vector2(150f, 44f));
                    b.interactable = !has && can;
                    if (!has && !can && !string.IsNullOrEmpty(reason))
                    {
                        var r = U.Txt(row, "Motivo", reason, 13, U.Bad, TextAnchor.UpperRight);
                        U.Place(r.rectTransform, U.TopRight, new Vector2(0f, -48f), new Vector2(170f, 30f));
                    }
                }
            }
        }

        public override void Tick(float dt)
        {
            if (_tab == Tab.Objetos && _selected != null && !_selectedSlotValid()) { _selected = null; RefreshDetails(); }
        }

        private bool _selectedSlotValid()
        {
            var s = Game.State;
            if (_selectedSlot != EquipSlot.Ninguna) return s.Equipment.Get(_selectedSlot) == _selected;
            foreach (var st in s.Inventory.Stacks) if (st == _selected) return true;
            return false;
        }
    }
}
