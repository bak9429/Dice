using System.Collections.Generic;
using UnityEngine;
using Rule.Combat.Boss;

namespace Rule.Field
{
    public class TelegraphOverlayApplier
    {
        private readonly HexGridBuilder _grid;

        public TelegraphOverlayApplier(HexGridBuilder grid)
        {
            _grid = grid;
        }

        // filterIndex:
        //  -1 : 전체
        //   0..n-1 : 해당 인덱스만
        public void Apply(List<TelegraphIntent> intents, int filterIndex)
        {
            if (_grid == null) return;

            // ✅ Telegraph 채널만 먼저 끔
            foreach (var kv in _grid.Tiles)
            {
                var tile = kv.Value;
                if (tile != null) tile.SetOverlay(HexOverlayChannel.Telegraph, false);
            }

            if (intents == null || intents.Count == 0) return;

            if (filterIndex >= 0 && filterIndex < intents.Count)
            {
                ApplyOne(intents[filterIndex]);
                return;
            }

            for (int i = 0; i < intents.Count; i++)
                ApplyOne(intents[i]);
        }

        public void ClearExecuteAll()
        {
            if (_grid == null) return;
            foreach (var kv in _grid.Tiles)
            {
                var tile = kv.Value;
                if (tile != null) tile.SetOverlay(HexOverlayChannel.Execute, false);
            }
        }

        /// <summary>
        /// ✅ BossController(groggy patch) compatibility
        /// Clears Telegraph/Danger/Execute overlays.
        /// </summary>
        public void ClearAll()
        {
            ClearTelegraphAll();
            ClearDangerAll();
            ClearExecuteAll();
        }

        public void ClearTelegraphAll()
        {
            if (_grid == null) return;
            foreach (var kv in _grid.Tiles)
            {
                var tile = kv.Value;
                if (tile != null) tile.SetOverlay(HexOverlayChannel.Telegraph, false);
            }
        }

        public void ClearDangerAll()
        {
            if (_grid == null) return;
            foreach (var kv in _grid.Tiles)
            {
                var tile = kv.Value;
                if (tile != null) tile.SetOverlay(HexOverlayChannel.Danger, false);
            }
        }

        public void ApplyExecute(HashSet<HexCoord> tiles)
        {
            if (_grid == null || tiles == null) return;

            // ✅ Execute는 “현재 실행 패턴”만 보여야 해서, 먼저 전부 끄고 켠다
            ClearExecuteAll();

            foreach (var c in tiles)
            {
                if (_grid.Tiles.TryGetValue(c, out var tile) && tile != null)
                    tile.SetOverlay(HexOverlayChannel.Execute, true);
            }
        }

        private void ApplyOne(TelegraphIntent intent)
        {
            if (intent == null || !intent.visible) return;
            foreach (var c in intent.tiles)
            {
                if (_grid.Tiles.TryGetValue(c, out var tile) && tile != null)
                    tile.SetOverlay(HexOverlayChannel.Telegraph, true);
            }
        }
    }
}
