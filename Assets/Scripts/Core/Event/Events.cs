using Scripts.Core.Manager;

namespace Scripts.Core.Event
{
    /// <summary>
    /// シーン遷移要求イベント
    /// </summary>
    public readonly struct RequestSceneChangeEvent
    {
        public readonly SceneType TargetScene;

        public RequestSceneChangeEvent(SceneType targetScene)
        {
            TargetScene = targetScene;
        }
    }
}