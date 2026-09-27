using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace GameJam.Editor.Automation
{
    /// <summary>
    ///     EditorCommandBridge からテストを走らせ、結果を Temp/EditorBridge/tests_&lt;id&gt;.json に書き出す。
    ///     PlayMode テストはドメインの再読み込みをまたぐので、実行中の id は SessionState に預けておく
    /// </summary>
    [InitializeOnLoad]
    public static class EditorTestRunner
    {
        private const string PendingIdKey = "GameJam.EditorTestRunner.PendingId";

        [Serializable]
        private class TestReport
        {
            public string id;
            public string mode;
            public string finishedAt;
            public int passed;
            public int failed;
            public int skipped;
            public double durationSeconds;
            public List<string> failures = new();
            public List<string> tests = new();
        }

        static EditorTestRunner()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Callbacks());
        }

        /// <param name="argument">"EditMode" / "PlayMode"。"PlayMode|名前の一部" のように絞り込みも書ける</param>
        public static void Run(string id, string argument)
        {
            var parts = (argument ?? "EditMode").Split('|');
            var mode = parts[0] == "PlayMode" ? TestMode.PlayMode : TestMode.EditMode;

            var filter = new Filter { testMode = mode };
            if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]))
                filter.groupNames = new[] { parts[1] };

            SessionState.SetString(PendingIdKey, id);

            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.Execute(new ExecutionSettings(filter));
        }

        private class Callbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }

            public void RunFinished(ITestResultAdaptor result)
            {
                var id = SessionState.GetString(PendingIdKey, string.Empty);
                if (string.IsNullOrEmpty(id)) return;
                SessionState.EraseString(PendingIdKey);

                var report = new TestReport
                {
                    id = id,
                    mode = result.Test.TestMode.ToString(),
                    finishedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    passed = result.PassCount,
                    failed = result.FailCount,
                    skipped = result.SkipCount,
                    durationSeconds = result.Duration
                };
                CollectLeaves(result, report);

                EditorCommandBridge.WriteJson(
                    Path.Combine(EditorCommandBridge.BridgeDirectory, $"tests_{id}.json"), report);
            }

            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor result) { }

            private static void CollectLeaves(ITestResultAdaptor node, TestReport report)
            {
                if (!node.HasChildren)
                {
                    report.tests.Add($"{node.TestStatus}: {node.Test.FullName}");
                    if (node.TestStatus == TestStatus.Failed)
                        report.failures.Add($"{node.Test.FullName}\n{node.Message}\n{node.StackTrace}");
                    return;
                }

                foreach (var child in node.Children) CollectLeaves(child, report);
            }
        }
    }
}
