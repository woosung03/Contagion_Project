using System;

namespace Contagion.UI
{
    /// <summary>
    /// Number Formatting Policy v2 — 값의 종류(정적/동적)가 아니라 컴포넌트의 역할(Role)로
    /// 표기 방식을 결정한다. Summary(요약, 5초 스캔 컴포넌트: Hero Stat 등) = 만/억/조 축약,
    /// Detail(상세, 정밀 조회 컴포넌트: data-row 목록/랭킹 등) = 전체 숫자.
    ///
    /// 화면이나 데이터 종류에 종속되지 않는 순수 문자열 포맷 유틸리티라 어느 컨트롤러가 호출해도
    /// 서로 의존성이 생기지 않는다(기존 CaseFatalityRate/MedicalLoad 등 계산식 독립 복제 관례와
    /// 달리, 이건 계산식이 아니라 표기 방식이라 여러 컨트롤러가 공유해도 그 관례를 어기지 않는다).
    /// </summary>
    public static class NumberFormatter
    {
        private const decimal Man = 10_000m;
        private const decimal Eok = 100_000_000m;
        private const decimal Jo = 1_000_000_000_000m;

        /// <summary>
        /// Summary 컴포넌트(Hero Stat 등)용 — 0~9,999는 그대로, 1만 이상은 만 단위(정수),
        /// 1억 이상은 억 단위(소수 1자리), 1조 이상은 조 단위(소수 1자리). decimal 연산으로
        /// 부동소수점 오차를 피하고, 만/억 경계에서 반올림으로 "10000만"처럼 어색해지는 경우
        /// 자동으로 억 단위로 승격한다.
        /// </summary>
        public static string FormatSummary(long value)
        {
            bool negative = value < 0;
            decimal abs = Math.Abs((decimal)value);
            string body;

            if (abs >= Jo)
            {
                decimal jo = Math.Round(abs / Jo, 1, MidpointRounding.AwayFromZero);
                body = $"{jo:0.#}조";
            }
            else if (abs >= Eok)
            {
                decimal eok = Math.Round(abs / Eok, 1, MidpointRounding.AwayFromZero);
                body = $"{eok:0.#}억";
            }
            else if (abs >= Man)
            {
                // [Number Formatting Policy v2 수정] 만 단위 내에서도 2단계로 나뉜다 — 1만~99.9만
                // 미만은 소수 1자리(예: 17.2만), 100만~1억 미만은 정수(예: 2453만). manRaw가 100
                // 미만인지로 먼저 자릿수를 고르고, 반올림 결과가 그 경계(100만 또는 1억)를 다시
                // 넘어서면(예: 99.95만→100.0만, 9999.6만→10000만) 상위 표기로 재승격한다.
                decimal manRaw = abs / Man;
                decimal manRounded = manRaw < 100m
                    ? Math.Round(manRaw, 1, MidpointRounding.AwayFromZero)
                    : Math.Round(manRaw, 0, MidpointRounding.AwayFromZero);

                if (manRounded >= 10_000m)
                {
                    // 반올림 결과가 만 단위 상한(10000만 = 1억)에 걸리는 경계 보정 — "10000만" 대신 억 단위로.
                    decimal eok = Math.Round(abs / Eok, 1, MidpointRounding.AwayFromZero);
                    body = $"{eok:0.#}억";
                }
                else if (manRounded >= 100m)
                {
                    // 소수 1자리로 반올림했더니 100만 경계를 넘은 경우(예: 99.95→100.0) 정수로 재표기.
                    decimal manInt = Math.Round(manRaw, 0, MidpointRounding.AwayFromZero);
                    body = $"{manInt:0.#}만";
                }
                else
                {
                    body = $"{manRounded:0.#}만";
                }
            }
            else
            {
                body = abs.ToString("N0");
            }

            return negative ? "-" + body : body;
        }

        /// <summary>Detail 컴포넌트(data-row/랭킹/리스트 등)용 — 항상 전체 숫자. 얇은 래퍼지만
        /// 호출부에서 "이 자리는 의도적으로 정밀 숫자"라는 의도를 코드로 드러내기 위해 둔다.</summary>
        public static string FormatDetail(long value) => value.ToString("N0");
    }
}
