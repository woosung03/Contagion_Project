using System;
using System.Collections.Generic;
using UnityEngine;

namespace Contagion.Data
{
    /// <summary>항공/해운 허브 구분. 시각 표현(스프라이트/속도)과 감염 전파 확률만 다르고 로직은 공유한다.</summary>
    public enum TransportHubType
    {
        Air,
        Sea
    }

    /// <summary>허브 하나가 같은 타입의 다른 허브로 연결되는 경로 + 가중치(스폰 시 목적지 랜덤 선택에 사용).</summary>
    [Serializable]
    public class TransportRouteLink
    {
        public string targetHubId;
        public float weight;

        public TransportRouteLink(string targetHubId, float weight)
        {
            this.targetHubId = targetHubId;
            this.weight = weight;
        }
    }

    /// <summary>
    /// 전세계 교통망(공항/항구) 허브 하나. 실제 모든 공항/항구를 시뮬레이션하지 않고 "글로벌 교통 흐름을
    /// 대표하는 주요 허브 15+15개"만 사용한다 (사용자 제공 설계 문서 "Global Transport Network Design" 참고).
    ///
    /// [정정] 이전 주석은 "이 프로젝트 세계지도가 대륙별 그리드 배치라 정확한 지리 좌표를 줄 수 없다"고
    /// 했었는데, 이는 Step 27/28 시점 기준 설명이 Step 29의 48개국 확장(world_base.png 배경 + 실제
    /// world.svg 기반 국가 실루엣 오버레이로 재구축, `CountryView` 참고) 이후 갱신되지 않은 낡은 내용이었다.
    /// 실제로는 각 국가의 `dnaSpawnLocalOffset`이 world.svg에서 뽑은 실제 위경도 기반 중심 좌표라, 허브를
    /// 국가 앵커(countryId → CountryView.DnaSpawnWorldPosition)에 연결하면 그 자체로 대략적인 실제 지리
    /// 위치가 된다. 문제는 오히려 그 반대였다 — 세계지도가 (구를 감싸지 않는) 평면 사각형이라 태평양처럼
    /// 지도 양 끝을 넘나드는 항로를 직선으로 그으면 대륙 한가운데를 관통해버린다. 그래서 해운(및 일부
    /// 장거리 항공) 노선은 `DefaultTransportHubFactory`의 경유점(waypoint) 테이블을 거쳐 우회한다 —
    /// `TransportManager.BuildPathPoints()` 참고.
    ///
    /// [Step 32 추가] 위 설명은 "국가 앵커 + 작은 오프셋"이 국가 하나에 허브가 하나뿐일 때는 맞지만,
    /// 미국(ATL/DFW/LAX)·중국(PVG/CAN/HKG)처럼 같은 나라에 허브가 여러 개면 문제가 된다 — 오프셋이
    /// 실제 도시 간 거리(수천 km)를 표현하기엔 너무 작게 잡혀 있어서 여러 공항이 지도 위 한 점 근처에
    /// 뭉쳐 보이고, 그 결과 출발/도착 지점이 실제 그 도시(공항) 자리가 아닌 엉뚱한 곳으로 어긋나 보였다.
    /// 그래서 항공 허브는 `useAbsoluteWorldOffset=true`로 바꿔 국가 앵커를 아예 거치지 않고, 각 공항의
    /// 실제 위경도를 국가 앵커들과 동일한 선형 변환(x=경도*0.021614-0.045251, y=위도*0.025372-0.285776 —
    /// 48개국 dnaSpawnLocalOffset 대 실제 위경도 최소제곱 회귀로 도출, 잔차 표준편차 x:0.056/y:0.025 유닛)으로
    /// 직접 좌표를 계산해 넣었다. 해운 허브는 Step 30-5에서 이미 실측 검증(A* + sea anchor)을 거쳐
    /// 사용자 확인까지 끝난 상태라 이번 수정 대상에서 제외 — 여전히 country 앵커 상대 오프셋 방식 그대로.
    /// </summary>
    [Serializable]
    public class TransportHub
    {
        public string id;
        public TransportHubType type;
        public string displayName;

        /// <summary>이 허브가 속한(감염 전파 대상이 되는) 국가 — CountryDatabase의 id와 일치해야 한다.</summary>
        public string countryId;

        /// <summary>
        /// localOffset의 해석 방식. false(기본, 해운 허브)면 countryId의 DnaSpawnWorldPosition 기준 추가
        /// 오프셋(월드 유닛) — 같은 국가 내 여러 허브 분리용. true(항공 허브, Step 32)면 country 앵커와
        /// 무관하게 WorldMap 원점 기준 절대 좌표(WorldMap.ToWorldPosition으로 직접 변환) — 실제 공항의
        /// 위경도를 국가 앵커와 같은 스케일(DevLog Step 30 참고: x=경도·y=위도 비례, 등장방형 투영)로
        /// 변환한 값이다. countryId는 이 경우에도 게임 로직(해당 대표 국가의 isAirportOpen 등)에는
        /// 그대로 쓰이지만 좌표 계산에는 관여하지 않는다.
        /// </summary>
        public bool useAbsoluteWorldOffset;

        /// <summary>useAbsoluteWorldOffset이 false면 countryId 앵커 기준 상대 오프셋, true면 WorldMap 절대 좌표.</summary>
        public Vector2 localOffset;

        public List<TransportRouteLink> connections = new List<TransportRouteLink>();

        public TransportHub(string id, TransportHubType type, string displayName, string countryId, Vector2 localOffset,
            bool useAbsoluteWorldOffset = false)
        {
            this.id = id;
            this.type = type;
            this.displayName = displayName;
            this.countryId = countryId;
            this.localOffset = localOffset;
            this.useAbsoluteWorldOffset = useAbsoluteWorldOffset;
        }
    }
}
