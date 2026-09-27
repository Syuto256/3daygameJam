using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace GameJam.Editor.SceneDsl
{
    /// <summary>
    /// "Canvas" や "AudioManager" のような短い型名から実際の System.Type を検索する。
    /// ロード済みの全アセンブリを対象にするため、
    /// UnityEngine / UnityEngine.UI / TMPro / ユーザー独自スクリプトのいずれも解決できる。
    ///
    /// 短い名前は他のアセンブリと衝突することがあるため（例: Transform は log4net.Util.Transform、
    /// EventSystem は UnityEngine.U2D.Interface.EventSystem にもある）、
    /// 次の優先度で1つに決める。
    ///   1. 名前空間まで書かれていればそれを最優先（例: UnityEngine.UI.Image）
    ///   2. Component を探しているときは Component 派生の型だけを候補にする
    ///   3. それでも複数あればアセンブリの優先度で決める（プロジェクトのスクリプト > Unity > その他）
    /// </summary>
    public static class TypeResolver
    {
        private static Dictionary<string, Type> _index;
        private static Dictionary<string, Type> _componentIndex;
        private static Dictionary<string, Type> _fullNameIndex;

        private static void BuildIndexIfNeeded()
        {
            if (_index != null) return;

            _index = new Dictionary<string, Type>();
            _componentIndex = new Dictionary<string, Type>();
            _fullNameIndex = new Dictionary<string, Type>();

            var priorities = new Dictionary<string, int>();
            var componentPriorities = new Dictionary<string, int>();

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types; }
                catch { continue; }

                int priority = GetAssemblyPriority(asm);

                foreach (var t in types)
                {
                    if (t == null) continue;

                    // 名前空間まで書いた指定用
                    if (!string.IsNullOrEmpty(t.FullName) && !_fullNameIndex.ContainsKey(t.FullName))
                    {
                        _fullNameIndex[t.FullName] = t;
                    }

                    Register(_index, priorities, t.Name, t, priority);

                    if (typeof(Component).IsAssignableFrom(t))
                    {
                        Register(_componentIndex, componentPriorities, t.Name, t, priority);
                    }
                }
            }
        }

        private static void Register(
            Dictionary<string, Type> index, Dictionary<string, int> priorities,
            string name, Type type, int priority)
        {
            if (index.TryGetValue(name, out _) && priorities[name] >= priority) return;

            index[name] = type;
            priorities[name] = priority;
        }

        /// <summary>
        /// 同じ短い名前が複数あったときにどれを採るかの優先度。大きいほど優先
        /// </summary>
        private static int GetAssemblyPriority(Assembly asm)
        {
            string name = asm.GetName().Name ?? string.Empty;

            // このプロジェクトのスクリプト（asmdef で分けたものを含む）
            if (name.StartsWith("Assembly-CSharp", StringComparison.Ordinal) ||
                name.StartsWith("GameJam", StringComparison.Ordinal)) return 4;

            // asmdef から作られたアセンブリ（パッケージ由来を含む）
            string location = null;
            try { location = asm.Location; }
            catch { /* 動的アセンブリは Location を持たない */ }

            if (!string.IsNullOrEmpty(location) &&
                location.Replace('\\', '/').IndexOf("/Library/ScriptAssemblies/", StringComparison.OrdinalIgnoreCase) >= 0)
                return 3;

            // Unity 本体
            if (name.StartsWith("Unity", StringComparison.Ordinal)) return 2;

            // log4net や Newtonsoft.Json など、同梱されているだけの外部ライブラリ
            return 1;
        }

        /// <summary>型名から Type を探す。名前空間つきの指定にも対応する</summary>
        public static Type FindType(string name) => Find(name, _ => _index);

        /// <summary>
        /// Component 派生の型に限定して探す。
        /// "Transform" や "EventSystem" のように他アセンブリと名前が衝突する型はこちらを使う
        /// </summary>
        public static Type FindComponentType(string name)
        {
            var found = Find(name, _ => _componentIndex);
            if (found != null && typeof(Component).IsAssignableFrom(found)) return found;

            // 名前空間つきで Component 以外を指定された場合は、呼び出し側でエラーにできるよう素直に返す
            return found ?? FindType(name);
        }

        private static Type Find(string name, Func<string, Dictionary<string, Type>> indexSelector)
        {
            if (string.IsNullOrEmpty(name)) return null;
            BuildIndexIfNeeded();

            var index = indexSelector(name);

            // 名前空間まで書かれている場合はそれを最優先で解決する
            if (name.IndexOf('.') >= 0 && _fullNameIndex.TryGetValue(name, out var full)) return full;

            if (index.TryGetValue(name, out var t)) return t;

            // 大文字小文字を無視した後方互換探索
            foreach (var kv in index)
            {
                if (string.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase))
                    return kv.Value;
            }
            return null;
        }

        /// <summary>キャッシュを破棄する（スクリプトの再コンパイル後などに呼ぶ）</summary>
        public static void ResetCache()
        {
            _index = null;
            _componentIndex = null;
            _fullNameIndex = null;
        }
    }

    /// <summary>
    /// フィールド／プロパティの検索と読み書きを行うヘルパー。
    /// public フィールドはもちろん、[SerializeField] private フィールドも対象にする。
    /// 継承元の private フィールドも辿れるよう、型階層を手動で walk する。
    /// </summary>
    public static class ReflectionHelper
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        public static MemberInfo FindMember(Type type, string name)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                var fi = t.GetField(name, Flags);
                if (fi != null) return fi;

                var pi = t.GetProperty(name, Flags);
                if (pi != null && pi.CanWrite) return pi;
            }

            // 大文字小文字無視のフォールバック
            for (var t = type; t != null; t = t.BaseType)
            {
                foreach (var fi in t.GetFields(Flags))
                    if (string.Equals(fi.Name, name, StringComparison.OrdinalIgnoreCase)) return fi;

                foreach (var pi in t.GetProperties(Flags))
                    if (pi.CanWrite && string.Equals(pi.Name, name, StringComparison.OrdinalIgnoreCase)) return pi;
            }

            return null;
        }

        public static Type GetMemberType(MemberInfo member)
        {
            if (member is FieldInfo fi) return fi.FieldType;
            if (member is PropertyInfo pi) return pi.PropertyType;
            throw new ArgumentException("FieldInfo か PropertyInfo のみ対応しています");
        }

        public static void SetMemberValue(object target, MemberInfo member, object value)
        {
            if (member is FieldInfo fi) { fi.SetValue(target, value); return; }
            if (member is PropertyInfo pi) { pi.SetValue(target, value); return; }
            throw new ArgumentException("FieldInfo か PropertyInfo のみ対応しています");
        }
    }
}
