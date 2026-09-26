using System;
using System.Collections;
using System.Collections.Generic;

namespace Scripts.Core.Event
{
    /// <summary>
    /// アプリケーション全体のイベントを仲介する静的イベントバス
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<object>> Subscribers = new();

        /// <summary>
        /// イベントの購読（リスナー登録）
        /// </summary>
        public static void Subscribe<T>(Action<T> action)
        {
            Type type = typeof(T);
            if (!Subscribers.ContainsKey(type))
            {
                Subscribers[type] = new List<object>();
            }
            Subscribers[type].Add(action);
        }

        /// <summary>
        /// イベントの購読解除
        /// </summary>
        public static void Unsubscribe<T>(Action<T> action)
        {
            Type type = typeof(T);
            if (Subscribers.TryGetValue(type, out var list))
            {
                list.Remove(action);
            }
        }

        /// <summary>
        /// イベントの発行
        /// </summary>
        public static void Publish<T>(T eventMessage)
        {
            Type type = typeof(T);
            if (Subscribers.TryGetValue(type, out var list))
            {
                // 呼び出し中のリスト変更に対応するため複製して実行
                var targets = new List<object>(list);
                foreach (var target in targets)
                {
                    if (target is Action<T> action)
                    {
                        action.Invoke(eventMessage);
                    }
                }
            }
        }
    }
}