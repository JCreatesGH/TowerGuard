using System.Collections.Generic;
using UnityEngine;

namespace TowerGuard.Core
{
    /// <summary>
    /// Pooled SFX + dedicated music source. Persists across scenes.
    /// Volume is clamped 0..1 and persisted via PlayerPrefs (sfx_vol / music_vol).
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private int sfxPoolSize = 10;

        private readonly List<AudioSource> sfxSources = new List<AudioSource>();
        private AudioSource musicSource;

        private const string SfxVolKey = "sfx_vol";
        private const string MusicVolKey = "music_vol";

        private float sfxVolume = 1f;
        private float musicVolume = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            BuildSfxPool();
            BuildMusicSource();
            LoadSavedVolumes();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void BuildSfxPool()
        {
            for (int i = 0; i < sfxPoolSize; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = false;
                sfxSources.Add(src);
            }
        }

        private void BuildMusicSource()
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
        }

        /// <summary>Play a one-shot SFX through the next free pooled AudioSource.</summary>
        public void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;

            for (int i = 0; i < sfxSources.Count; i++)
            {
                if (!sfxSources[i].isPlaying)
                {
                    sfxSources[i].clip = clip;
                    sfxSources[i].volume = sfxVolume;
                    sfxSources[i].Play();
                    return;
                }
            }

            // All sources busy — steal the first one.
            sfxSources[0].Stop();
            sfxSources[0].clip = clip;
            sfxSources[0].volume = sfxVolume;
            sfxSources[0].Play();
        }

        /// <summary>Play a music clip on the dedicated music source.</summary>
        public void PlayMusic(AudioClip clip, bool loop)
        {
            if (clip == null) return;
            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }

        public void StopMusic()
        {
            if (musicSource != null)
            {
                musicSource.Stop();
            }
        }

        public void SetSFXVolume(float vol)
        {
            sfxVolume = Mathf.Clamp01(vol);
            for (int i = 0; i < sfxSources.Count; i++)
            {
                sfxSources[i].volume = sfxVolume;
            }
            PlayerPrefs.SetFloat(SfxVolKey, sfxVolume);
            PlayerPrefs.Save();
        }

        public void SetMusicVolume(float vol)
        {
            musicVolume = Mathf.Clamp01(vol);
            if (musicSource != null)
            {
                musicSource.volume = musicVolume;
            }
            PlayerPrefs.SetFloat(MusicVolKey, musicVolume);
            PlayerPrefs.Save();
        }

        public float GetSFXVolume() => sfxVolume;
        public float GetMusicVolume() => musicVolume;

        public void LoadSavedVolumes()
        {
            sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolKey, 1f));
            musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolKey, 1f));

            for (int i = 0; i < sfxSources.Count; i++)
            {
                sfxSources[i].volume = sfxVolume;
            }
            if (musicSource != null)
            {
                musicSource.volume = musicVolume;
            }
        }
    }
}
