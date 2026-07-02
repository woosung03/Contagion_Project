using UnityEngine;

namespace Contagion.Gameplay
{
    /// <summary>
    /// 짧은 순간 피드백용 플로팅 텍스트 (예: DNA 버블 수집 시 "+N"). 프리팹/폰트 에셋 없이
    /// 런타임에 TextMesh를 동적으로 생성해서 위로 떠오르며 페이드아웃 후 자동 파괴된다.
    /// 기본 플레이 퀄리티 개선 항목 — Unity 내장 폰트(LegacyRuntime.ttf/Arial.ttf)만 사용하므로
    /// 별도 에셋 임포트나 씬 배선이 필요 없다.
    /// </summary>
    public class FloatingTextEffect : MonoBehaviour
    {
        [SerializeField] private float riseSpeed = 1.2f;
        [SerializeField] private float lifetime = 0.8f;

        private TextMesh _textMesh;
        private float _elapsed;
        private Color _startColor;

        /// <summary>월드 좌표 position에 text를 표시하며 위로 떠오르는 이펙트를 즉시 생성한다.</summary>
        public static FloatingTextEffect Spawn(Vector3 position, string text, Color color, float characterSize = 0.3f)
        {
            var go = new GameObject($"FloatingText_{text}");
            go.transform.position = position;

            var textMesh = go.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.characterSize = characterSize;
            textMesh.fontSize = 48;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = color;

            // Unity 내장 폰트를 사용 — 별도 폰트 에셋을 임포트하지 않아도 항상 존재한다.
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font != null)
            {
                textMesh.font = font;
                var meshRenderer = go.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    meshRenderer.material = font.material;
                    meshRenderer.sortingOrder = 100;
                }
            }

            var effect = go.AddComponent<FloatingTextEffect>();
            effect._textMesh = textMesh;
            effect._startColor = color;
            return effect;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            transform.position += Vector3.up * riseSpeed * Time.deltaTime;

            if (_textMesh != null)
            {
                float alpha = Mathf.Clamp01(1f - _elapsed / lifetime);
                _textMesh.color = new Color(_startColor.r, _startColor.g, _startColor.b, alpha);
            }

            if (_elapsed >= lifetime)
                Destroy(gameObject);
        }
    }
}
