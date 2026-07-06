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
    /// </summary>
    [Serializable]
    public class TransportHub
    {
        public string id;
        public TransportHubType type;
        public string displayName;

        /// <summary>이 허브가 속한(감염 전파 대상이 되는) 국가 — CountryDatabase의 id와 일치해야 한다.</summary>
        public string countryId;

        /// <summary>countryId의 DnaSpawnWorldPosition 기준 추가 오프셋(월드 유닛) — 같은 국가 내 여러 허브 분리용.</summary>
        public Vector2 localOffset;

        public List<TransportRouteLink> connections = new List<TransportRouteLink>();

        public TransportHub(string id, TransportHubType type, string displayName, string countryId, Vector2 localOffset)
        {
            this.id = id;
            this.type = type;
            this.displayName = displayName;
            this.countryId = countryId;
            this.localOffset = localOffset;
        }
    }
}
