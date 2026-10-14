using System.Collections.Generic;
using Kolotopir.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Kolotopir
{
    /// <summary>
    /// Interfaz en juego: salud, aguante, experiencia, objeto rápido, objetivo, avisos, subtítulos,
    /// Escucha, equilibrio, enemigos, pistas y ayudas de accesibilidad (señales visuales de sonido).
    /// </summary>
    public sealed class Hud
    {
        public RectTransform Root;

        // Vitales
        private Image _health, _healthGhost, _stamina, _xp;
        private Text _healthText, _levelText, _status;
        private float _ghost = 1f;
        private CanvasGroup _vitalsGroup;
        private float _vitalsShowUntil;

        // Objeto rápido
        private Image _quickIcon;
        private Text _quickName, _quickCount, _quickKeys;
        private string _quickShown;

        // Centro
        private Image _crosshair;
        private Text _prompt;
        private RectTransform _listenRoot;
        private Image _listenRing, _listenFill, _signal;
        private Text _listenLabel;
        private RectTransform _balanceRoot;
        private RectTransform _balanceMarker;
        private Image _scope;

        // Objetivo y avisos
        private RectTransform _objectiveRoot;
        private Text _objectiveTitle, _objectiveText;
        private CanvasGroup _objectiveGroup;
        private RectTransform _toastRoot;
        private readonly List<(Text text, CanvasGroup group, float until)> _toasts = new List<(Text, CanvasGroup, float)>();
        private readonly Queue<string> _toastQueue = new Queue<string>();
        private float _toastGap;

        // Subtítulos
        private RectTransform _subRoot;
        private Image _subBg;
        private Text _subText;
        private float _subUntil;

        // Resonancia
        private RectTransform _resRoot;
        private Text _resTitle, _resType;
        private bool _resonating;

        // Enemigos
        private RectTransform _targetRoot;
        private Text _targetName;
        private Image _targetFill;
        private IDamageable _target;
        private float _targetUntil;
        private RectTransform _bossRoot;
        private Text _bossName;
        private Image _bossFill;
        private CreatureBase _boss;

        // Efectos de pantalla
        private Image _damage;
        private float _damageAlpha;
        private RectTransform _damageArrow;
        private Image _damageArrowImg;
        private Vector3 _damageFrom;
        private float _damageArrowAlpha;

        // Banners y paneles
        private Text _banner, _bannerSub;
        private CanvasGroup _bannerGroup;
        private float _bannerUntil;
        private RectTransform _hintRoot;
        private Text _hintTitle, _hintText;
        private CanvasGroup _hintGroup;
        private float _hintUntil;
        private RectTransform _tutorialRoot;
        private Text _tutorialText;
        private CanvasGroup _tutorialGroup;
        private float _tutorialUntil;
        private Text _xpPopup;
        private float _xpPopupUntil;
        private int _xpAccum;
        private Text _clock;

        // Accesibilidad: señales visuales de sonido
        private RectTransform _cueRoot;
        private readonly List<(Text text, Vector3 pos, float until)> _cues = new List<(Text, Vector3, float)>();

        // Rendimiento
        private Text _perf;
        private float _fpsAccum;
        private int _fpsFrames;
        private float _fpsTimer;
        private string _fpsText = "";

        public bool PerformanceVisible;

        public void Build(RectTransform parent)
        {
            Root = U.Rect(parent, "HUD");
            U.Stretch(Root);

            // Efectos a pantalla completa (debajo de todo)
            _scope = U.Img(Root, "Catalejo", Color.black, TextureGen.ScopeMask());
            U.Stretch(_scope.rectTransform, -200f, -200f, 0f, 0f);
            _scope.preserveAspect = true;
            _scope.enabled = false;
            _damage = U.Img(Root, "Danio", new Color(0.7f, 0.05f, 0.05f, 0f), TextureGen.Vignette);
            U.Stretch(_damage.rectTransform);

            BuildVitals();
            BuildQuick();
            BuildCenter();
            BuildObjective();
            BuildToasts();
            BuildSubtitles();
            BuildEnemies();
            BuildBanners();
            _cueRoot = U.Rect(Root, "SenalesSonido");
            U.Stretch(_cueRoot);
            _perf = U.Outlined(U.Txt(Root, "Rendimiento", "", 16, new Color(0.8f, 1f, 0.8f)));
            U.Place(_perf.rectTransform, U.TopRight, new Vector2(-16f, -150f), new Vector2(380f, 220f));
            _perf.alignment = TextAnchor.UpperRight;
        }

        // ------------------------------------------------------------------ Construcción

        private void BuildVitals()
        {
            var v = U.Rect(Root, "Vitales");
            U.Place(v, U.BottomLeft, new Vector2(28f, 28f), new Vector2(420f, 110f));
            _vitalsGroup = v.gameObject.AddComponent<CanvasGroup>();
            _levelText = U.Outlined(U.Txt(v, "Nivel", "Nv. 1", 20, U.Accent, TextAnchor.LowerLeft, FontStyle.Bold));
            U.Place(_levelText.rectTransform, U.TopLeft, new Vector2(0f, 0f), new Vector2(200f, 26f));
            _status = U.Outlined(U.Txt(v, "Estado", "", 17, U.Dim, TextAnchor.LowerRight));
            U.Place(_status.rectTransform, U.TopRight, new Vector2(0f, 0f), new Vector2(240f, 26f));

            var hb = U.Img(v, "SaludFondo", new Color(0f, 0f, 0f, 0.55f), TextureGen.RoundedSmall, true);
            U.Place(hb.rectTransform, U.TopLeft, new Vector2(0f, -30f), new Vector2(420f, 26f));
            _healthGhost = U.Img(hb.transform, "Fantasma", new Color(1f, 0.85f, 0.7f, 0.6f), TextureGen.RoundedSmall, true);
            U.Stretch(_healthGhost.rectTransform, 2f, 2f, 2f, 2f);
            _health = U.Img(hb.transform, "Salud", Pal.Health, TextureGen.RoundedSmall, true);
            U.Stretch(_health.rectTransform, 2f, 2f, 2f, 2f);
            _healthText = U.Outlined(U.Txt(hb.transform, "Valor", "100 / 100", 16, Color.white, TextAnchor.MiddleCenter));
            U.Stretch(_healthText.rectTransform);

            _stamina = U.Bar(v, "Aguante", Pal.Stamina, out var sr);
            U.Place(sr, U.TopLeft, new Vector2(0f, -60f), new Vector2(340f, 14f));
            _xp = U.Bar(v, "Experiencia", Pal.Xp, out var xr);
            U.Place(xr, U.TopLeft, new Vector2(0f, -80f), new Vector2(340f, 8f));
            _xpPopup = U.Outlined(U.Txt(v, "XP", "", 17, Pal.ResonanceSoft, TextAnchor.MiddleLeft));
            U.Place(_xpPopup.rectTransform, U.TopLeft, new Vector2(0f, -90f), new Vector2(420f, 24f));
        }

        private void BuildQuick()
        {
            var q = U.PanelImg(Root, "ObjetoRapido", new Color(0.08f, 0.06f, 0.1f, 0.7f));
            U.Place(q.rectTransform, U.BottomRight, new Vector2(-28f, 28f), new Vector2(300f, 84f));
            _quickIcon = U.Img(q.transform, "Icono", Color.white);
            U.Place(_quickIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(60f, 60f));
            _quickName = U.Txt(q.transform, "Nombre", "", 19, U.TextColor, TextAnchor.UpperLeft, FontStyle.Bold);
            U.Place(_quickName.rectTransform, U.TopLeft, new Vector2(84f, -8f), new Vector2(206f, 26f));
            _quickCount = U.Txt(q.transform, "Cantidad", "", 17, U.Dim, TextAnchor.UpperLeft);
            U.Place(_quickCount.rectTransform, U.TopLeft, new Vector2(84f, -32f), new Vector2(206f, 22f));
            _quickKeys = U.Txt(q.transform, "Teclas", "", 15, U.Dim, TextAnchor.UpperLeft);
            U.Place(_quickKeys.rectTransform, U.TopLeft, new Vector2(84f, -54f), new Vector2(206f, 22f));
            _clock = U.Outlined(U.Txt(Root, "Reloj", "", 18, U.Dim, TextAnchor.LowerRight));
            U.Place(_clock.rectTransform, U.BottomRight, new Vector2(-30f, 118f), new Vector2(300f, 26f));
        }

        private void BuildCenter()
        {
            _crosshair = U.Img(Root, "Mira", new Color(1f, 1f, 1f, 0.75f), TextureGen.Circle);
            U.Place(_crosshair.rectTransform, U.Center, Vector2.zero, new Vector2(6f, 6f));
            _prompt = U.Outlined(U.Txt(Root, "Indicacion", "", 22, Color.white, TextAnchor.UpperCenter));
            U.Place(_prompt.rectTransform, U.Center, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(900f, 60f));

            _listenRoot = U.Rect(Root, "Escucha");
            U.Place(_listenRoot, U.Center, Vector2.zero, new Vector2(150f, 150f));
            _signal = U.Img(_listenRoot, "Senal", K.WithA(Pal.Resonance, 0f), TextureGen.Circle);
            U.Stretch(_signal.rectTransform, -40f, -40f, -40f, -40f);
            _listenRing = U.Img(_listenRoot, "Anillo", K.WithA(Pal.ResonanceSoft, 0.25f), RingSprite());
            U.Stretch(_listenRing.rectTransform);
            _listenFill = U.Img(_listenRoot, "Progreso", Pal.Resonance, RingSprite());
            U.Stretch(_listenFill.rectTransform);
            _listenFill.type = Image.Type.Filled;
            _listenFill.fillMethod = Image.FillMethod.Radial360;
            _listenFill.fillOrigin = (int)Image.Origin360.Top;
            _listenFill.fillClockwise = true;
            _listenLabel = U.Outlined(U.Txt(_listenRoot, "Etiqueta", "", 18, Pal.ResonanceSoft, TextAnchor.UpperCenter));
            U.Place(_listenLabel.rectTransform, U.Bottom, new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(600f, 30f));
            _listenRoot.gameObject.SetActive(false);

            _balanceRoot = U.Rect(Root, "Equilibrio");
            U.Place(_balanceRoot, U.Center, new Vector2(0f, -150f), new Vector2(420f, 40f));
            var bb = U.Img(_balanceRoot, "Barra", new Color(0f, 0f, 0f, 0.5f), TextureGen.RoundedSmall, true);
            U.Stretch(bb.rectTransform, 0f, 0f, 14f, 14f);
            var safe = U.Img(_balanceRoot, "Seguro", new Color(0.5f, 0.85f, 0.5f, 0.45f), TextureGen.RoundedSmall, true);
            U.Place(safe.rectTransform, U.Center, Vector2.zero, new Vector2(170f, 12f));
            var marker = U.Img(_balanceRoot, "Marca", Color.white, TextureGen.RoundedSmall, true);
            _balanceMarker = marker.rectTransform;
            U.Place(_balanceMarker, U.Center, Vector2.zero, new Vector2(10f, 34f));
            var bl = U.Outlined(U.Txt(_balanceRoot, "Ayuda", "", 17, Color.white, TextAnchor.UpperCenter));
            U.Place(bl.rectTransform, U.Bottom, new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(600f, 24f));
            bl.text = "Equilibrio: " + InputMap.KeyName(InputAction.Izquierda) + " / " + InputMap.KeyName(InputAction.Derecha);
            _balanceRoot.gameObject.SetActive(false);

            _damageArrow = U.Rect(Root, "DireccionDanio");
            U.Place(_damageArrow, U.Center, Vector2.zero, new Vector2(10f, 10f));
            _damageArrowImg = U.Img(_damageArrow, "Flecha", new Color(1f, 0.25f, 0.2f, 0f), TextureGen.RoundedSmall, true);
            U.Place(_damageArrowImg.rectTransform, U.Center, new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(90f, 12f));
        }

        private void BuildObjective()
        {
            _objectiveRoot = U.Rect(Root, "Objetivo");
            U.Place(_objectiveRoot, U.TopLeft, new Vector2(28f, -24f), new Vector2(520f, 140f));
            _objectiveGroup = _objectiveRoot.gameObject.AddComponent<CanvasGroup>();
            _objectiveTitle = U.Outlined(U.Txt(_objectiveRoot, "Mision", "", 19, U.Accent, TextAnchor.UpperLeft, FontStyle.Bold));
            U.Place(_objectiveTitle.rectTransform, U.TopLeft, Vector2.zero, new Vector2(520f, 26f));
            _objectiveText = U.Outlined(U.Txt(_objectiveRoot, "Paso", "", 19, Color.white));
            U.Place(_objectiveText.rectTransform, U.TopLeft, new Vector2(0f, -28f), new Vector2(520f, 110f));
        }

        private void BuildToasts()
        {
            _toastRoot = U.Rect(Root, "Avisos");
            U.Place(_toastRoot, U.TopRight, new Vector2(-24f, -24f), new Vector2(560f, 400f));
            var v = U.VLayout(_toastRoot, 6f, 0, true, TextAnchor.UpperRight);
            v.childControlHeight = true;
        }

        private void BuildSubtitles()
        {
            _subRoot = U.Rect(Root, "Subtitulos");
            U.Place(_subRoot, U.Bottom, new Vector2(0f, 150f), new Vector2(1100f, 10f));
            _subBg = U.Img(_subRoot, "Fondo", new Color(0f, 0f, 0f, 0.55f), TextureGen.RoundedSmall, true);
            _subText = U.Outlined(U.Txt(_subRoot, "Texto", "", 24, Color.white, TextAnchor.LowerCenter));
            U.Place(_subText.rectTransform, U.Bottom, Vector2.zero, new Vector2(1060f, 10f));
            _subText.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _subRoot.gameObject.SetActive(false);

            _resRoot = U.Rect(Root, "Resonancia");
            U.Place(_resRoot, U.Top, new Vector2(0f, -70f), new Vector2(900f, 90f));
            _resType = U.Outlined(U.Txt(_resRoot, "Tipo", "", 18, Pal.ResonanceSoft, TextAnchor.UpperCenter, FontStyle.Italic));
            U.Place(_resType.rectTransform, U.Top, Vector2.zero, new Vector2(900f, 26f));
            _resTitle = U.Outlined(U.Txt(_resRoot, "Titulo", "", 30, Color.white, TextAnchor.UpperCenter, FontStyle.Bold));
            U.Place(_resTitle.rectTransform, U.Top, new Vector2(0f, -26f), new Vector2(900f, 44f));
            var skip = U.Outlined(U.Txt(_resRoot, "Saltar", "", 15, U.Dim, TextAnchor.UpperCenter));
            U.Place(skip.rectTransform, U.Top, new Vector2(0f, -72f), new Vector2(900f, 22f));
            skip.text = "[" + InputMap.KeyName(InputAction.Interactuar) + "] siguiente fragmento";
            _resRoot.gameObject.SetActive(false);
        }

        private void BuildEnemies()
        {
            _targetRoot = U.Rect(Root, "Objetivo enemigo");
            U.Place(_targetRoot, U.Top, new Vector2(0f, -40f), new Vector2(360f, 44f));
            _targetName = U.Outlined(U.Txt(_targetRoot, "Nombre", "", 18, Color.white, TextAnchor.UpperCenter));
            U.Place(_targetName.rectTransform, U.Top, Vector2.zero, new Vector2(360f, 24f));
            _targetFill = U.Bar(_targetRoot, "Barra", Pal.Health, out var tr);
            U.Place(tr, U.Top, new Vector2(0f, -26f), new Vector2(300f, 10f));
            _targetRoot.gameObject.SetActive(false);

            _bossRoot = U.Rect(Root, "Jefe");
            U.Place(_bossRoot, U.Bottom, new Vector2(0f, 96f), new Vector2(760f, 50f));
            _bossName = U.Outlined(U.Txt(_bossRoot, "Nombre", "", 22, new Color(0.8f, 0.88f, 1f), TextAnchor.UpperCenter, FontStyle.Bold));
            U.Place(_bossName.rectTransform, U.Top, Vector2.zero, new Vector2(760f, 28f));
            _bossFill = U.Bar(_bossRoot, "Barra", Pal.Echo, out var br);
            U.Place(br, U.Top, new Vector2(0f, -30f), new Vector2(760f, 16f));
            _bossRoot.gameObject.SetActive(false);
        }

        private void BuildBanners()
        {
            var b = U.Rect(Root, "Banner");
            U.Place(b, U.Center, new Vector2(0f, 200f), new Vector2(1200f, 140f));
            _bannerGroup = b.gameObject.AddComponent<CanvasGroup>();
            _banner = U.Outlined(U.Txt(b, "Titulo", "", 46, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold), 0.9f);
            U.Place(_banner.rectTransform, U.Top, Vector2.zero, new Vector2(1200f, 70f));
            _bannerSub = U.Outlined(U.Txt(b, "Sub", "", 24, U.Accent, TextAnchor.UpperCenter, FontStyle.Italic));
            U.Place(_bannerSub.rectTransform, U.Top, new Vector2(0f, -72f), new Vector2(1200f, 60f));
            _bannerGroup.alpha = 0f;

            var h = U.PanelImg(Root, "Pista", new Color(0.12f, 0.08f, 0.18f, 0.92f));
            _hintRoot = h.rectTransform;
            U.Place(_hintRoot, U.TopLeft, new Vector2(28f, -170f), new Vector2(520f, 120f));
            _hintGroup = h.gameObject.AddComponent<CanvasGroup>();
            _hintGroup.blocksRaycasts = false;
            _hintTitle = U.Txt(h.transform, "Titulo", "", 17, Pal.ResonanceSoft, TextAnchor.UpperLeft, FontStyle.Bold);
            U.Stretch(_hintTitle.rectTransform, 16f, 16f, 10f, 80f);
            _hintText = U.Txt(h.transform, "Texto", "", 19, Color.white);
            U.Stretch(_hintText.rectTransform, 16f, 16f, 36f, 8f);
            _hintGroup.alpha = 0f;

            var t = U.PanelImg(Root, "Tutorial", new Color(0.1f, 0.08f, 0.06f, 0.9f));
            _tutorialRoot = t.rectTransform;
            U.Place(_tutorialRoot, U.BottomLeft, new Vector2(28f, 160f), new Vector2(560f, 96f));
            _tutorialGroup = t.gameObject.AddComponent<CanvasGroup>();
            _tutorialGroup.blocksRaycasts = false;
            var th = U.Txt(t.transform, "Cabecera", "Consejo", 15, U.Accent, TextAnchor.UpperLeft, FontStyle.Bold);
            U.Stretch(th.rectTransform, 14f, 14f, 8f, 70f);
            _tutorialText = U.Txt(t.transform, "Texto", "", 18, Color.white);
            U.Stretch(_tutorialText.rectTransform, 14f, 14f, 30f, 8f);
            _tutorialGroup.alpha = 0f;
        }

        private static Sprite _ring;

        private static Sprite RingSprite()
        {
            if (_ring != null) return _ring;
            const int s = 128;
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Anillo" };
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = (x + 0.5f) / s * 2f - 1f, dy = (y + 0.5f) / s * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.86f) / 0.07f);
                    px[y * s + x] = new Color(1f, 1f, 1f, a);
                }
            t.SetPixels32(px);
            t.Apply(false);
            _ring = Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
            return _ring;
        }

        // ------------------------------------------------------------------ API

        public void SetPrompt(string text) => _prompt.text = text ?? "";

        public void Toast(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            foreach (var t in _toasts) if (t.text != null && t.text.text == text && t.until - Time.unscaledTime > 1f) return;
            if (_toastQueue.Count < 12) _toastQueue.Enqueue(text);
        }

        public void Subtitle(string speaker, string text, float duration, bool resonance = false)
        {
            var d = Game.Settings != null ? Game.Settings.Data : null;
            if (d != null && !d.Subtitles && !resonance) return;
            string name = string.IsNullOrEmpty(speaker) ? "" : U.Colored(speaker + ": ", resonance ? Pal.ResonanceSoft : U.Accent);
            _subText.text = name + (resonance ? "<i>" + text + "</i>" : text);
            _subText.color = resonance ? new Color(0.92f, 0.88f, 1f) : Color.white;
            _subUntil = Time.unscaledTime + duration;
            _subRoot.gameObject.SetActive(true);
        }

        public void ResonanceBegin(string title, string type)
        {
            _resonating = true;
            _resTitle.text = title;
            _resType.text = "Resonancia " + type.ToLowerInvariant();
            _resRoot.gameObject.SetActive(true);
        }

        public void ResonanceLine(string speaker, string text) => Subtitle(speaker, text, 6f, true);

        public void ResonanceEnd()
        {
            _resonating = false;
            _resRoot.gameObject.SetActive(false);
            _subUntil = Mathf.Min(_subUntil, Time.unscaledTime + 1.5f);
        }

        public void DamageFlash(float amount, Vector3 from)
        {
            bool reduce = Game.Settings != null && Game.Settings.Data.ReduceFlashes;
            _damageAlpha = Mathf.Max(_damageAlpha, Mathf.Clamp01(amount) * (reduce ? 0.35f : 0.8f));
            _damageFrom = from;
            _damageArrowAlpha = 1f;
            _vitalsShowUntil = Time.unscaledTime + 4f;
        }

        public void ShowTarget(IDamageable d)
        {
            if (d == null || !d.Hostile && !(d is TrainingDummy)) return;
            _target = d;
            _targetUntil = Time.unscaledTime + 5f;
        }

        public void SetBoss(CreatureBase boss) => _boss = boss;

        public void SoundCue(Vector3 pos, string cue)
        {
            if (Game.Settings == null || !Game.Settings.Data.SoundCues) return;
            for (int i = 0; i < _cues.Count; i++)
                if (_cues[i].text != null && _cues[i].text.text.EndsWith(cue) && (_cues[i].pos - pos).sqrMagnitude < 9f)
                {
                    _cues[i] = (_cues[i].text, pos, Time.unscaledTime + 2f);
                    return;
                }
            if (_cues.Count >= 8) return;
            var t = U.Outlined(U.Txt(_cueRoot, "Senal", cue, 18, new Color(1f, 0.95f, 0.7f), TextAnchor.MiddleCenter, FontStyle.Bold));
            U.Place(t.rectTransform, U.Center, Vector2.zero, new Vector2(260f, 30f));
            _cues.Add((t, pos, Time.unscaledTime + 2f));
        }

        public void Banner(string title, string sub, float seconds)
        {
            _banner.text = title;
            _bannerSub.text = sub ?? "";
            _bannerUntil = Time.unscaledTime + seconds;
        }

        public void ShowHint(string quest, string text, int level, int count)
        {
            _hintTitle.text = "Pista " + level + "/" + count + " · " + quest;
            _hintText.text = text;
            _hintUntil = Time.unscaledTime + 10f;
        }

        public void Tutorial(string text)
        {
            _tutorialText.text = text;
            _tutorialUntil = Time.unscaledTime + 7f;
        }

        public void XpGained(int amount, string reason)
        {
            if (Time.unscaledTime > _xpPopupUntil) _xpAccum = 0;
            _xpAccum += amount;
            _xpPopup.text = "+" + _xpAccum + " XP" + (string.IsNullOrEmpty(reason) ? "" : " · " + reason);
            _xpPopupUntil = Time.unscaledTime + 3.5f;
            _vitalsShowUntil = Time.unscaledTime + 4f;
        }

        // ------------------------------------------------------------------ Actualización

        public void Tick(float dt, bool gameplayVisible)
        {
            var s = Game.State;
            bool show = gameplayVisible && s != null && Game.Player != null;
            Root.gameObject.SetActive(Game.InGame && s != null);
            if (!Game.InGame || s == null) return;
            var data = Game.Settings != null ? Game.Settings.Data : new SettingsData();
            float now = Time.unscaledTime;

            // Vitales
            float hp = s.Health.Normalized;
            U.SetFill(_health, hp);
            _ghost = _ghost > hp ? Mathf.MoveTowards(_ghost, hp, dt * 0.35f) : hp;
            U.SetFill(_healthGhost, _ghost);
            _healthText.text = Mathf.CeilToInt(s.Health.Current) + " / " + Mathf.RoundToInt(s.Health.Max);
            U.SetFill(_stamina, s.Stamina.Normalized);
            U.SetFill(_xp, s.Progression.LevelProgress);
            _levelText.text = "Nivel " + s.Progression.Level + ((s.Progression.AttributePoints + s.Progression.SkillPoints) > 0 ? "  " + U.Colored("(puntos por repartir)", Pal.ResonanceSoft) : "");
            _status.text = Game.Vitals != null && Game.Vitals.IsHealing ? "Curando…" : Game.Player.IsSwimming ? "Nadando" : Game.Player.IsCrouching ? "Agachado" : "";
            bool vitalsNeeded = hp < 0.999f || s.Stamina.Normalized < 0.999f || now < _vitalsShowUntil || Game.Combat != null && Game.Combat.IsAttacking;
            _vitalsGroup.alpha = Mathf.MoveTowards(_vitalsGroup.alpha, show ? (vitalsNeeded ? 1f : 0.55f) : 0f, dt * 3f);
            if (hp < 0.25f && show) _health.color = Color.Lerp(Pal.Health, new Color(1f, 0.55f, 0.45f), Mathf.PingPong(now * 2f, 1f));
            else _health.color = Pal.Health;
            _xpPopup.enabled = now < _xpPopupUntil;

            // Objeto rápido
            if (Game.Vitals != null)
            {
                string id = Game.Vitals.QuickItem;
                var def = GameDb.Item(id);
                if (_quickShown != id)
                {
                    _quickShown = id;
                    _quickIcon.sprite = def != null ? ItemIcons.Get(def) : null;
                    _quickIcon.enabled = def != null;
                    _quickName.text = def != null ? def.Name : "(vacío)";
                }
                int count = def != null ? s.Inventory.Count(id) : 0;
                _quickCount.text = def != null ? "× " + count : "";
                _quickCount.color = count > 0 ? U.Dim : U.Bad;
                _quickKeys.text = "[" + InputMap.KeyName(InputAction.UsarObjeto) + "] usar · [" + InputMap.KeyName(InputAction.SiguienteObjeto) + "] cambiar";
            }
            _clock.text = "Día " + s.Clock.Day + " · " + U.Hours(s.Clock.Hour);

            // Centro
            bool scoping = Game.Cam != null && Game.Cam.Scoping;
            _scope.enabled = scoping;
            _crosshair.enabled = show && !scoping;
            _prompt.enabled = show;
            var L = Game.Listener;
            bool listening = show && L != null && L.IsListening;
            _listenRoot.gameObject.SetActive(listening || show && L != null && L.Reveal > 0.01f);
            if (L != null)
            {
                _listenFill.fillAmount = L.Progress;
                float sig = L.NearestSignal(out _);
                _signal.color = K.WithA(Pal.Resonance, listening ? sig * (0.18f + 0.1f * Mathf.Sin(now * 6f)) : 0f);
                _listenLabel.text = L.Target != null ? L.Target.ListenLabel : listening ? (sig > 0.05f ? "Algo resuena cerca…" : "Silencio") : "";
                _listenRing.color = K.WithA(Pal.ResonanceSoft, listening ? 0.3f : L.Reveal * 0.2f);
            }
            bool balancing = show && Game.Player.IsBalancing;
            _balanceRoot.gameObject.SetActive(balancing);
            if (balancing) _balanceMarker.anchoredPosition = new Vector2(Game.Player.Balance * 200f, 0f);

            // Objetivo
            string obj = Game.Quests != null ? Game.Quests.ObjectiveText() : null;
            var tracked = Game.Quests != null ? Game.Quests.Tracked() : null;
            _objectiveTitle.text = tracked != null ? (tracked.Kind == QuestKind.Principal ? "◆ " : "◇ ") + tracked.Title : "";
            _objectiveText.text = obj ?? "";
            _objectiveGroup.alpha = Mathf.MoveTowards(_objectiveGroup.alpha, show && data.ShowObjective && obj != null && !_resonating ? 1f : 0f, dt * 3f);

            // Avisos
            _toastGap -= dt;
            if (_toastQueue.Count > 0 && _toastGap <= 0f)
            {
                var text = _toastQueue.Dequeue();
                var bg = U.PanelImg(_toastRoot, "Aviso", new Color(0.08f, 0.06f, 0.1f, 0.82f), true);
                bg.raycastTarget = false;
                U.Size(bg, 540f, -1f);
                var lay = U.HLayout(bg, 0f, 10, TextAnchor.MiddleRight, true);
                lay.childForceExpandHeight = true;
                var tx = U.Txt(bg.transform, "Texto", text, 19, Color.white, TextAnchor.MiddleRight);
                var g = bg.gameObject.AddComponent<CanvasGroup>();
                g.alpha = 0f;
                _toasts.Add((tx, g, now + 4.5f + text.Length * 0.025f));
                _toastGap = 0.35f;
                if (_toasts.Count > 6) { Object.Destroy(_toasts[0].group.gameObject); _toasts.RemoveAt(0); }
            }
            for (int i = _toasts.Count - 1; i >= 0; i--)
            {
                var t = _toasts[i];
                float left = t.until - now;
                t.group.alpha = Mathf.Clamp01(Mathf.Min(left / 0.6f, (t.group.alpha + dt * 5f)));
                if (left <= 0f) { Object.Destroy(t.group.gameObject); _toasts.RemoveAt(i); }
            }

            // Subtítulos
            if (_subRoot.gameObject.activeSelf)
            {
                bool dialogue = Game.Dialogue != null && Game.Dialogue.Active;
                if (now > _subUntil || dialogue) _subRoot.gameObject.SetActive(false);
                float scale = data.SubtitleScale;
                _subText.fontSize = Mathf.RoundToInt(24 * scale * U.TextScale);
                float h = _subText.preferredHeight + 18f;
                float w = Mathf.Min(1100f, _subText.preferredWidth + 40f);
                _subBg.rectTransform.anchorMin = _subBg.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                _subBg.rectTransform.pivot = new Vector2(0.5f, 0f);
                _subBg.rectTransform.anchoredPosition = new Vector2(0f, -9f);
                _subBg.rectTransform.sizeDelta = new Vector2(w, h);
                _subBg.color = new Color(0f, 0f, 0f, data.SubtitleBackground);
            }

            // Enemigos
            bool targetVisible = show && _target != null && now < _targetUntil && _target.IsAlive && _target as CreatureBase != _boss;
            _targetRoot.gameObject.SetActive(targetVisible);
            if (targetVisible)
            {
                _targetName.text = _target.DisplayName;
                U.SetFill(_targetFill, _target.HealthFraction);
            }
            bool bossVisible = show && _boss != null && _boss.IsAlive;
            _bossRoot.gameObject.SetActive(bossVisible);
            if (bossVisible)
            {
                _bossName.text = _boss.DisplayName;
                U.SetFill(_bossFill, _boss.HealthFraction);
            }

            // Efectos
            _damageAlpha = Mathf.MoveTowards(_damageAlpha, hp < 0.25f ? 0.25f + Mathf.Sin(now * 3f) * 0.05f : 0f, dt * 1.2f);
            _damage.color = new Color(0.7f, 0.05f, 0.05f, show ? _damageAlpha : 0f);
            _damageArrowAlpha = Mathf.MoveTowards(_damageArrowAlpha, 0f, dt * 0.8f);
            if (_damageArrowAlpha > 0f && Game.Player != null)
            {
                var fwd = Game.Player.transform.forward;
                var to = _damageFrom - Game.Player.transform.position;
                float ang = Vector3.SignedAngle(new Vector3(fwd.x, 0f, fwd.z), new Vector3(to.x, 0f, to.z), Vector3.up);
                _damageArrow.localRotation = Quaternion.Euler(0f, 0f, -ang);
            }
            _damageArrowImg.color = new Color(1f, 0.25f, 0.2f, show ? _damageArrowAlpha * 0.85f : 0f);

            // Banners
            _bannerGroup.alpha = Mathf.MoveTowards(_bannerGroup.alpha, now < _bannerUntil ? 1f : 0f, dt * 1.5f);
            _hintGroup.alpha = Mathf.MoveTowards(_hintGroup.alpha, show && now < _hintUntil ? 1f : 0f, dt * 3f);
            _tutorialGroup.alpha = Mathf.MoveTowards(_tutorialGroup.alpha, show && now < _tutorialUntil ? 1f : 0f, dt * 3f);

            // Señales de sonido
            var cam = Game.Cam != null ? Game.Cam.Cam : null;
            for (int i = _cues.Count - 1; i >= 0; i--)
            {
                var c = _cues[i];
                float left = c.until - now;
                if (left <= 0f || cam == null) { Object.Destroy(c.text.gameObject); _cues.RemoveAt(i); continue; }
                var to = c.pos - cam.transform.position;
                float ang = Vector3.SignedAngle(new Vector3(cam.transform.forward.x, 0f, cam.transform.forward.z), new Vector3(to.x, 0f, to.z), Vector3.up);
                float rad = ang * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
                c.text.rectTransform.anchoredPosition = new Vector2(dir.x * 420f, dir.y * 260f);
                string arrow = Mathf.Abs(ang) < 30f ? "▲" : Mathf.Abs(ang) > 150f ? "▼" : ang > 0f ? "►" : "◄";
                string label = c.text.text;
                int sp = label.IndexOf(' ');
                string bare = sp >= 0 && sp <= 2 ? label.Substring(sp + 1) : label;
                c.text.text = arrow + " " + bare;
                c.text.color = new Color(1f, 0.95f, 0.7f, Mathf.Clamp01(left));
                c.text.enabled = show;
            }

            // Rendimiento
            _fpsAccum += Time.unscaledDeltaTime;
            _fpsFrames++;
            _fpsTimer -= Time.unscaledDeltaTime;
            if (_fpsTimer <= 0f)
            {
                float avg = _fpsAccum / Mathf.Max(1, _fpsFrames);
                _fpsText = (1f / Mathf.Max(0.0001f, avg)).ToString("0") + " FPS · " + (avg * 1000f).ToString("0.0") + " ms";
                _fpsAccum = 0f; _fpsFrames = 0; _fpsTimer = 0.5f;
            }
            _perf.enabled = PerformanceVisible || data.ShowPerformance;
            if (_perf.enabled)
            {
                var n = Game.Npcs;
                var p = Game.Player.transform.position;
                _perf.text = _fpsText
                    + "\nSectores cargados: " + (Game.World != null ? Game.World.LoadedCount : 0) + " (250 m)"
                    + (n != null ? $"\nNPC: completos {n.LodCounts[0]} · simplificados {n.LodCounts[1]} · abstractos {n.LodCounts[2]}" : "")
                    + "\nCriaturas activas: " + CreatureBase.All.Count
                    + "\nLuces: " + LightBudget.ActiveCount + "/" + LightBudget.MaxLights
                    + $"\nPosición: {p.x:0} {p.y:0} {p.z:0} · {WorldShape.RegionName(WorldShape.RegionAt(p.x, p.z))}";
            }
        }
    }

    /// <summary>Caché de iconos de objetos.</summary>
    public static class ItemIcons
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Get(ItemDef def)
        {
            if (def == null) return null;
            if (!Cache.TryGetValue(def.Id, out var s) || s == null)
            {
                s = TextureGen.ItemIcon(def);
                Cache[def.Id] = s;
            }
            return s;
        }
    }
}
