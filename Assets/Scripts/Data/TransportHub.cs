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
    /// 이 프로젝트의 세계지도는 실제 위경도 투영이 아니라 대륙별 그리드로 배치돼 있어(Step 27/28 DevLog
    /// 참고) 허브 각각에 정확한 지리 좌표를 줄 수 없다. 대신 허브를 국가(countryId)에 연결해 그 국가의
    /// CountryView.DnaSpawnWorldPosition(이미 지도 위 정확한 위치를 가리키는 앵커)을 기준점으로 삼고,
    /// 같은 국가에 허브가 여러 개(예: 미국 5개, 중국 8개) 있을 때는 겹치지 않도록 localOffset만큼 살짝
    /// 떨어뜨려 그린다. 실제 지리적 정확도보다 "가독성 있는 글로벌 흐름 표현"이 목적이라는 설계 문서
    /// 방향과 일치한다.
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
