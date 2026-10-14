using System;
using System.Collections.Generic;
using Kolotopir.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Kolotopir
{
    public enum JournalTab { Misiones, Atlas, Misterios, Resonancias, Personajes, Bestiario, Culturas, Objetos, Decisiones }

    /// <summary>
    /// Cuaderno de Nilo (doc 12): misiones, atlas, misterios con hipótesis, resonancias, personajes,
    /// bestiario, culturas, objetos y decisiones. Distingue lo que Nilo sabe, observa o sospecha.
    /// </summary>
    public sealed class JournalScreen : UIScreen
    {
        public JournalTab Tab = JournalTab.Misiones;
        private readonly Dictionary<JournalTab, Button> _tabs = new Dictionary<JournalTab, Button>();
        private RectTransform _listPanel, _detailPanel, _list, _detail, _atlasPanel;
        private string _selected;
        private JournalTab _selectedTab;

        // Atlas
        private RawImage _map;
        private RectTransform _mapRect, _markers, _fog;
        private Text _atlasInfo;
        private static Texture2D _mapTex;
        private const float MapSize = 800f;

        public override void Build(RectTransform parent)
        {
            var w = Window(parent, "Cuaderno", new Vector2(1720f, 940f), "Cuaderno de Nilo");
            var tabs = U.Rect(w, "Pestanas");
            U.Place(tabs, U.TopLeft, new Vector2(24f, -84f), new Vector2(230f, 820f));
            U.VLayout(tabs, 6f);
            foreach (JournalTab t in Enum.GetValues(typeof(JournalTab)))
            {
                var tab = t;
                var b = U.Btn(tabs, TabName(t), () => { Tab = tab; _selected = null; Refresh(); }, 20);
                U.Label(b).alignment = TextAnchor.MiddleLeft;
                U.Size(b, -1f, 50f);
                _tabs[t] = b;
            }

            var lp = U.PanelImg(w, "Lista", new Color(0f, 0f, 0f, 0.25f));
            _listPanel = lp.rectTransform;
            U.Place(_listPanel, U.TopLeft, new Vector2(270f, -84f), new Vector2(470f, 832f));
            _list = U.Scroll(_listPanel, "Entradas", out _, 6f, 10);
            U.Stretch((RectTransform)_list.parent);

            var dp = U.PanelImg(w, "Detalle", new Color(0f, 0f, 0f, 0.3f));
            _detailPanel = dp.rectTransform;
            U.Place(_detailPanel, U.TopLeft, new Vector2(756f, -84f), new Vector2(940f, 832f));
            _detail = U.Scroll(_detailPanel, "Texto", out _, 12f, 22);
            U.Stretch((RectTransform)_detail.parent);

            BuildAtlas(w);
        }

        private static string TabName(JournalTab t)
        {
            switch (t)
            {
                case JournalTab.Misiones: return "Misiones";
                case JournalTab.Atlas: return "Atlas";
                case JournalTab.Misterios: return "Misterios";
                case JournalTab.Resonancias: return "Resonancias";
                case JournalTab.Personajes: return "Personajes";
                case JournalTab.Bestiario: return "Bestiario";
                case JournalTab.Culturas: return "Culturas";
                case JournalTab.Objetos: return "Objetos";
                default: return "Decisiones";
            }
        }

        private static NotebookCategory? Category(JournalTab t)
        {
            switch (t)
            {
                case JournalTab.Atlas: return NotebookCategory.Atlas;
                case JournalTab.Misterios: return NotebookCategory.Misterios;
                case JournalTab.Resonancias: return NotebookCategory.Resonancias;
                case JournalTab.Personajes: return NotebookCategory.Personajes;
                case JournalTab.Bestiario: return NotebookCategory.Bestiario;
                case JournalTab.Culturas: return NotebookCategory.Culturas;
                case JournalTab.Objetos: return NotebookCategory.Objetos;
                default: return null;
            }
        }

        public override void OnOpen() => Refresh();

        public void Refresh()
        {
            var s = Game.State;
            if (s == null) return;
            foreach (var kv in _tabs)
            {
                int unread = 0;
                var cat = Category(kv.Key);
                if (cat.HasValue) foreach (var id in s.Notebook.EntriesIn(cat.Value)) if (s.Notebook.IsUnread(id)) unread++;
                if (kv.Key == JournalTab.Misterios) foreach (var m in GameDb.Mysteries.Values) if (s.Notebook.IsUnread(m.Id)) unread++;
                U.Label(kv.Value).text = TabName(kv.Key) + (unread > 0 ? "  " + U.Colored("● " + unread, Pal.ResonanceSoft) : "");
                kv.Value.image.color = kv.Key == Tab ? U.ButtonHover : U.ButtonColor;
            }
            bool atlas = Tab == JournalTab.Atlas;
            _atlasPanel.gameObject.SetActive(atlas);
            _listPanel.gameObject.SetActive(!atlas);
            _detailPanel.gameObject.SetActive(!atlas);
            if (atlas) { RefreshAtlas(); return; }
            U.Clear(_list);
            U.Clear(_detail);
            switch (Tab)
            {
                case JournalTab.Misiones: ListQuests(); break;
                case JournalTab.Misterios: ListMysteries(); break;
                case JournalTab.Decisiones: ListDecisions(); break;
                default: ListEntries(Category(Tab).Value); break;
            }
        }

        private void Item(string id, string label, bool unread, Action show)
        {
            var b = U.Btn(_list, (unread ? U.Colored("● ", Pal.ResonanceSoft) : "") + label, () =>
            {
                _selected = id;
                _selectedTab = Tab;
                Refresh();
            }, 19, _selected == id ? U.ButtonHover : new Color(0.13f, 0.1f, 0.15f, 0.9f));
            U.Label(b).alignment = TextAnchor.MiddleLeft;
            U.Size(b, -1f, 48f);
            if (_selected == id && _selectedTab == Tab) show();
        }

        private void Empty(string text) => U.Para(_detail, text, 19, U.Dim);

        // ------------------------------------------------------------------ Misiones

        private void ListQuests()
        {
            var s = Game.State;
            var list = new List<QuestProgress>(s.Quests.All);
            list.Sort((a, b) =>
            {
                if (a.Status != b.Status) return a.Status == QuestStatus.Activa ? -1 : 1;
                return b.Order.CompareTo(a.Order);
            });
            var tracked = Game.Quests.Tracked();
            if (_selected == null && tracked != null) { _selected = tracked.Id; _selectedTab = Tab; }
            foreach (var p in list)
            {
                var def = GameDb.Quest(p.Id);
                if (def == null) continue;
                string mark = p.Status == QuestStatus.Completada ? "✔ " : def.Kind == QuestKind.Principal ? "◆ " : "◇ ";
                string label = mark + def.Title + (tracked == def ? U.Colored("  (seguida)", U.Dim) : "");
                var progress = p;
                Item(def.Id, p.Status == QuestStatus.Completada ? U.Colored(label, U.Dim) : label, false, () => ShowQuest(def, progress));
            }
            if (list.Count == 0) Empty("Todavía no tienes misiones.");
            else if (_selected == null) Empty("Selecciona una misión.");
        }

        private void ShowQuest(QuestDef def, QuestProgress p)
        {
            var s = Game.State;
            U.Header(_detail, def.Title, 30);
            U.Para(_detail, (def.Kind == QuestKind.Principal ? "Misión principal" : "Misión secundaria") + (string.IsNullOrEmpty(def.Chapter) ? "" : " · " + def.Chapter), 17, U.Dim);
            U.Para(_detail, def.Summary, 20);
            U.Separator(_detail);
            for (int i = 0; i < def.Steps.Count; i++)
            {
                var step = def.Steps[i];
                if (i < p.Step || p.Status == QuestStatus.Completada) U.Para(_detail, U.Colored("✔ " + Tokens.Apply(step.Text), U.Dim), 19);
                else if (i == p.Step)
                {
                    U.Para(_detail, U.Colored("▶ ", U.Accent) + Tokens.Apply(step.Text), 21);
                    if (step.Detail != null)
                    {
                        try { U.Para(_detail, "    " + step.Detail(s), 17, Pal.ResonanceSoft); } catch (Exception) { }
                    }
                    break;
                }
            }
            if (p.Status == QuestStatus.Activa)
            {
                U.Separator(_detail);
                var b = U.Btn(_detail, Game.Quests.TrackedQuest == def.Id ? "Misión seguida" : "Seguir esta misión", () => { Game.Quests.TrackedQuest = def.Id; Refresh(); }, 20);
                U.Size(b, -1f, 48f);
                U.Para(_detail, "¿Atascado? Pulsa " + InputMap.KeyName(InputAction.Pista) + " para pedir una pista sobre la misión seguida.", 16, U.Dim);
            }
            else U.Para(_detail, U.Colored("Completada.", U.Good), 19);
        }

        // ------------------------------------------------------------------ Entradas

        private static string KnowledgeTag(Knowledge k)
        {
            Color c = k == Knowledge.Sabe ? U.Good : k == Knowledge.Observado ? U.Accent : Pal.ResonanceSoft;
            return U.Colored("[" + Names.Knowledge(k) + "]", c);
        }

        private void ListEntries(NotebookCategory cat)
        {
            var s = Game.State;
            int n = 0;
            foreach (var id in s.Notebook.EntriesIn(cat))
            {
                var def = GameDb.Entry(id);
                if (def == null) continue;
                n++;
                var k = s.Notebook.Status(id);
                Item(id, def.Title + "  " + U.Colored(Names.Knowledge(k).ToLowerInvariant(), U.Dim), s.Notebook.IsUnread(id), () => ShowEntry(def, k));
            }
            if (n == 0) Empty(cat == NotebookCategory.Bestiario ? "Aún no has observado ninguna criatura. Míralas un rato (o con el catalejo) para anotarlas." : "Aún no hay nada anotado aquí.");
            else if (_selected == null) Empty("Selecciona una entrada.\n\n" + KnowledgeTag(Knowledge.Sabe) + " lo que Nilo sabe con certeza.\n" + KnowledgeTag(Knowledge.Observado) + " lo que ha visto con sus ojos.\n" + KnowledgeTag(Knowledge.Sospecha) + " lo que solo sospecha.");
        }

        private void ShowEntry(NotebookEntryDef def, Knowledge k)
        {
            var s = Game.State;
            s.Notebook.MarkRead(def.Id);
            U.Header(_detail, def.Title, 30);
            U.Para(_detail, KnowledgeTag(k) + "  " + U.Colored(Names.Notebook(def.Category), U.Dim), 18);
            U.Para(_detail, def.Text, 20);
            if (k == Knowledge.Sabe && !string.IsNullOrEmpty(def.KnownText)) U.Para(_detail, def.KnownText, 20, new Color(0.85f, 0.95f, 0.85f));
            if (def.Category == NotebookCategory.Personajes && def.Id.StartsWith("pj_"))
            {
                string npc = def.Id.Substring(3);
                var nd = GameDb.Npc(npc);
                if (nd != null)
                {
                    U.Separator(_detail);
                    int aff = s.Affinity(npc);
                    string rel = aff >= 30 ? "Confía mucho en ti" : aff >= 10 ? "Te aprecia" : aff <= -10 ? "Desconfía de ti" : "Relación neutral";
                    U.Para(_detail, U.Colored("Relación: ", U.Accent) + rel + " (" + (aff > 0 ? "+" : "") + aff + ")" + (string.IsNullOrEmpty(nd.Role) ? "" : "\n" + U.Colored("Papel: ", U.Accent) + nd.Role), 18);
                    var mem = s.Relationships.Get(npc).Memories;
                    if (mem.Count > 0) U.Para(_detail, U.Colored("Recuerda: ", U.Accent) + string.Join(", ", mem).Replace('_', ' '), 17, U.Dim);
                }
            }
            if (def.Category == NotebookCategory.Atlas)
            {
                var loc = GameDb.Location(def.Id);
                if (loc != null)
                {
                    var b = U.Btn(_detail, "Ver en el atlas", () => { Tab = JournalTab.Atlas; Refresh(); }, 19);
                    U.Size(b, 260f, 46f);
                }
            }
            RefreshBadges();
        }

        private void RefreshBadges()
        {
            var s = Game.State;
            foreach (var kv in _tabs)
            {
                int unread = 0;
                var cat = Category(kv.Key);
                if (cat.HasValue) foreach (var id in s.Notebook.EntriesIn(cat.Value)) if (s.Notebook.IsUnread(id)) unread++;
                if (kv.Key == JournalTab.Misterios) foreach (var m in GameDb.Mysteries.Values) if (s.Notebook.IsUnread(m.Id)) unread++;
                U.Label(kv.Value).text = TabName(kv.Key) + (unread > 0 ? "  " + U.Colored("● " + unread, Pal.ResonanceSoft) : "");
            }
        }

        // ------------------------------------------------------------------ Misterios

        private void ListMysteries()
        {
            var s = Game.State;
            int n = 0;
            foreach (var m in GameDb.Mysteries.Values)
            {
                if (!s.Notebook.HasEntry(m.Id)) continue;
                n++;
                bool resolved = s.Notebook.IsResolved(m.Id);
                var mystery = m;
                Item(m.Id, (resolved ? "✔ " : "? ") + m.Question, s.Notebook.IsUnread(m.Id), () => ShowMystery(mystery));
            }
            if (n == 0) Empty("Ningún misterio abierto todavía.");
            else if (_selected == null) Empty("Selecciona un misterio. Reúne evidencias (diálogos, Resonancias, objetos, lugares) y elige la hipótesis que te parezca más sólida. El cuaderno no lo resuelve por ti.");
        }

        private void ShowMystery(MysteryDef m)
        {
            var s = Game.State;
            s.Notebook.MarkRead(m.Id);
            bool resolved = s.Notebook.IsResolved(m.Id);
            U.Header(_detail, m.Question, 28);
            U.Para(_detail, resolved ? U.Colored("Resuelto", U.Good) : U.Colored("Abierto", Pal.ResonanceSoft), 18);
            U.Separator(_detail);
            U.Header(_detail, "Evidencias", 22);
            int unknown = 0;
            foreach (var e in m.Evidence)
            {
                if (s.Notebook.HasEvidence(e.Id)) U.Para(_detail, "• " + KnowledgeTag(e.Certainty) + " " + e.Text, 19);
                else unknown++;
            }
            if (unknown > 0) U.Para(_detail, U.Colored($"… y {unknown} pista(s) más por descubrir.", U.Dim), 17);
            U.Separator(_detail);
            U.Header(_detail, "Hipótesis", 22);
            string chosen = s.Notebook.Chosen(m.Id);
            foreach (var h in m.Hypotheses)
            {
                int sup = s.Notebook.HypothesisSupport(h);
                string state = sup > 0 ? U.Colored("apoyada por las pruebas", U.Good) : sup < 0 ? U.Colored("contradicha por las pruebas", U.Bad) : U.Colored("sin pruebas claras", U.Dim);
                var row = U.Rect(_detail, "Hipotesis");
                U.Size(row, -1f, 92f);
                var t = U.Txt(row, "Texto", (chosen == h.Id ? U.Colored("► ", U.Accent) : "") + h.Text + "\n" + state, 19, sup < 0 ? U.Dim : U.TextColor);
                U.Place(t.rectTransform, U.TopLeft, Vector2.zero, new Vector2(660f, 90f));
                if (!resolved)
                {
                    var hyp = h;
                    var b = U.Btn(row, chosen == h.Id ? "Tu hipótesis" : "Elegir", () =>
                    {
                        if (s.Notebook.ChooseHypothesis(m.Id, hyp.Id)) Game.Audio?.Play2D("examinar", 0.4f);
                        Refresh();
                    }, 18, chosen == h.Id ? U.ButtonHover : U.ButtonColor);
                    U.Place((RectTransform)b.transform, U.TopRight, Vector2.zero, new Vector2(190f, 46f));
                }
            }
            if (resolved)
            {
                U.Separator(_detail);
                U.Para(_detail, m.Resolution, 20, new Color(0.88f, 0.95f, 0.88f));
                U.Para(_detail, chosen == m.TrueHypothesis ? U.Colored("Tu hipótesis era correcta.", U.Good) : U.Colored("La verdad era otra. Las pruebas te obligaron a cambiar de idea.", U.Accent), 18);
            }
            RefreshBadges();
        }

        // ------------------------------------------------------------------ Decisiones

        private void ListDecisions()
        {
            var s = Game.State;
            for (int i = s.Decisions.Count - 1; i >= 0; i--)
            {
                var d = s.Decisions[i];
                Item(d.Id, d.Option + "  " + U.Colored(Names.Scale(d.Scale).ToLowerInvariant(), U.Dim), false, () =>
                {
                    U.Header(_detail, d.Option, 28);
                    U.Para(_detail, "Alcance: " + U.Colored(Names.Scale(d.Scale), U.Accent), 18);
                    U.Para(_detail, d.Summary, 20);
                    U.Separator(_detail);
                    U.Para(_detail, ScaleHelp(d.Scale), 17, U.Dim);
                });
            }
            if (s.Decisions.Count == 0) Empty("Todavía no has tomado decisiones importantes.\n\nEn Orom las decisiones tienen tres escalas: local (una persona o un lugar), comunitaria (una cultura) y global (el mundo).");
            else if (_selected == null) Empty("Tus decisiones quedan registradas. Sus consecuencias pueden aparecer mucho más tarde.");
        }

        public static string ScaleHelp(ConsequenceScale s)
        {
            switch (s)
            {
                case ConsequenceScale.Local: return "Consecuencia local: afecta a una persona, una relación o un lugar concreto.";
                case ConsequenceScale.Comunitaria: return "Consecuencia comunitaria: cambia cómo una cultura recuerda, comercia o confía.";
                default: return "Consecuencia global: puede alterar el equilibrio entre recordar y olvidar en todo Orom.";
            }
        }

        // ------------------------------------------------------------------ Atlas

        private void BuildAtlas(RectTransform w)
        {
            _atlasPanel = U.Rect(w, "Atlas");
            U.Place(_atlasPanel, U.TopLeft, new Vector2(270f, -84f), new Vector2(1426f, 832f));
            var frame = U.PanelImg(_atlasPanel, "Marco", new Color(0.12f, 0.1f, 0.08f, 1f));
            U.Place(frame.rectTransform, U.TopLeft, Vector2.zero, new Vector2(MapSize + 24f, MapSize + 24f));
            _mapRect = U.Rect(frame.transform, "Mapa");
            U.Place(_mapRect, U.TopLeft, new Vector2(12f, -12f), new Vector2(MapSize, MapSize));
            _map = _mapRect.gameObject.AddComponent<RawImage>();
            _fog = U.Rect(_mapRect, "Niebla");
            U.Stretch(_fog);
            _markers = U.Rect(_mapRect, "Marcas");
            U.Stretch(_markers);
            _atlasInfo = U.Txt(_atlasPanel, "Info", "", 18, U.TextColor);
            U.Place(_atlasInfo.rectTransform, U.TopLeft, new Vector2(MapSize + 50f, 0f), new Vector2(1426f - MapSize - 60f, 832f));
        }

        private static Vector2 MapPos(float x, float z) => new Vector2(x / WorldShape.WorldSize * MapSize, z / WorldShape.WorldSize * MapSize);

        private void RefreshAtlas()
        {
            if (_mapTex == null) _mapTex = BuildMapTexture();
            _map.texture = _mapTex;
            var s = Game.State;
            U.Clear(_fog);
            U.Clear(_markers);
            float cell = MapSize / WorldShape.SectorsPerSide;
            for (int sz = 0; sz < WorldShape.SectorsPerSide; sz++)
                for (int sx = 0; sx < WorldShape.SectorsPerSide; sx++)
                {
                    if (s.World.VisitedSectors.Contains(WorldShape.SectorIndex(sx, sz))) continue;
                    var f = U.Img(_fog, "Niebla", new Color(0.82f, 0.78f, 0.7f, 0.94f));
                    f.rectTransform.anchorMin = f.rectTransform.anchorMax = Vector2.zero;
                    f.rectTransform.pivot = Vector2.zero;
                    f.rectTransform.anchoredPosition = new Vector2(sx * cell, sz * cell);
                    f.rectTransform.sizeDelta = new Vector2(cell + 0.5f, cell + 0.5f);
                }
            var known = new List<string>();
            foreach (var loc in WorldLayout.Locations)
            {
                if (!s.Knows(loc.Id)) continue;
                known.Add(loc.Name);
                Marker(loc.Pos.X, loc.Pos.Z, loc.Landmark ? "◆" : "•", loc.Name, loc.Landmark ? U.Accent : new Color(0.25f, 0.18f, 0.12f), loc.Landmark ? 16 : 14);
            }
            foreach (var r in WorldLayout.RestPoints)
            {
                WorldShape.SectorOf(r.Pos.X, r.Pos.Z, out int sx, out int sz);
                if (!s.World.VisitedSectors.Contains(WorldShape.SectorIndex(sx, sz))) continue;
                Marker(r.Pos.X, r.Pos.Z, "⌂", null, new Color(0.35f, 0.22f, 0.1f), 18);
            }
            var step = Game.Quests.HintStep(out var quest);
            string target = "";
            if (step != null && Game.Quests.TryGetTargetPosition(step.HintTarget, out var tp))
            {
                Marker(tp.x, tp.z, "◎", null, Pal.Resonance, 30);
                target = "\n\n" + U.Colored("◎ Objetivo: ", Pal.Resonance) + Tokens.Apply(step.Text);
            }
            if (Game.Player != null)
            {
                var p = Game.Player.transform.position;
                var m = Marker(p.x, p.z, "▲", null, new Color(0.55f, 0.15f, 0.7f), 26);
                m.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -Game.Player.transform.eulerAngles.y);
            }
            int visited = s.World.VisitedSectors.Count;
            _atlasInfo.text = U.Colored("Orom — Capítulos I y II", U.Accent) + "\nTribu del Reverso, el Abismo y los Prados que Recuerdan.\n\n"
                + U.Colored("▲", new Color(0.7f, 0.3f, 0.9f)) + " Nilo   " + U.Colored("◆", U.Accent) + " lugar importante   • lugar   ⌂ descanso   " + U.Colored("◎", Pal.Resonance) + " objetivo"
                + "\n\nZonas exploradas: " + visited + " de " + (WorldShape.SectorsPerSide * WorldShape.SectorsPerSide) + " (sectores de 250 m)."
                + "\nLugares descubiertos: " + known.Count + "." + target
                + "\n\nNorte arriba. Más allá del puente natural del este se adivinan las cúpulas de la Ciudad de las Mil Cápsulas.";
        }

        private Text Marker(float x, float z, string glyph, string label, Color color, int size)
        {
            var t = U.Outlined(U.Txt(_markers, "Marca", glyph, size, color, TextAnchor.MiddleCenter, FontStyle.Bold), 0.5f);
            t.rectTransform.anchorMin = t.rectTransform.anchorMax = Vector2.zero;
            t.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            t.rectTransform.anchoredPosition = MapPos(x, z);
            t.rectTransform.sizeDelta = new Vector2(40f, 40f);
            if (!string.IsNullOrEmpty(label))
            {
                var l = U.Txt(_markers, "Nombre", label, 13, new Color(0.18f, 0.12f, 0.08f), TextAnchor.UpperCenter, FontStyle.Bold);
                l.rectTransform.anchorMin = l.rectTransform.anchorMax = Vector2.zero;
                l.rectTransform.pivot = new Vector2(0.5f, 1f);
                l.rectTransform.anchoredPosition = MapPos(x, z) + new Vector2(0f, -10f);
                l.rectTransform.sizeDelta = new Vector2(170f, 40f);
                var o = l.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(1f, 0.97f, 0.88f, 0.7f);
            }
            return t;
        }

        /// <summary>Mapa dibujado a mano a partir del relieve real (estilo de cuaderno de viaje).</summary>
        private static Texture2D BuildMapTexture()
        {
            const int n = 256;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "Atlas" };
            var px = new Color32[n * n];
            float step = WorldShape.WorldSize / n;
            var paper = new Color(0.93f, 0.88f, 0.76f);
            var heights = new float[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    heights[y * n + x] = WorldShape.Height((x + 0.5f) * step, (y + 0.5f) * step);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float wx = (x + 0.5f) * step, wz = (y + 0.5f) * step;
                    float h = heights[y * n + x];
                    Color c;
                    if (WorldShape.TryGetLake(wx, wz, out var lake) && h < lake.WaterLevel) c = new Color(0.5f, 0.66f, 0.74f);
                    else if (h < WorldShape.CloudSeaY + 6f) c = Color.Lerp(paper, new Color(0.8f, 0.82f, 0.86f), 0.6f);
                    else if (h < 0f) c = new Color(0.45f, 0.4f, 0.36f);
                    else
                    {
                        var region = WorldShape.RegionAt(wx, wz);
                        c = region == Region.TribuDelReverso ? new Color(0.62f, 0.68f, 0.45f)
                          : region == Region.ParadaAntigua ? new Color(0.7f, 0.66f, 0.55f)
                          : new Color(0.72f, 0.75f, 0.48f);
                        float hx = heights[y * n + Mathf.Min(n - 1, x + 1)] - h;
                        float hz = heights[Mathf.Min(n - 1, y + 1) * n + x] - h;
                        float shade = Mathf.Clamp((-hx - hz) * 0.06f, -0.25f, 0.25f);
                        c = K.Mul(c, 1f + shade);
                        if (Mathf.Repeat(h, 8f) < 0.45f) c = K.Mul(c, 0.86f); // curvas de nivel
                        if (WorldShape.DistanceToPath(wx, wz) < 2.6f) c = new Color(0.62f, 0.48f, 0.32f);
                    }
                    c = Color.Lerp(c, paper, 0.25f);
                    float grain = (Mathf.PerlinNoise(wx * 0.15f, wz * 0.15f) - 0.5f) * 0.08f;
                    px[y * n + x] = new Color(c.r + grain, c.g + grain, c.b + grain, 1f);
                }
            tex.SetPixels32(px);
            tex.Apply(false);
            return tex;
        }
    }
}
