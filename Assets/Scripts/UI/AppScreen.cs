namespace Contagion.UI
{
    /// <summary>
    /// UIManager가 조정하는 "화면" 단위 상태. GamePlay 씬 안에서 서로 배타적으로 열리는
    /// 패널 그룹(업그레이드 트리 / 국가현황 / 랭킹)과 "아무 패널도 안 열린 기본 지도 화면"을
    /// 하나의 상태값으로 표현한다.
    ///
    /// MainMenu/CountrySelect/EndingScreen(게임 시작 전후 플로우)과 CountryPopup/ResearchPopup
    /// (클릭 트리거 모달)은 이 상태 머신 밖에 있다 — HUD 버튼 3개가 여는 화면만 대상으로 한다.
    /// </summary>
    public enum AppScreen
    {
        /// <summary>기본 상태 — 지도 화면. 세 패널 모두 닫혀 있다.</summary>
        Gameplay,

        /// <summary>업그레이드 트리(전파/증상/능력 3페이지 통합) — 기존 "Upgrade" 버튼.</summary>
        Research,

        /// <summary>국가현황(GLOBAL STATUS CENTER) — 기존 "Country Status" 버튼.</summary>
        GlobalStatus,

        /// <summary>랭킹 패널 — 기존 "Ranking" 버튼.</summary>
        Leaderboard
    }
}
