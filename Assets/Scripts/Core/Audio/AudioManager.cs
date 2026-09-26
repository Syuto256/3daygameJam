using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Scripts.Core.Singleton;
using Scripts.Core.Event;

namespace Scripts.Core.Audio
{
    /// <summary>
    /// BGM・SEの再生・音量を管理するマネージャークラス
    /// </summary>
    public class AudioManager : SingletonMonoBehaviour<AudioManager>
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource seSource;

        [Header("Audio Clips")]
        [SerializeField] private List<AudioClip> bgmClips = new();
        [SerializeField] private List<AudioClip> seClips = new();

        private readonly Dictionary<string, AudioClip> bgmDict = new();
        private readonly Dictionary<string, AudioClip> seDict = new();

        private Coroutine bgmFadeCoroutine;

        protected override void Awake()
        {
            base.Awake();
            InitializeAudioSources();
            RegisterClipsToDictionary();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PlaySEEvent>(OnPlaySE);
            EventBus.Subscribe<PlayBGMEvent>(OnPlayBGM);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlaySEEvent>(OnPlaySE);
            EventBus.Unsubscribe<PlayBGMEvent>(OnPlayBGM);
        }

        private void InitializeAudioSources()
        {
            if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();
            if (seSource == null) seSource = gameObject.AddComponent<AudioSource>();

            bgmSource.loop = true;
            seSource.loop = false;
        }

        private void RegisterClipsToDictionary()
        {
            foreach (var clip in bgmClips)
            {
                if (clip != null && !bgmDict.ContainsKey(clip.name))
                    bgmDict.Add(clip.name, clip);
            }

            foreach (var clip in seClips)
            {
                if (clip != null && !seDict.ContainsKey(clip.name))
                    seDict.Add(clip.name, clip);
            }
        }

        // --- SE再生処理 ---
        private void OnPlaySE(PlaySEEvent evt)
        {
            PlaySE(evt.SeName, evt.Volume);
        }

        public void PlaySE(string seName, float volume = 1.0f)
        {
            if (seDict.TryGetValue(seName, out var clip))
            {
                seSource.PlayOneShot(clip, volume);
            }
            else
            {
                Debug.LogWarning($"[AudioManager] SE '{seName}' が見つかりません。");
            }
        }

        // --- BGM再生処理 ---
        private void OnPlayBGM(PlayBGMEvent evt)
        {
            PlayBGM(evt.BgmName, evt.Loop, evt.FadeDuration);
        }

        public void PlayBGM(string bgmName, bool loop = true, float fadeDuration = 0.5f)
        {
            if (!bgmDict.TryGetValue(bgmName, out var newClip))
            {
                Debug.LogWarning($"[AudioManager] BGM '{bgmName}' が見つかりません。");
                return;
            }

            if (bgmSource.isPlaying && bgmSource.clip == newClip) return;

            if (bgmFadeCoroutine != null)
            {
                StopCoroutine(bgmFadeCoroutine);
            }

            bgmFadeCoroutine = StartCoroutine(FadeChangeBGMRoutine(newClip, loop, fadeDuration));
        }

        private IEnumerator FadeChangeBGMRoutine(AudioClip nextClip, bool loop, float fadeDuration)
        {
            float startVolume = bgmSource.volume;

            if (bgmSource.isPlaying && fadeDuration > 0)
            {
                for (float t = 0; t < fadeDuration; t += Time.deltaTime)
                {
                    bgmSource.volume = Mathf.Lerp(startVolume, 0, t / fadeDuration);
                    yield return null;
                }
            }

            bgmSource.Stop();
            bgmSource.clip = nextClip;
            bgmSource.loop = loop;
            bgmSource.Play();

            if (fadeDuration > 0)
            {
                for (float t = 0; t < fadeDuration; t += Time.deltaTime)
                {
                    bgmSource.volume = Mathf.Lerp(0, startVolume, t / fadeDuration);
                    yield return null;
                }
            }

            bgmSource.volume = startVolume;
        }
    }
}