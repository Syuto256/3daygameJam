using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GameJam.Editor.SceneDsl
{
    /// <summary>
    /// "&lt;Sprite&gt;Option_Back" のようなアセット参照文字列を実アセットへ解決する。
    /// プロジェクト全体を対象に AssetDatabase.FindAssets で検索し、
    /// ファイル名が完全一致するものを優先する。結果はキャッシュする。
    /// </summary>
    public static class AssetResolver
    {
        private static readonly Dictionary<(string tag, string name), UnityEngine.Object> _cache
            = new Dictionary<(string, string), UnityEngine.Object>();

        public static void ResetCache() => _cache.Clear();

        public static UnityEngine.Object Resolve(string tag, string name, Type desiredType, BuildLog log)
        {
            var key = (tag, name);
            if (_cache.TryGetValue(key, out var cached)) return cached;

            Type searchType = ResolveAssetType(tag);
            UnityEngine.Object found = null;

            try
            {
                if (searchType == typeof(GameObject) || string.Equals(tag, "Prefab", StringComparison.OrdinalIgnoreCase))
                {
                    found = FindByName(name, "t:Prefab");
                }
                else
                {
                    string filter = searchType != null ? $"t:{searchType.Name}" : null;
                    found = FindByName(name, filter);
                }
            }
            catch (Exception e)
            {
                log?.Error($"アセット検索中に例外が発生しました (<{tag}>{name}): {e.Message}");
            }

            if (found == null)
            {
                log?.Warning($"アセットが見つかりません: <{tag}>{name}");
            }
            else if (desiredType != null && !desiredType.IsInstanceOfType(found))
            {
                // 型が完全一致しない場合でも、Texture2D<-Sprite の代表画像取得のような
                // 一般的なケースを試みる
                var adapted = TryAdapt(found, desiredType);
                if (adapted != null) found = adapted;
                else log?.Warning($"アセット <{tag}>{name} は {desiredType.Name} 型に変換できませんでした（実際の型: {found.GetType().Name}）");
            }

            _cache[key] = found;
            return found;
        }

        private static UnityEngine.Object TryAdapt(UnityEngine.Object asset, Type desiredType)
        {
            if (desiredType == typeof(Sprite) && asset is Texture2D tex)
            {
                string path = AssetDatabase.GetAssetPath(tex);
                foreach (var sub in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
                {
                    if (sub is Sprite s) return s;
                }
            }
            return null;
        }

        /// <summary>
        /// サブアセットを探すときの入れ物。
        /// Mesh は FBX の中、Sprite は画像の中にいるので、
        /// ファイル名では見つからず、親ファイルを開いて中身を見る必要がある
        /// </summary>
        private static string ContainerFilterFor(Type searchType)
        {
            if (searchType == typeof(Mesh) || searchType == typeof(AnimationClip)) return "t:Model";
            if (searchType == typeof(Sprite)) return "t:Texture2D";
            return null;
        }

        private static UnityEngine.Object FindByName(string name, string typeFilter)
        {
            Type searchType = null;
            if (!string.IsNullOrEmpty(typeFilter) && typeFilter.StartsWith("t:", StringComparison.Ordinal))
                searchType = TypeResolver.FindType(typeFilter.Substring(2));

            // まずファイル名で当てにいく
            string query = string.IsNullOrEmpty(typeFilter) ? name : $"{name} {typeFilter}";
            var byFileName = ScanPaths(AssetDatabase.FindAssets(query), name, searchType);
            if (byFileName != null) return byFileName;

            // 見つからなければ入れ物のファイルを開き、中のサブアセットを探す
            // （FBX の中の Mesh、画像の中の Sprite など）
            string container = ContainerFilterFor(searchType);
            if (container == null) return null;

            return ScanPaths(AssetDatabase.FindAssets(container), name, searchType);
        }

        /// <summary>
        /// 候補パスを順に開き、名前と型が合うものを返す。
        /// 名前の完全一致を最優先し、なければ型が合うものを拾う
        /// </summary>
        private static UnityEngine.Object ScanPaths(string[] guids, string name, Type searchType)
        {
            if (guids == null || guids.Length == 0) return null;

            UnityEngine.Object fallback = null;

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;

                bool fileNameMatches =
                    string.Equals(Path.GetFileNameWithoutExtension(path), name, StringComparison.OrdinalIgnoreCase);

                // メインアセットが目的の型なら、ファイル名が一致した時点で決まり
                var main = AssetDatabase.LoadMainAssetAtPath(path);
                if (IsWanted(main, searchType))
                {
                    if (fileNameMatches) return main;
                    if (fallback == null) fallback = main;
                }

                // サブアセットは名前で突き合わせる
                foreach (var sub in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
                {
                    if (!IsWanted(sub, searchType)) continue;

                    if (string.Equals(sub.name, name, StringComparison.OrdinalIgnoreCase)) return sub;

                    // 画像1枚に Sprite が1つだけなら、ファイル名の一致で決めてよい
                    if (fileNameMatches && fallback == null) fallback = sub;
                }
            }

            return fallback;
        }

        private static bool IsWanted(UnityEngine.Object asset, Type searchType)
        {
            if (asset == null) return false;
            return searchType == null || searchType.IsInstanceOfType(asset);
        }

        private static Type ResolveAssetType(string tag)
        {
            switch (tag)
            {
                case "Sprite": return typeof(Sprite);
                case "Material": return typeof(Material);
                case "Texture": return typeof(Texture2D);
                case "Texture2D": return typeof(Texture2D);
                case "Font": return typeof(Font);
                case "AudioClip": return typeof(AudioClip);
                case "Mesh": return typeof(Mesh);
                case "AnimationClip": return typeof(AnimationClip);
                case "Prefab": return typeof(GameObject);
                default:
                    // TMP_FontAsset や独自 ScriptableObject 型などはクラス名から解決する
                    return TypeResolver.FindType(tag);
            }
        }
    }
}
