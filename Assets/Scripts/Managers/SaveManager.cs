using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Contagion.Data;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using AppsInToss;
#endif

namespace Contagion.Managers
{
    /// <summary>저장 데이터 스냅샷. JsonUtility로 직렬화한다 (Dictionary 미사용 — 전부 List 기반이라 안전).</summary>
    [Serializable]
    public class SaveData
    {
        public WorldState worldState;
        public Pathogen pathogen;
        public List<Country> countries;
        public List<string> unlockedNodeIds;
        public Difficulty difficulty;
    }

    /// <summary>
    /// 저장 시스템. 설계 문서 Step 13, 15절 "모바일 특성상 세션 저장 필수 (백그라운드 종료 대응)".
    ///
    /// AIT.StorageGetItem/SetItem/RemoveItem(위키 api-storage)을 앱인토스 빌드에서 사용하고,
    /// 에디터/비 AIT 빌드에서는 Application.persistentDataPath에 JSON 파일로 저장하는 로컬 폴백을 쓴다.
    /// 두 경로 모두 같은 SaveData 포맷을 공유하므로 SDK 설치 여부와 무관하게 지금 바로 동작한다.
    ///
    /// userHashKey(설계 문서 14절) 기반 유저별 분리가 필요해지면, SaveKey에
    /// AIT.GetUserKeyForGame() 결과를 접두사로 붙이도록 확장할 것 (지금은 단일 슬롯 저장).
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        private const string SaveKey = "contagion_save_v1";

        [SerializeField, Tooltip("이 일수마다 자동 저장 (틱 = 1일)")]
        private int autoSaveIntervalDays = 5;

        private int _lastAutoSaveDay = -1;
        private static string LocalPath => Path.Combine(Application.persistentDataPath, "contagion_save.json");

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable() => Subscribe();
        private void Start() => Subscribe();

        private void Subscribe()
        {
            if (SimulationManager.Instance == null) return;
            SimulationManager.Instance.OnTickCompleted -= HandleTick;
            SimulationManager.Instance.OnTickCompleted += HandleTick;
        }

        private void OnDisable()
        {
            if (SimulationManager.Instance != null)
                SimulationManager.Instance.OnTickCompleted -= HandleTick;
        }

        private void HandleTick(WorldState state)
        {
            if (state.currentDay - _lastAutoSaveDay < autoSaveIntervalDays) return;
            _lastAutoSaveDay = state.currentDay;
            SaveGame();
        }

        /// <summary>
        /// 새 게임 시작(재시작 포함) 시 GameDataBootstrapper가 호출. 이 매니저는 DontDestroyOnLoad라
        /// _lastAutoSaveDay가 이전 판의 큰 day 값을 들고 있으면 새 게임에서 자동 저장이 한참 지연된다
        /// (치명적이진 않지만 — 재시작 직후 브라우저가 꺼지면 새 게임 진행이 저장 안 될 수 있음).
        /// </summary>
        public void ResetForNewGame() => _lastAutoSaveDay = -1;

        // 모바일 백그라운드 전환/종료 대응 (설계 문서 15절)
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) SaveGame();
        }

        private void OnApplicationQuit() => SaveGame();

        public void SaveGame()
        {
            if (WorldDataManager.Instance == null) return;

            var data = new SaveData
            {
                worldState = WorldDataManager.Instance.State,
                pathogen = WorldDataManager.Instance.CurrentPathogen,
                countries = new List<Country>(WorldDataManager.Instance.Countries),
                unlockedNodeIds = UpgradeManager.Instance != null
                    ? UpgradeManager.Instance.Tree.Where(n => n.isUnlocked).Select(n => n.id).ToList()
                    : new List<string>(),
                difficulty = GameManager.Instance != null ? GameManager.Instance.CurrentDifficulty : Difficulty.Normal
            };

            string json = JsonUtility.ToJson(data);

#if UNITY_WEBGL && !UNITY_EDITOR
            SaveRemoteAsync(json);
#else
            SaveLocal(json);
#endif
        }

        public bool HasLocalSave() => File.Exists(LocalPath);

        public void LoadGame(Action<bool> onCompleted = null)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            LoadRemoteAsync(onCompleted);
#else
            ApplyJsonAndNotify(LoadLocalJson(), onCompleted);
#endif
        }

        public void DeleteSave()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            DeleteRemoteAsync();
#endif
            try
            {
                if (File.Exists(LocalPath)) File.Delete(LocalPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] 로컬 세이브 삭제 실패: {e.Message}");
            }
        }

        private void SaveLocal(string json)
        {
            try
            {
                File.WriteAllText(LocalPath, json);
                Debug.Log("[SaveManager] 로컬 저장 완료.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] 로컬 저장 실패: {e.Message}");
            }
        }

        private string LoadLocalJson()
        {
            try
            {
                return File.Exists(LocalPath) ? File.ReadAllText(LocalPath) : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] 로컬 로드 실패: {e.Message}");
                return null;
            }
        }

        private void ApplyJsonAndNotify(string json, Action<bool> onCompleted)
        {
            if (string.IsNullOrEmpty(json))
            {
                onCompleted?.Invoke(false);
                return;
            }

            try
            {
                var data = JsonUtility.FromJson<SaveData>(json);
                Apply(data);
                onCompleted?.Invoke(true);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] 세이브 파싱 실패: {e.Message}");
                onCompleted?.Invoke(false);
            }
        }

        private void Apply(SaveData data)
        {
            if (WorldDataManager.Instance == null || data == null) return;

            WorldDataManager.Instance.SetCountries(data.countries ?? new List<Country>());
            WorldDataManager.Instance.SetPathogen(data.pathogen ?? new Pathogen());
            WorldDataManager.Instance.LoadState(data.worldState ?? new WorldState());

            if (UpgradeManager.Instance != null && data.unlockedNodeIds != null)
            {
                foreach (var id in data.unlockedNodeIds)
                {
                    var node = UpgradeManager.Instance.GetNode(id);
                    if (node != null) node.isUnlocked = true;
                }
            }

            GameManager.Instance?.SetDifficulty(data.difficulty);
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private async void SaveRemoteAsync(string json)
        {
            try
            {
                await AIT.StorageSetItem(SaveKey, json);
                Debug.Log("[SaveManager] AIT Storage 저장 완료.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] AIT Storage 저장 실패: {e.Message} — 로컬 폴백.");
                SaveLocal(json);
            }
        }

        private async void LoadRemoteAsync(Action<bool> onCompleted)
        {
            try
            {
                string json = await AIT.StorageGetItem(SaveKey);
                ApplyJsonAndNotify(json, onCompleted);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] AIT Storage 로드 실패: {e.Message}");
                onCompleted?.Invoke(false);
            }
        }

        private async void DeleteRemoteAsync()
        {
            try { await AIT.StorageRemoveItem(SaveKey); }
            catch (Exception e) { Debug.LogWarning($"[SaveManager] AIT Storage 삭제 실패: {e.Message}"); }
        }
#endif
    }
}
