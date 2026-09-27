using System.Collections.Generic;

namespace GameJam.Editor.SceneDsl
{
    /// <summary>
    /// Component { key: value ... } 1個分のデータ
    /// </summary>
    public class ComponentDef
    {
        public string TypeName;
        public readonly List<(string Key, string Value)> Fields = new List<(string, string)>();
    }

    /// <summary>
    /// Object { ... } 1個分のデータ
    /// </summary>
    public class ObjectDef
    {
        public string Name;
        public string Type;
        public string Parent;
        public readonly List<ComponentDef> Components = new List<ComponentDef>();

        public override string ToString() => $"Object(Name={Name}, Type={Type}, Parent={Parent})";
    }

    public enum LogLevel { Info, Warning, Error }

    public struct LogEntry
    {
        public LogLevel Level;
        public string Message;
    }

    /// <summary>
    /// ビルド/検証処理中のログを蓄積する
    /// </summary>
    public class BuildLog
    {
        public readonly List<LogEntry> Entries = new List<LogEntry>();

        public void Info(string message) => Entries.Add(new LogEntry { Level = LogLevel.Info, Message = message });
        public void Warning(string message) => Entries.Add(new LogEntry { Level = LogLevel.Warning, Message = message });
        public void Error(string message) => Entries.Add(new LogEntry { Level = LogLevel.Error, Message = message });

        public int ErrorCount
        {
            get
            {
                int c = 0;
                foreach (var e in Entries) if (e.Level == LogLevel.Error) c++;
                return c;
            }
        }

        public int WarningCount
        {
            get
            {
                int c = 0;
                foreach (var e in Entries) if (e.Level == LogLevel.Warning) c++;
                return c;
            }
        }
    }
}
