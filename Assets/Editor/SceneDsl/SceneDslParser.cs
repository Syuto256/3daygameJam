using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace GameJam.Editor.SceneDsl
{
    /// <summary>
    /// DSLテキストを ObjectDef のリストへ変換する。
    ///
    /// 文法:
    ///   Object
    ///   {
    ///       Name: XXX
    ///       Type: XXX
    ///       Parent: XXX      // 省略可（省略時はシーンルート）
    ///       Component: TypeName
    ///       {
    ///           key: value
    ///           key: value
    ///       }
    ///   }
    ///
    /// - "//" から行末まではコメントとして無視される
    /// - Component ブロックは 0 個以上、何個でも書ける
    /// - Type: Existing の場合、Component 以外の意味は「既存の子オブジェクトを探して値を上書きする」
    /// </summary>
    public static class SceneDslParser
    {
        public static List<ObjectDef> Parse(string dslText, BuildLog log)
        {
            var result = new List<ObjectDef>();
            if (string.IsNullOrEmpty(dslText)) return result;

            string stripped = StripComments(dslText);

            int searchPos = 0;
            while (true)
            {
                var match = Regex.Match(stripped.Substring(searchPos), @"\bObject\b");
                if (!match.Success) break;
                int objectKeywordIdx = searchPos + match.Index;

                int braceIdx = stripped.IndexOf('{', objectKeywordIdx);
                if (braceIdx < 0)
                {
                    log?.Error("'Object' の後に '{' が見つかりません。パースを中断します。");
                    break;
                }

                // "Object" と "{" の間に別のトークンが挟まっていないか簡易チェック
                string between = stripped.Substring(objectKeywordIdx + "Object".Length, braceIdx - (objectKeywordIdx + "Object".Length));
                if (!IsWhitespaceOnly(between))
                {
                    // "Object" という単語が別の文脈で使われているだけ。次を探す。
                    searchPos = objectKeywordIdx + "Object".Length;
                    continue;
                }

                string body;
                int endIdx;
                try
                {
                    (body, endIdx) = ExtractBalanced(stripped, braceIdx);
                }
                catch (Exception e)
                {
                    log?.Error($"波括弧の対応が取れていません（位置 {braceIdx} 付近）: {e.Message}");
                    break;
                }

                var def = ParseObjectBody(body, log);
                if (def != null) result.Add(def);

                searchPos = endIdx;
            }

            return result;
        }

        static bool IsWhitespaceOnly(string s)
        {
            foreach (char c in s) if (!char.IsWhiteSpace(c)) return false;
            return true;
        }

        /// <summary>
        /// s[openBraceIndex] が '{' である前提で、対応する '}' までの中身を返す。
        /// 戻り値の endIndex は対応する '}' の次の位置。
        /// </summary>
        public static (string inner, int endIndex) ExtractBalanced(string s, int openBraceIndex)
        {
            if (s[openBraceIndex] != '{') throw new ArgumentException("openBraceIndex が '{' を指していません");
            int depth = 0;
            for (int i = openBraceIndex; i < s.Length; i++)
            {
                if (s[i] == '{') depth++;
                else if (s[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        string inner = s.Substring(openBraceIndex + 1, i - openBraceIndex - 1);
                        return (inner, i + 1);
                    }
                }
            }
            throw new Exception("閉じ括弧 '}' が見つかりません");
        }

        static ObjectDef ParseObjectBody(string body, BuildLog log)
        {
            var (components, remainder) = ExtractComponents(body, log);

            var def = new ObjectDef();
            foreach (var (key, value) in ParseFieldLines(remainder))
            {
                switch (key)
                {
                    case "Name": def.Name = value; break;
                    case "Type": def.Type = value; break;
                    case "Parent": def.Parent = value; break;
                    default:
                        log?.Warning($"Object 直下の未知のキーです（無視されます）: {key}");
                        break;
                }
            }
            def.Components.AddRange(components);

            if (string.IsNullOrEmpty(def.Name))
            {
                log?.Error("Name が指定されていない Object ブロックがあります。このブロックはスキップされます。");
                return null;
            }
            if (string.IsNullOrEmpty(def.Type)) def.Type = "Empty";

            return def;
        }

        static (List<ComponentDef> comps, string remainder) ExtractComponents(string body, BuildLog log)
        {
            var comps = new List<ComponentDef>();
            var spans = new List<(int start, int end)>();

            int searchPos = 0;
            while (true)
            {
                var match = Regex.Match(body.Substring(searchPos), @"\bComponent\s*:");
                if (!match.Success) break;
                int idx = searchPos + match.Index;
                int colonIdx = body.IndexOf(':', idx);

                int braceIdx = body.IndexOf('{', colonIdx);
                if (braceIdx < 0)
                {
                    log?.Error("'Component:' の後に '{' が見つかりません。");
                    break;
                }

                string typeName = body.Substring(colonIdx + 1, braceIdx - colonIdx - 1).Trim();

                string inner;
                int endIdx;
                try
                {
                    (inner, endIdx) = ExtractBalanced(body, braceIdx);
                }
                catch (Exception e)
                {
                    log?.Error($"Component '{typeName}' の波括弧が対応していません: {e.Message}");
                    break;
                }

                var comp = new ComponentDef { TypeName = typeName };
                foreach (var (key, value) in ParseFieldLines(inner))
                {
                    comp.Fields.Add((key, value));
                }
                comps.Add(comp);
                spans.Add((idx, endIdx));

                searchPos = endIdx;
            }

            var sb = new StringBuilder();
            int cursor = 0;
            foreach (var (start, end) in spans)
            {
                sb.Append(body.Substring(cursor, start - cursor));
                cursor = end;
            }
            sb.Append(body.Substring(cursor));

            return (comps, sb.ToString());
        }

        static IEnumerable<(string Key, string Value)> ParseFieldLines(string text)
        {
            var lines = text.Replace("\r\n", "\n").Split('\n');
            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();
                if (line.Length == 0) continue;

                int colonIdx = line.IndexOf(':');
                if (colonIdx < 0) continue;

                string key = line.Substring(0, colonIdx).Trim();
                string value = line.Substring(colonIdx + 1).Trim();
                if (key.Length == 0) continue;

                yield return (key, value);
            }
        }

        static string StripComments(string text)
        {
            var lines = text.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                int idx = lines[i].IndexOf("//", StringComparison.Ordinal);
                if (idx >= 0) lines[i] = lines[i].Substring(0, idx);
            }
            return string.Join("\n", lines);
        }
    }
}
