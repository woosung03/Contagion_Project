using System.Collections.Generic;

namespace Contagion.Gameplay
{
    /// <summary>
    /// WorldMap 위에 열려 있는 화면(HUD 버튼 3개가 여는 AppScreen + AppScreen 밖의 MainMenu/
    /// CountrySelect/EndingScreen/ResearchPopup)을 사유(reason)로 구분해 추적한다. 이유가 하나라도
    /// 남아있으면 지도 클릭(CountryView.OnMouseUpAsButton)과 드래그(WorldMapCameraController)를
    /// 차단한다 — 실제 게이트는 그 두 클래스가 <see cref="IsLocked"/>를 읽어서 스스로 건다.
    ///
    /// bool 하나가 아니라 HashSet 기반으로 만든 이유: Research(AppScreen) 위에 ResearchPopup이 얹히는
    /// 것처럼 두 사유가 동시에 열려 있을 수 있다. bool이면 안쪽(ResearchPopup)이 먼저 닫힐 때
    /// 바깥쪽(Research)이 아직 열려 있는데도 잠금이 풀려버린다 — 사유별로 개별 추적해야 "모든 사유가
    /// 닫혀야 잠금 해제"가 정확히 성립한다.
    ///
    /// CountryPopup은 의도적으로 이 enum에 없다 — "화면(Screen)"이 아니라 Gameplay의 일부로 취급하기로
    /// 결정됐으므로(모달이지만 WorldMap 입력을 막지 않음) CountryPopupController는 이 클래스를 아예
    /// 참조하지 않는다.
    ///
    /// 순수 정적 클래스로 둔 이유: MonoBehaviour 싱글턴(WorldMap.Instance 등 이 프로젝트의 기존 관례)
    /// 으로 만들면 씬에 GameObject를 배치/배선해야 하고 Awake 순서에 따라 이르게 호출될 여지가 생긴다.
    /// 이 클래스는 상태(HashSet) 하나만 있으면 되는 순수 로직이라 씬 배선 없이 어디서든 즉시 안전하게
    /// Lock/Unlock을 호출할 수 있는 정적 클래스가 더 단순하다.
    /// </summary>
    public enum WorldMapLockReason
    {
        MainMenu,
        CountrySelect,
        Research,
        GlobalStatus,
        Leaderboard,
        EndingScreen,
        ResearchPopup,
    }

    public static class WorldMapInputLock
    {
        private static readonly HashSet<WorldMapLockReason> _activeLocks = new HashSet<WorldMapLockReason>();

        /// <summary>하나 이상의 사유가 열려 있으면 true — CountryView/WorldMapCameraController가
        /// 매 입력마다 이 값을 확인한다.</summary>
        public static bool IsLocked => _activeLocks.Count > 0;

        public static IReadOnlyCollection<WorldMapLockReason> ActiveLocks => _activeLocks;

        public static void Lock(WorldMapLockReason reason) => _activeLocks.Add(reason);

        /// <summary>Lock을 호출한 적 없는 사유를 Unlock해도 안전하다(HashSet.Remove는 없는 값에 no-op) —
        /// 화면 Hide()가 Show() 없이 방어적으로 호출되는 기존 패턴(예: OnEnable의 초기 Hide())과 맞물려도
        /// 예외가 나지 않는다.</summary>
        public static void Unlock(WorldMapLockReason reason) => _activeLocks.Remove(reason);

        /// <summary>모든 사유를 한 번에 해제한다 — 정적 클래스라 씬을 리로드(재시작)해도 상태가 이어지므로,
        /// 새 판이 시작되는 지점(UIManager.Start)에서 이전 판이 정리 못 하고 남긴 사유가 있어도 깨끗한
        /// 상태로 시작하도록 방어적으로 호출한다.</summary>
        public static void ClearAll() => _activeLocks.Clear();
    }
}
