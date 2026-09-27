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
    ///       - 正解・不正解の演出に使うキラキラ・画面の縁の光・パーティクル用マテリアル
    ///       - HP 表示の部品（配置見本の画像から、部品ごとに余白を切り落としたもの）
    ///       - 画面効果（Bloom / Vignette / 色調整）の Volume Profile
    ///
    ///     何度実行しても同じものが上書きされるだけなので、値を変えたら実行し直せばよい
    /// </summary>
    public static class VisualAssetGenerator
    {
        private const string TextureDirectory = "Assets/Visual/Textures";
        private const string MaterialDirectory = "Assets/Visual/Materials";
        private const string ParticleShaderName = "GameJam/Particle";
        private const string VolumeProfilePath = "Assets/Visual/InGameVolumeProfile.asset";
        private const string BackgroundPath = "Assets/Images/Background/In-GameBackground.png";
        private const string HPImageDirectory = "Assets/Images/HP";

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
            WriteSprite("Sparkle", CreateSparkle(64));
            WriteSprite("Edge_Glow", CreateEdgeGlow(256, 144));

            // HP の画像は画面全体の配置見本なので、部品ごとに切り出す。UI で縮めて使うので半分の大きさにする
            WriteSprite("HP_Panel", CropToContent($"{HPImageDirectory}/Panel_Base.png", 0.5f));
            WriteSprite("HP_NamePlate", CropToContent($"{HPImageDirectory}/Error_NamePlate.png", 0.5f));
            WriteSprite("HP_LampOn", CropToContent($"{HPImageDirectory}/MissLampe_on.png", 0.5f));
            WriteSprite("HP_LampOff", CropToContent($"{HPImageDirectory}/Miss_Lamp_off.png", 0.5f));

            // 正解のキラキラは加算で光らせ、不正解の煙は半透明で重ねる
            CreateParticleMaterial("ParticleAdditive", "Sparkle", BlendMode.SrcAlpha, BlendMode.One);
            CreateParticleMaterial("ParticleSmoke", "Glow_Soft", BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha);

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

        /// <summary>十字に光が伸びるキラキラ</summary>
        private static Texture2D CreateSparkle(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = (x - center) / center;
                float ny = (y - center) / center;
                float distance = Mathf.Sqrt(nx * nx + ny * ny);

                float core = Mathf.Clamp01(1f - distance * 3f);
                float rayX = Mathf.Exp(-Mathf.Abs(ny) * 14f) * Mathf.Clamp01(1f - Mathf.Abs(nx));
                float rayY = Mathf.Exp(-Mathf.Abs(nx) * 14f) * Mathf.Clamp01(1f - Mathf.Abs(ny));
                float alpha = Mathf.Clamp01(core * core + Mathf.Max(rayX, rayY));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }

            texture.Apply();
            return texture;
        }

        /// <summary>中央が透明で、画面の縁へ向かって濃くなる枠。画面全体に引き伸ばして使う</summary>
        private static Texture2D CreateEdgeGlow(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float u = Mathf.Abs((float)x / (width - 1) - 0.5f) * 2f;
                float v = Mathf.Abs((float)y / (height - 1) - 0.5f) * 2f;
                float dx = Mathf.Clamp01((u - 0.55f) / 0.45f);
                float dy = Mathf.Clamp01((v - 0.45f) / 0.55f);
                float alpha = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
            }

            texture.Apply();
            return texture;
        }

        /// <summary>透明な余白を切り落とし、scale 倍に縮めた画像を返す</summary>
        private static Texture2D CropToContent(string path, float scale)
        {
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            source.LoadImage(File.ReadAllBytes(path));

            int minX = source.width, minY = source.height, maxX = -1, maxY = -1;
            var pixels = source.GetPixels32();
            for (int y = 0; y < source.height; y++)
            for (int x = 0; x < source.width; x++)
            {
                if (pixels[y * source.width + x].a < 8) continue;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }

            int cropWidth = maxX - minX + 1;
            int cropHeight = maxY - minY + 1;
            int width = Mathf.Max(1, Mathf.RoundToInt(cropWidth * scale));
            int height = Mathf.Max(1, Mathf.RoundToInt(cropHeight * scale));

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float u = (minX + (x + 0.5f) / width * cropWidth) / source.width;
                float v = (minY + (y + 0.5f) / height * cropHeight) / source.height;
                texture.SetPixel(x, y, source.GetPixelBilinear(u, v));
            }

            texture.Apply();
            Object.DestroyImmediate(source);
            return texture;
        }

        private static void CreateParticleMaterial(string name, string textureName, BlendMode source, BlendMode destination)
        {
            var path = $"{MaterialDirectory}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find(ParticleShaderName));
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = Shader.Find(ParticleShaderName);
            material.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureDirectory}/{textureName}.png"));
            material.SetFloat("_SrcBlend", (float)source);
            material.SetFloat("_DstBlend", (float)destination);
            EditorUtility.SetDirty(material);
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
