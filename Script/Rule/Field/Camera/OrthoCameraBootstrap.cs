// Path: Assets/Script/Rule/Field/Camera/OrthoCameraBootstrap.cs
using UnityEngine;

namespace Rule.Field.CameraTools
{
    // 필드(헥사)만 보는 카메라 세팅
    public class OrthoCameraBootstrap : MonoBehaviour
    {
        public Camera cam;

        [Header("2D Ortho")]
        public bool autoFitToGrid = true;
        public float extraPadding = 2f;

        [Header("World")]
        public Vector3 position = new Vector3(0f, 0f, -10f);

        private void Awake()
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;

            if (cam == null)
            {
                Debug.LogError("[OrthoCameraBootstrap] Camera not found.");
                return;
            }

            cam.orthographic = true;
            cam.transform.rotation = Quaternion.identity;
            cam.transform.position = position;

            if (autoFitToGrid)
            {
                var builder = FindFirstObjectByType<Rule.Field.HexGridBuilder>();
                if (builder != null && builder.config != null)
                {
                    float approx = builder.config.radius * builder.config.hexSize * 2.0f;
                    cam.orthographicSize = approx + extraPadding;
                }
            }
        }
    }
}
