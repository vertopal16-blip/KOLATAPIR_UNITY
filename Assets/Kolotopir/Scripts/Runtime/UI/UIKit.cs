using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kolotopir
{
    /// <summary>Texto que respeta la escala de texto de accesibilidad.</summary>
    public sealed class ScaledText : MonoBehaviour
    {
        public int BaseSize = 22;
        public Text Text;

        public void Apply(float scale)
        {
            if (Text != null) Text.fontSize = Mathf.RoundToInt(BaseSize * scale);
        }
    }

    /// <summary>
    /// Utilidades para construir la interfaz en código con uGUI (sin recursos externos).
    /// Todas las pantallas usan estas piezas para mantener un estilo coherente.
    /// </summary>
    public static class U
    {
        private static Font _font;
        public static float TextScale = 1f;
        public static bool HighContrast;

        public static Font Font
        {
            get
            {
                if (_font != null) return _font;
                try { _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch (Exception) { _font = null; }
                if (_font == null)
                {
                    try { _font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch (Exception) { _font = null; }
                }
                if (_font == null) _font = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial", "Helvetica", "DejaVu Sans", "Liberation Sans" }, 22);
                return _font;
            }
        }

        public static Color Panel => HighContrast ? new Color(0f, 0f, 0f, 0.97f) : Pal.UIPanel;
        public static Color Bg => HighContrast ? new Color(0f, 0f, 0f, 0.95f) : Pal.UIBg;
        public static Color TextColor => HighContrast ? Color.white : Pal.UIText;
        public static Color Dim => HighContrast ? new Color(0.85f, 0.85f, 0.85f) : Pal.UITextDim;
        public static Color Accent => HighContrast ? new Color(1f, 0.85f, 0.2f) : Pal.UIAccent;
        public static readonly Color ButtonColor = new Color(0.2f, 0.16f, 0.22f, 0.95f);
        public static readonly Color ButtonHover = new Color(0.36f, 0.27f, 0.38f, 1f);
        public static readonly Color Good = new Color(0.55f, 0.85f, 0.5f);
        public static readonly Color Bad = new Color(0.95f, 0.5f, 0.42f);

        // ------------------------------------------------------------------ Rect básicos

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        /// <summary>Coloca un rect con un ancla y un pivote iguales (p. ej. (0,1) arriba a la izquierda).</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        public static readonly Vector2 TopRight = new Vector2(1f, 1f);
        public static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
        public static readonly Vector2 BottomRight = new Vector2(1f, 0f);
        public static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        public static readonly Vector2 Top = new Vector2(0.5f, 1f);
        public static readonly Vector2 Bottom = new Vector2(0.5f, 0f);

        // ------------------------------------------------------------------ Elementos

        public static Image Img(Transform parent, string name, Color color, Sprite sprite = null, bool sliced = false)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.sprite = sprite;
            img.raycastTarget = false;
            if (sprite != null && sliced) img.type = Image.Type.Sliced;
            return img;
        }

        public static Image PanelImg(Transform parent, string name, Color? color = null, bool small = false)
        {
            var img = Img(parent, name, color ?? Panel, small ? TextureGen.RoundedSmall : TextureGen.Rounded, true);
            img.raycastTarget = true;
            return img;
        }

        public static Text Txt(Transform parent, string name, string text, int size, Color? color = null, TextAnchor align = TextAnchor.UpperLeft, FontStyle style = FontStyle.Normal)
        {
            var rt = Rect(parent, name);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text ?? "";
            t.color = color ?? TextColor;
            t.alignment = align;
            t.fontStyle = style;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.lineSpacing = 1.05f;
            var st = rt.gameObject.AddComponent<ScaledText>();
            st.BaseSize = size;
            st.Text = t;
            st.Apply(TextScale);
            return t;
        }

        /// <summary>Contorno sutil para que el texto se lea sobre cualquier fondo.</summary>
        public static T Outlined<T>(T graphic, float alpha = 0.8f) where T : Graphic
        {
            var o = graphic.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, alpha);
            o.effectDistance = new Vector2(1.2f, -1.2f);
            return graphic;
        }

        public static Button Btn(Transform parent, string label, Action onClick, int fontSize = 22, Color? color = null)
        {
            var img = Img(parent, "Boton_" + label, color ?? ButtonColor, TextureGen.RoundedSmall, true);
            img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Button>();
            var cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.45f, 1.35f, 1.5f, 1f);
            cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            cb.selectedColor = new Color(1.25f, 1.2f, 1.3f, 1f);
            cb.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
            cb.colorMultiplier = 1.6f;
            cb.fadeDuration = 0.08f;
            b.colors = cb;
            b.targetGraphic = img;
            var nav = b.navigation;
            nav.mode = Navigation.Mode.None;
            b.navigation = nav;
            var t = Txt(img.transform, "Texto", label, fontSize, TextColor, TextAnchor.MiddleCenter);
            Stretch(t.rectTransform, 10f, 10f, 2f, 2f);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            if (onClick != null)
                b.onClick.AddListener(() =>
                {
                    Game.Audio?.Play2D("ui_clic", 0.35f);
                    onClick();
                });
            return b;
        }

        public static Text Label(Button b) => b.GetComponentInChildren<Text>();

        public static LayoutElement Size(Component c, float w = -1f, float h = -1f, float flexW = -1f, float flexH = -1f)
        {
            var le = c.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            if (w >= 0f) { le.preferredWidth = w; le.minWidth = w; }
            if (h >= 0f) { le.preferredHeight = h; le.minHeight = h; }
            if (flexW >= 0f) le.flexibleWidth = flexW;
            if (flexH >= 0f) le.flexibleHeight = flexH;
            return le;
        }

        public static VerticalLayoutGroup VLayout(Component c, float spacing = 8f, int pad = 0, bool expandWidth = true, TextAnchor align = TextAnchor.UpperLeft)
        {
            var v = c.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = new RectOffset(pad, pad, pad, pad);
            v.childAlignment = align;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = expandWidth;
            v.childForceExpandHeight = false;
            return v;
        }

        public static HorizontalLayoutGroup HLayout(Component c, float spacing = 8f, int pad = 0, TextAnchor align = TextAnchor.MiddleLeft, bool expandWidth = false)
        {
            var h = c.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.padding = new RectOffset(pad, pad, pad, pad);
            h.childAlignment = align;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = expandWidth;
            h.childForceExpandHeight = false;
            return h;
        }

        public static GridLayoutGroup Grid(Component c, Vector2 cell, Vector2 spacing, int columns)
        {
            var g = c.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = cell;
            g.spacing = spacing;
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = columns;
            return g;
        }

        public static void FitVertical(Component c)
        {
            var f = c.gameObject.AddComponent<ContentSizeFitter>();
            f.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            f.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        /// <summary>Lista desplazable. Devuelve el contenido (con disposición vertical) donde añadir filas.</summary>
        public static RectTransform Scroll(Transform parent, string name, out ScrollRect scroll, float spacing = 6f, int pad = 6)
        {
            var view = Rect(parent, name);
            var bg = view.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.001f);
            bg.raycastTarget = true;
            view.gameObject.AddComponent<RectMask2D>();
            scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 35f;
            scroll.inertia = false;
            var content = Rect(view, "Contenido");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            VLayout(content, spacing, pad);
            FitVertical(content);
            scroll.content = content;
            scroll.viewport = view;
            return content;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var c = t.GetChild(i).gameObject;
                c.SetActive(false);
                UnityEngine.Object.Destroy(c);
            }
        }

        /// <summary>Barra horizontal (fondo + relleno). Devuelve el relleno para ajustar su anchura.</summary>
        public static Image Bar(Transform parent, string name, Color fill, out RectTransform root)
        {
            var bg = Img(parent, name, new Color(0f, 0f, 0f, 0.55f), TextureGen.RoundedSmall, true);
            root = bg.rectTransform;
            var f = Img(bg.transform, "Relleno", fill, TextureGen.RoundedSmall, true);
            f.rectTransform.anchorMin = new Vector2(0f, 0f);
            f.rectTransform.anchorMax = new Vector2(1f, 1f);
            f.rectTransform.offsetMin = new Vector2(2f, 2f);
            f.rectTransform.offsetMax = new Vector2(-2f, -2f);
            return f;
        }

        public static void SetFill(Image fill, float t)
        {
            var rt = fill.rectTransform;
            rt.anchorMax = new Vector2(Mathf.Clamp01(t), 1f);
            fill.enabled = t > 0.001f;
        }

        public static string Colored(string text, Color c) => "<color=#" + ColorUtility.ToHtmlStringRGB(c) + ">" + text + "</color>";

        public static string Hours(float hour)
        {
            int h = Mathf.FloorToInt(hour) % 24;
            int m = Mathf.FloorToInt((hour - Mathf.Floor(hour)) * 60f);
            return h.ToString("00") + ":" + m.ToString("00");
        }

        /// <summary>Fila de ajuste: etiqueta + valor + botones − y +.</summary>
        public static Text StepperRow(Transform parent, string label, Func<string> value, Action minus, Action plus, string help = null)
        {
            var row = Rect(parent, "Fila_" + label);
            Size(row, -1f, 44f);
            HLayout(row, 10f, 0);
            var l = Txt(row, "Etiqueta", label, 21, TextColor, TextAnchor.MiddleLeft);
            Size(l, -1f, 40f, 1f);
            var bm = Btn(row, "−", null, 24);
            Size(bm, 44f, 40f);
            var v = Txt(row, "Valor", value(), 21, Accent, TextAnchor.MiddleCenter);
            Size(v, 150f, 40f);
            var bp = Btn(row, "+", null, 24);
            Size(bp, 44f, 40f);
            bm.onClick.AddListener(() => { minus(); v.text = value(); Game.Audio?.Play2D("ui_clic", 0.3f); });
            bp.onClick.AddListener(() => { plus(); v.text = value(); Game.Audio?.Play2D("ui_clic", 0.3f); });
            if (!string.IsNullOrEmpty(help))
            {
                var h = Txt(parent, "Ayuda", help, 16, Dim);
                Size(h, -1f, -1f);
            }
            return v;
        }

        public static Text ToggleRow(Transform parent, string label, Func<bool> get, Action<bool> set, string help = null)
        {
            var row = Rect(parent, "Fila_" + label);
            Size(row, -1f, 44f);
            HLayout(row, 10f, 0);
            var l = Txt(row, "Etiqueta", label, 21, TextColor, TextAnchor.MiddleLeft);
            Size(l, -1f, 40f, 1f);
            Text v = null;
            var b = Btn(row, get() ? "Sí" : "No", () => { set(!get()); v.text = get() ? "Sí" : "No"; }, 21);
            Size(b, 150f + 44f * 2f + 20f, 40f);
            v = Label(b);
            if (!string.IsNullOrEmpty(help))
            {
                var h = Txt(parent, "Ayuda", help, 16, Dim);
                Size(h, -1f, -1f);
            }
            return v;
        }

        public static Text Header(Transform parent, string text, int size = 26)
        {
            var t = Txt(parent, "Cabecera", text, size, Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            Size(t, -1f, size + 14f);
            return t;
        }

        public static Text Para(Transform parent, string text, int size = 20, Color? color = null)
        {
            var t = Txt(parent, "Parrafo", text, size, color ?? TextColor);
            return t;
        }

        public static Image Separator(Transform parent)
        {
            var i = Img(parent, "Separador", new Color(1f, 1f, 1f, 0.12f));
            Size(i, -1f, 2f);
            return i;
        }

        public static bool PointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
