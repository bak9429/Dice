// Path: Assets/Script/Rule/Field/Hex/HexTileHighlightController.cs
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Rule.Field
{
    public class HexTileHighlightController : MonoBehaviour
    {
        public HexGridBuilder grid;

        private readonly HashSet<HexCoord> _moveSet = new HashSet<HexCoord>();
        private readonly HashSet<HexCoord> _attackRangeSet = new HashSet<HexCoord>();
        private readonly HashSet<HexCoord> _attackPreviewSet = new HashSet<HexCoord>();

        // ✅ Gun range outline (blue)
        private readonly HashSet<HexCoord> _gunRangeOutlineSet = new HashSet<HexCoord>();

        // ✅ Candidate fill (red fill without outline)
        private readonly HashSet<HexCoord> _candidateFillSet = new HashSet<HexCoord>();

        [Header("Attack Range Visual")]
        public Color attackRangeOutlineColor = new Color(1.0f, 0.12f, 0.12f, 0.55f);
        public float attackRangeOutlineWidth = 0.06f;

        [Header("Gun Range Visual (Outline Only)")]
        public Color gunRangeOutlineColor = new Color(0.2f, 0.55f, 1.0f, 0.70f);
        public float gunRangeOutlineWidth = 0.05f;

        private void Awake()
        {
            ResolveGridIfNeeded();
        }
        private Camera FindRenderCameraForLayer(int layer)
        {
            var cams = Camera.allCameras;

            for (int i = 0; i < cams.Length; i++)
            {
                var cam = cams[i];
                if (cam == null) continue;

                int mask = cam.cullingMask;

                if ((mask & (1 << layer)) != 0)
                {
                    Debug.Log($"[OverlayCam] Using camera {cam.name} for layer {layer}");
                    return cam;
                }
            }

            Debug.LogWarning("[OverlayCam] No camera found for layer, fallback to Camera.main");
            return Camera.main;
        }
        
        private void ResolveGridIfNeeded()
        {
            if (grid != null) return;

            grid = FindFirstObjectByType<HexGridBuilder>(FindObjectsInactive.Exclude);
            if (grid == null)
                grid = FindFirstObjectByType<HexGridBuilder>(FindObjectsInactive.Include);

            if (grid != null)
                Debug.Log($"[HexTileHighlightController] Grid resolved: {grid.name} (activeInHierarchy={grid.gameObject.activeInHierarchy})");
            else
                Debug.LogError("[HexTileHighlightController] HexGridBuilder not found.");
        }

        private bool EnsureGridReady()
        {
            ResolveGridIfNeeded();
            if (grid == null) return false;
            grid.BuildIfNeeded();
            return grid.Tiles != null;
        }

        public void ClearMove()
        {
            if (!EnsureGridReady() || grid.Tiles == null) return;

            foreach (var c in _moveSet)
            {
                if (grid.Tiles.TryGetValue(c, out var tile) && tile != null)
                    tile.SetOverlay(HexOverlayChannel.Move, false);
            }
            _moveSet.Clear();
        }

        public void Clear()
        {
            ClearMove();
        }

        public void ClearAttack()
        {
            if (!EnsureGridReady() || grid.Tiles == null) return;

            // AttackRange: fill + outline 둘 다 OFF
            foreach (var c in _attackRangeSet)
            {
                if (grid.Tiles.TryGetValue(c, out var tile) && tile != null)
                {
                    tile.SetOverlay(HexOverlayChannel.AttackRange, false);
                    // ✅ Attack 전용 outline 끄기
                    tile.SetAttackRangeOutline(false, Color.clear, attackRangeOutlineWidth);
                }
            }
            _attackRangeSet.Clear();

            // AttackPreview OFF
            foreach (var c in _attackPreviewSet)
            {
                if (grid.Tiles.TryGetValue(c, out var tile) && tile != null)
                    tile.SetOverlay(HexOverlayChannel.AttackPreview, false);
            }
            _attackPreviewSet.Clear();
        }

        // ✅ 파란 테두리: Gun range only
        public void ClearGunRangeOutline()
        {
            if (!EnsureGridReady() || grid.Tiles == null) return;

            foreach (var c in _gunRangeOutlineSet)
            {
                if (grid.Tiles.TryGetValue(c, out var tile) && tile != null)
                    tile.SetGunRangeOutline(false, Color.clear, gunRangeOutlineWidth);
            }
            _gunRangeOutlineSet.Clear();
        }

        public void HighlightGunRangeOutline(HashSet<HexCoord> coords)
        {
            if (!EnsureGridReady() || grid.Tiles == null) return;

            ClearGunRangeOutline();
            if (coords == null) return;

            foreach (var c in coords)
            {
                if (grid.Tiles.TryGetValue(c, out var tile) && tile != null)
                    tile.SetGunRangeOutline(true, gunRangeOutlineColor, gunRangeOutlineWidth);

                _gunRangeOutlineSet.Add(c);
            }

            Debug.Log($"[HexTileHighlightController] GunRange OUTLINE ON: {coords.Count}");
        }

        // ✅ 후보 타일: 빨강 채움만 (outline은 건드리지 않음)
        public void ClearCandidateFill()
        {
            if (!EnsureGridReady() || grid.Tiles == null) return;

            foreach (var c in _candidateFillSet)
            {
                if (grid.Tiles.TryGetValue(c, out var tile) && tile != null)
                    tile.SetOverlay(HexOverlayChannel.AttackRange, false);
            }
            _candidateFillSet.Clear();
        }

        public void HighlightCandidateFill(HashSet<HexCoord> coords)
        {
            if (!EnsureGridReady() || grid.Tiles == null) return;

            ClearCandidateFill();
            if (coords == null) return;

            foreach (var c in coords)
            {
                if (grid.Tiles.TryGetValue(c, out var tile) && tile != null)
                    tile.SetOverlay(HexOverlayChannel.AttackRange, true);

                _candidateFillSet.Add(c);
            }

            Debug.Log($"[HexTileHighlightController] Candidate FILL ON: {coords.Count}");
        }

        public void HighlightMove(HashSet<HexCoord> reachable)
        {
            ClearMove();
            if (!EnsureGridReady() || grid.Tiles == null) return;

            foreach (var c in reachable)
            {
                if (grid.Tiles.TryGetValue(c, out var tile) && tile != null)
                    tile.SetOverlay(HexOverlayChannel.Move, true);

                _moveSet.Add(c);
            }
        }

        /// <summary>
        /// Attack range 표시:
        /// - 외곽선: candidates 전체
        /// - 채우기(fill): fill=true일 때만, fillOnly에 들어있는 칸만
        /// </summary>
        public void HighlightAttackRange(HashSet<HexCoord> candidates, bool fill, HashSet<HexCoord> fillOnly = null)
        {
            if (!EnsureGridReady() || grid.Tiles == null) return;

            // 이전 범위 OFF (fill/outline 둘 다)
            foreach (var c in _attackRangeSet)
            {
                if (grid.Tiles.TryGetValue(c, out var tile) && tile != null)
                {
                    tile.SetOverlay(HexOverlayChannel.AttackRange, false);
                    tile.SetAttackRangeOutline(false, Color.clear, attackRangeOutlineWidth);
                }
            }
            _attackRangeSet.Clear();

            // 새 범위 ON
            foreach (var c in candidates)
            {
                if (grid.Tiles.TryGetValue(c, out var tile) && tile != null)
                {
                    // ✅ 항상 공격 외곽선 ON (Gun보다 sortingOrder 높음)
                    tile.SetAttackRangeOutline(true, attackRangeOutlineColor, attackRangeOutlineWidth);

                    // 채우기는 조건 만족하는 타일만 ON
                    bool doFill = fill && (fillOnly == null || fillOnly.Contains(c));
                    tile.SetOverlay(HexOverlayChannel.AttackRange, doFill);
                }

                _attackRangeSet.Add(c);
            }

            Debug.Log($"[HexTileHighlightController] AttackRange ON: {candidates?.Count ?? 0} fill={fill} fillOnly={(fillOnly != null ? fillOnly.Count : -1)}");
        }

        public void HighlightAttackPreview(IEnumerable<HexCoord> affected)
        {
            if (!EnsureGridReady() || grid.Tiles == null) return;

            foreach (var c in _attackPreviewSet)
            {
                if (grid.Tiles.TryGetValue(c, out var tile) && tile != null)
                    tile.SetOverlay(HexOverlayChannel.AttackPreview, false);
            }
            _attackPreviewSet.Clear();

            foreach (var c in affected)
            {
                if (grid.Tiles.TryGetValue(c, out var tile) && tile != null)
                    tile.SetOverlay(HexOverlayChannel.AttackPreview, true);

                _attackPreviewSet.Add(c);
            }

            Debug.Log($"[HexTileHighlightController] AttackPreview SET: {_attackPreviewSet.Count}");
        }
    }
}