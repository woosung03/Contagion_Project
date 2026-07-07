using System;
using System.Collections.Generic;
using UnityEngine;

namespace Contagion.Data
{
    /// <summary>Resources/InfectionDotPoints.json의 국가 1개 항목. JsonUtility 호환을 위해
    /// points는 [x0,y0,x1,y1,...] 형태의 flat float 배열이다(JsonUtility가 List&lt;List&lt;float&gt;&gt;
    /// 같은 중첩 배열은 역직렬화하지 못하기 때문). diameter는 이 국가에 맞춰 오프라인 스크립트가 계산해둔
    /// "점 하나의 권장 지름"(로컬 유닛) — 국가마다 면적/점 개수가 달라(Step 36) 전역 고정값 대신 국가별
    /// 값을 그대로 쓴다(전부 이 점들로 국가 면적의 ~70%를 덮도록 역산한 값, CountryView 참고).</summary>
    [Serializable]
    internal class InfectionDotCountryEntry
    {
        public string id;
        public float[] points;
        public float diameter;
    }

    [Serializable]
    internal class InfectionDotPointsFile
    {
        public List<InfectionDotCountryEntry> countries;
    }

    /// <summary>
    /// 국가별 "감염 점(dot) 오버레이" 스폰 후보 좌표 로더 — 문제: 감염자 비율이 늘어도 CountryView의
    /// 반투명 색상 얼룩만으로는 시각적 피드백이 약하다(특히 Step 34로 인구가 실제 수치 그대로라 초반
    /// 감염 비율이 오랫동안 거의 0에 가깝게 보임). 해결책: 국가 실루엣 내부에 점을 몇 개 찍어서
    /// 감염 비율에 비례해 하나씩 드러나게 한다 — CountryView.UpdateInfectionDots()가 이 클래스에서
    /// 좌표를 받아 사용.
    ///
    /// 좌표는 런타임에 계산하지 않는다. Assets/Resources/CountryShapes/{id}.png는 isReadable=0이라
    /// 런타임에 알파 채널을 읽을 수 없다(해운 항로 A* 계산 때 world_base.png가 같은 이유로 오프라인
    /// 계산이었던 것과 동일한 사정 — DevLog Step 29~30 참고). 대신 오프라인 Python 스크립트(1회성 도구,
    /// 프로젝트에는 포함 안 함)가 각 국가 실루엣의 알파마스크에서 좌표를 미리 뽑아 CountryView와 동일한
    /// 로컬 좌표계(캔버스 4000x1714px, Sprite Pixels Per Unit 500, pivot 0.5/0.5 —
    /// localX=(px-2000)/500, localY=(857-py)/500)로 변환해 Resources/InfectionDotPoints.json에 저장해뒀다.
    ///
    /// [Step 36] 첫 버전(Step 35)은 국가당 고정 24개를 farthest-point sampling(균일하게 퍼뜨리기)으로
    /// 뽑아서 "너무 규칙적으로 보이고 개수가 적다"는 피드백을 받았다. 지금은:
    /// - 국가별 점 개수(14~90)와 점 지름을 실제 국가 면적(알파 픽셀 수)에 맞춰 개별 계산 — 감염률
    ///   100%(전체 점 활성화)일 때 원들이 그 국가 면적의 약 70%를 덮도록 지름을 역산했다(작은 나라는
    ///   점이 작고 적게, 큰 나라는 점이 크고 많게).
    /// - 주요 도시(대략적 위경도+상대 인구가중치, `gen_dots_v2.py`에 하드코딩— 정밀 통계 아님) 주변에
    ///   가우시안 지터로 클러스터를 만들고, 나머지는 국가 전역에 랜덤 배경(rural) 점으로 채운 뒤,
    ///   가중 라운드로빈으로 순서를 섞어 배열 앞부분만 잘라 써도 "주요 도시 위주 + 약간의 배경"이 항상
    ///   유지되게 했다 — 인구밀도에 비례해 보이면서도(도시가 먼저/더 많이 나타남) 배치 자체는 가우시안
    ///   지터라 규칙적인 격자처럼 안 보인다(farthest-point sampling의 "균일함"이 오히려 "규칙적"으로
    ///   보였던 문제 해결).
    /// 상세 배경은 Docs/DevLog.md Step 36 참고.
    /// </summary>
    public static class InfectionDotDatabase
    {
        /// <summary>국가 1개의 감염 점 배치 데이터 — 좌표 배열 + 이 국가에 맞춰 계산된 점 지름.</summary>
        public readonly struct InfectionDotLayout
        {
            public readonly Vector2[] Points;
            public readonly float Diameter;

            public InfectionDotLayout(Vector2[] points, float diameter)
            {
                Points = points;
                Diameter = diameter;
            }
        }

        private static readonly InfectionDotLayout EmptyLayout = new InfectionDotLayout(Array.Empty<Vector2>(), 0f);
        private static Dictionary<string, InfectionDotLayout> _layoutByCountry;

        /// <summary>해당 국가의 점 좌표+지름. 데이터가 없으면 빈 배열/지름 0(호출부가 그대로 스킵하면 됨).</summary>
        public static InfectionDotLayout GetLayout(string countryId)
        {
            EnsureLoaded();
            if (!string.IsNullOrEmpty(countryId) && _layoutByCountry.TryGetValue(countryId, out var layout))
                return layout;
            return EmptyLayout;
        }

        private static void EnsureLoaded()
        {
            if (_layoutByCountry != null) return;
            _layoutByCountry = new Dictionary<string, InfectionDotLayout>();

            TextAsset json = Resources.Load<TextAsset>("InfectionDotPoints");
            if (json == null)
            {
                Debug.LogWarning("[InfectionDotDatabase] Resources/InfectionDotPoints.json을 찾지 못했습니다 — 감염 점 오버레이가 표시되지 않습니다.");
                return;
            }

            InfectionDotPointsFile file = null;
            try
            {
                file = JsonUtility.FromJson<InfectionDotPointsFile>(json.text);
            }
            catch (Exception e)
            {
                Debug.LogError($"[InfectionDotDatabase] InfectionDotPoints.json 파싱 실패: {e.Message}");
                return;
            }

            if (file?.countries == null) return;

            foreach (var entry in file.countries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.id) || entry.points == null || entry.points.Length < 2)
                    continue;

                int pairCount = entry.points.Length / 2;
                var arr = new Vector2[pairCount];
                for (int i = 0; i < pairCount; i++)
                    arr[i] = new Vector2(entry.points[i * 2], entry.points[i * 2 + 1]);

                _layoutByCountry[entry.id] = new InfectionDotLayout(arr, entry.diameter);
            }

            Debug.Log($"[InfectionDotDatabase] 감염 점 좌표 로드 완료 — {_layoutByCountry.Count}개국");
        }
    }
}
