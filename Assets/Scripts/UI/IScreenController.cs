namespace Contagion.UI
{
    /// <summary>
    /// UIManager가 <see cref="AppScreen"/> 하나를 열고 닫을 때 사용하는 공통 계약.
    /// 화면 하나가 실제로는 컨트롤러 여러 개(예: Research = UpgradeTreeView 3개 페이징)로
    /// 구성되더라도, UIManager 입장에서는 Show()/Hide() 두 메서드만 아는 화면 하나로 다룬다.
    ///
    /// 기존 패널 컨트롤러 클래스(CountryStatusPanelController/RankingPanelController/
    /// UpgradeTreeView)는 이미 각자 public void Show()/Hide()를 갖고 있지만, 이번 리팩터링
    /// 범위(AppScreen/IScreenController/UIManager 전환 로직만)를 지키기 위해 그 클래스들을
    /// 직접 이 인터페이스로 캐스팅하지 않고 UIManager 내부 어댑터(ActionScreenController)로
    /// 감싼다 — 기존 컨트롤러 파일은 한 줄도 건드리지 않는다.
    /// </summary>
    public interface IScreenController
    {
        void Show();
        void Hide();
    }
}
