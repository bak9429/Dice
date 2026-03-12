// Path: Assets/Script/UI/UIControl/CombatUIController.Panel.cs
using System;
using UnityEngine;

namespace UI.UIControl
{
    public partial class CombatUIController
    {
        private void HideActionPanel()
        {
            if (action == null) return;

            action.gameObject.SetActive(false);

            var cg = action.canvasGroup;
            if (cg != null)
            {
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
                cg.gameObject.SetActive(false);
            }

            Debug.Log("[HexActionPanel] Hide");
        }

        private void ShowHexActionPanel(bool show)
        {
            if (action == null)
            {
                Debug.LogWarning("[HexActionPanel] action is null");
                return;
            }

            var root = action.gameObject;
            root.SetActive(show);

            var cg = action.canvasGroup;
            if (cg != null)
            {
                cg.gameObject.SetActive(show);

                if (show)
                {
                    cg.alpha = 1f;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
                else
                {
                    cg.alpha = 0f;
                    cg.interactable = false;
                    cg.blocksRaycasts = false;
                }

                Debug.Log($"[HexActionPanel] Show={show} rootActive={root.activeSelf} cgActive={cg.gameObject.activeSelf} alpha={cg.alpha} blocks={cg.blocksRaycasts}");
            }
            else
            {
                Debug.Log($"[HexActionPanel] Show={show} rootActive={root.activeSelf} (no CanvasGroup)");
            }
        }

        private void SetPanelTitle(string title)
        {
            if (action != null && action.titleText != null)
                action.titleText.text = title;
        }

        private void ShowRootRows(bool show)
        {
            if (action == null) return;
            if (action.btnMove != null) action.btnMove.gameObject.SetActive(show);
            if (action.btnAttack != null) action.btnAttack.gameObject.SetActive(show);
            if (action.btnDefend != null) action.btnDefend.gameObject.SetActive(show);
            if (action.btnBullet != null) action.btnBullet.gameObject.SetActive(show);
            if (action.btnConsumable != null) action.btnConsumable.gameObject.SetActive(show);
        }

        private void ClearDynamicRows()
        {
            for (int i = 0; i < _dynamicRows.Count; i++)
            {
                if (_dynamicRows[i] != null) Destroy(_dynamicRows[i]);
            }
            _dynamicRows.Clear();
        }

        private void AddDynamicRow(string name, string desc, string icon, Action onClick, bool interactable = true)
        {
            if (action == null || action.rowPrefab == null || action.listRoot == null)
            {
                Debug.LogWarning("[HexActionPanel] AddDynamicRow failed (rowPrefab/listRoot/action null)");
                return;
            }

            var row = Instantiate(action.rowPrefab, action.listRoot);
            row.gameObject.SetActive(true);
            row.gameObject.name = $"Row_{name}";
            _dynamicRows.Add(row.gameObject);

            var iconRt = row.Find("Icon");
            if (iconRt != null)
            {
                var t = iconRt.GetComponentInChildren<UnityEngine.UI.Text>();
                if (t != null) t.text = icon;
            }

            var nameT = row.Find("Texts/Name")?.GetComponent<UnityEngine.UI.Text>();
            if (nameT != null) nameT.text = name;

            var descT = row.Find("Texts/Desc")?.GetComponent<UnityEngine.UI.Text>();
            if (descT != null) descT.text = desc;

            var btn = row.GetComponent<UnityEngine.UI.Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => onClick?.Invoke());
                btn.interactable = interactable;
            }
        }

        private void ReturnToRootActions()
        {
            ClearDynamicRows();
            ShowRootRows(true);
            SetPanelTitle("ACTIONS");
        }

        private void OpenAttackConfirmList()
        {
            EnsureLoadoutResolved();
            if (action == null || action.rowPrefab == null || action.listRoot == null) return;
            if (_melee == null)
            {
                Debug.LogWarning("[AttackUI] No melee weapon.");
                return;
            }

            ClearDynamicRows();
            ShowRootRows(false);
            SetPanelTitle($"ATTACK - {_melee.displayName}");

            AddDynamicRow("< Back", "Return", "<", () => { ReturnToRootActions(); });

            foreach (var atk in _melee.attacks)
            {
                if (atk == null) continue;
                var desc = $"AP {atk.apCost} / {atk.shape}";
                AddDynamicRow(atk.displayName, desc, "A", () =>
                {
                    _selectedAttack = atk;
                    TryExecuteAttack(atk);
                });
            }
        }

        private void OpenConsumableList()
        {
            if (action == null || action.rowPrefab == null || action.listRoot == null) return;

            ClearDynamicRows();
            ShowRootRows(false);
            SetPanelTitle("CONSUMABLE");

            AddDynamicRow("< Back", "Return", "<", () => { ReturnToRootActions(); });

            var pc = Rule.Combat.Player.PlayerController.Instance;

            foreach (var c in equippedConsumables)
            {
                var id = string.IsNullOrWhiteSpace(c) ? "" : c.Trim();
                var label = string.IsNullOrWhiteSpace(id) ? "(None)" : id;

                int count = (pc != null && !string.IsNullOrWhiteSpace(id)) ? pc.GetConsumableCount(id) : 0;
                string right = (pc != null && pc.ConsumableUsedThisTurn) ? "Used" : $"x{count}";

                AddDynamicRow(label, right, "C", () =>
                {
                    if (pc == null)
                    {
                        Debug.Log("[Consumable] PlayerController missing.");
                        ReturnToRootActions();
                        return;
                    }

                    if (_busy || !_lastPlayerTurn)
                    {
                        Debug.Log("[Consumable] Not allowed outside player turn.");
                        ReturnToRootActions();
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(id))
                    {
                        Debug.Log("[Consumable] Empty slot.");
                        ReturnToRootActions();
                        return;
                    }

                    bool ok = pc.TryUseConsumable(id);
                    Debug.Log($"[Consumable] Use id={id} => ok={ok}, left={pc.GetConsumableCount(id)}, turnUsed={pc.ConsumableUsedThisTurn}");

                    RefreshHud();
                    ReturnToRootActions();
                    ShowHexActionPanel(false); // 사용 후 자동 닫기
                });
            }
        }
    }
}