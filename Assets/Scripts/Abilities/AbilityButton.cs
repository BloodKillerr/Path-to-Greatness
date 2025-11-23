using UnityEngine;
using UnityEngine.UI;

public class AbilityButton : MonoBehaviour
{
    public Ability abilityInstance;
    public Button bindButton;

    private void Awake()
    {
        bindButton.onClick.AddListener(OnBindClicked);
    }

    private void OnBindClicked()
    {
        BindingDialog.Instance.Open(abilityInstance);
    }
}
