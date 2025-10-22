using UnityEngine;
using UnityEngine.InputSystem;

public class ResetAllBindings : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string targetControlScheme;

    public void ResetBindings()
    {
        foreach (InputActionMap actionMap in inputActions.actionMaps)
        {
            actionMap.RemoveAllBindingOverrides();
        }
        PlayerPrefs.DeleteKey("rebinds");
    }

    public void ResetControlSchemeBindings()
    {
        foreach(InputActionMap actionMap in inputActions.actionMaps)
        {
            foreach(InputAction action in actionMap.actions)
            {
                action.RemoveBindingOverride(InputBinding.MaskByGroup(targetControlScheme));
            }
        }
    }
}
