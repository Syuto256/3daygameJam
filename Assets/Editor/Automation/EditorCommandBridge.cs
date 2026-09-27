using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace GameJam.Editor.Automation
{
    /// <summary>
    ///     開いているエディタへ、外部（コマンドラインのツールなど）から作業を頼むための窓口。
    ///
    ///     Temp/EditorBridge/request.json を置くと、エディタがそれを読んで実行し、
    ///     結果を Temp/EditorBridge/result_&lt;id&gt;.json に書き出す。
    ///     エディタを閉じずにアセットの再読み込み・静的メソッドの実行・テストの実行ができる。
    ///
    ///     request.json の例:
    ///       { "id": "1", "command": "refresh" }
    ///       { "id": "2", "command": "execute", "argument": "Namespace.Class.Method" }
    ///       { "id": "3", "command": "runTests", "argument": "EditMode" }
    /// </summary>
    [InitializeOnLoad]
    public static class EditorCommandBridge
    {
        public const string BridgeDirectory = "Temp/EditorBridge";

        private static string RequestPath => Path.Combine(BridgeDirectory, "request.json");
        private static string StatePath => Path.Combine(BridgeDirectory, "state.json");
        private static string CompilePath => Path.Combine(BridgeDirectory, "compile.json");

        private const double PollIntervalSeconds = 0.5;

        private static double _nextPollTime;

        [Serializable]
        private class Request
        {
            public string id;
            public string command;
            public string argument;
        }

        [Serializable]
        public class Result
        {
            public string id;
            public string command;
            public bool success;
            public string message;
        }

        [Serializable]
        private class State
        {
            public string loadedAt;
            public bool isCompiling;
        }

        [Serializable]
        private class CompileReport
        {
            public string finishedAt;
            public List<string> errors = new();
            public List<string> warnings = new();
        }

        private static readonly CompileReport _compileReport = new();

        static EditorCommandBridge()
        {
            Directory.CreateDirectory(BridgeDirectory);

            EditorApplication.update += Poll;
            CompilationPipeline.compilationStarted += _ => OnCompilationStarted();
            CompilationPipeline.assemblyCompilationFinished += CollectCompilerMessages;
            CompilationPipeline.compilationFinished += _ => WriteCompileReport();

            WriteJson(StatePath, new State { loadedAt = Now(), isCompiling = EditorApplication.isCompiling });
        }

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < _nextPollTime) return;
            _nextPollTime = EditorApplication.timeSinceStartup + PollIntervalSeconds;

            // コンパイル中や再生への切り替え中に実行すると、途中でドメインが入れ替わって結果を失う
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (!File.Exists(RequestPath)) return;

            Request request;
            try
            {
                request = JsonUtility.FromJson<Request>(File.ReadAllText(RequestPath, Encoding.UTF8));
                File.Delete(RequestPath);
            }
            catch (IOException)
            {
                // 書き込みの途中で読んだ。次の周回で読み直す
                return;
            }

            if (request == null || string.IsNullOrEmpty(request.id)) return;

            Execute(request);
        }

        private static void Execute(Request request)
        {
            var result = new Result { id = request.id, command = request.command };

            try
            {
                switch (request.command)
                {
                    case "refresh":
                        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                        result.success = true;
                        result.message = "Refresh を実行しました。コンパイル結果は compile.json を参照";
                        break;

                    case "execute":
                        result.message = InvokeStaticMethod(request.argument);
                        result.success = true;
                        break;

                    case "runTests":
                        EditorTestRunner.Run(request.id, request.argument);
                        result.success = true;
                        result.message = "テストを開始しました。結果は tests_<id>.json を参照";
                        break;

                    default:
                        result.message = $"未知のコマンドです: {request.command}";
                        break;
                }
            }
            catch (Exception e)
            {
                var inner = e is TargetInvocationException tie && tie.InnerException != null ? tie.InnerException : e;
                result.success = false;
                result.message = inner.ToString();
            }

            WriteResult(result);
        }

        /// <summary>"Namespace.Class.Method" 形式の引数なし static メソッドを呼ぶ。戻り値が string ならそれを返す</summary>
        private static string InvokeStaticMethod(string qualifiedName)
        {
            var separator = qualifiedName.LastIndexOf('.');
            if (separator <= 0) throw new ArgumentException($"メソッド名の形式が不正です: {qualifiedName}");

            var typeName = qualifiedName.Substring(0, separator);
            var methodName = qualifiedName.Substring(separator + 1);

            var type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(typeName, false))
                .FirstOrDefault(t => t != null);
            if (type == null) throw new ArgumentException($"型が見つかりません: {typeName}");

            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                null, Type.EmptyTypes, null);
            if (method == null) throw new ArgumentException($"引数なしの static メソッドが見つかりません: {qualifiedName}");

            var returned = method.Invoke(null, null);
            return returned as string ?? $"{qualifiedName} を実行しました";
        }

        public static void WriteResult(Result result) =>
            WriteJson(Path.Combine(BridgeDirectory, $"result_{result.id}.json"), result);

        public static void WriteJson(string path, object data)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            // 読み手が書きかけを読まないよう、別名で書いてから置き換える
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(data, true), new UTF8Encoding(false));
            if (File.Exists(path)) File.Delete(path);
            File.Move(temporary, path);
        }

        private static void OnCompilationStarted()
        {
            WriteJson(StatePath, new State { loadedAt = Now(), isCompiling = true });
            _compileReport.errors.Clear();
            _compileReport.warnings.Clear();
        }

        private static void CollectCompilerMessages(string assemblyPath, CompilerMessage[] messages)
        {
            foreach (var message in messages)
            {
                var text = $"{message.file}({message.line},{message.column}): {message.message}";
                if (message.type == CompilerMessageType.Error) _compileReport.errors.Add(text);
                else _compileReport.warnings.Add(text);
            }
        }

        private static void WriteCompileReport()
        {
            _compileReport.finishedAt = Now();
            WriteJson(CompilePath, _compileReport);
        }

        private static string Now() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}
