using UnityEngine;

namespace Contagion.Gameplay
{
    /// <summary>
    /// 세계지도 배경(Resources/WorldMap/world_base.png)을 런타임에 로드해 붙이는 아주 작은 컴포넌트.
    /// Step 29 — 18개국을 각각 다른 크기/위치로 배치하던 방식을 버리고, 세계지도 이미지 하나 위에
    /// 국가별 오버레이(CountryView, 같은 캔버스 기준으로 정렬됨)를 겹쳐 그리는 방식으로 바꾸면서 추가.
    /// CountryView.ApplyCountryShape()와 동일한 이유로 Resources.LoadAll을 쓴다 — 프로젝트 기본
    /// 텍스처 임포트가 Sprite(Multiple) 모드라 서브에셋 이름이 "world_base_0"가 되기 때문에
    /// Resources.Load&lt;Sprite&gt;만으로는 못 찾을 수 있음(Step 22에서 확인된 동작).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class WorldMapBackgroundLoader : MonoBehaviour
    {
        [SerializeField] private string resourcePath = "WorldMap/world_base";

        private void Awake()
        {
            var renderer = GetComponent<SpriteRenderer>();
            var sprites = Resources.LoadAll<Sprite>(resourcePath);
            Sprite sprite = sprites != null && sprites.Length > 0 ? sprites[0] : Resources.Load<Sprite>(resourcePath);

            if (sprite == null)
            {
                Debug.LogWarning($"[WorldMapBackgroundLoader] Resources/{resourcePath}.png를 찾지 못했습니다.");
                return;
            }

            renderer.sprite = sprite;
        }
    }
}
