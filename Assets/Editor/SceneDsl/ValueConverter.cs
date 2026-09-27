using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace GameJam.Editor.SceneDsl
{
    /// <summary>
    /// DSL の値文字列（例: "0,0.5" "#FFAA00" "@ConfigCanvas" "&lt;Sprite&gt;Foo" "OutCubic"）を
    /// フィールド／プロパティの実際の型に応じて変換する。
    /// </summary>
    public static class ValueConverter
    {
        private static readonly Regex AssetRefPattern = new Regex(@"^<([^>]+)>(.*)$", RegexOptions.Compiled);

        public static object Convert(string raw, Type targetType, Dictionary<string, GameObject> registry, BuildLog log)
        {
            raw = raw?.Trim() ?? string.Empty;

            if (raw.StartsWith("@"))
            {
                return ResolveObjectRef(raw.Substring(1).Trim(), targetType, registry);
            }

            var assetMatch = AssetRefPattern.Match(raw);
            if (assetMatch.Success)
            {
                string tag = assetMatch.Groups[1].Value;
                string name = assetMatch.Groups[2].Value.Trim();
                return AssetResolver.Resolve(tag, name, targetType, log);
            }

            if (targetType == typeof(string)) return Unescape(raw);

            if (targetType.IsEnum)
            {
                try { return Enum.Parse(targetType, raw, true); }
                catch
                {
                    throw new Exception($"'{raw}' は {targetType.Name} の値ではありません。有効な値: {string.Join(", ", Enum.GetNames(targetType))}");
                }
            }

            if (targetType == typeof(bool)) return ParseBool(raw);
            if (targetType == typeof(Color)) return ParseColor(raw);

            // TextMeshPro の faceColor などは Color32。書式は Color と同じ
            if (targetType == typeof(Color32)) return (Color32)ParseColor(raw);

            if (targetType == typeof(int)) return int.Parse(raw, CultureInfo.InvariantCulture);
            if (targetType == typeof(uint)) return uint.Parse(raw, CultureInfo.InvariantCulture);
            if (targetType == typeof(long)) return long.Parse(raw, CultureInfo.InvariantCulture);
            if (targetType == typeof(float)) return float.Parse(raw, NumberStyles.Float, CultureInfo.InvariantCulture);
            if (targetType == typeof(double)) return double.Parse(raw, NumberStyles.Float, CultureInfo.InvariantCulture);

            // レイヤーマスクは中身がただの数値。専用の型なので個別に包む
            if (targetType == typeof(LayerMask)) return (LayerMask)int.Parse(raw, CultureInfo.InvariantCulture);
            if (targetType == typeof(RenderingLayerMask))
                return (RenderingLayerMask)uint.Parse(raw, CultureInfo.InvariantCulture);

            if (targetType == typeof(Vector2)) return ParseVector2(raw);
            if (targetType == typeof(Vector3)) return ParseVector3(raw);
            if (targetType == typeof(Vector4)) return ParseVector4(raw);
            if (targetType == typeof(Vector2Int)) { var v = ParseVector2(raw); return new Vector2Int((int)v.x, (int)v.y); }
            if (targetType == typeof(Vector3Int)) { var v = ParseVector3(raw); return new Vector3Int((int)v.x, (int)v.y, (int)v.z); }
            if (targetType == typeof(Rect)) return ParseRect(raw);

            // フォールバック：標準変換を試みる
            try
            {
                return System.Convert.ChangeType(raw, targetType, CultureInfo.InvariantCulture);
            }
            catch
            {
                throw new Exception($"型 {targetType.Name} への変換に対応していません（値: '{raw}'）");
            }
        }

        private static object ResolveObjectRef(string refName, Type targetType, Dictionary<string, GameObject> registry)
        {
            if (!registry.TryGetValue(refName, out var targetGo) || targetGo == null)
            {
                // DSL で作っていない相手なら、既にシーンに居るオブジェクトを名前で探す
                // （作業シーンから取り込んだ Main Camera / Player を参照したいとき）
                targetGo = FindInSceneByName(refName);
                if (targetGo == null)
                    throw new Exception($"参照 '@{refName}' に対応するオブジェクトが見つかりません");
            }

            if (targetType == typeof(GameObject)) return targetGo;
            if (targetType == typeof(Transform)) return targetGo.transform;
            if (targetType == typeof(RectTransform))
            {
                var rt = targetGo.GetComponent<RectTransform>();
                if (rt == null) throw new Exception($"'{refName}' に RectTransform がありません");
                return rt;
            }

            if (typeof(Component).IsAssignableFrom(targetType))
            {
                var comp = targetGo.GetComponent(targetType);
                if (comp == null)
                    throw new Exception($"'{refName}' に {targetType.Name} コンポーネントが見つかりません");
                return comp;
            }

            throw new Exception($"参照型 {targetType.Name} には対応していません（@{refName}）");
        }

        /// <summary>既にシーンに存在するオブジェクトを名前で探す（非アクティブも対象）</summary>
        private static GameObject FindInSceneByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
                if (t.name == name)
                    return t.gameObject;

            return null;
        }

        public static bool ParseBool(string raw)
        {
            raw = raw.Trim().ToLowerInvariant();
            if (raw == "true" || raw == "1" || raw == "yes") return true;
            if (raw == "false" || raw == "0" || raw == "no") return false;
            return bool.Parse(raw);
        }

        private static Color ParseColor(string raw)
        {
            if (ColorUtility.TryParseHtmlString(raw, out var c)) return c;
            throw new Exception($"'{raw}' はカラーコードとして解釈できません（例: #FFAA00 / #FFAA00FF）");
        }

        private static float[] ParseFloats(string raw, int expectedCount)
        {
            var parts = raw.Split(',');
            var result = new float[expectedCount];
            for (int i = 0; i < expectedCount; i++)
            {
                if (i < parts.Length)
                    result[i] = float.Parse(parts[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
                else
                    result[i] = 0f;
            }
            return result;
        }

        private static Vector2 ParseVector2(string raw) { var f = ParseFloats(raw, 2); return new Vector2(f[0], f[1]); }
        private static Vector3 ParseVector3(string raw) { var f = ParseFloats(raw, 3); return new Vector3(f[0], f[1], f[2]); }
        private static Vector4 ParseVector4(string raw) { var f = ParseFloats(raw, 4); return new Vector4(f[0], f[1], f[2], f[3]); }
        private static Rect ParseRect(string raw) { var f = ParseFloats(raw, 4); return new Rect(f[0], f[1], f[2], f[3]); }

        /// <summary>DSL 中の "\n" "\t" のようなリテラル2文字を実際の改行・タブに戻す</summary>
        private static string Unescape(string raw)
        {
            var sb = new System.Text.StringBuilder(raw.Length);
            for (int i = 0; i < raw.Length; i++)
            {
                if (raw[i] == '\\' && i + 1 < raw.Length)
                {
                    char next = raw[i + 1];
                    if (next == 'n') { sb.Append('\n'); i++; continue; }
                    if (next == 't') { sb.Append('\t'); i++; continue; }
                    if (next == '\\') { sb.Append('\\'); i++; continue; }
                }
                sb.Append(raw[i]);
            }
            return sb.ToString();
        }
    }
}
