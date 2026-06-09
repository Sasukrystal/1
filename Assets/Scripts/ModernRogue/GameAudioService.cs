using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace ModernRogue
{
    [DefaultExecutionOrder(-850)]
    [DisallowMultipleComponent]
    public sealed class GameAudioService : MonoBehaviour
    {
        private const string NormalBgmFile = "generated-sound-1.mp3";
        private const string BossBgmFile = "generated-sound-2.mp3";

        private const string SfxHurt = "受击.mp3";
        private const string SfxDeath = "死亡.mp3";
        private const string SfxDodge = "翻滚闪避.mp3";
        private const string SfxShieldBlock = "盾牌格挡.mp3";
        private const string SfxWarriorSlash = "战士剑刃挥砍.mp3";
        private const string SfxWarriorHit = "战士剑刃命中.mp3";
        private const string SfxBowDraw = "拉弓2.mp3";
        private const string SfxBowShoot = "射箭.mp3";
        private const string SfxArrowHit = "箭矢命中.mp3";

        private const string BossTitanSlamWindup = "music/泰坦/首创闷响.mp3";
        private const string BossTitanSlam = "music/泰坦/重踏.mp3";
        private const string BossTitanShake = "music/泰坦/震动.mp3";
        private const string BossTitanShardBurst = "music/泰坦/碎片炸裂.mp3";

        private const string BossEmberFanWindup = "music/火焰术士/余烬发射箭.mp3";
        private const string BossEmberFanCast = "music/火焰术士/火系法术.mp3";
        private const string BossEmberRuneDrop = "music/火焰术士/火焰符文掉落.mp3";
        private const string BossEmberRuneExplosion = "music/火焰术士/符文爆炸.mp3";

        private const string BossStormCharge = "music/雷霆守卫/蓄力.mp3";
        private const string BossStormDashImpact = "music/雷霆守卫/撞击.mp3";
        private const string BossStormSlash = "music/雷霆守卫/斩击.mp3";
        private const string BossStormLightningHit = "music/雷霆守卫/电流命中.mp3";

        private const string BossBroodSummon = "music/蜘蛛/召唤尖啸.mp3";
        private const string BossBroodHatch = "music/蜘蛛/幼虫孵化挤压.mp3";
        private const string BossBroodPoisonDrop = "music/蜘蛛/毒池滴落.mp3";
        private const string BossBroodPoisonBurst = "music/蜘蛛/毒泡破裂.mp3";

        public static GameAudioService Instance { get; private set; }

        private AudioSource musicSource;
        private AudioSource sfxSource;
        private AudioClip normalClip;
        private AudioClip bossClip;
        private AudioClip fallbackClip;
        private readonly Dictionary<string, AudioClip> sfxClips = new Dictionary<string, AudioClip>();
        private bool bossBattleActive;
        private bool clipsReady;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureOnLoad()
        {
            Ensure();
        }

        public static GameAudioService Ensure()
        {
            if (Instance != null)
            {
                return Instance;
            }

            GameObject obj = new GameObject("GameAudioService");
            return obj.AddComponent<GameAudioService>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
            fallbackClip = CreateFallbackClip();
            StartCoroutine(LoadAllClipsRoutine());
        }

        public void PlayHeroHurt() => PlaySfx(SfxHurt);

        public void PlayHeroDeath() => PlaySfx(SfxDeath);

        public void PlayDodgeRoll() => PlaySfx(SfxDodge);

        public void PlayShieldBlock() => PlaySfx(SfxShieldBlock);

        public void PlayWarriorSlash() => PlaySfx(SfxWarriorSlash);

        public void PlayWarriorHit() => PlaySfx(SfxWarriorHit);

        public void PlayBowDraw() => PlaySfx(SfxBowDraw);

        public void PlayBowShoot() => PlaySfx(SfxBowShoot);

        public void PlayArrowHit() => PlaySfx(SfxArrowHit);

        public void PlayBossTitanSlamWindup() => PlaySfx(BossTitanSlamWindup);

        public void PlayBossTitanSlam() => PlaySfx(BossTitanSlam);

        public void PlayBossTitanShake() => PlaySfx(BossTitanShake);

        public void PlayBossTitanShardBurst() => PlaySfx(BossTitanShardBurst);

        public void PlayBossEmberFanWindup() => PlaySfx(BossEmberFanWindup);

        public void PlayBossEmberFanCast() => PlaySfx(BossEmberFanCast);

        public void PlayBossEmberRuneDrop() => PlaySfx(BossEmberRuneDrop);

        public void PlayBossEmberRuneExplosion() => PlaySfx(BossEmberRuneExplosion);

        public void PlayBossStormCharge() => PlaySfx(BossStormCharge);

        public void PlayBossStormDashImpact() => PlaySfx(BossStormDashImpact);

        public void PlayBossStormSlash() => PlaySfx(BossStormSlash);

        public void PlayBossStormLightningHit() => PlaySfx(BossStormLightningHit);

        public void PlayBossBroodSummon() => PlaySfx(BossBroodSummon);

        public void PlayBossBroodHatch() => PlaySfx(BossBroodHatch);

        public void PlayBossBroodPoisonDrop() => PlaySfx(BossBroodPoisonDrop);

        public void PlayBossBroodPoisonBurst() => PlaySfx(BossBroodPoisonBurst);

        public void ApplyVolume()
        {
            if (musicSource != null)
            {
                musicSource.volume = GameSettingsService.MusicVolume;
            }

            if (sfxSource != null)
            {
                sfxSource.volume = GameSettingsService.SfxVolume;
            }

            AudioListener.volume = Mathf.Lerp(0.35f, 1f, GameSettingsService.SfxVolume);
        }

        public void SetBossBattleActive(bool active)
        {
            bossBattleActive = active;
            if (clipsReady)
            {
                ApplyCurrentTrack();
            }
        }

        private void PlaySfx(string fileName)
        {
            if (sfxSource == null)
            {
                return;
            }

            if (sfxClips.TryGetValue(fileName, out AudioClip clip) && clip != null)
            {
                sfxSource.volume = GameSettingsService.SfxVolume;
                sfxSource.PlayOneShot(clip);
                return;
            }

            Debug.LogWarning("GameAudioService: Missing SFX clip " + fileName);
        }

        private IEnumerator LoadAllClipsRoutine()
        {
            yield return LoadClipRoutine(NormalBgmFile, clip => normalClip = clip);
            yield return LoadClipRoutine(BossBgmFile, clip => bossClip = clip);

            string[] sfxFiles =
            {
                SfxHurt,
                SfxDeath,
                SfxDodge,
                SfxShieldBlock,
                SfxWarriorSlash,
                SfxWarriorHit,
                SfxBowDraw,
                SfxBowShoot,
                SfxArrowHit,
                BossTitanSlamWindup,
                BossTitanSlam,
                BossTitanShake,
                BossTitanShardBurst,
                BossEmberFanWindup,
                BossEmberFanCast,
                BossEmberRuneDrop,
                BossEmberRuneExplosion,
                BossStormCharge,
                BossStormDashImpact,
                BossStormSlash,
                BossStormLightningHit,
                BossBroodSummon,
                BossBroodHatch,
                BossBroodPoisonDrop,
                BossBroodPoisonBurst
            };

            for (int i = 0; i < sfxFiles.Length; i++)
            {
                string fileName = sfxFiles[i];
                yield return LoadClipRoutine(fileName, clip =>
                {
                    if (clip != null)
                    {
                        sfxClips[fileName] = clip;
                    }
                });
            }

            clipsReady = true;
            ApplyCurrentTrack();
        }

        private IEnumerator LoadClipRoutine(string assetsRelativePath, System.Action<AudioClip> assign)
        {
            string normalized = assetsRelativePath.Replace('\\', '/');
            if (!TryResolveAudioFullPath(normalized, out string fullPath))
            {
                Debug.LogWarning("GameAudioService: Missing audio file at " + normalized);
                yield break;
            }

            string url = "file:///" + fullPath.Replace("\\", "/");
            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
            {
                yield return request.SendWebRequest();
#if UNITY_2020_2_OR_NEWER
                if (request.result != UnityWebRequest.Result.Success)
#else
                if (request.isNetworkError || request.isHttpError)
#endif
                {
                    Debug.LogWarning("GameAudioService: Failed to load " + normalized + " -> " + request.error);
                    yield break;
                }

                AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                if (clip != null)
                {
                    clip.name = Path.GetFileNameWithoutExtension(normalized);
                    assign(clip);
                }
            }
        }

        private static bool TryResolveAudioFullPath(string normalizedRelativePath, out string fullPath)
        {
            fullPath = CombinePath(Application.streamingAssetsPath, normalizedRelativePath);
            if (File.Exists(fullPath))
            {
                return true;
            }

#if UNITY_EDITOR
            fullPath = CombinePath(Application.dataPath, normalizedRelativePath);
            if (File.Exists(fullPath))
            {
                return true;
            }
#endif

            fullPath = CombinePath(Application.streamingAssetsPath, normalizedRelativePath);
            return false;
        }

        private static string CombinePath(string root, string normalizedRelativePath)
        {
            string combined = root;
            string[] segments = normalizedRelativePath.Split('/');
            for (int i = 0; i < segments.Length; i++)
            {
                if (!string.IsNullOrEmpty(segments[i]))
                {
                    combined = Path.Combine(combined, segments[i]);
                }
            }

            return combined;
        }

        private void ApplyCurrentTrack()
        {
            if (musicSource == null)
            {
                return;
            }

            AudioClip target = bossBattleActive ? bossClip : normalClip;
            if (target == null)
            {
                target = fallbackClip;
            }

            if (musicSource.clip == target && musicSource.isPlaying)
            {
                ApplyVolume();
                return;
            }

            musicSource.clip = target;
            ApplyVolume();
            musicSource.Play();
        }

        private static AudioClip CreateFallbackClip()
        {
            const int sampleRate = 44100;
            const float duration = 8f;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float wave = Mathf.Sin(t * 110f * Mathf.PI * 2f) * 0.08f
                    + Mathf.Sin(t * 165f * Mathf.PI * 2f) * 0.05f
                    + Mathf.Sin(t * 220f * Mathf.PI * 2f) * 0.03f;
                float envelope = 0.55f + 0.45f * Mathf.Sin(t * 0.35f * Mathf.PI * 2f);
                samples[i] = wave * envelope;
            }

            AudioClip clip = AudioClip.Create("ModernRogue_FallbackBgm", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
