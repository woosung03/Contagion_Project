using System;
using System.Collections;
using UnityEngine;

namespace Contagion.Gameplay
{
    /// <summary>
    /// DNA 수집 버블 하나의 동작. 설계 문서 4.4절 "감염자/사망자 발생 시 팝업 탭 -> 수집".
    /// BubbleSpawner의 오브젝트 풀에 의해 재사용된다 (직접 Instantiate/Destroy 하지 않음).
    /// </summary>
    // OnMouseDown()이 동작하려면 Collider2D가 필요하다.
    [RequireComponent(typeof(CircleCollider2D))]
    public class DnaBubble : MonoBehaviour
    {
        [SerializeField, Tooltip("스폰 시 작은 크기에서 원래 크기로 커지는 팝인 애니메이션 시간. " +
            "기본 플레이 퀄리티 개선 항목 — 버블이 뿅 나타나는 손맛을 위함.")]
        private float popInDuration = 0.25f;
        [SerializeField] private Color collectTextColor = new Color(1f, 0.85f, 0.2f);
        [SerializeField, Tooltip("[Step 47] 레이어 순서 정리 과정에서 발견 — 이 컴포넌트는 프리팹 " +
            "(BubbleSpawner.bubblePrefab)이 아직 씬/에셋에 배선되지 않아(CLAUDE.md '씬/에셋 배선 필요' " +
            "참고) 실제로 스폰되지 않는 상태지만, 나중에 프리팹이 만들어질 때 SpriteRenderer의 " +
            "sortingOrder를 깜빡하고 기본값(0)으로 두면 지도(0)와 같은 레이어에 걸려 다른 요소에 가려질 " +
            "수 있어 미리 방어적으로 추가해둠. 탭해서 수집하는 상호작용 요소라 교통 유닛(40)보다도 위에 " +
            "두어 항상 보이고 클릭 가능하게 잡았다(FloatingTextEffect의 100보다는 아래).")]
        private int sortingOrder = 45;

        public int DnaValue { get; private set; }

        /// <summary>플레이어가 탭해서 수집했을 때 발행.</summary>
        public event Action<DnaBubble> OnCollected;
        /// <summary>수명이 다해 수집되지 않고 사라질 때 발행.</summary>
        public event Action<DnaBubble> OnExpired;

        /// <summary>
        /// 인스턴스 이벤트와 별개로, 어떤 버블이든 수집되면 발행되는 정적 이벤트.
        /// BubbleSpawner 인스턴스에 접근할 필요 없이 AudioManager 등이 바로 구독할 수 있게 하기 위함.
        /// </summary>
        public static event Action<int /*dnaValue*/> OnAnyCollected;

        private Coroutine _lifeRoutine;
        private Coroutine _popRoutine;
        private Vector3 _baseScale;

        private void Awake()
        {
            _baseScale = transform.localScale;

            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null) spriteRenderer.sortingOrder = sortingOrder; // [Step 47]
        }

        /// <summary>풀에서 꺼내져 활성화될 때 호출.</summary>
        public void Activate(int dnaValue, float lifetimeSeconds)
        {
            DnaValue = dnaValue;
            if (_lifeRoutine != null) StopCoroutine(_lifeRoutine);
            _lifeRoutine = StartCoroutine(LifeTimer(lifetimeSeconds));

            if (_popRoutine != null) StopCoroutine(_popRoutine);
            transform.localScale = _baseScale * 0.2f;
            _popRoutine = StartCoroutine(PopIn());

            Debug.Log($"[DnaBubble] 스폰됨 at {transform.position}, dnaValue={dnaValue}, baseScale={_baseScale}, popInDuration={popInDuration}");
        }

        private IEnumerator PopIn()
        {
            float t = 0f;
            Vector3 from = transform.localScale;
            while (t < popInDuration)
            {
                t += Time.deltaTime;
                float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / popInDuration), 3f); // ease-out cubic
                transform.localScale = Vector3.LerpUnclamped(from, _baseScale, eased);
                yield return null;
            }
            transform.localScale = _baseScale;
            _popRoutine = null;
        }

        private IEnumerator LifeTimer(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            _lifeRoutine = null;
            Debug.Log($"[DnaBubble] 수명 만료로 소멸 at {transform.position} (수집 안 됨)");
            OnExpired?.Invoke(this);
        }

        private void OnMouseDown()
        {
            if (_lifeRoutine != null)
            {
                StopCoroutine(_lifeRoutine);
                _lifeRoutine = null;
            }
            if (_popRoutine != null)
            {
                StopCoroutine(_popRoutine);
                _popRoutine = null;
            }

            Debug.Log($"[DnaBubble] 수집됨 at {transform.position}, dnaValue={DnaValue}");
            FloatingTextEffect.Spawn(transform.position, $"+{DnaValue}", collectTextColor);

            OnCollected?.Invoke(this);
            OnAnyCollected?.Invoke(DnaValue);
        }

        private void OnDisable()
        {
            if (_lifeRoutine != null)
            {
                StopCoroutine(_lifeRoutine);
                _lifeRoutine = null;
            }
            if (_popRoutine != null)
            {
                StopCoroutine(_popRoutine);
                _popRoutine = null;
            }
        }
    }
}
