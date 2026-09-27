namespace GameJam.Editor.SceneDsl
{
    /// <summary>
    ///     Scene DSL ツールが使うファイルの場所。
    ///
    ///     このプロジェクトでは DSL での組み立て先を Imata シーンだけに限定している。
    ///     他のシーン（game / Title / Manager）はチームメンバーが手で編集しているため、
    ///     DSL から書き換えないようにする
    /// </summary>
    public static class SceneDslPaths
    {
        /// <summary>DSL で組み立ててよい唯一のシーン</summary>
        public const string TargetScenePath = "Assets/Scenes/Imata.unity";

        /// <summary>DSL ファイルの置き場</summary>
        public const string DslDirectory = "Assets/Editor/SceneDsl/Dsl";

        /// <summary>Imata シーンを組み立てる DSL</summary>
        public const string TargetDslPath = DslDirectory + "/imata_scene.txt";

        /// <summary>
        ///     シーンを DSL に書き出したときの出力先。
        ///     いまのシーンの中身を確認するための控えなので、組み立て用の DSL とは分けて Git の対象外に置く
        /// </summary>
        public const string ExportDirectory = "Logs/SceneDslExport";

        /// <summary>シーンごとのプレハブ置き場（無ければ使わない）</summary>
        public const string ScenePrefabDirectory = "Assets/Prefab/Scene";

        /// <summary>組み立て先として許可されたシーンか</summary>
        public static bool IsTargetScene(string scenePath) => scenePath == TargetScenePath;
    }
}
