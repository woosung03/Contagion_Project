using System;
using Contagion.Data;
using UnityEngine;

namespace Contagion.Gameplay
{
    /// <summary>
    /// 세계지도 위를 이동하는 비행기/배 한 대. 사용자 제공 "Global Transport Network Design" 문서의
    /// Transport Unit(Current Node/Destination Node/Progress/Speed/Infection Carrier Flag)을 그대로 구현.
    /// 비행기와 배는 완전히 같은 이동/도착 로직을 공유하고(공유 클래스), 속도·색상·감염 확률만
    /// TransportManager가 타입별로 다르게 넘겨준다.
    ///
    /// 별도 프리팹/스프라이트 에셋 없이 동작하도록, 첫 사용 시 아주 작은 삼각형(비행기)/타원형(배)
    /// 텍스처를 코드로 직접 그려서 씀 — 이 프로젝트의 FloatingTextEffect.cs(내장 폰트만 사용)와 같은
    /// "에셋 임포트/씬 배선 없이 바로 동작" 철학을 따른다. 나중에 실제 아이콘으로 바꾸고 싶다면
    /// TransportManager 인스펙터에 스프라이트 필드를 추가해 덮어쓰면 된다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class TransportUnit : MonoBehaviour
    {
        private static Sprite _airSprite;
        private static Sprite _seaSprite;

        private static readonly Color AirCarrierColor = new Color(1f, 0.55f, 0.15f, 0.95f);
        private static readonly Color AirIdleColor = new Color(0.55f, 0.85f, 1f, 0.85f);
        private static readonly Color SeaCarrierColor = new Color(0.95f, 0.3f, 0.25f, 0.95f);
        private static readonly Color SeaIdleColor = new Color(0.55f, 0.8f, 0.6f, 0.85f);

        private SpriteRenderer _renderer;

        /// <summary>도착할 때마다 발행 — TransportManager가 감염 전파 판정 + 다음 구간 배정을 수행.</summary>
        public event Action<TransportUnit> OnArrived;

        public TransportHubType HubType { get; private set; }
        public string CurrentHubId { get; private set; }
        public string DestinationHubId { get; private set; }

        /// <summary>이번 구간 출발 시점에 실제로 감염을 옮길 수 있는 상태였는지 (설계 문서 "Infection Carrier Flag").</summary>
        public bool IsCarrier { get; private set; }

        private Vector3 _from;
        private Vector3 _to;
        private float _progress;
        private float _speed;
        private float _legDistance;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _renderer.sortingOrder = 60; // DNA 버블(연출) 아래, 국가 오버레이 위 정도의 어중간한 레이어 — 필요시 조정
        }

        /// <summary>
        /// 새 구간 시작. 풀에서 꺼내질 때뿐 아니라 도착 직후 다음 목적지로 이어갈 때도 호출된다.
        /// speed는 월드 유닛/초 — TransportManager가 항공/해운별 기본 속도를 넘겨준다.
        /// </summary>
        public void BeginLeg(string fromHubId, string toHubId, Vector3 fromPos, Vector3 toPos,
            TransportHubType hubType, float speed, bool isCarrier)
        {
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();

            CurrentHubId = fromHubId;
            DestinationHubId = toHubId;
            _from = fromPos;
            _to = toPos;
            _progress = 0f;
            _legDistance = Vector3.Distance(fromPos, toPos);
            HubType = hubType;
            _speed = Mathf.Max(0.05f, speed);
            IsCarrier = isCarrier;

            _renderer.sprite = hubType == TransportHubType.Air ? GetAirSprite() : GetSeaSprite();
            _renderer.color = hubType == TransportHubType.Air
                ? (isCarrier ? AirCarrierColor : AirIdleColor)
                : (isCarrier ? SeaCarrierColor : SeaIdleColor);

            transform.position = _from;
            FaceTravelDirection();
        }

        private void Update()
        {
            if (_legDistance <= 0.0001f)
            {
                transform.position = _to;
                OnArrived?.Invoke(this);
                return;
            }

            _progress += (_speed * Time.deltaTime) / _legDistance;
            if (_progress >= 1f)
            {
                _progress = 1f;
                transform.position = _to;
                OnArrived?.Invoke(this);
            }
            else
            {
                transform.position = Vector3.LerpUnclamped(_from, _to, _progress);
            }
        }

        /// <summary>이동 방향을 향해 스프라이트를 회전 — 원본 게임처럼 비행기/배가 실제로 그 방향으로 "가는" 느낌.</summary>
        private void FaceTravelDirection()
        {
            Vector3 dir = _to - _from;
            if (dir.sqrMagnitude < 0.0001f) return;
            float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
        }

        // ---- 절차적 스프라이트 생성 (에셋 임포트 불필요) ----

        private static Sprite GetAirSprite()
        {
            if (_airSprite == null)
                _airSprite = BuildTriangleSprite();
            return _airSprite;
        }

        private static Sprite GetSeaSprite()
        {
            if (_seaSprite == null)
                _seaSprite = BuildHullSprite();
            return _seaSprite;
        }

        /// <summary>+X 방향을 가리키는 작은 삼각형 (비행기) — Atan2 회전 규칙(0도=+X)과 그대로 맞아떨어진다.</summary>
        private static Sprite BuildTriangleSprite()
        {
            const int w = 28, h = 16;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float t = x / (float)(w - 1); // 0(꼬리) -> 1(기수)
                    float halfHeight = (h / 2f) * (1f - t);
                    bool inside = Mathf.Abs(y - h / 2f) <= halfHeight;
                    pixels[y * w + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>+X 방향 이물(뱃머리)을 가진 길쭉한 선체 모양 (배).</summary>
        private static Sprite BuildHullSprite()
        {
            const int w = 30, h = 12;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixels = new Color32[w * h];
            float bowStart = w * 0.65f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool inside;
                    if (x < bowStart)
                    {
                        inside = true; // 선체 몸통 — 꽉 찬 직사각형
                    }
                    else
                    {
                        float t = (x - bowStart) / (w - bowStart); // 0~1, 뱃머리로 갈수록 뾰족해짐
                        float halfHeight = (h / 2f) * (1f - t);
                        inside = Mathf.Abs(y - h / 2f) <= halfHeight;
                    }
                    pixels[y * w + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
