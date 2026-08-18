using UnityEngine;

namespace Nestglow
{
    /// <summary>
    /// Автозапуск прототипа при Play — компонент на сцене не обязателен.
    /// </summary>
    public class NestglowBootstrap : MonoBehaviour
    {
        static bool _started;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _started = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoStart()
        {
            if (_started) return;
            if (Object.FindFirstObjectByType<BoardController>() != null) return;

            Debug.Log("Nestglow: AutoStart — создаю runtime bootstrap");
            var go = new GameObject("NestglowRuntime");
            go.AddComponent<NestglowBootstrap>();
        }

        void Awake()
        {
            if (_started)
            {
                Destroy(gameObject);
                return;
            }

            _started = true;
            Debug.Log("Nestglow: bootstrap Awake — собираю доску…");
            BuildPrototype();
        }

        void BuildPrototype()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                cam = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
                camGo.AddComponent<AudioListener>();
            }

            cam.orthographic = true;
            cam.orthographicSize = 6.1f;
            cam.transform.position = new Vector3(0f, 0.15f, -10f);
            // Hallmark Lumen Night Foundry paper
            cam.backgroundColor = NestglowTheme.Paper;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.cullingMask = ~0;

            var kit = VisualKit.Create();
            var boardGo = new GameObject("Board");
            var board = boardGo.AddComponent<BoardController>();
            board.Begin(cam, kit);

            Debug.Log("Nestglow: доска готова. Space/E — спавн, drag — merge, R — рестарт.");
        }
    }
}
