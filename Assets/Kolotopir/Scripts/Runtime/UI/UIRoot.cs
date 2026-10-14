using System;
using System.Collections;
using System.Collections.Generic;
using Kolotopir.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kolotopir
{
    /// <summary>Pantalla de la interfaz (inventario, cuaderno, menús...). Se apilan en <see cref="UIRoot"/>.</summary>
    public abstract class UIScreen
    {
        public RectTransform Root;
        public bool IsOpen => Root != null && Root.gameObject.activeSelf;
        /// <summary>Si detiene el tiempo del juego mientras está abierta.</summary>
        public virtual bool Pauses => true;
        public abstract void Build(RectTransform parent);
        public virtual void OnOpen() { }
        public virtual void OnClose() { }
        public virtual void Tick(float dt) { }
        /// <summary>Atrás/Escape: devuelve true si la pantalla lo gestiona (p. ej. cerrar un submenú).</summary>
        public virtual bool Back() => false;

        protected RectTransform Window(RectTransform parent, string name, Vector2 size, string title, bool dim = true)
        {
            Root = U.Rect(parent, name);
            U.Stretch(Root);
            if (dim)
            {
                var d = U.Img(Root, "Atenuado", new Color(0f, 0f, 0f, 0.55f));
                U.Stretch(d.rectTransform);
                d.raycastTarget = true;
            }
            var w = U.PanelImg(Root, "Ventana", U.Panel);
            U.Place(w.rectTransform, U.Center, Vector2.zero, size);
            if (!string.IsNullOrEmpty(title))
            {
                var t = U.Txt(w.transform, "Titulo", title, 30, U.Accent, TextAnchor.UpperLeft, FontStyle.Bold);
                U.Place(t.rectTransform, U.TopLeft, new Vector2(28f, -18f), new Vector2(size.x - 160f, 44f));
                var close = U.Btn(w.transform, "✕", () => Game.UI.Close(this), 24);
                U.Place((RectTransform)close.transform, U.TopRight, new Vector2(-14f, -14f), new Vector2(46f, 42f));
            }
            Root.gameObject.SetActive(false);
            return w.rectTransform;
        }
    }

    /// <summary>
    /// Raíz de la interfaz: lienzo, HUD, diálogo, pila de pantallas, ventanas modales, fundidos y cursor.
    /// Todo se construye en código (uGUI) para no depender de recursos externos.
    /// </summary>
    public sealed class UIRoot : MonoBehaviour
    {
        public Canvas Canvas { get; private set; }
        public Hud Hud { get; private set; }
        public DialogueView DialogueView { get; private set; }

        private RectTransform _rootRect, _hudLayer, _dialogueLayer, _screenLayer, _modalLayer, _topLayer;
        private readonly List<UIScreen> _stack = new List<UIScreen>();
        private readonly List<UIScreen> _all = new List<UIScreen>();

        public ConfirmScreen ConfirmS;
        public ExamineScreen ExamineS;
        public PauseScreen PauseS;
        public RestScreen RestS;
        public SettingsScreen SettingsS;
        public MainMenuScreen MainMenuS;
        public EndingScreen EndingS;
        public InventoryScreen InventoryS;
        public JournalScreen JournalS;
        public CraftingScreen CraftingS;
        public BarterScreen BarterS;
        public CapsuleScreen CapsuleS;

        private Image _fade;
        private Text _fadeText;
        private RectTransform _loading;
        private Image _loadingFill;
        private Text _loadingText, _loadingTip;
        private float _fadeAlpha;
        private bool _cursorFree = true;

        public float FadeAlpha => _fadeAlpha;
        public bool Loading => _loading != null && _loading.gameObject.activeSelf;

        public bool AnyScreen => _stack.Count > 0;
        public UIScreen Top => _stack.Count > 0 ? _stack[_stack.Count - 1] : null;
        public bool ModalOpen => _stack.Count > 0;
        public bool Paused
        {
            get
            {
                foreach (var s in _stack) if (s.Pauses) return true;
                return false;
            }
        }

        /// <summary>Si la interfaz impide mover a Nilo, atacar o interactuar.</summary>
        public bool BlocksGameplay => _stack.Count > 0 || (Game.Dialogue != null && Game.Dialogue.Active) || _fadeAlpha > 0.6f || Loading || Time.frameCount <= _holdUntilFrame;

        private int _holdUntilFrame = -1;

        /// <summary>
        /// Tras cerrar una pantalla o un diálogo, la misma pulsación (E, clic) no debe llegar al juego
        /// en ese fotograma ni en el siguiente (evita reabrir el diálogo o lanzar un tajo).
        /// </summary>
        public void HoldInput() => _holdUntilFrame = Time.frameCount + 1;
        public bool HideViewModel => _stack.Count > 0 || (Game.Dialogue != null && Game.Dialogue.Active) || (Game.Cam != null && Game.Cam.Scoping);

        public static UIRoot Create()
        {
            var go = new GameObject("Interfaz");
            DontDestroyOnLoad(go);
            var ui = go.AddComponent<UIRoot>();
            ui.Build();
            return ui;
        }

        private void Build()
        {
            if (FindAnyEventSystem() == null)
            {
                var es = new GameObject("EventSystem");
                es.transform.SetParent(transform, false);
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            var canvasGo = new GameObject("Lienzo", typeof(RectTransform));
            canvasGo.layer = 5;
            canvasGo.transform.SetParent(transform, false);
            Canvas = canvasGo.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.sortingOrder = 10;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            _rootRect = (RectTransform)canvasGo.transform;

            _hudLayer = U.Stretch(U.Rect(_rootRect, "CapaHUD"));
            _dialogueLayer = U.Stretch(U.Rect(_rootRect, "CapaDialogo"));
            _screenLayer = U.Stretch(U.Rect(_rootRect, "CapaPantallas"));
            _modalLayer = U.Stretch(U.Rect(_rootRect, "CapaModal"));
            _topLayer = U.Stretch(U.Rect(_rootRect, "CapaSuperior"));

            Hud = new Hud();
            Hud.Build(_hudLayer);
            DialogueView = new DialogueView();
            DialogueView.Build(_dialogueLayer);
            BuildScreens();
            BuildOverlay();
        }

        private static EventSystem FindAnyEventSystem() => EventSystem.current;

        private void BuildScreens()
        {
            foreach (var s in _all) if (s.Root != null) Destroy(s.Root.gameObject);
            _all.Clear();
            _stack.Clear();
            MainMenuS = Add(new MainMenuScreen(), _screenLayer);
            PauseS = Add(new PauseScreen(), _screenLayer);
            RestS = Add(new RestScreen(), _screenLayer);
            InventoryS = Add(new InventoryScreen(), _screenLayer);
            JournalS = Add(new JournalScreen(), _screenLayer);
            CraftingS = Add(new CraftingScreen(), _screenLayer);
            BarterS = Add(new BarterScreen(), _screenLayer);
            CapsuleS = Add(new CapsuleScreen(), _screenLayer);
            EndingS = Add(new EndingScreen(), _screenLayer);
            SettingsS = Add(new SettingsScreen(), _modalLayer);
            ExamineS = Add(new ExamineScreen(), _modalLayer);
            ConfirmS = Add(new ConfirmScreen(), _modalLayer);
        }

        private T Add<T>(T screen, RectTransform layer) where T : UIScreen
        {
            screen.Build(layer);
            if (screen.Root != null) screen.Root.gameObject.SetActive(false);
            _all.Add(screen);
            return screen;
        }

        private void BuildOverlay()
        {
            _loading = U.Rect(_topLayer, "Carga");
            U.Stretch(_loading);
            var bg = U.Img(_loading, "Fondo", new Color(0.05f, 0.04f, 0.07f, 1f));
            U.Stretch(bg.rectTransform);
            bg.raycastTarget = true;
            var title = U.Txt(_loading, "Titulo", "KOLOTOPIR", 64, U.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            U.Place(title.rectTransform, U.Center, new Vector2(0f, 120f), new Vector2(1200f, 90f));
            _loadingText = U.Txt(_loading, "Texto", "", 24, U.TextColor, TextAnchor.MiddleCenter);
            U.Place(_loadingText.rectTransform, U.Center, new Vector2(0f, 20f), new Vector2(1200f, 40f));
            _loadingFill = U.Bar(_loading, "Progreso", Pal.Resonance, out var br);
            U.Place(br, U.Center, new Vector2(0f, -30f), new Vector2(640f, 12f));
            _loadingTip = U.Txt(_loading, "Consejo", "", 20, U.Dim, TextAnchor.UpperCenter, FontStyle.Italic);
            U.Place(_loadingTip.rectTransform, U.Center, new Vector2(0f, -70f), new Vector2(1100f, 120f));
            _loading.gameObject.SetActive(false);

            _fade = U.Img(_topLayer, "Fundido", new Color(0f, 0f, 0f, 0f));
            U.Stretch(_fade.rectTransform);
            _fadeText = U.Txt(_topLayer, "TextoFundido", "", 34, new Color(0.95f, 0.9f, 0.85f, 0f), TextAnchor.MiddleCenter, FontStyle.Italic);
            U.Stretch(_fadeText.rectTransform, 200f, 200f, 0f, 0f);
        }

        // ------------------------------------------------------------------ Pila de pantallas

        public void Open(UIScreen s)
        {
            if (s == null) return;
            if (_stack.Contains(s)) { _stack.Remove(s); _stack.Add(s); s.Root.SetAsLastSibling(); return; }
            _stack.Add(s);
            s.Root.gameObject.SetActive(true);
            s.Root.SetAsLastSibling();
            s.OnOpen();
            Game.Audio?.Play2D("ui_abrir", 0.35f);
        }

        public void Close(UIScreen s)
        {
            if (s == null || !_stack.Contains(s)) return;
            _stack.Remove(s);
            s.Root.gameObject.SetActive(false);
            s.OnClose();
            HoldInput();
            Game.Audio?.Play2D("ui_cerrar", 0.3f);
        }

        public void CloseAll()
        {
            for (int i = _stack.Count - 1; i >= 0; i--) Close(_stack[i]);
        }

        public bool IsOpen(UIScreen s) => _stack.Contains(s);

        public void Toggle(UIScreen s)
        {
            if (IsOpen(s)) Close(s);
            else
            {
                // Solo una pantalla de juego a la vez (inventario, cuaderno...)
                for (int i = _stack.Count - 1; i >= 0; i--)
                    if (_stack[i] != MainMenuS && _stack[i] != EndingS) Close(_stack[i]);
                Open(s);
            }
        }

        public bool PointerOverButton()
        {
            var es = EventSystem.current;
            if (es == null || !es.IsPointerOverGameObject()) return false;
            var data = new PointerEventData(es) { position = Input.mousePosition };
            var results = new List<RaycastResult>();
            es.RaycastAll(data, results);
            foreach (var r in results)
                if (r.gameObject.GetComponentInParent<Selectable>() != null) return true;
            return false;
        }

        // ------------------------------------------------------------------ Bucle

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            bool dialogue = Game.Dialogue != null && Game.Dialogue.Active;
            Hud.Tick(dt, !AnyScreen && !dialogue);
            DialogueView.Tick(dt);
            for (int i = _stack.Count - 1; i >= 0; i--) if (i < _stack.Count) _stack[i].Tick(dt);
            HandleKeys(dialogue);
            UpdateCursor(dialogue);
        }

        private void HandleKeys(bool dialogue)
        {
            if (Game.Root == null) return;
            if (Input.GetKeyDown(KeyCode.F3)) Hud.PerformanceVisible = !Hud.PerformanceVisible;
            if (SettingsS.Capturing) return;

            bool back = Input.GetKeyDown(KeyCode.Escape) || InputMap.Down(InputAction.Pausa);
            if (back && _stack.Count > 0)
            {
                var top = Top;
                if (top == MainMenuS && _stack.Count == 1) { if (!top.Back()) { } return; }
                if (top == EndingS) return;
                if (!top.Back()) Close(top);
                return;
            }
            if (!Game.InGame || dialogue || Loading || _fadeAlpha > 0.5f) return;
            if (Game.Resonance != null && Game.Resonance.Playing) return;
            if (back) { Open(PauseS); return; }
            if (_stack.Count > 0 && Top != InventoryS && Top != JournalS) return;
            if (InputMap.Down(InputAction.Inventario)) Toggle(InventoryS);
            else if (InputMap.Down(InputAction.Cuaderno)) { if (IsOpen(JournalS) && JournalS.Tab != JournalTab.Atlas) Close(JournalS); else { JournalS.Tab = JournalTab.Misiones; if (!IsOpen(JournalS)) Toggle(JournalS); else JournalS.Refresh(); } }
            else if (InputMap.Down(InputAction.Mapa)) { if (IsOpen(JournalS) && JournalS.Tab == JournalTab.Atlas) Close(JournalS); else { JournalS.Tab = JournalTab.Atlas; if (!IsOpen(JournalS)) Toggle(JournalS); else JournalS.Refresh(); } }
        }

        private void UpdateCursor(bool dialogue)
        {
            bool free = !Game.InGame || AnyScreen || dialogue || Loading;
            if (free == _cursorFree && Cursor.lockState == (free ? CursorLockMode.None : CursorLockMode.Locked)) return;
            _cursorFree = free;
            Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = free;
        }

        private void OnApplicationFocus(bool focus)
        {
            if (focus) _cursorFree = !_cursorFree; // fuerza a reaplicar el bloqueo al volver a la ventana
        }

        // ------------------------------------------------------------------ Ajustes

        public void ApplySettings(SettingsData d)
        {
            bool rebuild = U.HighContrast != d.HighContrast;
            U.TextScale = d.TextScale;
            U.HighContrast = d.HighContrast;
            foreach (var st in GetComponentsInChildren<ScaledText>(true)) st.Apply(U.TextScale);
            if (rebuild)
            {
                bool menu = IsOpen(MainMenuS);
                bool settings = IsOpen(SettingsS);
                BuildScreens();
                if (menu) Open(MainMenuS);
                if (settings) Open(SettingsS);
            }
        }

        // ------------------------------------------------------------------ Fundidos y carga

        public IEnumerator Fade(float target, float duration, string text = null)
        {
            float start = _fadeAlpha;
            if (text != null) _fadeText.text = text;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                SetFade(Mathf.Lerp(start, target, Mathf.Clamp01(t / Mathf.Max(0.001f, duration))));
                yield return null;
            }
            SetFade(target);
            if (target <= 0f) _fadeText.text = "";
        }

        public void SetFade(float a)
        {
            _fadeAlpha = a;
            _fade.color = new Color(0f, 0f, 0f, a);
            _fade.raycastTarget = a > 0.5f;
            _fadeText.color = new Color(0.95f, 0.9f, 0.85f, Mathf.Clamp01(a * 1.5f - 0.5f));
        }

        private static readonly string[] Tips =
        {
            "En Orom, recordar también es actuar: escucha las Cápsulas y los lugares con la Escucha.",
            "Nilo distingue lo que sabe, lo que ha observado y lo que solo sospecha. Revisa los misterios del cuaderno.",
            "Los kolotapires no comercian con dinero: el trueque y los favores valen más que cualquier moneda.",
            "Los ecos odian el fuego. De noche, quédate cerca de las hogueras y los faroles.",
            "Si te pierdes, pide una pista. Cuantas más veces la pidas, más clara será.",
            "Algunos recuerdos son privados. Escucharlos sin permiso tiene consecuencias.",
            "Los jabalíes de musgo avisan antes de cargar. Aléjate despacio... o haz las paces con unas bayas.",
            "La túnica morada de Nilo se ve desde lejos. La abuela Gina sabe por qué.",
        };

        public void SetLoading(bool visible, float progress = 0f, string text = null)
        {
            if (visible && !_loading.gameObject.activeSelf) _loadingTip.text = Tips[UnityEngine.Random.Range(0, Tips.Length)];
            _loading.gameObject.SetActive(visible);
            if (!visible) return;
            U.SetFill(_loadingFill, progress);
            if (text != null) _loadingText.text = text;
        }

        // ------------------------------------------------------------------ API de juego

        public void SetPrompt(string text) => Hud.SetPrompt(text);
        public void Toast(string text) => Hud.Toast(text);
        public void Subtitle(string speaker, string text, float duration) => Hud.Subtitle(speaker, text, duration);
        public void DamageFlash(float amount, Vector3 from) => Hud.DamageFlash(amount, from);
        public void SoundCue(Vector3 pos, string cue) => Hud.SoundCue(pos, cue);
        public void ShowHint(string quest, string text, int level, int count) => Hud.ShowHint(quest, text, level, count);
        public void ShowTarget(IDamageable d) => Hud.ShowTarget(d);
        public void SetBoss(CreatureBase boss) => Hud.SetBoss(boss);
        public void ResonanceBegin(string title, string type) => Hud.ResonanceBegin(title, type);
        public void ResonanceLine(string speaker, string text) => Hud.ResonanceLine(speaker, text);
        public void ResonanceEnd() => Hud.ResonanceEnd();
        public void Tutorial(string text) => Hud.Tutorial(text);
        public void XpGained(int amount, string reason) => Hud.XpGained(amount, reason);

        public void LevelUp(int level)
        {
            Hud.Banner("Nivel " + level, "Tienes puntos de atributo y de habilidad por repartir (" + InputMap.KeyName(InputAction.Inventario) + " › Personaje)", 4f);
            Game.Quests?.Tutorial("nivel");
        }

        public void ObjectiveCompleted(QuestDef q, QuestStep step)
        {
            Toast("✔ " + Tokens.Apply(step.Text));
        }

        public void ChapterTitle(string title, string subtitle)
        {
            Hud.Banner(title, subtitle, 6f);
            Game.Audio?.Play2D("mision", 0.7f, 0.8f);
        }

        public void LocationDiscovered(LocationDef loc)
        {
            Hud.Banner(loc.Name, loc.Landmark ? "Lugar descubierto" : "", 3.5f);
            Game.Audio?.Play2D("descubrir", 0.6f);
        }

        public void RegionEntered(Region r)
        {
            Hud.Banner(WorldShape.RegionName(r), r == Region.PradosQueRecuerdan ? "Capítulo II" : r == Region.TribuDelReverso ? "Capítulo I" : "", 3.5f);
        }

        public void Confirm(string title, string body, string yes, string no, Action<bool> result)
        {
            ConfirmS.Set(title, body, yes, no, result);
            Open(ConfirmS);
        }

        public void ShowExamine(string title, string text)
        {
            ExamineS.Set(title, text);
            Open(ExamineS);
            Game.Audio?.Play2D("examinar", 0.5f);
        }

        public void OpenCapsule(CapsuleController c)
        {
            CapsuleS.Capsule = c;
            Open(CapsuleS);
        }

        public void OpenRestMenu(RestPointDef def)
        {
            RestS.Point = def;
            Open(RestS);
        }

        public void OpenCrafting(bool atWorkbench)
        {
            CraftingS.AtWorkbench = atWorkbench;
            Open(CraftingS);
        }

        public void OpenBarter(string merchantId)
        {
            var m = GameDb.Merchant(merchantId);
            if (m == null) return;
            BarterS.Merchant = m;
            Open(BarterS);
        }

        public void ShowDialogue(bool show) => DialogueView.Show(show);
        public void DialogueNodeChanged() => DialogueView.NodeChanged();

        public void ShowMainMenu()
        {
            CloseAll();
            Open(MainMenuS);
        }

        public void ShowEnding()
        {
            CloseAll();
            Open(EndingS);
        }
    }

    // ====================================================================== Pantallas pequeñas

    public sealed class ConfirmScreen : UIScreen
    {
        private Text _title, _body;
        private Button _yes, _no;
        private Action<bool> _result;

        public override void Build(RectTransform parent)
        {
            var w = Window(parent, "Confirmar", new Vector2(760f, 380f), null);
            _title = U.Txt(w, "Titulo", "", 30, U.Accent, TextAnchor.UpperLeft, FontStyle.Bold);
            U.Stretch(_title.rectTransform, 32f, 32f, 24f, 300f);
            _body = U.Txt(w, "Texto", "", 21, U.TextColor);
            U.Stretch(_body.rectTransform, 32f, 32f, 80f, 100f);
            _yes = U.Btn(w, "Sí", () => Resolve(true), 22);
            U.Place((RectTransform)_yes.transform, U.BottomRight, new Vector2(-32f, 28f), new Vector2(300f, 54f));
            _no = U.Btn(w, "No", () => Resolve(false), 22);
            U.Place((RectTransform)_no.transform, U.BottomLeft, new Vector2(32f, 28f), new Vector2(300f, 54f));
        }

        public void Set(string title, string body, string yes, string no, Action<bool> result)
        {
            _title.text = title;
            _body.text = body;
            U.Label(_yes).text = yes;
            U.Label(_no).text = no;
            _result = result;
        }

        private void Resolve(bool ok)
        {
            var r = _result;
            _result = null;
            Game.UI.Close(this);
            r?.Invoke(ok);
        }

        public override bool Back()
        {
            Resolve(false);
            return true;
        }
    }

    public sealed class ExamineScreen : UIScreen
    {
        private Text _title, _body;

        public override void Build(RectTransform parent)
        {
            var w = Window(parent, "Examinar", new Vector2(860f, 460f), null);
            _title = U.Txt(w, "Titulo", "", 30, U.Accent, TextAnchor.UpperLeft, FontStyle.Bold);
            U.Stretch(_title.rectTransform, 32f, 32f, 24f, 380f);
            _body = U.Txt(w, "Texto", "", 22, U.TextColor);
            U.Stretch(_body.rectTransform, 32f, 32f, 84f, 90f);
            var ok = U.Btn(w, "Cerrar", () => Game.UI.Close(this), 22);
            U.Place((RectTransform)ok.transform, U.Bottom, new Vector2(0f, 26f), new Vector2(260f, 52f));
        }

        private float _openedAt;

        public void Set(string title, string text) { _title.text = title; _body.text = text; }

        public override void OnOpen() => _openedAt = Time.unscaledTime;

        public override void Tick(float dt)
        {
            if (Time.unscaledTime - _openedAt < 0.25f) return;
            if (InputMap.Down(InputAction.Interactuar) || Input.GetKeyDown(KeyCode.Return)) Game.UI.Close(this);
        }
    }

    public sealed class PauseScreen : UIScreen
    {
        private Text _info;
        private Button _load;

        public override void Build(RectTransform parent)
        {
            Root = U.Rect(parent, "Pausa");
            U.Stretch(Root);
            var d = U.Img(Root, "Atenuado", new Color(0.03f, 0.02f, 0.05f, 0.82f));
            U.Stretch(d.rectTransform);
            d.raycastTarget = true;
            var t = U.Txt(Root, "Titulo", "Pausa", 48, U.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            U.Place(t.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(140f, 300f), new Vector2(600f, 70f));
            var col = U.Rect(Root, "Botones");
            U.Place(col, new Vector2(0f, 0.5f), new Vector2(0f, 1f), new Vector2(140f, 240f), new Vector2(420f, 520f));
            U.VLayout(col, 12f);
            Row(col, "Continuar", () => Game.UI.Close(this));
            Row(col, "Cuaderno", () => { Game.UI.Close(this); Game.UI.JournalS.Tab = JournalTab.Misiones; Game.UI.Open(Game.UI.JournalS); });
            Row(col, "Inventario", () => { Game.UI.Close(this); Game.UI.Open(Game.UI.InventoryS); });
            _load = Row(col, "Cargar última partida", () =>
                Game.UI.Confirm("Cargar partida", "Se perderá el progreso no guardado desde el último guardado.", "Cargar", "Cancelar", ok => { if (ok) Game.Root.ContinueGame(); }));
            Row(col, "Ajustes", () => Game.UI.Open(Game.UI.SettingsS));
            Row(col, "Menú principal", () =>
                Game.UI.Confirm("Volver al menú", "Se perderá el progreso no guardado. Las partidas se guardan automáticamente en los hitos y manualmente en los lugares de descanso.", "Volver al menú", "Cancelar", ok => { if (ok) Game.Root.ReturnToMenu(); }));
            Row(col, "Salir del juego", () => Game.UI.Confirm("Salir", "¿Salir de KOLOTOPIR? El progreso no guardado se perderá.", "Salir", "Cancelar", ok => { if (ok) Game.Root.Quit(); }));
            _info = U.Txt(Root, "Info", "", 19, U.Dim, TextAnchor.LowerLeft);
            U.Place(_info.rectTransform, U.BottomLeft, new Vector2(140f, 60f), new Vector2(1200f, 140f));
            Root.gameObject.SetActive(false);
        }

        private static Button Row(RectTransform col, string label, Action a)
        {
            var b = U.Btn(col, label, a, 24);
            U.Size(b, -1f, 58f);
            return b;
        }

        public override void OnOpen()
        {
            var s = Game.State;
            _load.interactable = SaveSystem.MostRecent() != null;
            if (s == null) { _info.text = ""; return; }
            var q = Game.Quests != null ? Game.Quests.Tracked() : null;
            int minutes = Mathf.RoundToInt(s.PlaytimeSeconds / 60f);
            string region = Game.Player != null ? WorldShape.RegionName(WorldShape.RegionAt(Game.Player.transform.position.x, Game.Player.transform.position.z)) : "";
            _info.text = (s.Chapter == 1 ? "Capítulo I — Tribu del Reverso" : "Capítulo II — Prados que Recuerdan") + " · " + region
                + "\nDía " + s.Clock.Day + ", " + U.Hours(s.Clock.Hour) + " · Nivel " + s.Progression.Level + " · " + minutes + " min de juego"
                + (q != null ? "\nMisión: " + q.Title : "")
                + "\nÚltimo guardado: " + SaveSystem.Describe(SaveSystem.MostRecent() ?? SaveSystem.AutoSlot);
        }
    }

    public sealed class RestScreen : UIScreen
    {
        public RestPointDef Point;
        private Text _title, _info;

        public override void Build(RectTransform parent)
        {
            var w = Window(parent, "Descanso", new Vector2(720f, 640f), " ");
            _title = U.Txt(w, "Lugar", "", 30, U.Accent, TextAnchor.UpperLeft, FontStyle.Bold);
            U.Place(_title.rectTransform, U.TopLeft, new Vector2(28f, -18f), new Vector2(560f, 44f));
            _info = U.Txt(w, "Info", "", 19, U.Dim);
            U.Place(_info.rectTransform, U.TopLeft, new Vector2(28f, -66f), new Vector2(660f, 60f));
            var col = U.Rect(w, "Opciones");
            U.Place(col, U.TopLeft, new Vector2(28f, -130f), new Vector2(664f, 480f));
            U.VLayout(col, 10f);
            Opt(col, "Dormir hasta el amanecer (06:00)", () => Sleep(6f, true));
            Opt(col, "Esperar hasta el mediodía (12:00)", () => Sleep(12f, false));
            Opt(col, "Esperar hasta el atardecer (18:00)", () => Sleep(18f, false));
            Opt(col, "Esperar hasta la noche (22:00)", () => Sleep(22f, false));
            Opt(col, "Descansar una hora", () => Game.Root.Rest(1f, false));
            Opt(col, "Guardar partida", () =>
            {
                if (Game.Root.ManualSave(Point)) Game.UI.Toast("Partida guardada.");
                OnOpen();
            });
            Opt(col, "Levantarse", () => Game.UI.Close(this));
        }

        private static void Opt(RectTransform col, string label, Action a) => U.Size(U.Btn(col, label, a, 22), -1f, 54f);

        private void Sleep(float targetHour, bool sleep)
        {
            float now = Game.State.Clock.Hour;
            float hours = targetHour - now;
            if (hours <= 0.05f) hours += 24f;
            Game.UI.Close(this);
            Game.Root.Rest(hours, sleep);
        }

        public override void OnOpen()
        {
            if (Point == null) return;
            Game.State.LastSafePoint = Point.Id;
            _title.text = Point.Name;
            _info.text = "Día " + Game.State.Clock.Day + ", " + U.Hours(Game.State.Clock.Hour) + ". Lugar seguro: si caes, despertarás aquí.\nGuardado manual: " + SaveSystem.Describe(SaveSystem.ManualSlot);
            Game.Quests?.Tutorial("descanso");
        }
    }
}
