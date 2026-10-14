using System;
using System.Collections.Generic;
using System.IO;
using Kolotopir.Core;
using UnityEngine;

namespace Kolotopir
{
    /// <summary>Ajustes de accesibilidad, audio, gráficos y controles (doc 18: accesibilidad).</summary>
    [Serializable]
    public sealed class SettingsData
    {
        public float TextScale = 1f;
        public bool Subtitles = true;
        public float SubtitleScale = 1f;
        public float SubtitleBackground = 0.55f;
        public bool SoundCues;
        public bool HeadBob = true;
        public float CameraShake = 1f;
        public bool ReduceMotion;
        public bool ReduceFlashes;
        public bool HighContrast;
        public bool Hints = true;
        public bool ShowObjective = true;
        public float Fov = 75f;
        public float MouseSensitivity = 1f;
        public bool InvertY;
        public float Master = 1f;
        public float Music = 0.6f;
        public float Sfx = 0.9f;
        public float Ambience = 0.7f;
        public float Voices = 0.8f;
        public int Quality = 2;
        public bool Shadows = true;
        public float Vegetation = 1f;
        public bool VSync = true;
        public bool ShowPerformance;
        public List<int> Keys = new List<int>();
    }

    public sealed class GameSettings
    {
        public SettingsData Data = new SettingsData();
        private static string PathFile => System.IO.Path.Combine(Application.persistentDataPath, "kolotopir_ajustes.json");

        public void Load()
        {
            try
            {
                if (File.Exists(PathFile)) Data = JsonUtility.FromJson<SettingsData>(File.ReadAllText(PathFile)) ?? new SettingsData();
            }
            catch (Exception e)
            {
                Debug.LogWarning("KOLOTOPIR: no se pudieron leer los ajustes: " + e.Message);
                Data = new SettingsData();
            }
            InputMap.Deserialize(Data.Keys);
        }

        public void Save()
        {
            try
            {
                Data.Keys = InputMap.Serialize();
                File.WriteAllText(PathFile, JsonUtility.ToJson(Data, true));
            }
            catch (Exception e) { Debug.LogWarning("KOLOTOPIR: no se pudieron guardar los ajustes: " + e.Message); }
        }

        public void Apply()
        {
            InputMap.MouseSensitivity = Data.MouseSensitivity;
            InputMap.InvertY = Data.InvertY;
            QualitySettings.vSyncCount = Data.VSync ? 1 : 0;
            Application.targetFrameRate = Data.VSync ? -1 : 144;
            var a = Game.Audio;
            if (a != null)
            {
                a.Master = Data.Master; a.MusicVolume = Data.Music; a.SfxVolume = Data.Sfx; a.AmbienceVolume = Data.Ambience; a.VoiceVolume = Data.Voices;
                a.ApplyVolumes();
            }
            if (Game.World != null)
            {
                Game.World.VegetationDensity = Data.Quality == 0 ? Data.Vegetation * 0.4f : Data.Vegetation;
                Game.World.GrassDistance = Data.Quality == 0 ? 80f : Data.Quality == 1 ? 110f : 150f;
            }
            LightBudget.MaxLights = Data.Quality == 0 ? 4 : Data.Quality == 1 ? 6 : 10;
            PostFX.ApplyQuality(Data.Quality, Data.Shadows);
            if (Game.Cam != null) Game.Cam.ApplySettings(Data);
            if (Game.UI != null) Game.UI.ApplySettings(Data);
        }
    }

    /// <summary>
    /// Guardado en JSON: automático en hitos importantes y manual en lugares seguros (decisión cerrada del doc).
    /// </summary>
    public static class SaveSystem
    {
        public const string AutoSlot = "auto";
        public const string ManualSlot = "manual";

        private static string FileFor(string slot) => Path.Combine(Application.persistentDataPath, $"kolotopir_{slot}.json");

        public static bool Exists(string slot) => File.Exists(FileFor(slot));

        public static bool Save(string slot, SaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, true);
                string tmp = FileFor(slot) + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(FileFor(slot))) File.Delete(FileFor(slot));
                File.Move(tmp, FileFor(slot));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("KOLOTOPIR: error al guardar: " + e);
                return false;
            }
        }

        public static SaveData Load(string slot)
        {
            try
            {
                if (!Exists(slot)) return null;
                var d = JsonUtility.FromJson<SaveData>(File.ReadAllText(FileFor(slot)));
                return d;
            }
            catch (Exception e)
            {
                Debug.LogError("KOLOTOPIR: guardado dañado (" + slot + "): " + e.Message);
                return null;
            }
        }

        /// <summary>La ranura guardada más recientemente (para «Continuar»).</summary>
        public static string MostRecent()
        {
            string best = null;
            DateTime bestTime = DateTime.MinValue;
            foreach (var slot in new[] { AutoSlot, ManualSlot })
            {
                if (!Exists(slot)) continue;
                var t = File.GetLastWriteTimeUtc(FileFor(slot));
                if (t > bestTime) { bestTime = t; best = slot; }
            }
            return best;
        }

        public static string Describe(string slot)
        {
            var d = Load(slot);
            if (d == null) return "Vacío";
            int minutes = Mathf.RoundToInt(d.Playtime / 60f);
            return $"{d.SavedAt} · Capítulo {d.Chapter} · {d.RegionName} · Nivel {d.Level} · {minutes} min";
        }

        public static void Delete(string slot)
        {
            try { if (Exists(slot)) File.Delete(FileFor(slot)); } catch (Exception) { }
        }
    }
}
