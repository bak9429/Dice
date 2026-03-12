// Path: Assets/Script/Rule/Field/Camera/CameraLayerBootstrap.cs
using UnityEngine;

namespace Rule.Field.CameraTools
{
    public class CameraLayerBootstrap : MonoBehaviour
    {
        public Camera mainCamera;
        public Camera fieldCamera;

        private void Awake()
        {
            int fieldLayer = LayerMask.NameToLayer("Field");
            if (fieldLayer < 0)
            {
                Debug.LogError("[CameraLayerBootstrap] Layer 'Field' not found.");
                return;
            }

            int fieldMask = 1 << fieldLayer;

            if (mainCamera != null)
                mainCamera.cullingMask &= ~fieldMask; // Field 제거

            if (fieldCamera != null)
                fieldCamera.cullingMask = fieldMask; // Field만
        }
    }
}
