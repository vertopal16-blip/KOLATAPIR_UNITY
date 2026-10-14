using System;
using System.Collections.Generic;
using Kolotopir.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Kolotopir
{
    /// <summary>Menú principal sobre una vista aérea de Orom.</summary>
    public sealed class MainMenuScreen : UIScreen
    {
        private Button _continue;
        private Text _saveInfo;
        private RectTransform _about;

        public override bool Pauses => false;

        public override void Build(RectTransform parent)
        {
            Root = U.Rect(parent, "MenuPrincipal");
            U.Stretch(Root);
            var left = U.Img(Root, "Panel", new Color(0.04f, 0.02f, 0.06f, 0.6f));
            U.Place(left.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(700f, 2200f));

            var title = U.Outlined(U.Txt(Root, "Titulo", "KOLOTOPIR", 92, U.Accent, TextAnchor.MiddleLeft, FontStyle.Bold), 0.9f);
            U.Place(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(110f, 330f), new Vector2(900f, 120f));
            var sub = U.Outlined(U.Txt(Root, "Subtitulo", "El mundo de Orom · Capítulo I: Tribu del Reverso · Capítulo II: Prados que Recuerdan", 22, U.TextColor, TextAnchor.UpperLeft, FontStyle.Italic));
            U.Place(sub.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(114f, 250f), new Vector2(560f, 70f));

            var col = U.Rect(Root, "Botones");
            U.Place(col, new Vector2(0f, 0.5f), new Vector2(0f, 1f), new Vector2(110f, 170f), new Vector2(440f, 420f));
            U.VLayout(col, 12f);
            _continue = Row(col, "Continuar", () => Game.Root.ContinueGame());
            Row(col, "Nueva partida", () =>
            {
                if (SaveSystem.MostRecent() != null)
                    Game.UI.Confirm("Nueva partida", "Empezar de nuevo sustituirá tu guardado automático cuando se guarde el primer hito. ¿Seguro?", "Empezar", "Cancelar", ok => { if (ok) Game.Root.NewGame(); });
                else Game.Root.NewGame();
            });
            Row(col, "Ajustes", () => Game.UI.Open(Game.UI.SettingsS));
            Row(col, "Acerca de", () => _about.gameObject.SetActive(!_about.gameObject.activeSelf));
            Row(col, "Salir", () => Game.Root.Quit());
            _saveInfo = U.Outlined(U.Txt(Root, "Guardado", "", 18, U.Dim, TextAnchor.UpperLeft));
            U.Place(_saveInfo.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 1f), new Vector2(114f, -270f), new Vector2(560f, 80f));
            var credit = U.Outlined(U.Txt(Root, "Version", "Prototipo jugable · Unity 6 + URP · contenido generado por código", 15, U.Dim, TextAnchor.LowerLeft));
            U.Place(credit.rectTransform, U.BottomLeft, new Vector2(110f, 30f), new Vector2(900f, 30f));

            var about = U.PanelImg(Root, "AcercaDe", new Color(0.07f, 0.05f, 0.09f, 0.95f));
            _about = about.rectTransform;
            U.Place(_about, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-90f, 0f), new Vector2(900f, 820f));
            var at = U.Scroll(_about, "Texto", out _, 10f, 28);
            U.Stretch((RectTransform)at.parent);
            U.Header(at, "Acerca de KOLOTOPIR", 30);
            U.Para(at, "Nilo, un niño de doce años de la Tribu del Reverso, emprende su primer viaje para descubrir qué ocurrió con sus padres, Eira y Daren, desaparecidos tras investigar una Cápsula sellada.\n\nEn Orom, los recuerdos de las personas, los lugares y los objetos dejan huella. La Escucha permite oír las Resonancias que quedan en las Cápsulas y en el mundo. Recordar también es actuar: lo que decidas hacer con lo que oyes tiene consecuencias.", 19);
            U.Header(at, "Controles", 24);
            var keys = new List<string>();
            foreach (InputAction a in Enum.GetValues(typeof(InputAction))) keys.Add(U.Colored(InputMap.KeyName(a), U.Accent) + "  " + InputMap.Label(a));
            U.Para(at, string.Join("\n", keys) + "\n" + U.Colored("1–4", U.Accent) + "  Elegir objeto rápido\n" + U.Colored("F3", U.Accent) + "  Información de rendimiento", 17);
            U.Header(at, "Este prototipo", 24);
            U.Para(at, "Contiene los capítulos I y II: la Tribu del Reverso y los Prados que Recuerdan. Diez habitantes con rutinas, combate con espada, ecos y un guardián, progresión de nivel y atributos, inventario y equipo, Cápsulas con Resonancias, misiones principal y secundarias, guardado y carga, mundo por sectores de 250 m y opciones de accesibilidad.\n\nTodo el contenido visual y sonoro se genera por código.", 17, U.Dim);
            _about.gameObject.SetActive(false);
            Root.gameObject.SetActive(false);
        }

        private static Button Row(RectTransform col, string label, Action a)
        {
            var b = U.Btn(col, label, a, 26, new Color(0.18f, 0.12f, 0.22f, 0.85f));
            U.Size(b, -1f, 62f);
            return b;
        }

        public override void OnOpen()
        {
            string slot = SaveSystem.MostRecent();
            _continue.interactable = slot != null;
            _saveInfo.text = slot != null ? "Última partida: " + SaveSystem.Describe(slot) : "No hay partidas guardadas.";
            _about.gameObject.SetActive(false);
        }

        public override bool Back()
        {
            if (_about.gameObject.activeSelf) { _about.gameObject.SetActive(false); }
            return true;
        }
    }

    /// <summary>Ajustes (doc 18): accesibilidad, audio, gráficos y controles reasignables.</summary>
    public sealed class SettingsScreen : UIScreen
    {
        private enum Tab { Accesibilidad, Audio, Graficos, Controles }
        private Tab _tab;
        private RectTransform _content;
        private readonly Dictionary<Tab, Button> _tabs = new Dictionary<Tab, Button>();
        private InputAction? _capturing;
        private float _captureStart;
        public bool Capturing => _capturing.HasValue;

        public override bool Pauses => true;

        public override void Build(RectTransform parent)
        {
            var w = Window(parent, "Ajustes", new Vector2(1200f, 900f), "Ajustes");
            var tabs = U.Rect(w, "Pestanas");
            U.Place(tabs, U.TopLeft, new Vector2(24f, -80f), new Vector2(1152f, 50f));
            U.HLayout(tabs, 8f, 0, TextAnchor.MiddleLeft, true);
            foreach (Tab t in Enum.GetValues(typeof(Tab)))
            {
                var tab = t;
                var b = U.Btn(tabs, t == Tab.Graficos ? "Gráficos" : t.ToString(), () => { _tab = tab; Refresh(); }, 20);
                U.Size(b, -1f, 48f);
                _tabs[t] = b;
            }
            var p = U.PanelImg(w, "Contenido", new Color(0f, 0f, 0f, 0.25f));
            U.Place(p.rectTransform, U.TopLeft, new Vector2(24f, -140f), new Vector2(1152f, 736f));
            _content = U.Scroll(p.transform, "Lista", out _, 6f, 20);
            U.Stretch((RectTransform)_content.parent);
        }

        public override void OnOpen() { _capturing = null; Refresh(); }

        public override void OnClose()
        {
            _capturing = null;
            Game.Settings.Save();
            Game.Settings.Apply();
        }

        public override bool Back()
        {
            if (_capturing.HasValue) { _capturing = null; Refresh(); return true; }
            return false;
        }

        private static SettingsData D => Game.Settings.Data;
        private static void Apply() => Game.Settings.Apply();

        private void Refresh()
        {
            foreach (var kv in _tabs) kv.Value.image.color = kv.Key == _tab ? U.ButtonHover : U.ButtonColor;
            U.Clear(_content);
            switch (_tab)
            {
                case Tab.Accesibilidad: Accessibility(); break;
                case Tab.Audio: Audio(); break;
                case Tab.Graficos: Graphics(); break;
                case Tab.Controles: Controls(); break;
            }
        }

        private static string Pct(float v) => Mathf.RoundToInt(v * 100f) + " %";

        private void Accessibility()
        {
            U.Header(_content, "Lectura");
            U.StepperRow(_content, "Tamaño del texto", () => Pct(D.TextScale), () => { D.TextScale = Mathf.Max(0.8f, D.TextScale - 0.1f); Apply(); }, () => { D.TextScale = Mathf.Min(1.6f, D.TextScale + 0.1f); Apply(); });
            U.ToggleRow(_content, "Subtítulos", () => D.Subtitles, v => { D.Subtitles = v; Apply(); }, "Muestra las frases de los habitantes. Las Resonancias siempre se subtitulan.");
            U.StepperRow(_content, "Tamaño de los subtítulos", () => Pct(D.SubtitleScale), () => D.SubtitleScale = Mathf.Max(0.8f, D.SubtitleScale - 0.1f), () => D.SubtitleScale = Mathf.Min(1.8f, D.SubtitleScale + 0.1f));
            U.StepperRow(_content, "Fondo de los subtítulos", () => Pct(D.SubtitleBackground), () => D.SubtitleBackground = Mathf.Max(0f, D.SubtitleBackground - 0.1f), () => D.SubtitleBackground = Mathf.Min(1f, D.SubtitleBackground + 0.1f));
            U.ToggleRow(_content, "Alto contraste en menús", () => D.HighContrast, v => { D.HighContrast = v; Apply(); });
            U.Header(_content, "Percepción");
            U.ToggleRow(_content, "Señales visuales de sonido", () => D.SoundCues, v => D.SoundCues = v, "Indica en pantalla la dirección de los sonidos importantes (ataques, pasos de ecos, Resonancias).");
            U.ToggleRow(_content, "Reducir destellos", () => D.ReduceFlashes, v => { D.ReduceFlashes = v; Apply(); });
            U.Header(_content, "Movimiento");
            U.ToggleRow(_content, "Reducir movimiento de cámara", () => D.ReduceMotion, v => { D.ReduceMotion = v; Apply(); }, "Quita el balanceo al caminar, suaviza sacudidas y desactiva los planos cinematográficos.");
            U.ToggleRow(_content, "Balanceo al caminar", () => D.HeadBob, v => { D.HeadBob = v; Apply(); });
            U.StepperRow(_content, "Intensidad de las sacudidas", () => Pct(D.CameraShake), () => { D.CameraShake = Mathf.Max(0f, D.CameraShake - 0.25f); Apply(); }, () => { D.CameraShake = Mathf.Min(1f, D.CameraShake + 0.25f); Apply(); });
            U.StepperRow(_content, "Campo de visión", () => Mathf.RoundToInt(D.Fov) + "°", () => { D.Fov = Mathf.Max(60f, D.Fov - 5f); Apply(); }, () => { D.Fov = Mathf.Min(100f, D.Fov + 5f); Apply(); });
            U.Header(_content, "Ayudas");
            U.ToggleRow(_content, "Pistas graduales (Miga)", () => D.Hints, v => D.Hints = v, "Pide pistas cada vez más claras sobre el objetivo actual.");
            U.ToggleRow(_content, "Mostrar objetivo en pantalla", () => D.ShowObjective, v => D.ShowObjective = v);
        }

        private void Audio()
        {
            U.Header(_content, "Volumen");
            U.StepperRow(_content, "General", () => Pct(D.Master), () => { D.Master = Mathf.Max(0f, D.Master - 0.1f); Apply(); }, () => { D.Master = Mathf.Min(1f, D.Master + 0.1f); Apply(); });
            U.StepperRow(_content, "Música", () => Pct(D.Music), () => { D.Music = Mathf.Max(0f, D.Music - 0.1f); Apply(); }, () => { D.Music = Mathf.Min(1f, D.Music + 0.1f); Apply(); });
            U.StepperRow(_content, "Efectos", () => Pct(D.Sfx), () => { D.Sfx = Mathf.Max(0f, D.Sfx - 0.1f); Apply(); }, () => { D.Sfx = Mathf.Min(1f, D.Sfx + 0.1f); Apply(); });
            U.StepperRow(_content, "Ambiente", () => Pct(D.Ambience), () => { D.Ambience = Mathf.Max(0f, D.Ambience - 0.1f); Apply(); }, () => { D.Ambience = Mathf.Min(1f, D.Ambience + 0.1f); Apply(); });
            U.StepperRow(_content, "Voces", () => Pct(D.Voices), () => { D.Voices = Mathf.Max(0f, D.Voices - 0.1f); Apply(); }, () => { D.Voices = Mathf.Min(1f, D.Voices + 0.1f); Apply(); });
            U.Para(_content, "Los habitantes hablan con un murmullo inventado: todas sus frases aparecen subtituladas.", 16, U.Dim);
        }

        private void Graphics()
        {
            string[] q = { "Baja", "Media", "Alta" };
            U.Header(_content, "Rendimiento");
            U.StepperRow(_content, "Calidad", () => q[Mathf.Clamp(D.Quality, 0, 2)], () => { D.Quality = Mathf.Max(0, D.Quality - 1); Apply(); }, () => { D.Quality = Mathf.Min(2, D.Quality + 1); Apply(); },
                "Baja: menos luces, menos hierba y resolución interna reducida. Alta: sombras y vegetación completas.");
            U.ToggleRow(_content, "Sombras", () => D.Shadows, v => { D.Shadows = v; Apply(); });
            U.StepperRow(_content, "Densidad de vegetación", () => Pct(D.Vegetation), () => { D.Vegetation = Mathf.Max(0.2f, D.Vegetation - 0.2f); Apply(); }, () => { D.Vegetation = Mathf.Min(1f, D.Vegetation + 0.2f); Apply(); }, "Se aplica a los sectores que se carguen a partir de ahora.");
            U.ToggleRow(_content, "Sincronización vertical", () => D.VSync, v => { D.VSync = v; Apply(); });
            U.ToggleRow(_content, "Mostrar rendimiento (F3)", () => D.ShowPerformance, v => D.ShowPerformance = v);
            U.ToggleRow(_content, "Pantalla completa", () => Screen.fullScreen, v => Screen.fullScreen = v);
        }

        private void Controls()
        {
            U.Header(_content, "Ratón");
            U.StepperRow(_content, "Sensibilidad", () => D.MouseSensitivity.ToString("0.0"), () => { D.MouseSensitivity = Mathf.Max(0.2f, D.MouseSensitivity - 0.1f); Apply(); }, () => { D.MouseSensitivity = Mathf.Min(4f, D.MouseSensitivity + 0.1f); Apply(); });
            U.ToggleRow(_content, "Invertir eje vertical", () => D.InvertY, v => { D.InvertY = v; Apply(); });
            U.Header(_content, "Teclas (pulsa una acción y después la tecla nueva; Esc cancela)");
            foreach (InputAction a in Enum.GetValues(typeof(InputAction)))
            {
                var action = a;
                var row = U.Rect(_content, "Tecla");
                U.Size(row, -1f, 44f);
                U.HLayout(row, 10f, 0);
                var l = U.Txt(row, "Accion", InputMap.Label(a), 20, U.TextColor, TextAnchor.MiddleLeft);
                U.Size(l, -1f, 40f, 1f);
                string alt = InputMap.GetAlt(a) != KeyCode.None ? "  /  " + InputMap.Pretty(InputMap.GetAlt(a)) : "";
                bool cap = _capturing.HasValue && _capturing.Value == a;
                var b = U.Btn(row, cap ? "Pulsa una tecla…" : InputMap.KeyName(a) + U.Colored(alt, U.Dim), () => { _capturing = action; _captureStart = Time.unscaledTime; Refresh(); }, 19,
                    cap ? U.ButtonHover : U.ButtonColor);
                U.Size(b, 300f, 40f);
                b.interactable = a != InputAction.Pausa;
            }
            var reset = U.Btn(_content, "Restablecer controles", () => { InputMap.ResetDefaults(); Refresh(); }, 20);
            U.Size(reset, 320f, 48f);
        }

        public override void Tick(float dt)
        {
            if (!_capturing.HasValue || Time.unscaledTime - _captureStart < 0.2f) return;
            var k = InputMap.AnyKeyDown();
            if (k == KeyCode.None) return;
            if (k == KeyCode.Escape) { _capturing = null; Refresh(); return; }
            InputMap.Set(_capturing.Value, k);
            Game.Audio?.Play2D("ui_clic", 0.4f);
            _capturing = null;
            Refresh();
        }
    }

    /// <summary>Resumen de fin del capítulo II: decisiones con su alcance y estadísticas.</summary>
    public sealed class EndingScreen : UIScreen
    {
        private RectTransform _body;
        public override bool Pauses => true;

        public override void Build(RectTransform parent)
        {
            Root = U.Rect(parent, "Final");
            U.Stretch(Root);
            var bg = U.Img(Root, "Fondo", new Color(0.03f, 0.02f, 0.05f, 0.92f));
            U.Stretch(bg.rectTransform);
            bg.raycastTarget = true;
            var p = U.Rect(Root, "Contenido");
            U.Place(p, U.Center, Vector2.zero, new Vector2(1300f, 960f));
            _body = U.Scroll(p, "Texto", out _, 12f, 20);
            U.Stretch((RectTransform)_body.parent);
            Root.gameObject.SetActive(false);
        }

        public override bool Back() => true;

        public override void OnOpen()
        {
            var s = Game.State;
            U.Clear(_body);
            var t = U.Txt(_body, "Titulo", "Fin del Capítulo II", 56, U.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            U.Size(t, -1f, 90f);
            U.Para(_body, "El puente natural se adentra en la bruma. Al otro lado, las cúpulas de la Ciudad de las Mil Cápsulas brillan como si recordaran algo.\n\nEira y Daren siguieron este camino. Ahora sabes por qué se fueron, y que esperaban volver.", 21).alignment = TextAnchor.UpperCenter;
            U.Separator(_body);
            U.Header(_body, "Tus decisiones", 28);
            if (s.Decisions.Count == 0) U.Para(_body, "No tomaste decisiones importantes. A veces, no decidir también es una forma de decidir.", 19, U.Dim);
            foreach (var d in s.Decisions)
                U.Para(_body, U.Colored(d.Option, U.Accent) + "  " + U.Colored("· alcance " + Names.Scale(d.Scale).ToLowerInvariant(), Pal.ResonanceSoft) + "\n" + d.Summary, 19);
            U.Separator(_body);
            U.Header(_body, "Tu viaje", 28);
            int heard = 0;
            foreach (var r in GameDb.Resonances.Keys) if (s.Heard(r)) heard++;
            int mysteries = 0, matched = 0;
            foreach (var m in GameDb.Mysteries.Values)
                if (s.Notebook.IsResolved(m.Id)) { mysteries++; if (s.Notebook.Chosen(m.Id) == m.TrueHypothesis) matched++; }
            int quests = 0;
            foreach (var q in s.Quests.All) if (q.Status == QuestStatus.Completada) quests++;
            int entries = 0;
            foreach (var _ in s.Notebook.AllEntries) entries++;
            int places = 0;
            foreach (var l in WorldLayout.Locations) if (s.Knows(l.Id)) places++;
            U.Para(_body,
                $"Nivel alcanzado: {s.Progression.Level}\n" +
                $"Resonancias escuchadas: {heard} de {GameDb.Resonances.Count}\n" +
                $"Misterios resueltos: {mysteries} (con tu hipótesis acertada: {matched})\n" +
                $"Misiones completadas: {quests}\n" +
                $"Lugares descubiertos: {places} de {WorldLayout.Locations.Count}\n" +
                $"Entradas del cuaderno: {entries}\n" +
                $"Ecos derrotados: {s.Flags.Get("ecos_derrotados")}\n" +
                $"Tiempo de juego: {Mathf.RoundToInt(s.PlaytimeSeconds / 60f)} minutos", 20);
            U.Separator(_body);
            var next = U.Txt(_body, "Continuara", "Continuará: Capítulo III — Bosque del Revés", 30, Pal.ResonanceSoft, TextAnchor.MiddleCenter, FontStyle.Italic);
            U.Size(next, -1f, 60f);
            var row = U.Rect(_body, "Botones");
            U.Size(row, -1f, 64f);
            U.HLayout(row, 20f, 0, TextAnchor.MiddleCenter, false);
            var a = U.Btn(row, "Seguir explorando los Prados", () => Game.Root.ContinueAfterEnding(), 22);
            U.Size(a, 420f, 58f);
            var b = U.Btn(row, "Volver al menú principal", () => Game.Root.ReturnToMenu(), 22);
            U.Size(b, 420f, 58f);
        }
    }
}
