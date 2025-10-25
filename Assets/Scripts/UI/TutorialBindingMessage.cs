using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialBindingMessage : MonoBehaviour
{
    public InputActionReference ActionReference;

    public string KeyboardGroup = "Keyboard";
    public string GamepadGroup = "Gamepad";

    public TMP_Text DisplayText;

    public string Message = "";
    public string Separator = " / ";
    public string partSeparator = "|";
    public string MissingFallback = "—";

    void OnEnable()
    {
        UpdateDisplay();
        InputSystem.onActionChange += OnActionChange;
    }

    void OnDisable()
    {
        InputSystem.onActionChange -= OnActionChange;
    }

    private void OnActionChange(object obj, InputActionChange change) 
    { 
        UpdateDisplay(); 
    }

    public void UpdateDisplay()
    {
        if (DisplayText == null || ActionReference == null || ActionReference.action == null)
        {
            return;
        }   

        InputAction action = ActionReference.action;

        string kbText = GetDisplayForGroup(action, KeyboardGroup);
        string gpText = GetDisplayForGroup(action, GamepadGroup);

        if (string.IsNullOrEmpty(kbText))
        {
            kbText = MissingFallback;
        }

        if (string.IsNullOrEmpty(gpText))
        {
            gpText = MissingFallback;
        }

        DisplayText.text = $"{Message} {kbText}{Separator}{gpText}";
    }

    private string GetDisplayForGroup(InputAction action, string group)
    {
        var bindings = action.bindings;

        for (int i = 0; i < bindings.Count; ++i)
        {
            InputBinding b = bindings[i];
            if (!b.isComposite)
            {
                continue;
            }

            if (!BindingHasGroup(b, group))
            {
                continue;
            }

            List<string> parts = new List<string>();
            int j = i + 1;
            while (j < bindings.Count && bindings[j].isPartOfComposite)
            {
                if (BindingHasGroup(bindings[j], group) || string.IsNullOrEmpty(bindings[j].groups))
                {
                    string ds = action.GetBindingDisplayString(bindingIndex: j);
                    if (!string.IsNullOrEmpty(ds))
                    {
                        parts.Add(ds);
                    } 
                }
                j++;
            }

            if (parts.Count > 0)
            {
                string[] uniqueParts = parts.Distinct().ToArray();
                return string.Join($" {partSeparator} ", uniqueParts);
            }
        }

        for (int i = 0; i < bindings.Count; ++i)
        {
            InputBinding b = bindings[i];
            if (b.isComposite || b.isPartOfComposite)
            {
                continue;
            }

            if (!BindingHasGroup(b, group))
            {
                continue;
            }

            string ds = action.GetBindingDisplayString(bindingIndex: i);
            if (!string.IsNullOrEmpty(ds))
            {
                return ds;
            }
        }

        for (int i = 0; i < bindings.Count; ++i)
        {
            InputBinding b = bindings[i];
            if (!b.isComposite)
            {
                continue;
            }

            List<string> parts = new List<string>();
            int j = i + 1;
            while (j < bindings.Count && bindings[j].isPartOfComposite)
            {
                string ds = action.GetBindingDisplayString(bindingIndex: j);
                if (!string.IsNullOrEmpty(ds))
                {
                    parts.Add(ds);
                }
                j++;
            }
            if (parts.Count > 0)
            {
                return string.Join($" {partSeparator} ", parts.Distinct().ToArray());
            }
        }

        for (int i = 0; i < bindings.Count; ++i)
        {
            InputBinding b = bindings[i];
            if (b.isComposite || b.isPartOfComposite)
            {
                continue;
            }
            string ds = action.GetBindingDisplayString(bindingIndex: i);
            if (!string.IsNullOrEmpty(ds))
            {
                return ds;
            }
        }

        return null;
    }

    private bool BindingHasGroup(InputBinding binding, string group)
    {
        if (string.IsNullOrEmpty(group))
        {
            return true;
        }
        if (string.IsNullOrEmpty(binding.groups))
        {
            return false;
        }

        var groups = binding.groups.Replace(';', ',').Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim());
        foreach (string g in groups)
        {
            if (string.Equals(g, group, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }   
        }
            
        return false;
    }
}
