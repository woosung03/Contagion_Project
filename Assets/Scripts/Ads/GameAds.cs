namespace Contagion.Ads
{
    /// <summary>
    /// 앱 전역 광고 보관소. 설계 문서 13절 "광고 수익 모델 설계 (보상형 광고 전용)".
    ///
    /// 설계 문서 1절 표에는 "배너 광고 + 보상형 광고"라고 적혀 있지만, 13절 본문은
    /// "앱인토스 정책상... 보상형 광고(Rewarded Ad)만 사용"이라고 명시한다. 더 상세하고 명시적인
    /// 13절 정책을 따라 배너 광고는 사용하지 않는다 (TossBannerAd 드롭인은 위키에 있으니 추후 필요 시 추가).
    ///
    /// 위키 인앱광고 함정 #3: "같은 adGroupId는 동시에 1개만 선로드 가능 — 앱 전역 인스턴스 1개로 고정".
    /// 업그레이드 보너스/부활/메인메뉴 보너스 3곳 모두 보상형이므로, 테스트 단계에서는
    /// 광고 그룹 1개(Rewarded)를 공유하는 인스턴스 하나로 처리한다. 출시 전 콘솔에서 배치별로
    /// 별도 adGroupId를 발급받아 채우기(fill rate)를 분리하고 싶다면 그때 인스턴스를 나누면 된다.
    /// </summary>
    public static class GameAds
    {
        /// <summary>
        /// 보상형 광고 공용 인스턴스. 업그레이드 화면 "DNA +10", 게임오버 부활, 메인메뉴 "DNA +5"
        /// 세 곳 모두 이 인스턴스의 Show(onSuccess, onFailed)를 순차적으로 호출한다.
        /// </summary>
        public static readonly TossFullScreenAd Rewarded =
            new TossFullScreenAd("ait-ad-test-rewarded-id", TossFullScreenAd.AdKind.Rewarded);
    }
}
