using System.Collections.Generic;
using UnityEngine;

namespace Contagion.Data
{
    /// <summary>
    /// 사용자 제공 "Global Transport Network Design" 문서의 항공 15개 + 해운 15개 허브를 코드로 정의한다.
    /// DefaultUpgradeTreeFactory와 같은 패턴 — 별도 ScriptableObject 에셋 없이 바로 플레이 가능하도록
    /// TransportManager가 이 팩토리를 기본값으로 사용한다.
    ///
    /// 허브 도시의 국가가 CountryDatabase의 48개국에 없는 경우(UAE/네덜란드/벨기에/싱가포르 등은 독립
    /// 국가로 존재하지 않음) 지리적으로 가장 가까운 대표 국가로 연결한다(예: 두바이·제벨알리→사우디,
    /// 로테르담·안트베르펜·함부르크→독일, 싱가포르·포트클랑→말레이시아, 홍콩→중국). 설계 문서 자체가
    /// "실제 교통량을 그대로 시뮬레이션하지 않고 게임적 가독성을 우선한다"고 명시하고 있어 이 정도
    /// 근사는 의도에 부합한다.
    ///
    /// [Step 32] 항공 허브 15개는 이제 country 앵커 상대 오프셋이 아니라 WorldMap 절대 좌표를 쓴다
    /// (사용자 지적: "비행기 경로 출발/도착 지점이 공항이여야 하는데 어긋난 부분이 있다" — 미국/중국처럼
    /// 한 나라에 허브가 여럿이면 기존 방식(국가 앵커 + 지도 위에서 눈대중으로 잡은 작은 오프셋)으로는
    /// 실제 도시 간 거리를 표현할 수 없어 여러 공항이 한 점 근처에 뭉쳤다). 좌표 산출 방법:
    /// 1) `Assets/Scenes/GamePlay.unity`에서 48개국 CountryView의 `dnaSpawnLocalOffset` 값을 추출.
    /// 2) 그중 국경이 단순하고(군소 섬 산재 X) 실제 지리 중심 위경도를 신뢰성 있게 알 수 있는 39개국을
    ///    골라 (실제 위경도) → (dnaSpawnLocalOffset) 최소제곱 선형 회귀(x=a*경도+b, y=c*위도+d)를 계산.
    ///    이 지도가 등장방형(Plate Carrée) 투영이라는 건 Step 30에서 이미 확인된 사실이라 선형 피팅으로
    ///    충분하다(회전/왜곡 없음). 결과: a=0.021614, b=-0.045251, c=0.025372, d=-0.285776, 잔차 표준편차
    ///    x=0.056/y=0.025 유닛(전체 지도 폭 대비 1~2% 수준).
    /// 3) 이 회귀식이 실제로 world_base.png 마스크와 맞는지 독립적으로 검증하기 위해, Step 30-4에서 픽셀
    ///    단위로 직접 확인해 확정한 제벨알리 해운 허브 좌표(1.10,0.35 — 실제 페르시아만 연안)와 비교:
    ///    회귀식으로 두바이 실제 위경도(25.2532N, 55.3657E)를 변환하면 (1.151,0.355)가 나와 거의 일치
    ///    (오차 0.05 미만) — 회귀식 신뢰도 확인.
    /// 4) 15개 공항의 실제 위경도(ATL/DFW/LAX/LHR/IST/DXB/DEL/ICN/HND/PVG/CAN/SIN/HKG/SYD/GRU)를 이
    ///    회귀식에 대입해 절대 좌표를 계산, `Air(...)` 호출의 offset 인자로 그대로 사용.
    /// 해운 허브(15개)는 Step 30-5에서 이미 A*+sea anchor로 실측 검증되고 사용자 플레이 확인까지 끝난
    /// 상태라 이번 변경 대상에서 제외 — 계속 country 앵커 상대 오프셋 방식(`useAbsoluteWorldOffset=false`).
    /// </summary>
    public static class DefaultTransportHubFactory
    {
        public static List<TransportHub> BuildDefaultHubs()
        {
            var hubs = new List<TransportHub>();

            // ================= 항공 허브 15개 =================
            // [Step 32] 좌표는 국가 앵커 기준 상대 오프셋이 아니라 WorldMap 절대 좌표(실제 공항 위경도
            // 기반). 배경/근거는 이 클래스 상단 요약 주석 참고 — 요약하면 48개국 dnaSpawnLocalOffset
            // 대 실제 위경도로 최소제곱 회귀를 돌려 x=경도*0.021614-0.045251, y=위도*0.025372-0.285776
            // 변환식을 구하고, 각 공항의 실제 위경도를 그대로 대입했다. 이전 버전은 "국가 앵커 + 작은
            // 오프셋(-0.25~0.25 수준)"이라 같은 나라 안 여러 공항(ATL/DFW/LAX, PVG/CAN/HKG 등)이 지도
            // 위 한 점 근처에 뭉쳐 실제 도시(공항) 위치와 어긋나 보였다 — 이번 수정으로 해소.
            hubs.Add(Air("ATL", "애틀랜타", "USA", new Vector2(-1.870f, 0.568f),
                ("DFW", 9), ("LAX", 8), ("LHR", 9), ("GRU", 6), ("YYZ", 7), ("MEX", 7)));
            hubs.Add(Air("DFW", "댈러스", "USA", new Vector2(-2.143f, 0.549f),
                ("ATL", 9), ("LAX", 8), ("LHR", 7), ("MEX", 6)));
            hubs.Add(Air("LAX", "로스앤젤레스", "USA", new Vector2(-2.605f, 0.575f),
                ("ATL", 8), ("DFW", 8), ("HND", 9), ("ICN", 6), ("SYD", 7), ("GRU", 5), ("LIM", 7)));
            hubs.Add(Air("LHR", "런던 히스로", "UK", new Vector2(-0.055f, 1.020f),
                ("ATL", 9), ("DFW", 7), ("IST", 8), ("DXB", 8), ("DEL", 7), ("SVO", 6), ("YYZ", 8)));
            hubs.Add(Air("IST", "이스탄불", "TUR", new Vector2(0.576f, 0.761f),
                ("LHR", 8), ("DXB", 9), ("DEL", 6), ("CAN", 5), ("ADD", 6), ("SVO", 7)));
            hubs.Add(Air("DXB", "두바이", "SAU", new Vector2(1.151f, 0.355f),
                ("LHR", 8), ("IST", 9), ("DEL", 9), ("SIN", 9), ("HKG", 6), ("ADD", 7)));
            hubs.Add(Air("DEL", "델리", "IND", new Vector2(1.621f, 0.439f),
                ("LHR", 7), ("IST", 6), ("DXB", 9), ("SIN", 7), ("ICN", 5)));
            hubs.Add(Air("ICN", "인천", "KOR", new Vector2(2.688f, 0.665f),
                ("HND", 10), ("PVG", 9), ("SIN", 8), ("DXB", 7), ("LAX", 6)));
            // [Step 32 후속] 회귀식 그대로 계산한 (2.976,0.616)은 world_base.png 알파채널로 확인해보니
            // 도쿄만(灣) 입구 바다 위(사용자 지적: "일본만 살짝 오른쪽으로 치우쳐져서 바다에서 출발/도착")
            // — 실제 하네다는 만 안쪽 매립지라 이 지도의 단순화된 해안선 해상도로는 정확히 못 찍는다.
            // JPN.png 마스크에서 그 지점 기준 가장 가까운 "충분히 육지 안쪽"(해안선에서 12px+ 여유) 픽셀을
            // 탐색해 (2.916,0.624)로 서쪽 30px(약 0.06유닛)만 당겨 미우라반도 쪽 육지 위로 옮김.
            hubs.Add(Air("HND", "도쿄 하네다", "JPN", new Vector2(2.916f, 0.624f),
                ("ICN", 10), ("PVG", 8), ("LAX", 9), ("SYD", 7)));
            hubs.Add(Air("PVG", "상하이 푸둥", "CHI", new Vector2(2.588f, 0.504f),
                ("ICN", 9), ("HND", 8), ("HKG", 8), ("SIN", 7), ("CAN", 6)));
            hubs.Add(Air("CAN", "광저우", "CHI", new Vector2(2.404f, 0.308f),
                ("PVG", 6), ("HKG", 9), ("SIN", 7), ("IST", 5)));
            hubs.Add(Air("SIN", "싱가포르 창이", "MAS", new Vector2(2.202f, -0.251f),
                ("ICN", 8), ("PVG", 8), ("DXB", 9), ("SYD", 8)));
            hubs.Add(Air("HKG", "홍콩", "CHI", new Vector2(2.417f, 0.280f),
                ("PVG", 8), ("CAN", 9), ("DXB", 6), ("SYD", 6)));
            hubs.Add(Air("SYD", "시드니", "AUS", new Vector2(3.222f, -1.147f),
                ("LAX", 7), ("HND", 7), ("SIN", 8), ("HKG", 6)));
            hubs.Add(Air("GRU", "상파울루", "BRA", new Vector2(-1.050f, -0.880f),
                ("ATL", 6), ("LAX", 5), ("LIM", 6)));

            // ================= 항공 허브 추가 5개 (핵심 공백 지역 채우기) =================
            // 대륙별 허브 공백(아프리카 0개, 유럽 2/7, 남미 1/4, 북미 USA만)을 메우기 위해 추가.
            // 좌표 산출 방식은 위 15개와 동일 — 각 도시 실제 위경도를 Step 32 회귀식
            // (x=경도*0.021614-0.045251, y=위도*0.025372-0.285776)에 대입한 절대 좌표.
            // 항로 직선이 태평양을 가로지르는 것도 아니고 기존 허브와 위경도대가 겹치지 않아
            // BuildAirWaypoints() 우회가 필요한 장거리 노선(HND/ICN-LAX, LAX-SYD)에 해당하지
            // 않는다 — 직선 그대로 사용.
            hubs.Add(Air("ADD", "아디스아바바", "ETH", new Vector2(0.793f, -0.058f),
                ("DXB", 7), ("IST", 6)));
            hubs.Add(Air("SVO", "모스크바", "RUS", new Vector2(0.763f, 1.134f),
                ("IST", 7), ("LHR", 6)));
            hubs.Add(Air("LIM", "리마", "PER", new Vector2(-1.712f, -0.591f),
                ("LAX", 7), ("GRU", 6)));
            hubs.Add(Air("YYZ", "토론토", "CAN", new Vector2(-1.766f, 0.822f),
                ("ATL", 7), ("LHR", 8)));
            hubs.Add(Air("MEX", "멕시코시티", "MEX", new Vector2(-2.187f, 0.207f),
                ("ATL", 7), ("DFW", 6)));

            // ================= 해운 허브 15개 =================
            hubs.Add(Sea("SEA_SHA", "상하이항", "CHI", new Vector2(0.35f, -0.1f),
                ("SEA_NGB", 10), ("SEA_SHE", 8), ("SEA_BUS", 9), ("SEA_SIN", 8), ("SEA_HKG", 7)));
            hubs.Add(Sea("SEA_SIN", "싱가포르항", "MAS", new Vector2(0.3f, -0.2f),
                ("SEA_SHA", 8), ("SEA_PKL", 10), ("SEA_HKG", 8), ("SEA_JEA", 9), ("SEA_ROT", 6), ("SEA_MEL", 7)));
            hubs.Add(Sea("SEA_NGB", "닝보-저우산항", "CHI", new Vector2(0.3f, 0f),
                ("SEA_SHA", 10), ("SEA_SHE", 7), ("SEA_GUA", 7)));
            hubs.Add(Sea("SEA_SHE", "선전항", "CHI", new Vector2(0.1f, -0.4f),
                ("SEA_SHA", 8), ("SEA_GUA", 9), ("SEA_HKG", 9), ("SEA_NGB", 7)));
            hubs.Add(Sea("SEA_GUA", "광저우항", "CHI", new Vector2(0.2f, -0.35f),
                ("SEA_SHE", 9), ("SEA_HKG", 8), ("SEA_NGB", 7)));
            hubs.Add(Sea("SEA_BUS", "부산항", "KOR", new Vector2(-0.1f, -0.05f),
                ("SEA_SHA", 9), ("SEA_LAX", 7), ("SEA_LGB", 6)));
            hubs.Add(Sea("SEA_HKG", "홍콩항", "CHI", new Vector2(0.15f, -0.45f),
                ("SEA_SHE", 9), ("SEA_GUA", 8), ("SEA_SIN", 8), ("SEA_SHA", 7)));
            hubs.Add(Sea("SEA_ROT", "로테르담항", "GER", new Vector2(-0.2f, 0.1f),
                ("SEA_HAM", 9), ("SEA_ANT", 9), ("SEA_JEA", 7), ("SEA_SIN", 6),
                ("SEA_TNG", 6), ("SEA_LEH", 9), ("SEA_BUE", 5)));
            // [Step 30-4 버그 수정] 기존 오프셋(-0.15,0.15)은 SAU 중심점 기준 (0.78,0.48)로 계산돼
            // 실제로는 사우디 내륙(요르단/이스라엘 인접) 쪽에 찍혔다 — 제벨알리(두바이)는 페르시아만
            // 연안이어야 하는데 엉뚱한 곳에 있었던 것. 그 결과 이 허브에서 출발/도착하는 모든 해로가
            // "육지 위 지점 → 저장된 경유점"까지 검증되지 않은 직선으로 이어져 사우디아라비아 위를
            // 그대로 관통해 보였다(사용자 피드백: "사우디 위를 지나다녀"). 오프셋을 (0.17,0.02)로
            // 바꿔 실제 페르시아만 연안 바다 위(world_base.png 알파채널 기준 검증됨)로 이동.
            hubs.Add(Sea("SEA_JEA", "제벨알리항", "SAU", new Vector2(0.17f, 0.02f),
                ("SEA_SIN", 9), ("SEA_ROT", 7), ("SEA_SHA", 5), ("SEA_DUR", 7)));
            // [Step 30-5 버그 수정] 기존 오프셋(0.3,0.15)의 실제 위치는 (world_base.png 마스크 기준)
            // 좁은 만/방파제 안쪽의 다른 바다 조각(주변 육지에 완전히 둘러싸여 메인 대양과 안 이어지는
            // 작은 웅덩이)에 걸려 있었다 — LAX↔부산/상하이 항로 A* 계산이 아예 "경로 없음"으로 실패하는
            // 원인이었다. 오프셋을 (0.3,-0.05)로 낮춰 롱비치(SEA_LGB, 오프셋 (0.3,-0.1))와 거의 같은
            // 위도로 옮기니 메인 대양 쪽 바다에 정상적으로 연결됨(실제로도 LA항과 롱비치항은 같은
            // 항만 지역에 인접해 있어 위치가 비슷해지는 것 자체는 지리적으로도 이상하지 않다).
            hubs.Add(Sea("SEA_LAX", "로스앤젤레스항", "USA", new Vector2(0.3f, -0.05f),
                ("SEA_LGB", 10), ("SEA_BUS", 7), ("SEA_SHA", 6)));
            hubs.Add(Sea("SEA_LGB", "롱비치항", "USA", new Vector2(0.3f, -0.1f),
                ("SEA_LAX", 10), ("SEA_BUS", 6), ("SEA_SAN", 5)));
            hubs.Add(Sea("SEA_ANT", "안트베르펜항", "GER", new Vector2(-0.3f, 0.05f),
                ("SEA_ROT", 9), ("SEA_HAM", 8), ("SEA_LEH", 7)));
            hubs.Add(Sea("SEA_HAM", "함부르크항", "GER", new Vector2(0.05f, 0.2f),
                ("SEA_ROT", 9), ("SEA_ANT", 8), ("SEA_GDN", 8)));
            hubs.Add(Sea("SEA_PKL", "포트클랑항", "MAS", new Vector2(-0.2f, 0.1f),
                ("SEA_SIN", 10), ("SEA_SHA", 6), ("SEA_MEL", 5)));
            hubs.Add(Sea("SEA_SAN", "산투스항", "BRA", new Vector2(-0.1f, -0.15f),
                ("SEA_LGB", 5), ("SEA_ROT", 5),
                ("SEA_LOS", 6), ("SEA_DUR", 5), ("SEA_BUE", 9), ("SEA_CTG", 6)));

            // ================= 해운 허브 추가 11개 (핵심 공백 지역 채우기) =================
            // 항공 허브와 달리 기존 15개 해운 허브는 countryId 앵커(국가 실루엣 중심) 기준 상대
            // 오프셋으로 배치돼 있었는데, 신규 항구는 씬의 국가 앵커 좌표를 몰라도 실제 위경도만으로
            // 바로 배치할 수 있도록 SeaAbs()(항공 허브와 같은 회귀식 기반 절대 좌표 방식)를 쓴다.
            // 좌표는 실제 항구 위경도를 회귀식에 대입한 뒤, world_base.png 알파채널이 바다(투명)가
            // 되도록 필요시 인근 픽셀로 미세 조정(대개 20px 이내)했다. 모든 신규 항로는 아래
            // BuildSeaWaypoints()에 실제 해안선을 우회하는 경유점을 추가해 육지를 관통하지 않는지
            // 8방향 A*(다운샘플 factor 2, 대각선 코너 관통 금지, world_base.png 알파채널 기준)로
            // 전수 검증했다 — 단, 수에즈 운하/파나마 운하는 이 지도에 뭍으로만 그려져 있어(실제로
            // 좁은 인공 수로라 지도 해상도상 바다 픽셀이 아예 없음, Step 30-5의 JEA-ROT/LGB-SAN
            // 우회 사례와 동일한 한계) 이집트↔두바이 직결 항로나 콜롬비아/멕시코↔롱비치 태평양 항로는
            // 만들지 않고 각 지역 내 다른 허브를 거쳐 다구간으로 연결되도록 설계했다(예:
            // 이집트→제노바→알헤시라스→탕헤르메드→로테르담→제벨알리).
            hubs.Add(SeaAbs("SEA_PSD", "포트사이드항", "EGY", new Vector2(0.654f, 0.508f),
                ("SEA_GOA", 7)));
            hubs.Add(SeaAbs("SEA_TNG", "탕헤르메드항", "MAR", new Vector2(-0.158f, 0.624f),
                ("SEA_ALC", 9), ("SEA_LOS", 6), ("SEA_ROT", 6)));
            hubs.Add(SeaAbs("SEA_LOS", "라고스항", "NGA", new Vector2(0.030f, -0.136f),
                ("SEA_TNG", 6), ("SEA_SAN", 6)));
            hubs.Add(SeaAbs("SEA_DUR", "더반항", "RSA", new Vector2(0.634f, -1.048f),
                ("SEA_JEA", 7), ("SEA_SAN", 5)));
            hubs.Add(SeaAbs("SEA_LEH", "르아브르항", "FRA", new Vector2(-0.050f, 0.984f),
                ("SEA_ROT", 9), ("SEA_ANT", 7)));
            hubs.Add(SeaAbs("SEA_GOA", "제노바항", "ITA", new Vector2(0.146f, 0.832f),
                ("SEA_ALC", 7), ("SEA_PSD", 7)));
            hubs.Add(SeaAbs("SEA_ALC", "알헤시라스항", "ESP", new Vector2(-0.162f, 0.632f),
                ("SEA_TNG", 9), ("SEA_GOA", 7)));
            hubs.Add(SeaAbs("SEA_GDN", "그단스크항", "POL", new Vector2(0.342f, 1.116f),
                ("SEA_HAM", 8)));
            hubs.Add(SeaAbs("SEA_BUE", "부에노스아이레스항", "ARG", new Vector2(-1.298f, -1.180f),
                ("SEA_SAN", 9), ("SEA_ROT", 5)));
            hubs.Add(SeaAbs("SEA_CTG", "카르타헤나항", "COL", new Vector2(-1.666f, -0.008f),
                ("SEA_SAN", 6)));
            hubs.Add(SeaAbs("SEA_MEL", "멜버른항", "AUS", new Vector2(3.106f, -1.264f),
                ("SEA_SIN", 7), ("SEA_PKL", 5)));

            return hubs;
        }

        /// <summary>
        /// [Step 32] worldOffset은 이제 countryId 앵커 기준 상대 오프셋이 아니라 WorldMap 절대 좌표다 —
        /// 실제 공항의 위경도를 국가 앵커들과 같은 선형 변환(이 클래스 상단 요약 주석의 회귀식)으로
        /// 미리 계산해 넣은 값. countryId는 게임 로직(대표 국가의 isAirportOpen 판정 등)에만 쓰인다.
        /// </summary>
        private static TransportHub Air(string id, string name, string countryId, Vector2 worldOffset,
            params (string target, float weight)[] links)
            => Build(id, TransportHubType.Air, name, countryId, worldOffset, useAbsolute: true, links);

        private static TransportHub Sea(string id, string name, string countryId, Vector2 offset,
            params (string target, float weight)[] links)
            => Build(id, TransportHubType.Sea, name, countryId, offset, useAbsolute: false, links);

        /// <summary>
        /// [핵심 공백 채우기] 기존 Sea()는 countryId 앵커 기준 상대 오프셋만 지원하는데, 신규 항구는
        /// 국가 앵커 좌표를 몰라도(씬 파일을 읽지 않고도) 실제 위경도만으로 바로 배치할 수 있도록
        /// Air()와 동일한 회귀식 기반 절대 좌표 방식을 쓴다. world_base.png 알파채널로 좌표가 바다
        /// 위(또는 해안 인접)인지 전부 개별 검증했다 — DefaultTransportHubFactory 클래스 상단 요약 참고.
        /// </summary>
        private static TransportHub SeaAbs(string id, string name, string countryId, Vector2 worldOffset,
            params (string target, float weight)[] links)
            => Build(id, TransportHubType.Sea, name, countryId, worldOffset, useAbsolute: true, links);

        private static TransportHub Build(string id, TransportHubType type, string name, string countryId,
            Vector2 offset, bool useAbsolute, (string target, float weight)[] links)
        {
            var hub = new TransportHub(id, type, name, countryId, offset, useAbsolute);
            foreach (var (target, weight) in links)
                hub.connections.Add(new TransportRouteLink(target, weight));
            return hub;
        }

        /// <summary>
        /// 해운 항로 경유점(waypoint) 테이블. Key는 "허브ID(사전순 작은 쪽)|허브ID(큰 쪽)" 형식(canonical
        /// pair key — TransportManager.PairKey와 동일 규칙)이고, 값은 그 "작은 쪽 → 큰 쪽" 방향으로 지나가는
        /// 중간 경유점들(WorldMap 로컬 오프셋 좌표, CountryView.dnaSpawnLocalOffset과 같은 좌표계)이다.
        /// TransportManager가 실제 이동 방향이 반대(큰 쪽 → 작은 쪽)면 이 배열을 뒤집어서 쓴다.
        ///
        /// [Step 30-5 전면 재계산] Step 30~30-4까지는 문제가 보고된 항로만 그때그때 고쳤는데, 사용자가
        /// "다른 배 경로도 확인해서 육지로 올라가는 문제 있으면 고쳐줘"라고 요청해 26쌍 전부를 다시
        /// 점검했다. 그 결과 이전에 "직선으로 충분"이라고 남겨뒀던 쌍들(선전↔광저우/홍콩, 광저우↔홍콩,
        /// 상하이↔부산, 로테르담↔함부르크 등)도 실제로는 **허브의 실제 위치(국가 앵커+오프셋)에서
        /// 곧장 직선으로 이으면 전부 육지를 스쳐 지나간다**는 걸 발견했다(항구가 "정확한 해안선 좌표"가
        /// 아니라 "국가 대표 지점 근처의 근사 오프셋"이라 생기는, 이 프로젝트 설계 자체의 근본적인 특성).
        /// 그래서 이번엔 방식을 바꿔, 모든 허브마다 "실제 위치에서 가장 가까운 진짜 바다 지점"(sea anchor)을
        /// 먼저 찾고, 이 sea anchor들 사이를 A*로 잇는 방식으로 26쌍 전부를 재계산했다 — 그 결과 저장된
        /// 배열의 첫/마지막 점은 이제 전부 "그 허브의 실제 해상 진입점"이고, TransportManager가 붙이는
        /// "허브 실제 위치 → 이 첫 점" 구간은 짧은 해안 근접 오차(대개 수십 픽셀, world_base.png 기준
        /// 4000px 폭 대비 미미한 수준) 하나로만 남는다.
        ///
        /// 이 재계산 과정에서 로스앤젤레스항(SEA_LAX)이 실제로는 주변 육지에 완전히 둘러싸인 작은 바다
        /// 조각(방파제 안쪽 같은 곳)에 있어서 메인 대양과 아예 연결되지 않는다는 것도 발견 — 부산↔LA,
        /// LA↔롱비치, LA↔상하이 항로 전부 "경로 없음"으로 계산 실패했다. `BuildDefaultHubs()`에서
        /// SEA_LAX 오프셋을 롱비치(SEA_LGB)와 비슷한 위도로 낮춰 정상적인 바다로 옮겨서 해결.
        ///
        /// (과거 기록 — 지금도 유효한 부분만 남김) 첫 자동 계산 때 육지 마스크를 "48개국 실루엣의
        /// 합집합"으로만 만들어서 미등록 국가 영토가 바다로 취급돼 경로가 아프리카를 관통하는 버그가
        /// 있었다(→ world_base.png 알파채널 전체를 마스크로 교체해 해결, Step 30-2). 이후 키 순서가
        /// canonical 규칙과 반대로 저장되는 버그(Step 30-3), 제벨알리 허브가 엉뚱한 나라 내륙에 찍혀있던
        /// 버그(Step 30-4)도 있었으나 이번 30-5 전면 재계산으로 전부 포함해서 다시 만들었다. 계산은
        /// 다운샘플(factor 3) 격자에서 8방향 A*(대각선 코너 관통 금지) + 위도 페널티로 하되, 검증은
        /// 반드시 원본 4000×1714 픽셀 기준으로 각 구간(허브 sea anchor 사이 전 구간)을 재확인했다.
        /// </summary>
        public static Dictionary<string, Vector2[]> BuildSeaWaypoints()
        {
            return new Dictionary<string, Vector2[]>
            {
                // 안트베르펜 ↔ 함부르크: 북해 연안, sea anchor 사이 짧은 경유.
                ["SEA_ANT|SEA_HAM"] = new[]
                {
                    new Vector2(-0.15f, 1.06f), new Vector2(-0.175f, 1.165f), new Vector2(-0.151f, 1.201f),
                    new Vector2(0.131f, 1.177f), new Vector2(0.149f, 1.177f), new Vector2(0.162f, 1.168f),
                },

                // 안트베르펜 ↔ 로테르담: 북해 연안 짧은 구간.
                ["SEA_ANT|SEA_ROT"] = new[]
                {
                    new Vector2(-0.15f, 1.06f), new Vector2(-0.175f, 1.165f), new Vector2(-0.151f, 1.201f),
                    new Vector2(-0.103f, 1.201f), new Vector2(-0.05f, 1.11f),
                },

                // 부산 ↔ 로스앤젤레스: [Step 30-5] LAX 오프셋을 고친 뒤 다시 계산 — 지도가 구를 감싸지
                // 않는 평면이라 날짜변경선 부근의 진짜 태평양 횡단은 표현할 수 없어, 남중국해 → 인도양 →
                // 희망봉을 돌아 남대서양을 가로질러 접근하는 훨씬 긴 경로를 쓴다.
                ["SEA_BUS|SEA_LAX"] = new[]
                {
                    new Vector2(2.56f, 0.59f), new Vector2(2.615f, 0.451f), new Vector2(2.597f, 0.259f),
                    new Vector2(2.651f, -0.041f), new Vector2(2.645f, -0.281f), new Vector2(2.555f, -0.521f),
                    new Vector2(0.485f, -1.169f), new Vector2(0.359f, -1.193f), new Vector2(-1.744f, 0.574f),
                },
                ["SEA_BUS|SEA_LGB"] = new[]
                {
                    new Vector2(2.56f, 0.59f), new Vector2(2.615f, 0.451f), new Vector2(2.597f, 0.259f),
                    new Vector2(2.651f, -0.041f), new Vector2(2.645f, -0.281f), new Vector2(2.555f, -0.521f),
                    new Vector2(0.485f, -1.169f), new Vector2(0.359f, -1.193f), new Vector2(-1.774f, 0.55f),
                },

                // 광저우 ↔ 홍콩: 주강 삼각주 안 인접 항구지만, 허브 실제 위치 자체가 해안선에서 살짝
                // 안쪽이라 sea anchor를 거치지 않으면 육지를 스친다 — 짧은 2점 경유로 충분.
                ["SEA_GUA|SEA_HKG"] = new[] { new Vector2(2.354f, 0.262f), new Vector2(2.306f, 0.198f) },

                // 광저우 ↔ 닝보: 중국 연안을 따라가는 경로.
                ["SEA_GUA|SEA_NGB"] = new[]
                {
                    new Vector2(2.354f, 0.262f), new Vector2(2.369f, 0.175f), new Vector2(2.411f, 0.175f),
                    new Vector2(2.585f, 0.355f), new Vector2(2.615f, 0.451f), new Vector2(2.537f, 0.679f),
                    new Vector2(2.489f, 0.685f), new Vector2(2.462f, 0.672f),
                },

                // 홍콩 ↔ 상하이: 중국 연안을 따라 대만해협 서쪽으로.
                ["SEA_HKG|SEA_SHA"] = new[]
                {
                    new Vector2(2.306f, 0.198f), new Vector2(2.489f, 0.253f), new Vector2(2.591f, 0.361f),
                    new Vector2(2.615f, 0.451f), new Vector2(2.579f, 0.541f), new Vector2(2.534f, 0.576f),
                },

                // 홍콩 ↔ 선전: 인접 항구, sea anchor 사이 짧은 직선.
                ["SEA_HKG|SEA_SHE"] = new[] { new Vector2(2.306f, 0.198f), new Vector2(2.306f, 0.214f) },

                // 홍콩 ↔ 싱가포르: 남중국해를 가로질러 접근.
                ["SEA_HKG|SEA_SIN"] = new[]
                {
                    new Vector2(2.306f, 0.198f), new Vector2(2.765f, -0.245f), new Vector2(2.789f, -0.245f),
                    new Vector2(2.84f, -0.4f),
                },

                // 제벨알리 ↔ 로테르담: 페르시아만 → 아라비아해 → 희망봉을 돌아 유럽 서안을 북상해 진입
                // (수에즈 진입부가 너무 좁아 A*가 못 찾아 대신 찾아낸 우회로).
                ["SEA_JEA|SEA_ROT"] = new[]
                {
                    new Vector2(1.1f, 0.35f), new Vector2(1.193f, 0.391f), new Vector2(1.211f, 0.361f),
                    new Vector2(1.277f, 0.283f), new Vector2(0.773f, -0.881f), new Vector2(0.557f, -1.145f),
                    new Vector2(0.485f, -1.169f), new Vector2(0.359f, -1.193f), new Vector2(0.329f, -1.163f),
                    new Vector2(-0.439f, 0.031f), new Vector2(-0.457f, 0.097f), new Vector2(-0.433f, 0.283f),
                    new Vector2(-0.157f, 1.099f), new Vector2(-0.175f, 1.153f), new Vector2(-0.157f, 1.201f),
                    new Vector2(-0.103f, 1.201f), new Vector2(-0.05f, 1.11f),
                },

                // 제벨알리 ↔ 상하이: 페르시아만 → 아라비아해 → 인도양 → 말라카 해협 → 남중국해로 북상.
                ["SEA_JEA|SEA_SHA"] = new[]
                {
                    new Vector2(1.1f, 0.35f), new Vector2(1.193f, 0.391f), new Vector2(1.211f, 0.361f),
                    new Vector2(1.703f, -0.095f), new Vector2(1.763f, -0.143f), new Vector2(2.375f, -0.497f),
                    new Vector2(2.555f, -0.521f), new Vector2(2.651f, -0.251f), new Vector2(2.651f, 0.037f),
                    new Vector2(2.627f, 0.127f), new Vector2(2.597f, 0.349f), new Vector2(2.615f, 0.469f),
                    new Vector2(2.573f, 0.547f), new Vector2(2.534f, 0.576f),
                },

                // 제벨알리 ↔ 싱가포르: 페르시아만(호르무즈 해협) → 아라비아해 → 말라카 해협.
                ["SEA_JEA|SEA_SIN"] = new[]
                {
                    new Vector2(1.1f, 0.35f), new Vector2(1.193f, 0.391f), new Vector2(1.223f, 0.343f),
                    new Vector2(1.703f, -0.095f), new Vector2(1.763f, -0.143f), new Vector2(2.375f, -0.497f),
                    new Vector2(2.561f, -0.521f), new Vector2(2.615f, -0.497f), new Vector2(2.84f, -0.4f),
                },

                // 로스앤젤레스 ↔ 롱비치: 같은 항만 지역, sea anchor 사이 짧은 경유.
                ["SEA_LAX|SEA_LGB"] = new[]
                {
                    new Vector2(-1.744f, 0.574f), new Vector2(-1.771f, 0.547f), new Vector2(-1.774f, 0.55f),
                },

                // 로스앤젤레스 ↔ 상하이: 부산↔LA와 같은 희망봉 우회(태평양 미포장 한계).
                ["SEA_LAX|SEA_SHA"] = new[]
                {
                    new Vector2(-1.744f, 0.574f), new Vector2(0.365f, -1.193f), new Vector2(0.539f, -1.157f),
                    new Vector2(2.567f, -0.509f), new Vector2(2.651f, -0.251f), new Vector2(2.651f, 0.037f),
                    new Vector2(2.627f, 0.127f), new Vector2(2.597f, 0.349f), new Vector2(2.615f, 0.469f),
                    new Vector2(2.573f, 0.547f), new Vector2(2.534f, 0.576f),
                },

                // 롱비치 ↔ 산투스(브라질): 파나마 지협 근처를 지나는 경로(실제 파나마 운하 위치 근사).
                ["SEA_LGB|SEA_SAN"] = new[]
                {
                    new Vector2(-1.774f, 0.55f), new Vector2(-0.841f, -0.443f), new Vector2(-0.841f, -0.533f),
                    new Vector2(-0.967f, -0.875f), new Vector2(-1.114f, -0.944f),
                },

                // 닝보 ↔ 상하이: 저우산 군도 연안을 살짝 피해가는 짧은 경로.
                ["SEA_NGB|SEA_SHA"] = new[]
                {
                    new Vector2(2.462f, 0.672f), new Vector2(2.507f, 0.685f), new Vector2(2.549f, 0.667f),
                    new Vector2(2.534f, 0.576f),
                },

                // 닝보 ↔ 선전: 중국 연안을 따라가는 경로.
                ["SEA_NGB|SEA_SHE"] = new[]
                {
                    new Vector2(2.462f, 0.672f), new Vector2(2.507f, 0.685f), new Vector2(2.549f, 0.667f),
                    new Vector2(2.615f, 0.445f), new Vector2(2.561f, 0.319f), new Vector2(2.393f, 0.169f),
                    new Vector2(2.306f, 0.214f),
                },

                // 포트클랑 ↔ 상하이: 말라카 해협을 지나 남중국해를 따라 북상.
                ["SEA_PKL|SEA_SHA"] = new[]
                {
                    new Vector2(2.34f, -0.1f), new Vector2(2.411f, 0.013f), new Vector2(2.615f, 0.433f),
                    new Vector2(2.597f, 0.517f), new Vector2(2.534f, 0.576f),
                },

                // 포트클랑 ↔ 싱가포르: 말라카 해협 초입 짧은 구간.
                ["SEA_PKL|SEA_SIN"] = new[]
                {
                    new Vector2(2.34f, -0.1f), new Vector2(2.609f, -0.113f), new Vector2(2.789f, -0.251f),
                    new Vector2(2.84f, -0.4f),
                },

                // 함부르크 ↔ 로테르담: 북해 직항, sea anchor 사이 짧은 경유.
                ["SEA_HAM|SEA_ROT"] = new[] { new Vector2(0.162f, 1.168f), new Vector2(0.149f, 1.183f), new Vector2(-0.05f, 1.11f) },

                // 로테르담 ↔ 산투스: 남미 동안을 따라 남하 후 대서양을 가로질러 접근.
                ["SEA_ROT|SEA_SAN"] = new[]
                {
                    new Vector2(-0.05f, 1.11f), new Vector2(-0.103f, 1.201f), new Vector2(-0.157f, 1.201f),
                    new Vector2(-0.175f, 1.147f), new Vector2(-0.157f, 1.081f), new Vector2(-0.973f, -0.881f),
                    new Vector2(-1.114f, -0.944f),
                },

                // 로테르담 ↔ 싱가포르: 유럽 서안을 남하해 희망봉을 크게 돌아 인도양 → 인도네시아 남쪽 →
                // 말라카 해협으로 접근(수에즈 진입부가 렌더링 해상도로는 너무 좁아 A*가 대신 찾아낸 우회로).
                ["SEA_ROT|SEA_SIN"] = new[]
                {
                    new Vector2(-0.05f, 1.11f), new Vector2(-0.103f, 1.201f), new Vector2(-0.157f, 1.201f),
                    new Vector2(-0.175f, 1.147f), new Vector2(-0.157f, 1.081f), new Vector2(-0.439f, 0.259f),
                    new Vector2(-0.457f, 0.079f), new Vector2(-0.421f, -0.005f), new Vector2(0.347f, -1.187f),
                    new Vector2(0.383f, -1.193f), new Vector2(1.949f, -0.809f), new Vector2(2.615f, -0.497f),
                    new Vector2(2.84f, -0.4f),
                },

                // 상하이 ↔ 부산: sea anchor 사이 짧은 직선(동중국해 직항).
                ["SEA_BUS|SEA_SHA"] = new[] { new Vector2(2.56f, 0.59f), new Vector2(2.534f, 0.576f) },

                // 상하이 ↔ 선전: 중국 연안을 따라 대만해협 서쪽으로.
                ["SEA_SHA|SEA_SHE"] = new[]
                {
                    new Vector2(2.534f, 0.576f), new Vector2(2.597f, 0.511f), new Vector2(2.615f, 0.433f),
                    new Vector2(2.561f, 0.319f), new Vector2(2.393f, 0.169f), new Vector2(2.306f, 0.214f),
                },

                // 상하이 ↔ 싱가포르: 중국 연안을 따라 내려간 뒤 남중국해로.
                ["SEA_SHA|SEA_SIN"] = new[]
                {
                    new Vector2(2.534f, 0.576f), new Vector2(2.597f, 0.511f), new Vector2(2.819f, -0.101f),
                    new Vector2(2.84f, -0.4f),
                },

                // 광저우 ↔ 선전: 인접 항구, sea anchor 사이 짧은 직선.
                ["SEA_GUA|SEA_SHE"] = new[] { new Vector2(2.354f, 0.262f), new Vector2(2.306f, 0.214f) },

                // ===== 신규 해운 허브 11개 경유점 (핵심 공백 채우기, world_base.png 알파채널 기준
                // 다운샘플(factor 2) 8방향 A* + 대각선 코너 관통 금지로 계산, 전 구간 육지 미관통 검증) =====

                // 알헤시라스 ↔ 제노바: 지중해 서→동, 스페인 남해안-발레아레스 사이를 지나 리구리아해로.
                ["SEA_ALC|SEA_GOA"] = new[]
                {
                    new Vector2(-0.162f, 0.632f), new Vector2(-0.050f, 0.660f), new Vector2(0.118f, 0.820f),
                    new Vector2(0.146f, 0.832f),
                },

                // 제노바 ↔ 포트사이드: 지중해 동안을 따라 이탈리아 남단-그리스 인근을 지나 이집트로.
                ["SEA_GOA|SEA_PSD"] = new[]
                {
                    new Vector2(0.146f, 0.832f), new Vector2(0.158f, 0.808f), new Vector2(0.274f, 0.688f),
                    new Vector2(0.274f, 0.680f), new Vector2(0.278f, 0.676f), new Vector2(0.426f, 0.612f),
                    new Vector2(0.514f, 0.524f), new Vector2(0.622f, 0.524f), new Vector2(0.654f, 0.508f),
                },

                // 로테르담 ↔ 탕헤르메드: 대서양 유럽 서안(포르투갈-스페인 연안)을 남하해 지브롤터 해협으로.
                // [수에즈 우회 참고] 수에즈 운하는 지도에 뭍으로만 그려져 있어 이집트↔중동 항로는 이
                // 경로(로테르담↔탕헤르메드↔알헤시라스↔제노바↔포트사이드) 다구간 경유로 대체한다.
                ["SEA_ROT|SEA_TNG"] = new[]
                {
                    new Vector2(-0.050f, 1.108f), new Vector2(-0.018f, 1.052f), new Vector2(-0.022f, 1.016f),
                    new Vector2(-0.042f, 1.000f), new Vector2(-0.106f, 0.996f), new Vector2(-0.150f, 0.952f),
                    new Vector2(-0.150f, 0.912f), new Vector2(-0.250f, 0.812f), new Vector2(-0.258f, 0.696f),
                    new Vector2(-0.246f, 0.684f), new Vector2(-0.246f, 0.656f), new Vector2(-0.242f, 0.652f),
                    new Vector2(-0.214f, 0.648f), new Vector2(-0.210f, 0.644f), new Vector2(-0.198f, 0.644f),
                    new Vector2(-0.182f, 0.628f), new Vector2(-0.162f, 0.628f), new Vector2(-0.158f, 0.624f),
                },

                // 르아브르 ↔ 로테르담: 영불해협-북해 연안 짧은 구간.
                ["SEA_LEH|SEA_ROT"] = new[]
                {
                    new Vector2(-0.050f, 0.984f), new Vector2(-0.022f, 1.016f), new Vector2(-0.018f, 1.044f),
                    new Vector2(-0.018f, 1.052f), new Vector2(-0.050f, 1.108f),
                },

                // 안트베르펜 ↔ 르아브르: 북해-영불해협 연안 짧은 구간.
                ["SEA_ANT|SEA_LEH"] = new[]
                {
                    new Vector2(-0.150f, 1.060f), new Vector2(-0.170f, 1.020f), new Vector2(-0.170f, 0.988f),
                    new Vector2(-0.166f, 0.984f), new Vector2(-0.050f, 0.984f),
                },

                // 그단스크 ↔ 함부르크: 발트해에서 덴마크 해협을 지나 북해로.
                ["SEA_GDN|SEA_HAM"] = new[]
                {
                    new Vector2(0.342f, 1.116f), new Vector2(0.202f, 1.116f), new Vector2(0.174f, 1.100f),
                    new Vector2(0.154f, 1.112f), new Vector2(0.146f, 1.124f), new Vector2(0.158f, 1.168f),
                },

                // 라고스 ↔ 산투스: 대서양을 가로지르는 나이지리아-브라질 항로(실제 서아프리카-남미 무역로).
                ["SEA_LOS|SEA_SAN"] = new[]
                {
                    new Vector2(0.030f, -0.136f), new Vector2(-0.502f, -0.668f), new Vector2(-1.110f, -0.944f),
                },

                // 라고스 ↔ 탕헤르메드: 서아프리카 대서양 연안을 따라 북상.
                ["SEA_LOS|SEA_TNG"] = new[]
                {
                    new Vector2(0.030f, -0.136f), new Vector2(-0.030f, -0.144f), new Vector2(-0.094f, -0.176f),
                    new Vector2(-0.242f, -0.184f), new Vector2(-0.330f, -0.116f), new Vector2(-0.406f, -0.040f),
                    new Vector2(-0.406f, -0.012f), new Vector2(-0.426f, 0.008f), new Vector2(-0.434f, 0.020f),
                    new Vector2(-0.438f, 0.068f), new Vector2(-0.454f, 0.084f), new Vector2(-0.438f, 0.100f),
                    new Vector2(-0.438f, 0.260f), new Vector2(-0.414f, 0.316f), new Vector2(-0.386f, 0.348f),
                    new Vector2(-0.386f, 0.364f), new Vector2(-0.378f, 0.380f), new Vector2(-0.350f, 0.412f),
                    new Vector2(-0.270f, 0.492f), new Vector2(-0.270f, 0.516f), new Vector2(-0.254f, 0.552f),
                    new Vector2(-0.182f, 0.628f), new Vector2(-0.162f, 0.628f), new Vector2(-0.158f, 0.624f),
                },

                // 더반 ↔ 제벨알리: 동아프리카 연안(모잠비크 해협)을 따라 북상해 아라비아해로.
                ["SEA_DUR|SEA_JEA"] = new[]
                {
                    new Vector2(0.634f, -1.048f), new Vector2(0.654f, -1.032f), new Vector2(0.670f, -0.976f),
                    new Vector2(0.966f, -0.676f), new Vector2(0.966f, -0.260f), new Vector2(1.022f, -0.200f),
                    new Vector2(1.074f, -0.108f), new Vector2(1.102f, -0.020f), new Vector2(1.102f, 0.060f),
                    new Vector2(1.238f, 0.196f), new Vector2(1.238f, 0.220f), new Vector2(1.254f, 0.236f),
                    new Vector2(1.274f, 0.272f), new Vector2(1.274f, 0.288f), new Vector2(1.186f, 0.388f),
                    new Vector2(1.102f, 0.348f),
                },

                // 더반 ↔ 산투스: 남대서양을 가로지르는 남아공-브라질 항로.
                ["SEA_DUR|SEA_SAN"] = new[]
                {
                    new Vector2(0.634f, -1.048f), new Vector2(0.610f, -1.084f), new Vector2(0.538f, -1.152f),
                    new Vector2(0.498f, -1.168f), new Vector2(0.426f, -1.168f), new Vector2(0.374f, -1.192f),
                    new Vector2(0.354f, -1.192f), new Vector2(0.234f, -1.072f), new Vector2(-0.838f, -1.068f),
                    new Vector2(-1.110f, -0.944f),
                },

                // 부에노스아이레스 ↔ 산투스: 남미 대서양 연안을 따라 북상하는 짧은 구간.
                ["SEA_BUE|SEA_SAN"] = new[]
                {
                    new Vector2(-1.298f, -1.180f), new Vector2(-1.230f, -1.196f), new Vector2(-1.202f, -1.176f),
                    new Vector2(-1.186f, -1.148f), new Vector2(-1.182f, -1.124f), new Vector2(-1.154f, -1.096f),
                    new Vector2(-1.118f, -1.028f), new Vector2(-1.110f, -0.944f),
                },

                // 부에노스아이레스 ↔ 로테르담: 남대서양을 가로질러 유럽으로 북상하는 장거리 항로.
                ["SEA_BUE|SEA_ROT"] = new[]
                {
                    new Vector2(-1.298f, -1.180f), new Vector2(-1.230f, -1.196f), new Vector2(-1.210f, -1.184f),
                    new Vector2(-0.930f, -0.888f), new Vector2(-0.922f, -0.804f), new Vector2(-0.418f, 0.308f),
                    new Vector2(-0.354f, 0.432f), new Vector2(-0.258f, 0.648f), new Vector2(-0.258f, 0.716f),
                    new Vector2(-0.250f, 0.724f), new Vector2(-0.250f, 0.812f), new Vector2(-0.150f, 0.912f),
                    new Vector2(-0.150f, 0.952f), new Vector2(-0.106f, 0.996f), new Vector2(-0.042f, 1.000f),
                    new Vector2(-0.022f, 1.016f), new Vector2(-0.018f, 1.052f), new Vector2(-0.050f, 1.108f),
                },

                // 카르타헤나 ↔ 산투스: 카리브해-남미 대서양 연안을 따라 남하(파나마 지협은 관통하지
                // 않음 — 이 지도에 운하가 뭍으로 그려져 있어 태평양 쪽 롱비치와는 직결하지 않았다).
                ["SEA_CTG|SEA_SAN"] = new[]
                {
                    new Vector2(-1.666f, -0.008f), new Vector2(-1.630f, 0.024f), new Vector2(-1.618f, 0.012f),
                    new Vector2(-1.558f, 0.012f), new Vector2(-1.538f, -0.008f), new Vector2(-1.434f, -0.008f),
                    new Vector2(-1.306f, -0.136f), new Vector2(-1.290f, -0.136f), new Vector2(-1.254f, -0.148f),
                    new Vector2(-1.230f, -0.168f), new Vector2(-1.046f, -0.352f), new Vector2(-1.030f, -0.352f),
                    new Vector2(-0.998f, -0.364f), new Vector2(-0.958f, -0.364f), new Vector2(-0.926f, -0.384f),
                    new Vector2(-0.894f, -0.416f), new Vector2(-0.862f, -0.420f), new Vector2(-0.846f, -0.436f),
                    new Vector2(-0.838f, -0.472f), new Vector2(-0.842f, -0.520f), new Vector2(-0.922f, -0.636f),
                    new Vector2(-0.926f, -0.740f), new Vector2(-0.938f, -0.800f), new Vector2(-0.958f, -0.828f),
                    new Vector2(-0.958f, -0.856f), new Vector2(-0.986f, -0.888f), new Vector2(-1.110f, -0.944f),
                },

                // 멜버른 ↔ 싱가포르: 호주 서안-인도네시아 군도 사이를 지나 접근.
                ["SEA_MEL|SEA_SIN"] = new[]
                {
                    new Vector2(3.106f, -1.264f), new Vector2(3.126f, -1.252f), new Vector2(3.266f, -1.100f),
                    new Vector2(3.310f, -1.024f), new Vector2(3.322f, -0.944f), new Vector2(3.294f, -0.900f),
                    new Vector2(3.294f, -0.872f), new Vector2(3.262f, -0.816f), new Vector2(3.218f, -0.772f),
                    new Vector2(3.214f, -0.676f), new Vector2(3.182f, -0.640f), new Vector2(3.166f, -0.568f),
                    new Vector2(3.126f, -0.528f), new Vector2(3.058f, -0.512f), new Vector2(3.030f, -0.484f),
                    new Vector2(2.982f, -0.472f), new Vector2(2.842f, -0.400f),
                },

                // 멜버른 ↔ 포트클랑: 인도네시아 군도 사이를 지나 말라카 해협 쪽으로 더 서진.
                ["SEA_MEL|SEA_PKL"] = new[]
                {
                    new Vector2(3.106f, -1.264f), new Vector2(3.126f, -1.252f), new Vector2(3.266f, -1.100f),
                    new Vector2(3.310f, -1.024f), new Vector2(3.322f, -0.960f), new Vector2(3.322f, -0.944f),
                    new Vector2(3.294f, -0.900f), new Vector2(3.294f, -0.872f), new Vector2(3.270f, -0.848f),
                    new Vector2(3.262f, -0.816f), new Vector2(3.218f, -0.772f), new Vector2(3.214f, -0.676f),
                    new Vector2(3.182f, -0.640f), new Vector2(3.166f, -0.568f), new Vector2(3.110f, -0.512f),
                    new Vector2(3.058f, -0.512f), new Vector2(2.998f, -0.452f), new Vector2(2.998f, -0.436f),
                    new Vector2(2.966f, -0.404f), new Vector2(2.958f, -0.404f), new Vector2(2.914f, -0.360f),
                    new Vector2(2.874f, -0.344f), new Vector2(2.782f, -0.248f), new Vector2(2.742f, -0.248f),
                    new Vector2(2.642f, -0.148f), new Vector2(2.598f, -0.112f), new Vector2(2.366f, -0.112f),
                    new Vector2(2.342f, -0.100f),
                },
            };
        }

        /// <summary>
        /// 항공 노선 경유점 테이블. 규칙은 BuildSeaWaypoints()와 동일(canonical pair key, 작은→큰 방향).
        /// 비행기는 육지 위를 지나가도 시각적으로 어색하지 않지만(실제로도 대륙 상공을 비행), LA↔동아시아/
        /// 호주처럼 태평양을 가로지르는 노선을 직선으로 그으면 지도 반대편(유라시아)을 관통하는 "완전히
        /// 엉뚱한 방향"으로 보인다 — 실제 장거리 노선처럼 북태평양(또는 남태평양)을 도는 곡선으로 우회.
        /// 그 외 항로(대서양 횡단 등)는 지도상에서 이미 올바른 방향이라 직선 그대로 둔다.
        /// </summary>
        public static Dictionary<string, Vector2[]> BuildAirWaypoints()
        {
            return new Dictionary<string, Vector2[]>
            {
                // 도쿄 하네다 ↔ 로스앤젤레스: 북태평양 우회.
                ["HND|LAX"] = new[] { new Vector2(1.2f, 1.5f), new Vector2(-0.8f, 1.3f) },

                // 인천 ↔ 로스앤젤레스: 위와 동일한 북태평양 우회.
                ["ICN|LAX"] = new[] { new Vector2(1.3f, 1.5f), new Vector2(-0.8f, 1.3f) },

                // 로스앤젤레스 ↔ 시드니: 남태평양 우회(적도 아래, 아르헨티나/남아공 실루엣보다 남쪽).
                ["LAX|SYD"] = new[] { new Vector2(-1.6f, -1.3f), new Vector2(1.0f, -1.6f) },
            };
        }
    }
}
