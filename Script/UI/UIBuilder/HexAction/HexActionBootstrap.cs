// Path: Assets/Script/UI/UIBuilder/HexAction/HexActionBootstrap.cs
using UnityEngine;

namespace UI.UIBuilder.HexAction
{
    public class HexActionBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            // 중복 생성 방지
            var existing = FindFirstObjectByType<HexActionPanelPresenter>(FindObjectsInactive.Include);
            if (existing != null)
            {
                Debug.Log("[HexActionBootstrap] Presenter already exists.");
                return;
            }

            var go = new GameObject("HexActionPresenter");
            go.transform.SetParent(transform, false);
            go.AddComponent<HexActionPanelPresenter>();

            Debug.Log("[HexActionBootstrap] Presenter created.");
        }
    }
}
