using System.Collections.Generic;
using Kolotopir.Core;
using UnityEngine;

namespace Kolotopir
{
    /// <summary>Acceso estático a los sistemas del juego en ejecución (los crea <see cref="GameRoot"/>).</summary>
    public static class Game
    {
        public static GameRoot Root;
        public static GameState State;
        public static RuntimeGameContext Ctx;
        public static PlayerController Player;
        public static PlayerCamera Cam;
        public static PlayerCombat Combat;
        public static PlayerVitals Vitals;
        public static PlayerInteractor Interactor;
        public static ResonanceListener Listener;
        public static UIRoot UI;
        public static AudioManager Audio;
        public static WorldStreamer World;
        public static Atmosphere Sky;
        public static NpcManager Npcs;
        public static DialogueController Dialogue;
        public static QuestTracker Quests;
        public static ResonanceSystem Resonance;
        public static GameSettings Settings;
        public static Miga Miga;

        public static bool InGame => Root != null && Root.Phase == GamePhase.Playing;
        public static float Now => Time.time;

        /// <summary>Marca el estado como modificado: las misiones se reevalúan al final del fotograma.</summary>
        public static void MarkDirty()
        {
            if (Quests != null) Quests.MarkDirty();
        }
    }

    public enum GamePhase { Booting, MainMenu, Loading, Playing, Ending }

    /// <summary>Utilidades de conversión y color.</summary>
    public static class K
    {
        public static Vector3 V3(P2 p, float y = 0f) => new Vector3(p.X, y, p.Z);
        public static P2 P(Vector3 v) => new P2(v.x, v.z);

        public static Vector3 OnGround(P2 p, float yOffset = 0f) => new Vector3(p.X, Ground.Height(p.X, p.Z) + yOffset, p.Z);
        public static Vector3 OnGround(float x, float z, float yOffset = 0f) => new Vector3(x, Ground.Height(x, z) + yOffset, z);

        private static readonly Dictionary<string, Color> HexCache = new Dictionary<string, Color>();

        public static Color Hex(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return Color.magenta;
            if (HexCache.TryGetValue(hex, out var c)) return c;
            if (!ColorUtility.TryParseHtmlString(hex, out c)) c = Color.magenta;
            HexCache[hex] = c;
            return c;
        }

        public static Color Mul(Color c, float f) => new Color(c.r * f, c.g * f, c.b * f, c.a);
        public static Color WithA(Color c, float a) => new Color(c.r, c.g, c.b, a);

        public static Quaternion Yaw(float deg) => Quaternion.Euler(0f, deg, 0f);

        public static Transform Child(Transform parent, string name, Vector3 localPos = default, Quaternion? localRot = null)
        {
            var go = new GameObject(name);
            var t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            t.localRotation = localRot ?? Quaternion.identity;
            return t;
        }

        public static float Damp(float current, float target, float lambda, float dt) => Mathf.Lerp(current, target, 1f - Mathf.Exp(-lambda * dt));
        public static Vector3 Damp(Vector3 current, Vector3 target, float lambda, float dt) => Vector3.Lerp(current, target, 1f - Mathf.Exp(-lambda * dt));
        public static Quaternion Damp(Quaternion current, Quaternion target, float lambda, float dt) => Quaternion.Slerp(current, target, 1f - Mathf.Exp(-lambda * dt));

        public static float FlatDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        public static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform) SetLayerRecursive(t.gameObject, layer);
        }
    }

    /// <summary>Paleta de color de Orom (cálida, con acentos violetas para la Resonancia).</summary>
    public static class Pal
    {
        public static readonly Color Tunic = new Color(0.42f, 0.18f, 0.55f);
        public static readonly Color NiloHair = new Color(0.04f, 0.04f, 0.05f);
        public static readonly Color NiloEyes = new Color(0.5f, 0.83f, 0.94f);
        public static readonly Color NiloSkin = new Color(0.79f, 0.6f, 0.47f);
        public static readonly Color Resonance = new Color(0.62f, 0.45f, 1f);
        public static readonly Color ResonanceSoft = new Color(0.75f, 0.65f, 1f);
        public static readonly Color Echo = new Color(0.5f, 0.68f, 1f);
        public static readonly Color Warm = new Color(1f, 0.78f, 0.45f);
        public static readonly Color Fire = new Color(1f, 0.55f, 0.18f);
        public static readonly Color Wood = new Color(0.45f, 0.31f, 0.2f);
        public static readonly Color WoodDark = new Color(0.3f, 0.2f, 0.13f);
        public static readonly Color WoodLight = new Color(0.62f, 0.47f, 0.3f);
        public static readonly Color Stone = new Color(0.55f, 0.53f, 0.5f);
        public static readonly Color StoneDark = new Color(0.38f, 0.36f, 0.34f);
        public static readonly Color StoneWarm = new Color(0.66f, 0.58f, 0.48f);
        public static readonly Color Roof = new Color(0.66f, 0.27f, 0.18f);
        public static readonly Color RoofDark = new Color(0.48f, 0.2f, 0.14f);
        public static readonly Color Thatch = new Color(0.74f, 0.62f, 0.38f);
        public static readonly Color Cloth = new Color(0.8f, 0.72f, 0.58f);
        public static readonly Color Rope = new Color(0.68f, 0.58f, 0.4f);
        public static readonly Color Leaf = new Color(0.3f, 0.5f, 0.2f);
        public static readonly Color LeafLight = new Color(0.45f, 0.62f, 0.25f);
        public static readonly Color LeafDream = new Color(0.62f, 0.48f, 0.66f);
        public static readonly Color Bark = new Color(0.36f, 0.27f, 0.2f);
        public static readonly Color Moss = new Color(0.36f, 0.48f, 0.22f);
        public static readonly Color Gold = new Color(0.79f, 0.63f, 0.29f);
        public static readonly Color UIBg = new Color(0.07f, 0.05f, 0.09f, 0.88f);
        public static readonly Color UIPanel = new Color(0.13f, 0.1f, 0.15f, 0.94f);
        public static readonly Color UIAccent = new Color(0.86f, 0.66f, 0.36f);
        public static readonly Color UIText = new Color(0.95f, 0.91f, 0.84f);
        public static readonly Color UITextDim = new Color(0.7f, 0.65f, 0.6f);
        public static readonly Color Health = new Color(0.82f, 0.28f, 0.24f);
        public static readonly Color Stamina = new Color(0.86f, 0.74f, 0.32f);
        public static readonly Color Xp = new Color(0.55f, 0.42f, 0.85f);
    }
}
