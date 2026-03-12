using UnityEngine;

namespace Rule.Field.CameraTools
{
    // 목적: Field 레이어는 FieldCamera만 렌더, MainCamera는 제외
    public class FieldCameraCullingFix : MonoBehaviour
    {
        public string fieldLayerName = Rule.Field.HexGridBuilder.FieldLayers.Field;
        public Camera mainCamera;
        public Camera fieldCamera;

        private void Start()
        {
            int fieldLayer = LayerMask.NameToLayer(fieldLayerName);
            if (fieldLayer < 0)
            {
                Debug.LogWarning($"[FieldCameraCullingFix] Layer '{fieldLayerName}' not found.");
                return;
            }

            if (mainCamera == null) mainCamera = Camera.main;

            // FieldCamera는 보통 오브젝트 이름/태그로 찾아도 되는데, 우선 가장 단순하게:
            if (fieldCamera == null)
            {
                // 메인 카메라가 아닌 다른 카메라 하나 찾기(프로토용)
                var cams = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var c in cams)
                {
                    if (c != null && c != mainCamera)
                    {
                        fieldCamera = c;
                        break;
                    }
                }
            }

            int fieldBit = 1 << fieldLayer;

            if (mainCamera != null)
            {
                mainCamera.cullingMask &= ~fieldBit;
                Debug.Log("[FieldCameraCullingFix] MainCamera removed Field layer from cullingMask.");
            }

            if (fieldCamera != null)
            {
                fieldCamera.cullingMask |= fieldBit;
                Debug.Log("[FieldCameraCullingFix] FieldCamera added Field layer to cullingMask.");
            }
            else
            {
                Debug.LogWarning("[FieldCameraCullingFix] FieldCamera not found.");
            }
        }
    }
}
