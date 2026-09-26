using UnityEngine;

namespace Scripts.Core.Singleton
{
    /// <summary>
    /// MonoBehaviour用の汎用シングルトン基底クラス
    /// </summary>
    public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T instance;

        /// <summary>
        /// インスタンスが存在するかどうか（安全なヌルチェック用）
        /// </summary>
        public static bool HasInstance => instance != null;
        
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
            if (instance == null)
            {
                instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}