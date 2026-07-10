using UnityEditor;
using UnityEngine;

namespace Contagion.EditorTools
{
    /// <summary>
    /// [Step 73] PolygonCollider2D 전환 배치 툴 — Resources/CountryShapes/{id}.png 48개 전부에 대해
    /// "Generate Physics Shape" 임포트 옵션(TextureImporterSettings.spriteGenerateFallbackPhysicsShape —
    /// TextureImporter의 직접 프로퍼티가 아니라 ReadTextureSettings()/SetTextureSettings()로 읽고
    /// 쓰는 별도 구조체 소속이다)을 켜고 재임포트한다.
    ///
    /// 이 옵션이 켜지면 Unity가 임포트 시점(에디터, 원본 PNG 소스에서 직접)에 알파 채널을 분석해 국가
    /// 실루엣에 맞는 polygon physics shape를 스프라이트 에셋에 구워 넣는다 — CountryShapes 텍스처는
    /// isReadable=0(런타임 CPU 픽셀 접근 차단)이지만, 이 임포트 시점 처리는 그 설정과 무관하다(조사
    /// 단계에서 확인 — 같은 텍스처들이 spriteMeshType=Tight로 이미 알파 기반 렌더 메시를 생성하고
    /// 있다는 사실이 근거). 구워진 physics shape 데이터는 CountryView.ApplyColliderShapeFromSprite()가
    /// Sprite.GetPhysicsShapeCount()/GetPhysicsShape()로 읽어 PolygonCollider2D 경로에 그대로 쓴다.
    ///
    /// 48개 텍스처를 인스펙터에서 하나씩 켜는 대신 이 메뉴 항목 한 번으로 일괄 처리한다. 이미 켜져
    /// 있는 텍스처는 건드리지 않으므로(idempotent) 여러 번 실행해도 안전하다.
    ///
    /// 실행 방법(Unity 에디터 필요 — 코드 도구로는 실행 불가, Docs/unity-editor-task.md 참고):
    /// 상단 메뉴 Contagion → Country Shapes → Generate Physics Shapes (48개국) 클릭 후, 콘솔 로그로
    /// 처리 결과(신규 활성화/이미 켜짐/실패 개수)를 확인한다. 이어서 Validate Physics Shapes로
    /// 실제로 데이터가 구워졌는지 재검증한다.
    /// </summary>
    public static class CountryShapePhysicsShapeGenerator
    {
        private const string CountryShapesFolder = "Assets/Resources/CountryShapes";
        private const int ExpectedCountryCount = 48;

        [MenuItem("Contagion/Country Shapes/Generate Physics Shapes (48개국)")]
        public static void GeneratePhysicsShapes()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { CountryShapesFolder });
            int changed = 0, alreadyOn = 0, failed = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    Debug.LogWarning($"[CountryShapePhysicsShapeGenerator] {path} — TextureImporter를 가져오지 못해 스킵.");
                    failed++;
                    continue;
                }

                // [Step 73 수정] spriteGenerateFallbackPhysicsShape는 TextureImporter의 직접 프로퍼티가
                // 아니라 TextureImporterSettings(ReadTextureSettings/SetTextureSettings로 읽고 쓰는
                // 별도 구조체) 소속이다 — 처음엔 TextureImporter에 직접 있는 걸로 착각해 CS1061
                // 컴파일 에러가 났다(Unity 스크립팅 API 실제 확인 없이 메타파일 필드명만 보고 작성한
                // 실수). Sprite Editor의 "Generate Physics Shape" 체크박스가 내부적으로 이 경로를
                // 쓰는 것과 동일하게 맞췄다.
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);

                if (settings.spriteGenerateFallbackPhysicsShape)
                {
                    alreadyOn++;
                    continue;
                }

                settings.spriteGenerateFallbackPhysicsShape = true;
                importer.SetTextureSettings(settings);
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                changed++;
            }

            AssetDatabase.Refresh();

            Debug.Log($"[CountryShapePhysicsShapeGenerator] 완료 — 총 {guids.Length}개 중 " +
                $"{changed}개 신규 활성화, {alreadyOn}개는 이미 켜져 있었음, {failed}개 실패.");

            if (guids.Length != ExpectedCountryCount)
            {
                Debug.LogWarning($"[CountryShapePhysicsShapeGenerator] 발견된 텍스처 수({guids.Length})가 " +
                    $"예상 국가 수({ExpectedCountryCount})와 다릅니다 — CountryShapes 폴더 구성을 확인하세요.");
            }
        }

        /// <summary>생성된 physics shape 데이터가 실제로 스프라이트에 구워졌는지 검증한다 —
        /// GeneratePhysicsShapes() 실행 후 QA 체크리스트("PolygonCollider2D 전환") 1번 항목 확인용.</summary>
        [MenuItem("Contagion/Country Shapes/Validate Physics Shapes (48개국)")]
        public static void ValidatePhysicsShapes()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { CountryShapesFolder });
            int withShape = 0, withoutShape = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                Sprite sprite = null;
                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is Sprite s)
                    {
                        sprite = s;
                        break;
                    }
                }

                if (sprite == null)
                {
                    Debug.LogWarning($"[CountryShapePhysicsShapeGenerator] {path} — Sprite 서브에셋을 찾지 못해 검증 스킵.");
                    continue;
                }

                if (sprite.GetPhysicsShapeCount() > 0)
                {
                    withShape++;
                }
                else
                {
                    withoutShape++;
                    Debug.LogWarning($"[CountryShapePhysicsShapeGenerator] {path} — physics shape가 비어 " +
                        "있습니다(GetPhysicsShapeCount()==0). Generate Physics Shapes를 먼저 실행했는지 확인하세요.");
                }
            }

            Debug.Log($"[CountryShapePhysicsShapeGenerator] 검증 완료 — physics shape 있음 {withShape}개 / " +
                $"없음 {withoutShape}개 (총 {withShape + withoutShape}개, 예상 {ExpectedCountryCount}개).");
        }
    }
}
