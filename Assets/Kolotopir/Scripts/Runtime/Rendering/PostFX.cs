using UnityEngine;
#if KOLO_URP
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

namespace Kolotopir
{
    /// <summary>
    /// Postprocesado de URP creado en código: bloom suave, tonemapping, color cálido y viñeta;
    /// un segundo volumen violeta se mezcla mientras Nilo usa la Escucha.
    /// Sin URP (pipeline integrado) todas las llamadas son inocuas.
    /// </summary>
    public static class PostFX
    {
#if KOLO_URP
        private static Volume _base, _resonance;
        private static ColorAdjustments _resColor;
#endif
        private static float _resonanceWeight;

        public static void Init(Transform parent)
        {
#if KOLO_URP
            if (_base != null) return;
            var go = new GameObject("Postproceso");
            go.transform.SetParent(parent, false);

            _base = go.AddComponent<Volume>();
            _base.isGlobal = true;
            _base.priority = 0f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "Orom";
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.55f);
            bloom.threshold.Override(1.05f);
            bloom.scatter.Override(0.62f);
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);
            var color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.35f);
            color.contrast.Override(8f);
            color.saturation.Override(10f);
            color.colorFilter.Override(new Color(1f, 0.97f, 0.93f));
            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(0.2f);
            vig.smoothness.Override(0.45f);
            _base.sharedProfile = profile;

            var go2 = new GameObject("PostprocesoResonancia");
            go2.transform.SetParent(parent, false);
            _resonance = go2.AddComponent<Volume>();
            _resonance.isGlobal = true;
            _resonance.priority = 1f;
            _resonance.weight = 0f;
            var rp = ScriptableObject.CreateInstance<VolumeProfile>();
            rp.name = "Escucha";
            _resColor = rp.Add<ColorAdjustments>(true);
            _resColor.colorFilter.Override(new Color(0.86f, 0.8f, 1f));
            _resColor.saturation.Override(-30f);
            _resColor.contrast.Override(14f);
            var ca = rp.Add<ChromaticAberration>(true);
            ca.intensity.Override(0.3f);
            var rv = rp.Add<Vignette>(true);
            rv.intensity.Override(0.38f);
            rv.color.Override(new Color(0.25f, 0.1f, 0.4f));
            _resonance.sharedProfile = rp;
#endif
        }

        public static void SetupCamera(Camera cam)
        {
#if KOLO_URP
            if (cam == null) return;
            var data = cam.GetUniversalAdditionalCameraData();
            if (data == null) return;
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.Medium;
#endif
        }

        /// <summary>Intensidad (0–1) del efecto de la Escucha.</summary>
        public static void SetResonance(float weight)
        {
            bool reduce = Game.Settings != null && Game.Settings.Data.ReduceFlashes;
            _resonanceWeight = Mathf.Clamp01(weight) * (reduce ? 0.5f : 1f);
#if KOLO_URP
            if (_resonance != null) _resonance.weight = _resonanceWeight;
#endif
        }

        public static void ApplyQuality(int quality, bool shadows)
        {
#if KOLO_URP
            var asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (asset == null) return;
            asset.renderScale = quality == 0 ? 0.8f : 1f;
            asset.shadowDistance = !shadows ? 0f : quality == 0 ? 55f : quality == 1 ? 90f : 140f;
            asset.msaaSampleCount = quality == 2 ? 4 : 1;
#else
            QualitySettings.shadowDistance = !shadows ? 0f : quality == 0 ? 55f : quality == 1 ? 90f : 140f;
#endif
        }
    }
}
