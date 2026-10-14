using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kolotopir
{
    /// <summary>Acciones de juego (los nombres se usan como {Token} en los textos del contenido).</summary>
    public enum InputAction
    {
        Adelante, Atras, Izquierda, Derecha, Saltar, Correr, Agacharse,
        Interactuar, Atacar, Bloquear, Esquivar, Escuchar, UsarObjeto, SiguienteObjeto, Habilidad,
        Catalejo, Pista, Inventario, Cuaderno, Mapa, Pausa
    }

    /// <summary>
    /// Controles reasignables (doc: accesibilidad). Teclado y ratón como plataforma prioritaria.
    /// Usa el Input Manager clásico de Unity, que no requiere paquetes adicionales.
    /// </summary>
    public static class InputMap
    {
        private static readonly Dictionary<InputAction, KeyCode> Keys = new Dictionary<InputAction, KeyCode>();
        private static readonly Dictionary<InputAction, KeyCode> AltKeys = new Dictionary<InputAction, KeyCode>();
        private static bool _axisError;

        public static float MouseSensitivity = 1f;
        public static bool InvertY;

        static InputMap() { ResetDefaults(); }

        public static void ResetDefaults()
        {
            Keys.Clear();
            AltKeys.Clear();
            Keys[InputAction.Adelante] = KeyCode.W; AltKeys[InputAction.Adelante] = KeyCode.UpArrow;
            Keys[InputAction.Atras] = KeyCode.S; AltKeys[InputAction.Atras] = KeyCode.DownArrow;
            Keys[InputAction.Izquierda] = KeyCode.A; AltKeys[InputAction.Izquierda] = KeyCode.LeftArrow;
            Keys[InputAction.Derecha] = KeyCode.D; AltKeys[InputAction.Derecha] = KeyCode.RightArrow;
            Keys[InputAction.Saltar] = KeyCode.Space;
            Keys[InputAction.Correr] = KeyCode.LeftShift;
            Keys[InputAction.Agacharse] = KeyCode.C; AltKeys[InputAction.Agacharse] = KeyCode.LeftControl;
            Keys[InputAction.Interactuar] = KeyCode.E;
            Keys[InputAction.Atacar] = KeyCode.Mouse0;
            Keys[InputAction.Bloquear] = KeyCode.Mouse1;
            Keys[InputAction.Esquivar] = KeyCode.Q;
            Keys[InputAction.Escuchar] = KeyCode.R;
            Keys[InputAction.UsarObjeto] = KeyCode.F;
            Keys[InputAction.SiguienteObjeto] = KeyCode.X;
            Keys[InputAction.Habilidad] = KeyCode.Z;
            Keys[InputAction.Catalejo] = KeyCode.V;
            Keys[InputAction.Pista] = KeyCode.H;
            Keys[InputAction.Inventario] = KeyCode.Tab; AltKeys[InputAction.Inventario] = KeyCode.I;
            Keys[InputAction.Cuaderno] = KeyCode.J;
            Keys[InputAction.Mapa] = KeyCode.M;
            Keys[InputAction.Pausa] = KeyCode.Escape; AltKeys[InputAction.Pausa] = KeyCode.P;
        }

        public static KeyCode Get(InputAction a) => Keys.TryGetValue(a, out var k) ? k : KeyCode.None;
        public static KeyCode GetAlt(InputAction a) => AltKeys.TryGetValue(a, out var k) ? k : KeyCode.None;

        public static void Set(InputAction a, KeyCode k)
        {
            // Evita duplicados: si otra acción usaba esa tecla, se queda sin ella.
            foreach (var other in new List<InputAction>(Keys.Keys))
                if (other != a && Keys[other] == k) Keys[other] = KeyCode.None;
            Keys[a] = k;
        }

        public static IEnumerable<KeyValuePair<InputAction, KeyCode>> All => Keys;

        public static bool Held(InputAction a)
        {
            var k = Get(a); var alt = GetAlt(a);
            return (k != KeyCode.None && Input.GetKey(k)) || (alt != KeyCode.None && Input.GetKey(alt));
        }

        public static bool Down(InputAction a)
        {
            var k = Get(a); var alt = GetAlt(a);
            return (k != KeyCode.None && Input.GetKeyDown(k)) || (alt != KeyCode.None && Input.GetKeyDown(alt));
        }

        public static bool Up(InputAction a)
        {
            var k = Get(a); var alt = GetAlt(a);
            return (k != KeyCode.None && Input.GetKeyUp(k)) || (alt != KeyCode.None && Input.GetKeyUp(alt));
        }

        public static Vector2 Move()
        {
            float x = (Held(InputAction.Derecha) ? 1f : 0f) - (Held(InputAction.Izquierda) ? 1f : 0f);
            float y = (Held(InputAction.Adelante) ? 1f : 0f) - (Held(InputAction.Atras) ? 1f : 0f);
            var v = new Vector2(x, y);
            return v.sqrMagnitude > 1f ? v.normalized : v;
        }

        public static Vector2 Look()
        {
            if (_axisError) return Vector2.zero;
            try
            {
                float x = Input.GetAxisRaw("Mouse X") * MouseSensitivity * 2f;
                float y = Input.GetAxisRaw("Mouse Y") * MouseSensitivity * 2f * (InvertY ? -1f : 1f);
                return new Vector2(x, y);
            }
            catch (ArgumentException)
            {
                _axisError = true;
                Debug.LogError("KOLOTOPIR: faltan los ejes 'Mouse X'/'Mouse Y' en el Input Manager.");
                return Vector2.zero;
            }
        }

        public static float Scroll()
        {
            if (_axisError) return 0f;
            try { return Input.GetAxisRaw("Mouse ScrollWheel"); }
            catch (ArgumentException) { return 0f; }
        }

        public static string KeyName(InputAction a) => Pretty(Get(a));

        public static string Pretty(KeyCode k)
        {
            switch (k)
            {
                case KeyCode.None: return "—";
                case KeyCode.Mouse0: return "Clic izq.";
                case KeyCode.Mouse1: return "Clic der.";
                case KeyCode.Mouse2: return "Rueda";
                case KeyCode.Mouse3: return "Ratón 4";
                case KeyCode.Mouse4: return "Ratón 5";
                case KeyCode.Space: return "Espacio";
                case KeyCode.LeftShift: return "Mayús";
                case KeyCode.RightShift: return "Mayús der.";
                case KeyCode.LeftControl: return "Ctrl";
                case KeyCode.RightControl: return "Ctrl der.";
                case KeyCode.LeftAlt: return "Alt";
                case KeyCode.Tab: return "Tab";
                case KeyCode.Escape: return "Esc";
                case KeyCode.Return: return "Intro";
                case KeyCode.Backspace: return "Retroceso";
                case KeyCode.UpArrow: return "↑";
                case KeyCode.DownArrow: return "↓";
                case KeyCode.LeftArrow: return "←";
                case KeyCode.RightArrow: return "→";
                default:
                    string s = k.ToString();
                    if (s.StartsWith("Alpha")) return s.Substring(5);
                    if (s.StartsWith("Keypad")) return "Num " + s.Substring(6);
                    return s;
            }
        }

        public static string Label(InputAction a)
        {
            switch (a)
            {
                case InputAction.Adelante: return "Avanzar";
                case InputAction.Atras: return "Retroceder";
                case InputAction.Izquierda: return "Izquierda";
                case InputAction.Derecha: return "Derecha";
                case InputAction.Saltar: return "Saltar / soltarse";
                case InputAction.Correr: return "Correr";
                case InputAction.Agacharse: return "Agacharse";
                case InputAction.Interactuar: return "Interactuar";
                case InputAction.Atacar: return "Atacar (mantener: golpe fuerte)";
                case InputAction.Bloquear: return "Bloquear";
                case InputAction.Esquivar: return "Esquivar";
                case InputAction.Escuchar: return "Escucha de Resonancias";
                case InputAction.UsarObjeto: return "Usar objeto rápido";
                case InputAction.SiguienteObjeto: return "Cambiar objeto rápido";
                case InputAction.Habilidad: return "Habilidad activa";
                case InputAction.Catalejo: return "Catalejo";
                case InputAction.Pista: return "Pedir pista a Miga";
                case InputAction.Inventario: return "Inventario";
                case InputAction.Cuaderno: return "Cuaderno de campo";
                case InputAction.Mapa: return "Mapa (Atlas)";
                default: return "Pausa";
            }
        }

        /// <summary>Devuelve la tecla pulsada este fotograma (para reasignar), o None.</summary>
        public static KeyCode AnyKeyDown()
        {
            if (!Input.anyKeyDown) return KeyCode.None;
            foreach (KeyCode k in Enum.GetValues(typeof(KeyCode)))
            {
                if (k == KeyCode.None || (int)k >= (int)KeyCode.JoystickButton0) continue;
                if (Input.GetKeyDown(k)) return k;
            }
            return KeyCode.None;
        }

        public static List<int> Serialize()
        {
            var list = new List<int>();
            foreach (var kv in Keys) { list.Add((int)kv.Key); list.Add((int)kv.Value); }
            return list;
        }

        public static void Deserialize(List<int> data)
        {
            if (data == null || data.Count < 2) return;
            for (int i = 0; i + 1 < data.Count; i += 2)
            {
                var a = (InputAction)data[i];
                if (!Enum.IsDefined(typeof(InputAction), a)) continue;
                Keys[a] = (KeyCode)data[i + 1];
            }
        }
    }
}
