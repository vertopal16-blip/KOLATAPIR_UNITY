using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kolotopir
{
    /// <summary>
    /// Crea y reutiliza materiales. Usa las plantillas de Resources/KolotopirMaterials (creadas por el
    /// configurador del editor para URP) y, si faltan, los shaders de URP o del pipeline integrado.
    /// Reutilizar materiales por color mantiene el SRP Batcher eficiente.
    /// </summary>
    public static class MaterialLibrary
    {
        private static Material _lit, _litTransparent, _unlit, _unlitTransparent, _particleAdd, _particleAlpha, _litDetail, _litCutout, _terrain;
        private static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();
        public static bool IsUrp { get; private set; }

        public static void Init()
        {
            if (_lit != null) return;
            IsUrp = GraphicsSettings.currentRenderPipeline != null;
            _lit = Template("Lit", IsUrp ? "Universal Render Pipeline/Lit" : "Standard", false, false);
            _litTransparent = Template("LitTransparent", IsUrp ? "Universal Render Pipeline/Lit" : "Standard", true, false);
            _unlit = Template("Unlit", IsUrp ? "Universal Render Pipeline/Unlit" : "Unlit/Color", false, false);
            _unlitTransparent = Template("UnlitTransparent", IsUrp ? "Universal Render Pipeline/Unlit" : "Sprites/Default", true, false);
            _particleAdd = Template("ParticlesAdditive", IsUrp ? "Universal Render Pipeline/Particles/Unlit" : "Legacy Shaders/Particles/Additive", true, true);
            _particleAlpha = Template("ParticlesAlpha", IsUrp ? "Universal Render Pipeline/Particles/Unlit" : "Sprites/Default", true, false);
            // Variantes con mapa de normales y con recorte alfa (hojas, hierba). Las plantillas del configurador
            // garantizan que estas variantes del shader se incluyan en las compilaciones.
            _litDetail = Template("LitDetail", IsUrp ? "Universal Render Pipeline/Lit" : "Standard", false, false);
            _litDetail.EnableKeyword("_NORMALMAP");
            _litCutout = Template("LitCutout", IsUrp ? "Universal Render Pipeline/Lit" : "Standard", false, false);
            ConfigureCutout(_litCutout, 0.45f);
        }

        private static Material Template(string resource, string shaderName, bool transparent, bool additive)
        {
            var res = Resources.Load<Material>("KolotopirMaterials/" + resource);
            Material m;
            if (IsUrp && res != null && res.shader != null && res.shader.isSupported)
            {
                m = new Material(res);
            }
            else
            {
                var shader = Shader.Find(shaderName);
                if (shader == null) shader = Shader.Find(IsUrp ? "Universal Render Pipeline/Simple Lit" : "Standard");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                m = new Material(shader);
                if (transparent) ConfigureTransparent(m, additive);
            }
            m.name = "Kolotopir_" + resource;
            m.enableInstancing = true;
            return m;
        }

        /// <summary>Configura un material de URP o del pipeline integrado como transparente (alfa o aditivo).</summary>
        public static void ConfigureTransparent(Material m, bool additive)
        {
            if (m.HasProperty("_Surface"))
            {
                m.SetFloat("_Surface", 1f);
                if (m.HasProperty("_Blend")) m.SetFloat("_Blend", additive ? 2f : 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
                if (m.HasProperty("_SrcBlendAlpha")) m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                if (m.HasProperty("_DstBlendAlpha")) m.SetFloat("_DstBlendAlpha", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.DisableKeyword("_ALPHATEST_ON");
                if (additive) m.EnableKeyword("_BLENDMODE_ADD"); else m.DisableKeyword("_BLENDMODE_ADD");
                m.renderQueue = (int)RenderQueue.Transparent;
                m.SetShaderPassEnabled("DepthOnly", false);
                m.SetShaderPassEnabled("ShadowCaster", false);
            }
            else if (m.HasProperty("_Mode"))
            {
                m.SetFloat("_Mode", 3f);
                m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", additive ? (int)BlendMode.One : (int)BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.DisableKeyword("_ALPHATEST_ON");
                m.EnableKeyword("_ALPHABLEND_ON");
                m.renderQueue = (int)RenderQueue.Transparent;
            }
            else
            {
                m.renderQueue = (int)RenderQueue.Transparent;
            }
        }

        /// <summary>Recorte alfa: las zonas transparentes de la textura (hojas, briznas) no se dibujan ni proyectan sombra.</summary>
        public static void ConfigureCutout(Material m, float cutoff)
        {
            if (m.HasProperty("_Cutoff")) m.SetFloat("_Cutoff", cutoff);
            if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 1f);
            if (m.HasProperty("_Mode")) m.SetFloat("_Mode", 1f);
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 0f);
            m.EnableKeyword("_ALPHATEST_ON");
            m.SetOverrideTag("RenderType", "TransparentCutout");
            m.renderQueue = (int)RenderQueue.AlphaTest;
        }

        /// <summary>Mapa de normales (relieve) con su intensidad.</summary>
        public static void SetNormal(Material m, Texture normal, float scale)
        {
            if (normal == null || !m.HasProperty("_BumpMap")) return;
            m.SetTexture("_BumpMap", normal);
            if (m.HasProperty("_BumpScale")) m.SetFloat("_BumpScale", scale);
            m.EnableKeyword("_NORMALMAP");
        }

        public static void SetTiling(Material m, float metersPerTile)
        {
            var scale = Vector2.one / Mathf.Max(0.01f, metersPerTile);
            if (m.HasProperty("_BaseMap")) m.SetTextureScale("_BaseMap", scale);
            if (m.HasProperty("_MainTex")) m.SetTextureScale("_MainTex", scale);
        }

        public static void SetColor(Material m, Color c)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", c);
        }

        public static void SetEmission(Material m, Color e)
        {
            if (!m.HasProperty("_EmissionColor")) return;
            m.SetColor("_EmissionColor", e);
            if (e.maxColorComponent > 0.001f)
            {
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else m.DisableKeyword("_EMISSION");
        }

        public static void SetTexture(Material m, Texture tex)
        {
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
        }

        private static void SetSurface(Material m, float smoothness, float metallic)
        {
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_EnvironmentReflections")) m.SetFloat("_EnvironmentReflections", smoothness > 0.4f ? 1f : 0f);
        }

        private static string Key(string kind, Color c, float a = 0f, float b = 0f)
        {
            return kind + ColorUtility.ToHtmlStringRGBA(c) + "_" + a.ToString("0.00") + "_" + b.ToString("0.00");
        }

        /// <summary>
        /// Material con textura de detalle y relieve (madera, piedra, paja, tela...). El color se multiplica por la
        /// textura en escala de grises, compensando su oscurecimiento medio. Las UV del <see cref="MeshBuilder"/> van en metros.
        /// </summary>
        public static Material Surface(DetailKind kind, Color c, float smoothness = 0.12f, float metallic = 0f)
        {
            Init();
            string key = Key("s" + (int)kind, c, smoothness, metallic);
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            var d = SurfaceTextures.Get(kind);
            if (d == null) return Lit(c, smoothness, metallic);
            m = new Material(_litDetail) { name = kind + "_" + ColorUtility.ToHtmlStringRGB(c) };
            var tint = c * d.Compensation;
            tint.a = c.a;
            SetColor(m, tint);
            SetSurface(m, smoothness, metallic);
            SetTexture(m, d.Albedo);
            SetNormal(m, d.Normal, 1f);
            SetTiling(m, d.Tile);
            m.enableInstancing = true;
            Cache[key] = m;
            return m;
        }

        /// <summary>Material con textura de detalle no compartido (para criaturas que parpadean al recibir golpes).</summary>
        public static Material SurfaceInstance(DetailKind kind, Color c, float smoothness = 0.12f)
        {
            Init();
            var d = SurfaceTextures.Get(kind);
            if (d == null) return LitInstance(c, smoothness);
            var m = new Material(_litDetail) { name = kind + "_Instancia" };
            var tint = c * d.Compensation;
            tint.a = c.a;
            SetColor(m, tint);
            SetSurface(m, smoothness, 0f);
            SetTexture(m, d.Albedo);
            SetNormal(m, d.Normal, 1f);
            SetTiling(m, d.Tile);
            return m;
        }

        /// <summary>
        /// Material para el mundo construido (edificios, atrezo, vegetación): elige una superficie según el color
        /// (madera, piedra, paja, tejas, corteza, follaje...). Los acabados pulidos o metálicos quedan lisos.
        /// </summary>
        public static Material World(Color c, float smoothness = 0.12f, float metallic = 0f)
        {
            if (metallic > 0.3f || smoothness >= 0.45f) return Lit(c, smoothness, metallic);
            var kind = Classify(c);
            return kind.HasValue ? Surface(kind.Value, c, smoothness, metallic) : Lit(c, smoothness, metallic);
        }

        private static bool Same(Color a, Color b) => Mathf.Abs(a.r - b.r) < 0.002f && Mathf.Abs(a.g - b.g) < 0.002f && Mathf.Abs(a.b - b.b) < 0.002f;

        public static DetailKind? Classify(Color c)
        {
            if (Same(c, Pal.Roof) || Same(c, Pal.RoofDark)) return DetailKind.Tiles;
            if (Same(c, Pal.Thatch)) return DetailKind.Thatch;
            if (Same(c, Pal.Bark)) return DetailKind.Bark;
            if (Same(c, Pal.Cloth) || Same(c, Pal.Rope)) return DetailKind.Fabric;
            Color.RGBToHSV(c, out float h, out float s, out float v);
            h *= 360f;
            if (v < 0.08f) return DetailKind.Generic;
            if (s < 0.14f) return v > 0.78f ? DetailKind.Plaster : DetailKind.Stone;
            if (h >= 55f && h <= 175f && s > 0.18f) return DetailKind.Foliage;
            if (h >= 15f && h < 55f)
            {
                if (s < 0.3f) return v > 0.72f ? DetailKind.Plaster : DetailKind.Stone;
                if (v >= 0.7f) return DetailKind.Thatch;
                return DetailKind.Wood;
            }
            return DetailKind.Generic;
        }

        /// <summary>Hojas, hierba y flores: textura con recorte alfa, compartida por textura y color.</summary>
        public static Material Cutout(Texture tex, Color tint, float smoothness = 0.08f, float cutoff = 0.45f)
        {
            Init();
            string key = Key("cut" + (tex != null ? tex.GetInstanceID() : 0), tint, smoothness, cutoff);
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(_litCutout) { name = "Recorte_" + ColorUtility.ToHtmlStringRGB(tint) };
            ConfigureCutout(m, cutoff);
            SetTexture(m, tex);
            SetColor(m, tint);
            SetSurface(m, smoothness, 0f);
            if (m.HasProperty("_SpecularHighlights")) m.SetFloat("_SpecularHighlights", 0f);
            m.enableInstancing = true;
            Cache[key] = m;
            return m;
        }

        /// <summary>Material del terreno con mapas de normales activados (las capas aportan el relieve).</summary>
        public static Material TerrainMaterial()
        {
            Init();
            if (_terrain != null) return _terrain;
            var res = IsUrp ? Resources.Load<Material>("KolotopirMaterials/TerrainLit") : null;
            if (res != null && res.shader != null && res.shader.isSupported) _terrain = new Material(res);
            else
            {
                var sh = Shader.Find(IsUrp ? "Universal Render Pipeline/Terrain/Lit" : "Nature/Terrain/Standard");
                if (sh == null) return null;
                _terrain = new Material(sh);
            }
            _terrain.name = "Kolotopir_Terreno";
            _terrain.EnableKeyword("_NORMALMAP");
            return _terrain;
        }

        /// <summary>Material opaco iluminado (compartido por color).</summary>
        public static Material Lit(Color c, float smoothness = 0.12f, float metallic = 0f)
        {
            Init();
            string key = Key("lit", c, smoothness, metallic);
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(_lit) { name = "Lit_" + ColorUtility.ToHtmlStringRGB(c) };
            SetColor(m, c);
            SetSurface(m, smoothness, metallic);
            Cache[key] = m;
            return m;
        }

        public static Material Emissive(Color c, Color emission, float intensity = 1f)
        {
            Init();
            string key = Key("emi", c, intensity) + ColorUtility.ToHtmlStringRGB(emission);
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(_lit) { name = "Emissive_" + ColorUtility.ToHtmlStringRGB(emission) };
            SetColor(m, c);
            SetSurface(m, 0.2f, 0f);
            SetEmission(m, emission * intensity);
            Cache[key] = m;
            return m;
        }

        public static Material Transparent(Color c, float smoothness = 0.6f)
        {
            Init();
            string key = Key("tr", c, smoothness);
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(_litTransparent) { name = "Transparent_" + ColorUtility.ToHtmlStringRGBA(c) };
            SetColor(m, c);
            SetSurface(m, smoothness, 0f);
            Cache[key] = m;
            return m;
        }

        public static Material Unlit(Color c)
        {
            Init();
            string key = Key("unlit", c);
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(_unlit) { name = "Unlit_" + ColorUtility.ToHtmlStringRGB(c) };
            SetColor(m, c);
            Cache[key] = m;
            return m;
        }

        public static Material UnlitTransparent(Color c, Texture tex = null)
        {
            Init();
            var m = new Material(_unlitTransparent) { name = "UnlitTransparent" };
            SetColor(m, c);
            if (tex != null) SetTexture(m, tex);
            return m;
        }

        /// <summary>Material nuevo (no compartido) para objetos que cambian de color o brillo en tiempo real.</summary>
        public static Material LitInstance(Color c, float smoothness = 0.2f)
        {
            Init();
            var m = new Material(_lit) { name = "LitInstance" };
            SetColor(m, c);
            SetSurface(m, smoothness, 0f);
            return m;
        }

        public static Material TransparentInstance(Color c)
        {
            Init();
            var m = new Material(_litTransparent) { name = "TransparentInstance" };
            SetColor(m, c);
            SetSurface(m, 0.5f, 0f);
            return m;
        }

        public static Material Particle(bool additive, Texture tex)
        {
            Init();
            var m = new Material(additive ? _particleAdd : _particleAlpha) { name = additive ? "ParticleAdd" : "ParticleAlpha" };
            if (tex != null) SetTexture(m, tex);
            SetColor(m, Color.white);
            return m;
        }

        public static Material Textured(Texture tex, Color tint, float smoothness = 0.05f)
        {
            Init();
            var m = new Material(_lit) { name = "Textured" };
            SetTexture(m, tex);
            SetColor(m, tint);
            SetSurface(m, smoothness, 0f);
            return m;
        }
    }
}
