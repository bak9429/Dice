// Path: Assets/Script/Rule/Core/ResultSceneController.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UI.UIBuilder.Result;

namespace Rule.Core
{
    public class ResultSceneController : MonoBehaviour
    {
        private ResultSceneBuilder.ViewRefs _view;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (sceneName != SceneFlow.ResultScene) return;

            var existing = FindFirstObjectByType<ResultSceneController>(FindObjectsInactive.Include);
            if (existing != null) return;

            var go = new GameObject("ResultSceneController");
            go.AddComponent<ResultSceneController>();
        }

        private void Start()
        {
            _view = ResultSceneBuilder.Build();
            Refresh();
            StartCoroutine(PlayRoulette());
        }

        private void Refresh()
        {
            var session = RunSession.Instance;

            if (!session.HasPendingResult)
            {
                _view.titleText.text = "NO RESULT";
                _view.bodyText.text = "표시할 전투 결과가 없다.";
                _view.rouletteResultText.text = "";
                BindButton(_view.primaryButton, "Return to Hub", session.ReturnToHubAfterResult, true);
                BindButton(_view.secondaryButton, "", null, false);
                return;
            }

            if (!session.LastCombatWon)
            {
                _view.titleText.text = "DEFEATED";
                _view.bodyText.text =
                    "전투에서 패배했다.\n" +
                    session.BuildResultSummary();
                _view.rouletteResultText.text = "DEFEAT";

                BindButton(_view.primaryButton, "Return to Hub", session.ReturnToHubAfterResult, true);
                BindButton(_view.secondaryButton, "", null, false);
                return;
            }

            if (session.LastCombatWasGateBoss)
            {
                _view.titleText.text = "GATE CLEARED";
                _view.bodyText.text =
                    "관문보스를 격파했다.\n" +
                    session.BuildResultSummary();

                BindButton(_view.primaryButton, "Return to Hub", session.ReturnToHubAfterResult, true);
                BindButton(_view.secondaryButton, "", null, false);
                return;
            }

            _view.titleText.text = "VICTORY";
            _view.bodyText.text =
                "중간보스를 격파했다.\n" +
                session.BuildResultSummary() +
                "\n다음 조사로 진행하거나 허브로 복귀할 수 있다.";

            BindButton(_view.primaryButton, "Next Investigation", session.ContinueAfterResult, true);
            BindButton(_view.secondaryButton, "Return to Hub", session.ReturnToHubAfterResult, true);
        }

        private IEnumerator PlayRoulette()
        {
            var session = RunSession.Instance;

            if (!session.LastCombatWon || session.LastRewardDiceRoll <= 0)
            {
                yield break;
            }

            int targetIndex = Mathf.Clamp(session.LastRewardDiceRoll - 1, 0, 5);

            float startZ = _view.wheelRoot.localEulerAngles.z;
            float targetZ = startZ - (360f * 4f) - (targetIndex * 60f);

            float duration = 2.15f;
            float t = 0f;

            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                float eased = 1f - Mathf.Pow(1f - p, 3f);

                float z = Mathf.Lerp(startZ, targetZ, eased);
                _view.wheelRoot.localEulerAngles = new Vector3(0f, 0f, z);

                yield return null;
            }

            _view.wheelRoot.localEulerAngles = new Vector3(0f, 0f, targetZ);
            _view.rouletteResultText.text = BuildRewardLine(session);
        }

        private static string BuildRewardLine(RunSession session)
        {
            if (!string.IsNullOrWhiteSpace(session.LastRewardEquipmentId))
                return $"DICE {session.LastRewardDiceRoll}  →  [{session.LastRewardEquipmentType}] {session.LastRewardEquipmentId}";

            return $"DICE {session.LastRewardDiceRoll}  →  Currency +{session.LastRewardCurrency}";
        }

        private static void BindButton(Button button, string label, System.Action action, bool visible)
        {
            if (button == null) return;

            button.gameObject.SetActive(visible);
            button.onClick.RemoveAllListeners();

            var text = button.GetComponentInChildren<Text>(true);
            if (text != null)
                text.text = label;

            if (action != null)
                button.onClick.AddListener(() => action());
        }
    }
}