using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GameJam.Editor.Visual
{
    /// <summary>
    ///     インゲームの見た目に使う素材をコードで作る。
    ///       - 光のにじみ・光の筋・ほこりの粒（グラデーション画像）
    ///       - 背景画像から切り抜いたバルブ（回して動かす）
    ///       - 画面効果（Bloom / Vignette / 色調整）の Volume Profile
    ///
    ///     何度実行しても同じものが上書きされるだけなので、値を変えたら実行し直せばよい
    /// </summary>
    public static class VisualAssetGenerator
    {
        private const string TextureDirectory = "Assets/Visual/Textures";
        private const string VolumeProfilePath = "Assets/Visual/InGameVolumeProfile.asset";
        private const string BackgroundPath = "Assets/Images/Background/In-GameBackground.png";

        /// <summary>背景画像の中のバルブの位置（左上原点のピクセル座標）と半径</summary>
        public static readonly Vector2 ValveCenterPixel = new(46f, 134f);
        private const float ValveRadiusPixel = 29f;

        [MenuItem("Tools/Visual/Generate Assets", false, 0)]
        public static void GenerateFromMenu() => Debug.Log($"[VisualAssetGenerator] {GenerateAll()}");

        public static string GenerateAll()
        {
            Directory.CreateDirectory(TextureDirectory);

            WriteSprite("Glow_Soft", CreateRadialGlow(256));
            WriteSprite("Light_Beam", CreateBeam(128, 512));
            WriteSprite("Dust", CreateRadialGlow(32));
            WriteSprite("Valve", CreateValveCutout());

            CreateVolumeProfile();

            AssetDatabase.SaveAssets();
            return "[OK] 見た目の素材を生成しました";
        }

        // ---- 画像 ------------------------------------------------------

        /// <summary>中心が明るく、外へ向かってなめらかに消える丸</summary>
        private static Texture2D CreateRadialGlow(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center)) / center;
                float alpha = Mathf.Clamp01(1f - distance);
                alpha = alpha * alpha * (3f - 2f * alpha);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
            }

            texture.Apply();
            return texture;
        }

        /// <summary>上から差し込む光の筋。上ほど明るく、左右と下へ向かって消える</summary>
        private static Texture2D CreateBeam(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var centerX = (width - 1) * 0.5f;

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float fromTop = 1f - (float)y / (height - 1);
                // 下へ行くほど筋が太く、薄くなる
                float spread = Mathf.Lerp(0.35f, 1f, fromTop);
                float across = Mathf.Abs(x - centerX) / centerX / spread;
                float side = Mathf.Clamp01(1f - across);
                side = side * side * (3f - 2f * side);
                float along = Mathf.Pow(1f - fromTop, 0.6f) * Mathf.Clamp01(fromTop * 8f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, side * along));
            }

            texture.Apply();
            return texture;
        }

        /// <summary>背景画像からバルブの丸だけを切り抜く。縁は1ピクセルぼかして背景となじませる</summary>
        private static Texture2D CreateValveCutout()
        {
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            source.LoadImage(File.ReadAllBytes(BackgroundPath));

            int size = Mathf.CeilToInt(ValveRadiusPixel * 2f) + 2;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;

            // 画像の座標は左上原点、Texture2D は左下原点
            float sourceCenterX = ValveCenterPixel.x;
            float sourceCenterY = source.height - ValveCenterPixel.y;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - half;
                float dy = y + 0.5f - half;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(ValveRadiusPixel - distance + 0.5f);

                var color = source.GetPixelBilinear(
                    (sourceCenterX + dx) / source.width, (sourceCenterY + dy) / source.height);
                color.a = alpha;
                texture.SetPixel(x, y, color);
            }

            texture.Apply();
            Object.DestroyImmediate(source);
            return texture;
        }

        private static void WriteSprite(string name, Texture2D texture)
        {
            var path = $"{TextureDirectory}/{name}.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            // グラデーションに縞が出ないよう圧縮しない
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        // ---- 画面効果 --------------------------------------------------

        private static void CreateVolumeProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, VolumeProfilePath);
            }

            // 光のスプライトだけがにじむよう、しきい値は床の明るさより上にする
            var bloom = GetOrAdd<Bloom>(profile);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.8f);
            bloom.scatter.Override(0.7f);

            // 画面の端を落として、中央のコンベアへ目が行くようにする
            var vignette = GetOrAdd<Vignette>(profile);
            vignette.color.Override(new Color(0.05f, 0.06f, 0.1f));
            vignette.intensity.Override(0.35f);
            vignette.smoothness.Override(0.45f);

            var colorAdjustments = GetOrAdd<ColorAdjustments>(profile);
            colorAdjustments.contrast.Override(12f);
            colorAdjustments.saturation.Override(12f);

            EditorUtility.SetDirty(profile);
        }

        private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet<T>(out var existing)) return existing;

            var component = profile.Add<T>(true);
            component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }
    }
}
