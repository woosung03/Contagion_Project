using UnityEngine;

namespace Contagion.Gameplay
{
    /// <summary>
    /// 세계 지도를 화면보다 넓게 만들고(실제 경도/위도 비율에 가깝게 가로로 긴 배치 — Step 28)
    /// 좌우 드래그로 카메라를 스크롤할 수 있게 하는 컨트롤러.
    ///
    /// 이전(Step 24~27)엔 "전체 지도가 스크롤 없이 한눈에 들어오게" 방식이었는데, 세로로 긴 모바일
    /// 화면(9:19.5)에 가로로 넓은 실제 지구 비율(경도 244도 : 위도 91도 ≈ 2.7:1)을 욱여넣다 보니
    /// 국가 간 동서 거리가 원근감 없이 뭉개져서 "세계지도 같지 않다"는 피드백이 반복됐다. 그래서
    /// 지도 자체를 화면보다 훨씬 넓게 배치하고(국가 좌표는 DevLog Step 28 참고) 좌우로만 스크롤하는
    /// 방식으로 바꿨다 — 세로는 여전히 스크롤 없이 한 화면에 다 들어온다(카메라 orthographic size는
    /// 그대로 3.8 유지).
    ///
    /// 입력은 프로젝트 전역에서 이미 쓰고 있는 레거시 Input Manager(Input.mousePosition 등,
    /// Input System 패키지 아님) 기준 — 모바일 터치도 Unity가 마우스 이벤트로 에뮬레이션해주므로
    /// 별도 터치 전용 코드 없이 동작한다(OnMouseDown 계열과 동일한 전제).
    ///
    /// 국가 클릭(CountryView)과 드래그가 충돌하지 않도록, 이 컨트롤러가 마우스/터치가 눌린 순간부터
    /// 뗄 때까지 누적 이동 거리를 추적해 <see cref="WasDragging"/> 플래그를 세워둔다. CountryView는
    /// (OnMouseDown이 아니라) OnMouseUpAsButton 시점에 이 플래그를 확인해서, 드래그였으면 클릭을
    /// 무시한다 — 지도를 스와이프하다가 우연히 국가 위를 스쳐도 팝업이 안 열리게 하기 위함.
    /// </summary>
    public class WorldMapCameraController : MonoBehaviour
    {
        public static WorldMapCameraController Instance { get; private set; }

        [SerializeField] private Camera targetCamera;

        [SerializeField, Tooltip("지도 전체 콘텐츠의 가로 절반 폭(월드 유닛) — 카메라가 이 범위를 " +
            "벗어나 스크롤하지 못하게 클램프. Step 29에서 세계지도 배경 이미지 한 장 + 국가별 오버레이 " +
            "스프라이트 방식으로 바뀌면서 지도의 실제 크기가 명확해졌다(4000x1714px, Pixels Per Unit " +
            "500 → 월드 폭 8.0유닛) — 절반인 4.0으로 설정.")]
        private float mapHalfWidth = 4.0f;

        [SerializeField, Tooltip("이 픽셀 이상 움직이면 드래그로 판정(국가 클릭 무시)")]
        private float dragThresholdPixels = 20f;

        /// <summary>현재(또는 방금 끝난) 누름 사이클에서 드래그 임계값을 넘었는지 — CountryView가
        /// 클릭 처리 전에 확인한다. 다음 누름이 시작될 때만 초기화하므로, 뗀 직후 프레임에 값을 읽어도
        /// 항상 이번 사이클의 최종 판정을 볼 수 있다(초기화 시점을 늦춰 프레임 순서 문제를 회피).</summary>
        public bool WasDragging { get; private set; }

        private bool _isPressed;
        private Vector3 _pressScreenPos;
        private Vector3 _lastScreenPos;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            if (targetCamera == null)
                targetCamera = Camera.main;
        }

        private void Update()
        {
            if (targetCamera == null) return;

            if (Input.GetMouseButtonDown(0))
            {
                _isPressed = true;
                WasDragging = false;
                _pressScreenPos = Input.mousePosition;
                _lastScreenPos = Input.mousePosition;
            }
            else if (_isPressed && Input.GetMouseButton(0))
            {
                Vector3 currentScreenPos = Input.mousePosition;

                if (!WasDragging)
                {
                    float movedPixels = Vector3.Distance(currentScreenPos, _pressScreenPos);
                    if (movedPixels > dragThresholdPixels)
                        WasDragging = true;
                }

                if (WasDragging)
                    PanCamera(currentScreenPos.x - _lastScreenPos.x);

                _lastScreenPos = currentScreenPos;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                _isPressed = false;
                // WasDragging은 여기서 리셋하지 않는다 — CountryView.OnMouseUpAsButton()이 같은 프레임에
                // 이번 사이클 판정을 읽어야 하기 때문. 다음 GetMouseButtonDown 때 리셋한다.
            }
        }

        /// <summary>화면 픽셀 x 이동량만큼 카메라를 반대로 옮겨 "손가락을 따라 지도가 움직이는" 느낌을 준다.</summary>
        private void PanCamera(float deltaScreenPixelsX)
        {
            float worldPerPixel = (targetCamera.orthographicSize * 2f) / Screen.height;
            float deltaWorldX = deltaScreenPixelsX * worldPerPixel;

            Vector3 pos = targetCamera.transform.position;
            pos.x -= deltaWorldX;
            pos.x = Mathf.Clamp(pos.x, MinCameraX(), MaxCameraX());
            targetCamera.transform.position = pos;
        }

        private float HalfViewWidth() =>
            targetCamera.orthographicSize * ((float)Screen.width / Screen.height);

        private float MinCameraX()
        {
            float min = -mapHalfWidth + HalfViewWidth();
            float max = mapHalfWidth - HalfViewWidth();
            return min <= max ? min : 0f; // 지도가 화면보다 좁으면(비정상 설정) 스크롤 없이 중앙 고정
        }

        private float MaxCameraX()
        {
            float min = -mapHalfWidth + HalfViewWidth();
            float max = mapHalfWidth - HalfViewWidth();
            return min <= max ? max : 0f;
        }
    }
}
