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
    /// </summary>
    public static class DefaultTransportHubFactory
    {
        public static List<TransportHub> BuildDefaultHubs()
        {
            var hubs = new List<TransportHub>();

            // ================= 항공 허브 15개 =================
            hubs.Add(Air("ATL", "애틀랜타", "USA", new Vector2(-0.25f, 0.15f),
                ("DFW", 9), ("LAX", 8), ("LHR", 9), ("GRU", 6)));
            hubs.Add(Air("DFW", "댈러스", "USA", new Vector2(-0.05f, 0.05f),
                ("ATL", 9), ("LAX", 8), ("LHR", 7)));
            hubs.Add(Air("LAX", "로스앤젤레스", "USA", new Vector2(0.2f, -0.1f),
                ("ATL", 8), ("DFW", 8), ("HND", 9), ("ICN", 6), ("SYD", 7), ("GRU", 5)));
            hubs.Add(Air("LHR", "런던 히스로", "UK", new Vector2(0.1f, 0.1f),
                ("ATL", 9), ("DFW", 7), ("IST", 8), ("DXB", 8), ("DEL", 7)));
            hubs.Add(Air("IST", "이스탄불", "TUR", new Vector2(0.15f, 0.1f),
                ("LHR", 8), ("DXB", 9), ("DEL", 6), ("CAN", 5)));
            hubs.Add(Air("DXB", "두바이", "SAU", new Vector2(0.2f, -0.05f),
                ("LHR", 8), ("IST", 9), ("DEL", 9), ("SIN", 9), ("HKG", 6)));
            hubs.Add(Air("DEL", "델리", "IND", new Vector2(-0.15f, 0.2f),
                ("LHR", 7), ("IST", 6), ("DXB", 9), ("SIN", 7), ("ICN", 5)));
            hubs.Add(Air("ICN", "인천", "KOR", new Vector2(0.1f, 0.1f),
                ("HND", 10), ("PVG", 9), ("SIN", 8), ("DXB", 7), ("LAX", 6)));
            hubs.Add(Air("HND", "도쿄 하네다", "JPN", new Vector2(-0.1f, 0.1f),
                ("ICN", 10), ("PVG", 8), ("LAX", 9), ("SYD", 7)));
            hubs.Add(Air("PVG", "상하이 푸둥", "CHI", new Vector2(0.25f, -0.15f),
                ("ICN", 9), ("HND", 8), ("HKG", 8), ("SIN", 7), ("CAN", 6)));
            hubs.Add(Air("CAN", "광저우", "CHI", new Vector2(0.15f, -0.3f),
                ("PVG", 6), ("HKG", 9), ("SIN", 7), ("IST", 5)));
            hubs.Add(Air("SIN", "싱가포르 창이", "MAS", new Vector2(0.2f, -0.2f),
                ("ICN", 8), ("PVG", 8), ("DXB", 9), ("SYD", 8)));
            hubs.Add(Air("HKG", "홍콩", "CHI", new Vector2(0.05f, -0.35f),
                ("PVG", 8), ("CAN", 9), ("DXB", 6), ("SYD", 6)));
            hubs.Add(Air("SYD", "시드니", "AUS", new Vector2(0.1f, 0.1f),
                ("LAX", 7), ("HND", 7), ("SIN", 8), ("HKG", 6)));
            hubs.Add(Air("GRU", "상파울루", "BRA", new Vector2(0.1f, -0.1f),
                ("ATL", 6), ("LAX", 5)));

            // ================= 해운 허브 15개 =================
            hubs.Add(Sea("SEA_SHA", "상하이항", "CHI", new Vector2(0.35f, -0.1f),
                ("SEA_NGB", 10), ("SEA_SHE", 8), ("SEA_BUS", 9), ("SEA_SIN", 8), ("SEA_HKG", 7)));
            hubs.Add(Sea("SEA_SIN", "싱가포르항", "MAS", new Vector2(0.3f, -0.2f),
                ("SEA_SHA", 8), ("SEA_PKL", 10), ("SEA_HKG", 8), ("SEA_JEA", 9), ("SEA_ROT", 6)));
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
                ("SEA_HAM", 9), ("SEA_ANT", 9), ("SEA_JEA", 7), ("SEA_SIN", 6)));
            hubs.Add(Sea("SEA_JEA", "제벨알리항", "SAU", new Vector2(-0.15f, 0.15f),
                ("SEA_SIN", 9), ("SEA_ROT", 7), ("SEA_SHA", 5)));
            hubs.Add(Sea("SEA_LAX", "로스앤젤레스항", "USA", new Vector2(0.3f, 0.15f),
                ("SEA_LGB", 10), ("SEA_BUS", 7), ("SEA_SHA", 6)));
            hubs.Add(Sea("SEA_LGB", "롱비치항", "USA", new Vector2(0.3f, -0.1f),
                ("SEA_LAX", 10), ("SEA_BUS", 6), ("SEA_SAN", 5)));
            hubs.Add(Sea("SEA_ANT", "안트베르펜항", "GER", new Vector2(-0.3f, 0.05f),
                ("SEA_ROT", 9), ("SEA_HAM", 8)));
            hubs.Add(Sea("SEA_HAM", "함부르크항", "GER", new Vector2(0.05f, 0.2f),
                ("SEA_ROT", 9), ("SEA_ANT", 8)));
            hubs.Add(Sea("SEA_PKL", "포트클랑항", "MAS", new Vector2(-0.2f, 0.1f),
                ("SEA_SIN", 10), ("SEA_SHA", 6)));
            hubs.Add(Sea("SEA_SAN", "산투스항", "BRA", new Vector2(-0.1f, -0.15f),
                ("SEA_LGB", 5), ("SEA_ROT", 5)));

            return hubs;
        }

        private static TransportHub Air(string id, string name, string countryId, Vector2 offset,
            params (string target, float weight)[] links)
            => Build(id, TransportHubType.Air, name, countryId, offset, links);

        private static TransportHub Sea(string id, string name, string countryId, Vector2 offset,
            params (string target, float weight)[] links)
            => Build(id, TransportHubType.Sea, name, countryId, offset, links);

        private static TransportHub Build(string id, TransportHubType type, string name, string countryId,
            Vector2 offset, (string target, float weight)[] links)
        {
            var hub = new TransportHub(id, type, name, countryId, offset);
            foreach (var (target, weight) in links)
                hub.connections.Add(new TransportRouteLink(target, weight));
            return hub;
        }
    }
}
