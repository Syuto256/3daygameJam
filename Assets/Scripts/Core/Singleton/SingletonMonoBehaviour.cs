using UnityEngine;

namespace Scripts.Core.Singleton
{
    /// <summary>
    /// MonoBehaviour用の汎用シングルトン基底クラス
    /// </summary>
    public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T instance;

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<T>();

                    if (instance == null)
                    {
                        Debug.LogError($"{typeof(T).Name} のインスタンスがシーン内に見つかりません。");
                    }
                }
                return instance;
            }
        }

        protected virtual void Awake()
        {
            if (this != Instance)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
        }
    }
}