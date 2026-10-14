using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Kolotopir.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
#if KOLO_URP
using UnityEngine.Rendering.Universal;
#endif

namespace Kolotopir.EditorTools
{
    /// <summary>
    /// Configura el proyecto la primera vez que se abre en Unity 6: crea el asset de URP y lo activa,
    /// genera las plantillas de materiales (para que los shaders se incluyan en las compilaciones)
    /// y abre la escena principal. Se puede repetir desde el menú «KOLOTOPIR».
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        private const string SettingsDir = "Assets/Kolotopir/Settings";
        private const string MaterialsDir = "Assets/Kolotopir/Resources/KolotopirMaterials";
        private const string PipelinePath = SettingsDir + "/Kolotopir_URP.asset";
        private const string RendererPath = SettingsDir + "/Kolotopir_URP_Renderer.asset";
        private const string MainScene = "Assets/Kolotopir/Scenes/Main.unity";
        private const string UrpPackage = "Packages/com.unity.render-pipelines.universal";

        static ProjectSetup()
        {
            EditorApplication.delayCall += () => Run(false);
        }

        [MenuItem("KOLOTOPIR/Configurar proyecto (URP y materiales)")]
        private static void RunFromMenu() => Run(true);

        [MenuItem("KOLOTOPIR/Abrir escena principal")]
        private static void OpenMainScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(MainScene);
        }

        [MenuItem("KOLOTOPIR/Borrar partidas guardadas")]
        private static void DeleteSaves()
        {
            SaveSystem.Delete(SaveSystem.AutoSlot);
            SaveSystem.Delete(SaveSystem.ManualSlot);
            Debug.Log("KOLOTOPIR: partidas guardadas borradas (" + Application.persistentDataPath + ").");
        }

        [MenuItem("KOLOTOPIR/Validar contenido narrativo")]
        private static void ValidateContent()
        {
            var problems = GameDb.Validate();
            if (problems.Count == 0) EditorUtility.DisplayDialog("KOLOTOPIR", "El contenido es coherente: no se han encontrado referencias rotas.", "Bien");
            else
            {
                foreach (var p in problems) Debug.LogWarning("KOLOTOPIR: " + p);
                EditorUtility.DisplayDialog("KOLOTOPIR", problems.Count + " problema(s). Mira la consola.", "Vale");
            }
        }

        private static void Run(bool interactive)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += () => Run(interactive);
                return;
            }
            var report = new System.Text.StringBuilder();
            try
            {
                EnsurePipeline(report, interactive);
                EnsureMaterials(report);
                EnsureBuildSettings(report);
                AssetDatabase.SaveAssets();
            }
            catch (Exception e)
            {
                Debug.LogWarning("KOLOTOPIR: la configuración automática no se pudo completar (" + e.Message + "). El juego funcionará con el pipeline integrado.");
            }
            if (!SessionState.GetBool("kolotopir_scene_opened", false))
            {
                SessionState.SetBool("kolotopir_scene_opened", true);
                var active = EditorSceneManager.GetActiveScene();
                if (string.IsNullOrEmpty(active.path) && !active.isDirty && File.Exists(MainScene)) EditorSceneManager.OpenScene(MainScene);
            }
            if (report.Length > 0) Debug.Log("KOLOTOPIR · configuración:\n" + report);
            if (interactive)
                EditorUtility.DisplayDialog("KOLOTOPIR", report.Length > 0 ? report.ToString() : "Todo estaba configurado.", "Vale");
        }

        // ------------------------------------------------------------------ URP

        private static void EnsurePipeline(System.Text.StringBuilder report, bool force)
        {
#if KOLO_URP
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (asset == null)
            {
                Directory.CreateDirectory(SettingsDir);
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                var post = AssetDatabase.LoadAssetAtPath<PostProcessData>(UrpPackage + "/Runtime/Data/PostProcessData.asset");
                if (post != null) renderer.postProcessData = post;
                AssetDatabase.CreateAsset(renderer, RendererPath);
                asset = UniversalRenderPipelineAsset.Create(renderer);
                asset.supportsHDR = true;
                asset.renderScale = 1f;
                asset.shadowDistance = 140f;
                asset.shadowCascadeCount = 4;
                asset.msaaSampleCount = 4;
                asset.maxAdditionalLightsCount = 8;
                AssetDatabase.CreateAsset(asset, PipelinePath);
                report.AppendLine("• Creado el asset de URP: " + PipelinePath);
            }
            EnsureAmbientOcclusion(report);
            if (GraphicsSettings.defaultRenderPipeline != asset)
            {
                if (GraphicsSettings.defaultRenderPipeline == null || force)
                {
                    GraphicsSettings.defaultRenderPipeline = asset;
                    report.AppendLine("• URP activado como pipeline gráfico.");
                }
            }
            int current = QualitySettings.GetQualityLevel();
            string[] names = QualitySettings.names;
            for (int i = 0; i < names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                if (QualitySettings.renderPipeline != null && QualitySettings.renderPipeline != asset && !force) continue;
                if (QualitySettings.renderPipeline != asset) QualitySettings.renderPipeline = asset;
            }
            QualitySettings.SetQualityLevel(current, false);
#else
            report.AppendLine("• El paquete URP no está instalado: se usa el pipeline integrado.");
#endif
        }

#if KOLO_URP
        /// <summary>
        /// Oclusión ambiental en pantalla (SSAO): oscurece rincones, bases de árboles y uniones con el suelo,
        /// lo que da mucha más sensación de volumen. Se añade por reflexión porque la clase no es pública en todas las versiones.
        /// </summary>
        private static void EnsureAmbientOcclusion(System.Text.StringBuilder report)
        {
            try
            {
                var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
                if (data == null) return;
                foreach (var f in data.rendererFeatures)
                    if (f != null && f.GetType().Name == "ScreenSpaceAmbientOcclusion") return;
                var type = typeof(UniversalRendererData).Assembly.GetType("UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion");
                if (type == null) return;
                var feature = ScriptableObject.CreateInstance(type) as ScriptableRendererFeature;
                if (feature == null) return;
                feature.name = "OclusionAmbiental";
                AssetDatabase.AddObjectToAsset(feature, data);
                data.rendererFeatures.Add(feature);
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out string _, out long localId))
                {
                    var mapField = typeof(ScriptableRendererData).GetField("m_RendererFeatureMap", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (mapField != null && mapField.GetValue(data) is List<long> map) map.Add(localId);
                }
                data.SetDirty();
                EditorUtility.SetDirty(data);
                report.AppendLine("• Añadida la oclusión ambiental (SSAO).");
            }
            catch (Exception e)
            {
                Debug.LogWarning("KOLOTOPIR: no se pudo añadir la oclusión ambiental: " + e.Message);
            }
        }
#endif

        // ------------------------------------------------------------------ Materiales

        private static void EnsureMaterials(System.Text.StringBuilder report)
        {
            if (GraphicsSettings.defaultRenderPipeline == null) return;
            Directory.CreateDirectory(MaterialsDir);
            int created = 0;
            created += MakeMaterial("Lit", "Universal Render Pipeline/Lit", false, false);
            created += MakeMaterial("LitDetail", "Universal Render Pipeline/Lit", false, false, m => m.EnableKeyword("_NORMALMAP"));
            created += MakeMaterial("LitCutout", "Universal Render Pipeline/Lit", false, false, m => MaterialLibrary.ConfigureCutout(m, 0.45f));
            created += MakeMaterial("LitTransparentDetail", "Universal Render Pipeline/Lit", true, false, m => m.EnableKeyword("_NORMALMAP"));
            created += MakeMaterial("LitTransparent", "Universal Render Pipeline/Lit", true, false);
            created += MakeMaterial("Unlit", "Universal Render Pipeline/Unlit", false, false);
            created += MakeMaterial("UnlitTransparent", "Universal Render Pipeline/Unlit", true, false);
            created += MakeMaterial("ParticlesAdditive", "Universal Render Pipeline/Particles/Unlit", true, true);
            created += MakeMaterial("ParticlesAlpha", "Universal Render Pipeline/Particles/Unlit", true, false);
            created += MakeMaterial("SimpleLit", "Universal Render Pipeline/Simple Lit", false, false);
            created += MakeMaterial("TerrainLit", "Universal Render Pipeline/Terrain/Lit", false, false, m => m.EnableKeyword("_NORMALMAP"));
            // Plantillas creadas por versiones anteriores del configurador: activar el relieve del terreno.
            var terrain = AssetDatabase.LoadAssetAtPath<Material>(MaterialsDir + "/TerrainLit.mat");
            if (terrain != null && !terrain.IsKeywordEnabled("_NORMALMAP")) { terrain.EnableKeyword("_NORMALMAP"); EditorUtility.SetDirty(terrain); }
            if (created > 0) report.AppendLine("• Creadas " + created + " plantillas de material en " + MaterialsDir);
        }

        private static int MakeMaterial(string name, string shaderName, bool transparent, bool additive, Action<Material> configure = null)
        {
            string path = MaterialsDir + "/" + name + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return 0;
            var shader = Shader.Find(shaderName);
            if (shader == null) return 0;
            var m = new Material(shader) { name = name, enableInstancing = true };
            if (transparent) MaterialLibrary.ConfigureTransparent(m, additive);
            configure?.Invoke(m);
            AssetDatabase.CreateAsset(m, path);
            return 1;
        }

        // ------------------------------------------------------------------ Escena en la compilación

        private static void EnsureBuildSettings(System.Text.StringBuilder report)
        {
            foreach (var s in EditorBuildSettings.scenes) if (s.path == MainScene) return;
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            list.Insert(0, new EditorBuildSettingsScene(MainScene, true));
            EditorBuildSettings.scenes = list.ToArray();
            report.AppendLine("• Escena principal añadida a la compilación.");
        }
    }
}
