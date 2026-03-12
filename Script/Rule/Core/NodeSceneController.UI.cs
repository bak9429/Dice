
using UnityEngine;
using UI.UIBuilder.Node;

namespace Rule.Core
{
    public partial class NodeSceneController : MonoBehaviour
    {
        NodeSceneViewRefs view;
        bool hintVisible;

        void EnsureNodeView()
        {
            if (view != null) return;

            view = FindObjectOfType<NodeSceneViewRefs>();
            if (view == null)
                view = NodeSceneBuilder.Build();

            view.hintButton.onClick.AddListener(ToggleHint);
        }

        void ToggleHint()
        {
            hintVisible = !hintVisible;
            view.hintPanel.SetActive(hintVisible);
        }
    }
}
