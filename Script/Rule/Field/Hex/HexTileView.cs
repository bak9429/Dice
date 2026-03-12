// Path: Assets/Script/Rule/Field/HexTileView.cs
using UnityEngine;

namespace Rule.Field
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class HexTileView : MonoBehaviour
    {
        public HexCoord coord;
        public float size = 1f;

        private MeshRenderer _mr;
        private MeshCollider _mc;

        private static Mesh _sharedHexMesh;

        private void Awake()
        {
            _mr = GetComponent<MeshRenderer>();
            _mc = GetComponent<MeshCollider>();

            EnsureMesh();
        }

        public void Init(HexCoord c, float hexSize)
        {
            coord = c;
            size = hexSize;
            name = $"Hex_{coord.q}_{coord.r}";
        }

        public void SetSelected(bool selected)
        {
            // MVP: 색만 바꿔서 선택 표시
            if (_mr == null) _mr = GetComponent<MeshRenderer>();
            var baseColor = new Color(1f, 1f, 1f, 0.10f);
            var selColor  = new Color(1f, 1f, 1f, 0.22f);
            _mr.sharedMaterial.color = selected ? selColor : baseColor;
        }

        private void EnsureMesh()
        {
            if (_sharedHexMesh == null)
                _sharedHexMesh = BuildHexMesh();

            var mf = GetComponent<MeshFilter>();
            mf.sharedMesh = _sharedHexMesh;

            _mc.sharedMesh = _sharedHexMesh;
        }

        private static Mesh BuildHexMesh()
        {
            // pointy-top hex on XZ plane, centered at origin, radius=1
            var m = new Mesh();
            m.name = "HexMesh_Shared";

            Vector3[] v = new Vector3[7];
            v[0] = Vector3.zero;

            for (int i = 0; i < 6; i++)
            {
                float ang = Mathf.Deg2Rad * (60f * i - 30f); // pointy-top
                v[i + 1] = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
            }

            int[] tris = new int[18];
            int t = 0;
            for (int i = 0; i < 6; i++)
            {
                int a = 0;
                int b = i + 1;
                int c = (i == 5) ? 1 : (i + 2);
                tris[t++] = a; tris[t++] = b; tris[t++] = c;
            }

            m.vertices = v;
            m.triangles = tris;
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }
    }
}
