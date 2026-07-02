using Contagion.Data;
using Contagion.Gameplay;
using UnityEngine;

namespace Contagion.Managers
{
    /// <summary>
    /// 효과음/배경음악 재생 전담 매니저. 기본 플레이 퀄리티 개선 항목 — 지금까지 프로젝트에
    /// 사운드가 전혀 없었다. 기존 매니저들의 이벤트(DnaBubble.OnAnyCollected,
    /// SimulationManager.OnInfectionMilestone/OnDeathMilestone/OnGameEnded, EventManager.OnNewsEvent)에
    /// 구독만 하고, 실제 재생은 인스펙터에 연결된 AudioClip으로 한다.
    ///
    /// 필드를 비워두면 조용히 무시하고 에러를 내지 않는다 — 오디오 에셋이 아직 준비되지 않아도
    /// 안전하게 동작한다 (에셋은 사용자가 직접 준비/임포트해야 함, 코드만으로는 생성 불가).
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("효과음 (비워두면 재생 안 함 — 에러 없음)")]
        [SerializeField] private AudioClip dnaCollectClip;
        [SerializeField] private AudioClip milestoneClip;
        [SerializeField] private AudioClip newsPositiveClip;
        [SerializeField] private AudioClip newsNegativeClip;
        [SerializeField] private AudioClip victoryClip;
        [SerializeField] private AudioClip defeatClip;

        [Header("배경음악 (선택 — 사용하려면 별도 AudioSource를 만들어 loop 재생용으로 연결)")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioClip backgroundMusicClip;

        private AudioSource _sfxSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _sfxSource = GetComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
        }

        private void OnEnable() => Subscribe();
        private void Start()
        {
            Subscribe(); // SimulationManager/EventManager.Instance가 OnEnable 시점엔 아직 없을 수 있어 재시도

            if (musicSource != null && backgroundMusicClip != null && !musicSource.isPlaying)
            {
                musicSource.clip = backgroundMusicClip;
                musicSource.loop = true;
                musicSource.Play();
            }
        }

        private void Subscribe()
        {
            DnaBubble.OnAnyCollected -= HandleDnaCollected;
            DnaBubble.OnAnyCollected += HandleDnaCollected;

            if (SimulationManager.Instance != null)
            {
                SimulationManager.Instance.OnInfectionMilestone -= HandleMilestone;
                SimulationManager.Instance.OnInfectionMilestone += HandleMilestone;
                SimulationManager.Instance.OnDeathMilestone -= HandleMilestone;
                SimulationManager.Instance.OnDeathMilestone += HandleMilestone;
                SimulationManager.Instance.OnGameEnded -= HandleGameEnded;
                SimulationManager.Instance.OnGameEnded += HandleGameEnded;
            }

            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnNewsEvent -= HandleNewsEvent;
                EventManager.Instance.OnNewsEvent += HandleNewsEvent;
            }
        }

        private void OnDisable()
        {
            DnaBubble.OnAnyCollected -= HandleDnaCollected;

            if (SimulationManager.Instance != null)
            {
                SimulationManager.Instance.OnInfectionMilestone -= HandleMilestone;
                SimulationManager.Instance.OnDeathMilestone -= HandleMilestone;
                SimulationManager.Instance.OnGameEnded -= HandleGameEnded;
            }

            if (EventManager.Instance != null)
                EventManager.Instance.OnNewsEvent -= HandleNewsEvent;
        }

        private void HandleDnaCollected(int dnaValue) => Play(dnaCollectClip);
        private void HandleMilestone(Country country) => Play(milestoneClip);

        private void HandleNewsEvent(NewsEvent evt) =>
            Play(evt.category == NewsEventCategory.Positive ? newsPositiveClip : newsNegativeClip);

        private void HandleGameEnded(bool isVictory) => Play(isVictory ? victoryClip : defeatClip);

        private void Play(AudioClip clip)
        {
            if (clip == null || _sfxSource == null) return;
            _sfxSource.PlayOneShot(clip);
        }
    }
}
