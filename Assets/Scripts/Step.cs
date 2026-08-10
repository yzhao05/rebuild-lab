using System.Collections;
using System.Collections.Generic;
using EMVR.Assist.Core;
using UnityEngine;
using UnityEngine.UI;

public class Step : MonoBehaviour
{
    public Button previousButton;
    public Button nextButton;
    public Text text;
    public KeyCode previousKey = KeyCode.Q;
    public KeyCode nextKey = KeyCode.E;

    private int currentStep = 0;
    private readonly string[] fallbackStepIds = {
        "step_1",
        "step_2",
        "step_3",
        "step_4",
        "step_5"
    };

    private readonly string[] stepTexts = {
        "Choose a point, line, or plane charge from the source area on the right.",
        "Move the copied charge object into the charging area until its charge changes.",
        "Place the charged object into the observation area to create electric field lines.",
        "Observe how the electric field lines change for this charge geometry.",
        "Repeat the experiment with another geometry and compare the field pattern."
    };

    private bool canClick = true;

    void Start()
    {
        if (previousButton != null) previousButton.onClick.AddListener(OnPreviousButtonClicked);
        if (nextButton != null) nextButton.onClick.AddListener(OnNextButtonClicked);

        if (StepProgressManager.Instance != null)
        {
            StepProgressManager.Instance.OnStepChanged += HandleStepChanged;
        }

        UpdateText();
    }

    void Update()
    {
        if (Input.GetKeyDown(previousKey))
        {
            OnPreviousButtonClicked();
        }

        if (Input.GetKeyDown(nextKey))
        {
            OnNextButtonClicked();
        }
    }

    private void OnDestroy()
    {
        if (StepProgressManager.Instance != null)
        {
            StepProgressManager.Instance.OnStepChanged -= HandleStepChanged;
        }
    }

    public void OnPreviousButtonClicked()
    {
        if (!canClick) return;

        if (StepProgressManager.Instance != null && StepProgressManager.Instance.StepCount > 0)
        {
            int index = StepProgressManager.Instance.CurrentStepIndex;
            if (index > 0)
            {
                StepProgressManager.Instance.SetStepByIndex(index - 1);
                StartCoroutine(Cooldown());
            }
            return;
        }

        if (currentStep > 0)
        {
            currentStep--;
            UpdateText();
            StartCoroutine(Cooldown());
        }
    }

    public void OnNextButtonClicked()
    {
        if (!canClick) return;

        if (StepProgressManager.Instance != null && StepProgressManager.Instance.StepCount > 0)
        {
            int index = StepProgressManager.Instance.CurrentStepIndex;
            if (index >= 0 && index < StepProgressManager.Instance.StepCount - 1)
            {
                StepProgressManager.Instance.SetStepByIndex(index + 1);
                StartCoroutine(Cooldown());
            }
            return;
        }

        if (currentStep < stepTexts.Length - 1)
        {
            currentStep++;
            UpdateText();
            StartCoroutine(Cooldown());
        }
    }

    private IEnumerator Cooldown()
    {
        canClick = false;
        yield return new WaitForSeconds(1f);
        canClick = true;
    }

    private void HandleStepChanged(string stepId)
    {
        UpdateText();
    }

    private void UpdateText()
    {
        if (text == null) return;

        if (StepProgressManager.Instance != null && StepProgressManager.Instance.CurrentStep != null)
        {
            LabStep step = StepProgressManager.Instance.CurrentStep;
            int index = StepProgressManager.Instance.CurrentStepIndex;
            currentStep = Mathf.Max(0, index);
            text.text = FormatManagedStep(index, StepProgressManager.Instance.StepCount, step);
            return;
        }

        if (currentStep >= 0 && currentStep < stepTexts.Length)
        {
            text.text = fallbackStepIds[currentStep] + ":\n" + stepTexts[currentStep];
        }
        else
        {
            Debug.LogWarning("Step index out of bounds: " + currentStep);
        }
    }

    private static string FormatManagedStep(int index, int total, LabStep step)
    {
        string stepId = step != null ? step.stepId : "step";
        string description = step != null ? step.description : "";

        if (index >= 0 && total > 0)
        {
            return stepId + " (" + (index + 1) + "/" + total + "):\n" + description;
        }

        return stepId + ":\n" + description;
    }
}

