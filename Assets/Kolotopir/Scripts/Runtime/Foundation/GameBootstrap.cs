using UnityEngine;

namespace Kolotopir
{
    /// <summary>
    /// Punto de entrada colocado en la escena principal. Si se pulsa Play en cualquier otra escena,
    /// el juego también arranca (ver <see cref="AutoBoot"/>).
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        private void Awake() => GameRoot.Boot();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBoot()
        {
            if (Game.Root == null) GameRoot.Boot();
        }
    }
}
