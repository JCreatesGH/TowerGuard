#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TowerGuard.EditorTools
{
    /// <summary>
    /// Phase 5 — Synthesizes every SFX + the two music loops as in-engine PCM,
    /// writes them to disk as 16-bit 44.1kHz mono WAV files, then triggers an
    /// AssetDatabase refresh so they import as AudioClips.
    /// Menu: Tools > TowerGuard > Create Audio Clips.
    /// </summary>
    public static class CreateAudioClips
    {
        private const int SampleRate = 44100;
        private const string SfxFolder   = "Assets/Audio/SFX";
        private const string MusicFolder = "Assets/Audio/Music";

        [MenuItem("Tools/TowerGuard/Create Audio Clips", priority = 400)]
        public static void RunAll()
        {
            EnsureFolder(SfxFolder);
            EnsureFolder(MusicFolder);

            // ---- Tower fires ----
            WriteWav($"{SfxFolder}/SFX_Tower_Basic_Fire.wav",  TowerBasicFire());
            WriteWav($"{SfxFolder}/SFX_Tower_Sniper_Fire.wav", TowerSniperFire());
            WriteWav($"{SfxFolder}/SFX_Tower_Slow_Fire.wav",   TowerSlowFire());
            WriteWav($"{SfxFolder}/SFX_Tower_Area_Fire.wav",   TowerAreaFire());

            // ---- Enemy deaths ----
            WriteWav($"{SfxFolder}/SFX_Enemy_Death_Basic.wav", EnemyDeathBasic());
            WriteWav($"{SfxFolder}/SFX_Enemy_Death_Tank.wav",  EnemyDeathTank());
            WriteWav($"{SfxFolder}/SFX_Enemy_Death_Boss.wav",  EnemyDeathBoss());

            // ---- Misc gameplay ----
            WriteWav($"{SfxFolder}/SFX_Enemy_ReachedEnd.wav", EnemyReachedEnd());
            WriteWav($"{SfxFolder}/SFX_WaveStart.wav",        WaveStart());
            WriteWav($"{SfxFolder}/SFX_CurrencyEarned.wav",   CurrencyEarned());
            WriteWav($"{SfxFolder}/SFX_Purchase.wav",         Purchase());
            WriteWav($"{SfxFolder}/SFX_UIButton.wav",         UIButton());
            WriteWav($"{SfxFolder}/SFX_Combo.wav",            Combo());

            // ---- Music ----
            WriteWav($"{MusicFolder}/Music_Gameplay.wav", GameplayMusicLoop(8f));
            WriteWav($"{MusicFolder}/Music_MainMenu.wav", MainMenuMusicLoop(6f));

            AssetDatabase.Refresh();
            Debug.Log("[CreateAudioClips] Generated 13 SFX + 2 music loops in Assets/Audio/.");
        }

        // =====================================================================
        // SFX synthesis — each returns a float[] in [-1, 1]
        // =====================================================================
        private static float[] TowerBasicFire()
        {
            // Sharp click: 440Hz for 0.02s, then frequency descent to 220Hz over 0.1s.
            float dur = 0.12f;
            int n = (int)(SampleRate * dur);
            float[] s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float freq = (t < 0.02f) ? 440f : Mathf.Lerp(440f, 220f, (t - 0.02f) / 0.1f);
                float env  = Mathf.Exp(-t * 14f);
                float v = Mathf.Sin(2f * Mathf.PI * freq * t) * env;
                s[i] = Mathf.Clamp(v, -0.7f, 0.7f); // soft clip
            }
            return s;
        }

        private static float[] TowerSniperFire()
        {
            // Very sharp 1200Hz crack, fast tail.
            float dur = 0.2f;
            int n = (int)(SampleRate * dur);
            float[] s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 60f);
                s[i] = Mathf.Sin(2f * Mathf.PI * 1200f * t) * env;
            }
            return s;
        }

        private static float[] TowerSlowFire()
        {
            // Soft whoosh: noise sweep 200→100Hz with gentle envelope.
            float dur = 0.25f;
            int n = (int)(SampleRate * dur);
            float[] s = new float[n];
            var rng = new System.Random(7);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float freq = Mathf.Lerp(200f, 100f, t / dur);
                float env  = Mathf.Sin(Mathf.PI * t / dur) * 0.6f;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.3f;
                float wave  = Mathf.Sin(2f * Mathf.PI * freq * t);
                s[i] = (wave * 0.6f + noise * 0.4f) * env;
            }
            return s;
        }

        private static float[] TowerAreaFire()
        {
            // Low thud: 80Hz with heavy distortion, fast decay.
            float dur = 0.3f;
            int n = (int)(SampleRate * dur);
            float[] s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 12f);
                float v = Mathf.Sin(2f * Mathf.PI * 80f * t) * env;
                s[i] = Mathf.Clamp(v * 2f, -0.85f, 0.85f); // hard clip = distortion
            }
            return s;
        }

        private static float[] EnemyDeathBasic()
        {
            // 600Hz pop with sharp attack and exponential decay.
            float dur = 0.2f;
            int n = (int)(SampleRate * dur);
            float[] s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 24f);
                s[i] = Mathf.Sin(2f * Mathf.PI * 600f * t) * env;
            }
            return s;
        }

        private static float[] EnemyDeathTank()
        {
            // Heavy crunch: noise burst @ 0.5 amplitude for 0.1s, decays over 0.3s.
            float dur = 0.4f;
            int n = (int)(SampleRate * dur);
            float[] s = new float[n];
            var rng = new System.Random(13);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 10f);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                s[i] = noise * 0.5f * env;
            }
            return s;
        }

        private static float[] EnemyDeathBoss()
        {
            // Deep rumble (60Hz + 80Hz layered) + a high ping at 0.3s.
            float dur = 0.8f;
            int n = (int)(SampleRate * dur);
            float[] s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 4f);
                float low = Mathf.Sin(2f * Mathf.PI * 60f * t) * env * 0.5f
                          + Mathf.Sin(2f * Mathf.PI * 80f * t) * env * 0.4f;
                float ping = 0f;
                if (t > 0.30f && t < 0.40f)
                {
                    float pt = t - 0.30f;
                    float pe = Mathf.Exp(-pt * 60f);
                    ping = Mathf.Sin(2f * Mathf.PI * 1800f * pt) * pe * 0.5f;
                }
                s[i] = Mathf.Clamp(low + ping, -1f, 1f);
            }
            return s;
        }

        private static float[] EnemyReachedEnd()
        {
            // Alarm: alternating 800/400Hz every 0.1s.
            float dur = 0.5f;
            int n = (int)(SampleRate * dur);
            float[] s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float freq = (((int)(t / 0.1f)) % 2 == 0) ? 800f : 400f;
                float env  = 0.6f * Mathf.Sin(Mathf.PI * t / dur);
                s[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env;
            }
            return s;
        }

        private static float[] WaveStart()
        {
            // Rising tone 200→800 over 0.4s, hold 0.2s.
            float dur = 0.6f;
            int n = (int)(SampleRate * dur);
            float[] s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float freq = t < 0.4f ? Mathf.Lerp(200f, 800f, t / 0.4f) : 800f;
                float env  = 0.6f * Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, (dur - t) / 0.05f));
                s[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env;
            }
            return s;
        }

        private static float[] CurrencyEarned()
        {
            // Bright ting at 1800Hz, 0.02s attack, fast decay.
            float dur = 0.15f;
            int n = (int)(SampleRate * dur);
            float[] s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float env = (t < 0.02f) ? (t / 0.02f) : Mathf.Exp(-(t - 0.02f) * 30f);
                s[i] = Mathf.Sin(2f * Mathf.PI * 1800f * t) * env * 0.7f;
            }
            return s;
        }

        private static float[] Purchase()
        {
            // Two-tone confirmation: 600Hz then 900Hz, each 0.12s.
            float dur = 0.3f;
            int n = (int)(SampleRate * dur);
            float[] s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float freq = (t < 0.12f) ? 600f : 900f;
                float env = (t < 0.12f) ? (1f - t / 0.12f) : (1f - (t - 0.12f) / 0.18f);
                env = Mathf.Max(0f, env) * 0.6f;
                s[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env;
            }
            return s;
        }

        private static float[] UIButton()
        {
            // Soft 800Hz click.
            float dur = 0.08f;
            int n = (int)(SampleRate * dur);
            float[] s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 50f);
                s[i] = Mathf.Sin(2f * Mathf.PI * 800f * t) * env * 0.5f;
            }
            return s;
        }

        private static float[] Combo()
        {
            // Three ascending tones 440/660/880, each 0.1s + reverb-ish tail.
            float dur = 0.4f;
            int n = (int)(SampleRate * dur);
            float[] s = new float[n];
            float[] freqs = { 440f, 660f, 880f };
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                int slot = Mathf.Clamp((int)(t / 0.1f), 0, 2);
                float local = t - slot * 0.1f;
                float env = Mathf.Exp(-local * 12f);
                float v = Mathf.Sin(2f * Mathf.PI * freqs[slot] * t) * env * 0.6f;
                // Fake reverb tail: faint copy 0.03s back.
                if (i > 1300) v += s[i - 1300] * 0.25f;
                s[i] = Mathf.Clamp(v, -0.9f, 0.9f);
            }
            return s;
        }

        // =====================================================================
        // Music loops
        // =====================================================================
        private static float[] GameplayMusicLoop(float seconds)
        {
            int n = (int)(SampleRate * seconds);
            float[] s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float bass  = Mathf.Sin(2f * Mathf.PI * 55f  * t) * 0.15f;
                float lfo   = (Mathf.Sin(2f * Mathf.PI * 0.3f * t) + 1f) * 0.5f;
                float pad   = (Mathf.Sin(2f * Mathf.PI * 220f * t) + Mathf.Sin(2f * Mathf.PI * 330f * t)) * 0.5f * 0.18f * (0.5f + lfo * 0.5f);
                float pulse = 0f;
                float beatT = t % 0.5f;
                if (beatT < 0.08f)
                {
                    pulse = Mathf.Sin(2f * Mathf.PI * 110f * beatT) * Mathf.Exp(-beatT * 18f) * 0.2f;
                }
                s[i] = Mathf.Clamp((bass + pad + pulse) * 1.0f, -0.55f, 0.55f);
            }
            return s;
        }

        private static float[] MainMenuMusicLoop(float seconds)
        {
            int n = (int)(SampleRate * seconds);
            float[] s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float bass = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.10f;
                float lfo  = (Mathf.Sin(2f * Mathf.PI * 0.18f * t) + 1f) * 0.5f;
                float pad  = (Mathf.Sin(2f * Mathf.PI * 330f * t) + Mathf.Sin(2f * Mathf.PI * 440f * t)) * 0.5f * 0.20f * (0.4f + lfo * 0.6f);
                s[i] = Mathf.Clamp(bass + pad, -0.5f, 0.5f);
            }
            return s;
        }

        // =====================================================================
        // WAV writer (16-bit signed PCM, mono, 44.1kHz)
        // =====================================================================
        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace('\\', '/');
                string name   = Path.GetFileName(path);
                if (!AssetDatabase.IsValidFolder(parent))
                {
                    EnsureFolder(parent);
                }
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static void WriteWav(string projectRelativePath, float[] samples)
        {
            string abs = Path.Combine(Directory.GetParent(Application.dataPath).FullName, projectRelativePath);
            using (var fs = new FileStream(abs, FileMode.Create, FileAccess.Write))
            using (var bw = new BinaryWriter(fs))
            {
                int byteRate = SampleRate * 2; // mono * 16-bit
                int dataBytes = samples.Length * 2;

                bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                bw.Write(36 + dataBytes);
                bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
                bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                bw.Write(16);             // PCM fmt chunk size
                bw.Write((short)1);       // PCM format
                bw.Write((short)1);       // channels
                bw.Write(SampleRate);
                bw.Write(byteRate);
                bw.Write((short)2);       // block align (mono * 16-bit / 8)
                bw.Write((short)16);      // bits per sample
                bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                bw.Write(dataBytes);
                for (int i = 0; i < samples.Length; i++)
                {
                    short v = (short)Mathf.Clamp(Mathf.RoundToInt(samples[i] * 32767f), -32768, 32767);
                    bw.Write(v);
                }
            }
        }
    }
}
#endif
