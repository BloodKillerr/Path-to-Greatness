using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BindingDialog : MonoBehaviour
{
    public CanvasGroup dialogRoot;
    public Button[] slotButtons;
    public Button unbindButton;
    public Button closeButton;
    public TMP_Text titleText;

    private Ability currentAbility;

    public static BindingDialog Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        HideCanvas();

        for (int i = 0; i < slotButtons.Length; i++)
        {
            int idx = i;
            slotButtons[i].onClick.AddListener(() => OnSlotClicked(idx));
        }

        unbindButton.onClick.AddListener(OnUnbindClicked);
        closeButton.onClick.AddListener(Close);
    }

    public void OpenCanvas()
    {
        dialogRoot.alpha = 1f;
        dialogRoot.interactable = true;
        dialogRoot.blocksRaycasts = true;
    }

    public void HideCanvas()
    {
        dialogRoot.alpha = 0f;
        dialogRoot.interactable = false;
        dialogRoot.blocksRaycasts = false;
    }

    public void Open(Ability ability)
    {
        currentAbility = ability;
        titleText.text = $"Bind '{ability.AbilityName}'";
        OpenCanvas();
        UIManager.Instance.CloseMenuTemporarily(MenuType.ABILITIES);
        RefreshSlotLabels();
    }

    private void OnSlotClicked(int slot)
    {
        AbilityManager.Instance.BindAbilityToSlot(currentAbility, slot);
        RefreshSlotLabels();
        Close();
    }

    private void OnUnbindClicked()
    {
        for (int i = 0; i < 4; i++)
        {
            Ability b = AbilityManager.Instance.GetBoundAbility(i);
            if (b != null && b.AbilityName == currentAbility.AbilityName)
            {
                AbilityManager.Instance.UnbindSlot(i);
            }
        }

        RefreshSlotLabels();
    }

    private void RefreshSlotLabels()
    {
        for (int i = 0; i < slotButtons.Length; i++)
        {
            Ability bound = AbilityManager.Instance.GetBoundAbility(i);
            TMP_Text txt = slotButtons[i].GetComponentInChildren<TMP_Text>();
            if (txt != null)
            {
                txt.text = bound == null ? $"Slot {i + 1} (Empty)" : $"{bound.AbilityName}";
            }
        }
    }

    public void Close()
    {
        HideCanvas();
        UIManager.Instance.OpenMenuTemporarily(MenuType.ABILITIES);
    }
}
