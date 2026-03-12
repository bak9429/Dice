// Path: Assets/Script/Rule/Field/HexSelectionController.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Rule.Field;

namespace Rule.Field
{
    public class HexSelectionController : MonoBehaviour
    {
        public static event Action<HexCoord, Vector3> OnHexSelected;   // coord, worldPos
        public static event Action OnHexDeselected;

        [Header("Raycast")]
        public Camera cam;
        public float raycastMaxDistance = 10f;

        private SpriteTile _selected;

        private void Awake()
        {
            ResolveCamera();
        }

        private void ResolveCamera()
        {
            if (cam != null) return;
            cam = Camera.main;
            if (cam == null)
                cam = FindFirstObjectByType<Camera>();
        }

        private void Update()
        {
            if (cam == null) { ResolveCamera(); if (cam == null) return; }
            if (Mouse.current == null) return;
            if (!Mouse.current.leftButton.wasPressedThisFrame) return;

            // ✅ "버튼/Selectable 같은 인터랙티브 UI" 위에서만 필드 입력 차단
            if (IsPointerOverInteractiveUI())
                return;

            var mp = Mouse.current.position.ReadValue();

            // 2D(z=0) 평면 기준 변환
            float z = -cam.transform.position.z;
            var wp = cam.ScreenToWorldPoint(new Vector3(mp.x, mp.y, z));

            var hit = Physics2D.Raycast(wp, Vector2.zero, raycastMaxDistance);
            if (!hit)
            {
                Deselect();
                return;
            }

            var tile = hit.collider.GetComponent<SpriteTile>();
            if (tile == null)
            {
                Deselect();
                return;
            }

            Select(tile);
        }

        /// <summary>
        /// RaycastAll 결과 중 "Selectable(Button/Toggle/etc)"이 있을 때만 UI로 판정.
        /// 배경/레이아웃(의도치 않은 full screen 히트)은 무시해서 필드 클릭이 살아나게 한다.
        /// </summary>
        private static bool IsPointerOverInteractiveUI()
        {
var es = EventSystem.current;
if (es == null) return false;

Vector2 pos = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;

var ped = new PointerEventData(es) { position = pos };
var results = new List<RaycastResult>();
es.RaycastAll(ped, results);
if (results == null || results.Count == 0) return false;

// ✅ Only block field clicks when the TOPMOST UI element under the cursor is interactive (Selectable).
// This prevents full-screen panels/containers (non-interactive backgrounds) from blocking field selection
// while still allowing actual buttons to work.
var top = results[0].gameObject;
if (top == null) return false;

var sel = top.GetComponentInParent<Selectable>(true);
return sel != null;
        }

        private void Select(SpriteTile tile)
        {

            if (_selected != null)
                _selected.SetSelected(false);

            _selected = tile;
            _selected.SetSelected(true);

            Debug.Log($"[HexSelect] SELECT coord={tile.coord.q},{tile.coord.r}");
            OnHexSelected?.Invoke(tile.coord, tile.transform.position);
        }

        private void Deselect()
        {
            if (_selected == null) return;

            _selected.SetSelected(false);
            _selected = null;

            Debug.Log("[HexSelect] DESELECT");
            OnHexDeselected?.Invoke();
        }
        public void ForceDeselect()
        {
            // Deselect() 내용과 동일하게
            if (_selected != null)
            {
                _selected.SetSelected(false);
                _selected = null;
                OnHexDeselected?.Invoke();
                Debug.Log("[HexSelect] DESELECT (forced)");
            }
        }
    }
}