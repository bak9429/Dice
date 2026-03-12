using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.UIBuilder.Node
{
    public class NodeSceneViewRefs : MonoBehaviour
    {
        [Header("Root")]
        public Canvas canvas;
        public RectTransform root;

        [Header("Panels")]
        public RectTransform leftPanel;
        public RectTransform centerPanel;
        public RectTransform rightPanel;
        public RectTransform bottomPanel;

        [Header("Left")]
        public Image environmentImage;
        public Image bossSilhouetteImage;
        public Text bossNameText;

        [Header("Center")]
        public Text titleText;
        public Text descriptionText;
        public Text resultText;

        [Header("Right")]
        public Button[] choiceButtons = new Button[4];
        public Text[] choiceButtonTexts = new Text[4];

        [Header("Bottom")]
        public Text progressText;
        public Button hintButton;
        public GameObject hintPanel;
        public Text hintText;
        public Button nextButton;
        public Text nextButtonText;
        public Button submitDeductionButton;
        public Text submitDeductionButtonText;

        [Header("Deduction")]
        public Button[] weaknessButtons = new Button[3];
        public Button[] placeButtons = new Button[3];

        public void SetBossName(string text)
        {
            if (bossNameText != null) bossNameText.text = text ?? "";
        }

        public void SetProgressText(string text)
        {
            if (progressText != null) progressText.text = text ?? "";
        }

        public void SetTitle(string text)
        {
            if (titleText != null) titleText.text = text ?? "";
        }

        public void SetDescription(string text)
        {
            if (descriptionText != null) descriptionText.text = text ?? "";
        }

        public void SetResultText(string text)
        {
            if (resultText != null) resultText.text = text ?? "";
        }

        public void SetHintTexts(List<string> hints)
        {
            if (hintText == null) return;

            hintText.text = (hints == null || hints.Count == 0)
                ? "힌트 없음"
                : string.Join("\n", hints);
        }

        public void ShowInvestigationMode()
        {
            SetChoicesVisible(true);
            SetDeductionButtonsVisible(false);
            SetNextButtonVisible(true);
            SetSubmitDeductionVisible(false);
        }

        public void ShowDeductionMode()
        {
            SetChoicesVisible(false);
            SetDeductionButtonsVisible(true);
            SetNextButtonVisible(false);
            SetSubmitDeductionVisible(true);
        }

        public void ClearChoiceListeners()
        {
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (choiceButtons[i] != null)
                    choiceButtons[i].onClick.RemoveAllListeners();
            }
        }

        public void SetChoiceButton(int index, bool active, string text)
        {
            if (index < 0 || index >= choiceButtons.Length) return;

            var btn = choiceButtons[index];
            var txt = choiceButtonTexts[index];
            if (btn == null) return;

            btn.gameObject.SetActive(active);
            btn.onClick.RemoveAllListeners();
            btn.interactable = true;

            if (txt != null) txt.text = text ?? "";
        }

        public void SetChoiceInteractable(int index, bool active)
        {
            if (index < 0 || index >= choiceButtons.Length) return;
            if (choiceButtons[index] != null)
                choiceButtons[index].interactable = active;
        }

        public void BindChoice(int index, UnityAction action)
        {
            if (index < 0 || index >= choiceButtons.Length) return;
            if (choiceButtons[index] == null) return;

            choiceButtons[index].onClick.RemoveAllListeners();
            choiceButtons[index].onClick.AddListener(action);
        }

        public void SetNextButtonVisible(bool visible)
        {
            if (nextButton != null) nextButton.gameObject.SetActive(visible);
        }

        public void SetSubmitDeductionVisible(bool visible)
        {
            if (submitDeductionButton != null)
                submitDeductionButton.gameObject.SetActive(visible);
        }

        public void SetHintPanelVisible(bool visible)
        {
            if (hintPanel != null) hintPanel.SetActive(visible);
        }

        public void SetChoicesVisible(bool visible)
        {
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (choiceButtons[i] != null)
                    choiceButtons[i].gameObject.SetActive(visible);
            }
        }

        public void SetDeductionButtonsVisible(bool visible)
        {
            for (int i = 0; i < weaknessButtons.Length; i++)
            {
                if (weaknessButtons[i] != null)
                    weaknessButtons[i].gameObject.SetActive(visible);
            }

            for (int i = 0; i < placeButtons.Length; i++)
            {
                if (placeButtons[i] != null)
                    placeButtons[i].gameObject.SetActive(visible);
            }
        }
    }
}