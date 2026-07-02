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
        public int DnaValue { get; private set; }

        /// <summary>플레이어가 탭해서 수집했을 때 발행.</summary>
        public event Action<DnaBubble> OnCollected;
        /// <summary>수명이 다해 수집되지 않고 사라질 때 발행.</summary>
        public event Action<DnaBubble> OnExpired;

        private Coroutine _lifeRoutine;

        /// <summary>풀에서 꺼내져 활성화될 때 호출.</summary>
        public void Activate(int dnaValue, float lifetimeSeconds)
        {
            DnaValue = dnaValue;
            if (_lifeRoutine != null) StopCoroutine(_lifeRoutine);
            _lifeRoutine = StartCoroutine(LifeTimer(lifetimeSeconds));
        }

        private IEnumerator LifeTimer(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            _lifeRoutine = null;
            OnExpired?.Invoke(this);
        }

        private void OnMouseDown()
        {
            if (_lifeRoutine != null)
            {
                StopCoroutine(_lifeRoutine);
                _lifeRoutine = null;
            }
            OnCollected?.Invoke(this);
        }

        private void OnDisable()
        {
            if (_lifeRoutine != null)
            {
                StopCoroutine(_lifeRoutine);
                _lifeRoutine = null;
            }
        }
    }
}
