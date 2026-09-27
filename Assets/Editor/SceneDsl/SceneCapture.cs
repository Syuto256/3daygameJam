using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameJam.Editor.SceneDsl
{
    /// <summary>
    ///     Imata シーンをメインカメラから撮って PNG に保存する。
    ///     エディタを開いたまま、組み立てた見た目を外から確認するためのもの（EditorCommandBridge から呼ぶ）。
    ///
    ///     Screen Space - Overlay の Canvas はカメラに映らないので、撮る間だけ
    ///     Screen Space - Camera に切り替えてから元に戻す（シーンは保存しない）
    /// </summary>
    public static class SceneCapture
    {
        public const string OutputPath = "Temp/Screenshots/imata.png";

        private const int Width = 1920;
        private const int Height = 1080;

        [MenuItem("Tools/Scene DSL/Capture Imata Scene", false, 80)]
        public static void CaptureFromMenu() => Debug.Log($"[SceneCapture] {CaptureImataForAutomation()}");

        public static string CaptureImataForAutomation()
        {
            var scenePath = SceneDslPaths.TargetScenePath;
            var scene = SceneManager.GetSceneByPath(scenePath);
            var openedHere = false;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                openedHere = true;
            }

            try
            {
                var camera = FindMainCamera(scene);
                if (camera == null) return $"[NG] {scenePath} に MainCamera タグのカメラがありません";

                return Capture(scene, camera);
            }
            finally
            {
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static string Capture(Scene scene, Camera camera)
        {
            var switched = new List<(Canvas canvas, Camera worldCamera, float planeDistance)>();
            foreach (var root in scene.GetRootGameObjects())
            foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;

                switched.Add((canvas, canvas.worldCamera, canvas.planeDistance));
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = camera.nearClipPlane + 0.1f;
            }

            var previousTarget = camera.targetTexture;
            var renderTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            var previousActive = RenderTexture.active;

            try
            {
                Canvas.ForceUpdateCanvases();

                camera.targetTexture = renderTexture;
                camera.Render();

                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                texture.Apply();

                Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
                File.WriteAllBytes(OutputPath, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                Object.DestroyImmediate(renderTexture);
                Object.DestroyImmediate(texture);

                foreach (var (canvas, worldCamera, planeDistance) in switched)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.worldCamera = worldCamera;
                    canvas.planeDistance = planeDistance;
                }
            }

            return $"[OK] {Path.GetFullPath(OutputPath)}";
        }

        private static Camera FindMainCamera(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            foreach (var camera in root.GetComponentsInChildren<Camera>(true))
                if (camera.CompareTag("MainCamera"))
                    return camera;

            return null;
        }
    }
}
