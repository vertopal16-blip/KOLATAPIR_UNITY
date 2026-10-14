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
        private static Material _lit, _litTransparent, _unlit, _unlitTransparent, _particleAdd, _particleAlpha;
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
        }

        private static Material Template(string resource, string shaderName, bool transparent, bool additive)
        {
            var res = Resources.Load<Material>("KolotopirMaterials/" + resource);
            Material m;
            if (res != null && res.shader != null && res.shader.isSupported)
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
