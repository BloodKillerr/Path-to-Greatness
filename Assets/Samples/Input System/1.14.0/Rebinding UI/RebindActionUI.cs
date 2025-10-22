using System;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.XInput;

////TODO: localization support

////TODO: deal with composites that have parts bound in different control schemes

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    /// <summary>
    /// A reusable component with a self-contained UI for rebinding a single action.
    /// </summary>
    public class RebindActionUI : MonoBehaviour
    {
        /// <summary>
        /// Reference to the action that is to be rebound.
        /// </summary>
        public InputActionReference actionReference
        {
            get => m_Action;
            set
            {
                m_Action = value;
                UpdateActionLabel();
                UpdateBindingDisplay();
            }
        }

        /// <summary>
        /// ID (in string form) of the binding that is to be rebound on the action.
        /// </summary>
        /// <seealso cref="InputBinding.id"/>
        public string bindingId
        {
            get => m_BindingId;
            set
            {
                m_BindingId = value;
                UpdateBindingDisplay();
            }
        }

        public InputBinding.DisplayStringOptions displayStringOptions
        {
            get => m_DisplayStringOptions;
            set
            {
                m_DisplayStringOptions = value;
                UpdateBindingDisplay();
            }
        }

        /// <summary>
        /// Text component that receives the name of the action. Optional.
        /// </summary>
        public TMP_Text actionLabel
        {
            get => m_ActionLabel;
            set
            {
                m_ActionLabel = value;
                UpdateActionLabel();
            }
        }

        /// <summary>
        /// Text component that receives the display string of the binding. Can be <c>null</c> in which
        /// case the component entirely relies on <see cref="updateBindingUIEvent"/>.
        /// </summary>
        public TMP_Text bindingText
        {
            get => m_BindingText;
            set
            {
                m_BindingText = value;
                UpdateBindingDisplay();
            }
        }

        /// <summary>
        /// Optional text component that receives a text prompt when waiting for a control to be actuated.
        /// </summary>
        /// <seealso cref="startRebindEvent"/>
        /// <seealso cref="rebindOverlay"/>
        public TMP_Text rebindPrompt
        {
            get => m_RebindText;
            set => m_RebindText = value;
        }

        /// <summary>
        /// Optional UI that is activated when an interactive rebind is started and deactivated when the rebind
        /// is finished. This is normally used to display an overlay over the current UI while the system is
        /// waiting for a control to be actuated.
        /// </summary>
        /// <remarks>
        /// If neither <see cref="rebindPrompt"/> nor <c>rebindOverlay</c> is set, the component will temporarily
        /// replaced the <see cref="bindingText"/> (if not <c>null</c>) with <c>"Waiting..."</c>.
        /// </remarks>
        /// <seealso cref="startRebindEvent"/>
        /// <seealso cref="rebindPrompt"/>
        public GameObject rebindOverlay
        {
            get => m_RebindOverlay;
            set => m_RebindOverlay = value;
        }

        /// <summary>
        /// Event that is triggered every time the UI updates to reflect the current binding.
        /// This can be used to tie custom visualizations to bindings.
        /// </summary>
        public UpdateBindingUIEvent updateBindingUIEvent
        {
            get
            {
                if (m_UpdateBindingUIEvent == null)
                    m_UpdateBindingUIEvent = new UpdateBindingUIEvent();
                return m_UpdateBindingUIEvent;
            }
        }

        /// <summary>
        /// Event that is triggered when an interactive rebind is started on the action.
        /// </summary>
        public InteractiveRebindEvent startRebindEvent
        {
            get
            {
                if (m_RebindStartEvent == null)
                    m_RebindStartEvent = new InteractiveRebindEvent();
                return m_RebindStartEvent;
            }
        }

        /// <summary>
        /// Event that is triggered when an interactive rebind has been completed or canceled.
        /// </summary>
        public InteractiveRebindEvent stopRebindEvent
        {
            get
            {
                if (m_RebindStopEvent == null)
                    m_RebindStopEvent = new InteractiveRebindEvent();
                return m_RebindStopEvent;
            }
        }

        /// <summary>
        /// When an interactive rebind is in progress, this is the rebind operation controller.
        /// Otherwise, it is <c>null</c>.
        /// </summary>
        public InputActionRebindingExtensions.RebindingOperation ongoingRebind => m_RebindOperation;

        /// <summary>
        /// Return the action and binding index for the binding that is targeted by the component
        /// according to
        /// </summary>
        /// <param name="action"></param>
        /// <param name="bindingIndex"></param>
        /// <returns></returns>
        public bool ResolveActionAndBinding(out InputAction action, out int bindingIndex)
        {
            bindingIndex = -1;

            action = m_Action?.action;
            if (action == null)
                return false;

            if (string.IsNullOrEmpty(m_BindingId))
                return false;

            // Look up binding index.
            var bindingId = new Guid(m_BindingId);
            bindingIndex = action.bindings.IndexOf(x => x.id == bindingId);
            if (bindingIndex == -1)
            {
                Debug.LogError($"Cannot find binding with ID '{bindingId}' on '{action}'", this);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Trigger a refresh of the currently displayed binding.
        /// </summary>
        public void UpdateBindingDisplay()
        {
            var displayString = string.Empty;
            var deviceLayoutName = default(string);
            var controlPath = default(string);

            // Get display string from action.
            var action = m_Action?.action;
            if (action != null)
            {
                var bindingIndex = action.bindings.IndexOf(x => x.id.ToString() == m_BindingId);
                if (bindingIndex != -1)
                    displayString = action.GetBindingDisplayString(bindingIndex, out deviceLayoutName, out controlPath, displayStringOptions);
            }

            // Set on label (if any).
            if (m_BindingText != null)
                m_BindingText.text = displayString;

            // Give listeners a chance to configure UI in response.
            m_UpdateBindingUIEvent?.Invoke(this, displayString, deviceLayoutName, controlPath);
        }

        /// <summary>
        /// Remove currently applied binding overrides.
        /// </summary>
        public void ResetToDefault()
        {
            if (!ResolveActionAndBinding(out var action, out var bindingIndex))
                return;

            if (action.bindings[bindingIndex].isComposite)
            {
                for (int partIndex = bindingIndex + 1;
                     partIndex < action.bindings.Count && action.bindings[partIndex].isPartOfComposite;
                     ++partIndex)
                {
                    if (!SwapResetBindings(action, partIndex))
                    {
                        action.RemoveBindingOverride(partIndex);
                    }
                }

                UpdateBindingDisplay();
                return;
            }

            if (SwapResetBindings(action, bindingIndex))
            {
                UpdateBindingDisplay();
                return;
            }

            action.RemoveBindingOverride(bindingIndex);
            UpdateBindingDisplay();
        }

        private bool SwapResetBindings(InputAction action, int bindingIndex)
        {
            var newBinding = action.bindings[bindingIndex];

            if (string.IsNullOrEmpty(newBinding.overridePath))
                return false;

            var defaultPath = newBinding.path;

            for (int siblingIndex = 0; siblingIndex < action.bindings.Count; ++siblingIndex)
            {
                var sibling = action.bindings[siblingIndex];
                if (sibling.id == newBinding.id)
                    continue;

                if (sibling.effectivePath == defaultPath)
                {
                    action.ApplyBindingOverride(siblingIndex, newBinding.overridePath);
                    action.RemoveBindingOverride(bindingIndex);
                    return true;
                }
            }

            foreach (var globalBinding in action.actionMap.bindings)
            {
                if (globalBinding.action == newBinding.action)
                    continue; // skip any binding belonging to the same action (we already checked those)

                if (globalBinding.effectivePath == defaultPath)
                {
                    var otherAction = action.actionMap.FindAction(globalBinding.action);
                    if (otherAction == null)
                        continue;

                    int otherLocalIndex = otherAction.bindings
                        .IndexOf(x => x.id == globalBinding.id);
                    if (otherLocalIndex == -1)
                    {
                        Debug.LogError(
                            $"SwapResetBindings: Could not find local index for binding '{globalBinding.id}' in action '{otherAction.name}'.");
                        continue;
                    }

                    otherAction.ApplyBindingOverride(otherLocalIndex, newBinding.overridePath);
                    action.RemoveBindingOverride(bindingIndex);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Initiate an interactive rebind that lets the player actuate a control to choose a new binding
        /// for the action.
        /// </summary>
        public void StartInteractiveRebind()
        {
            if (!ResolveActionAndBinding(out var action, out var bindingIndex))
                return;

            // Grab the very binding we want to rebind.
            var binding = action.bindings[bindingIndex];

            // 1) If this binding belongs to a group (e.g. "Xbox", "PlayStation", or "Keyboard&Mouse"),
            //    check if at least one device for that scheme is actually present.
            if (!IsBindingCurrentlySupported(action, bindingIndex))
            {
                // Show a quick "controller not detected" prompt (optional).
                if (m_RebindText != null)
                    m_RebindText.text = "<Device not detected>";
                return; // bail out before calling PerformInteractiveRebind.
            }

            // 2) If we get here, the correct device‐family is present. Proceed as before:
            if (EventSystem.current != null)
                EventSystem.current.sendNavigationEvents = false;

            if (binding.isComposite)
            {
                var firstPartIndex = bindingIndex + 1;
                if (firstPartIndex < action.bindings.Count && action.bindings[firstPartIndex].isPartOfComposite)
                    PerformInteractiveRebind(action, firstPartIndex, allCompositeParts: true);
            }
            else
            {
                PerformInteractiveRebind(action, bindingIndex);
            }
        }

        private static bool IsBindingSupportedByGroup(InputBinding binding)
        {
            if (string.IsNullOrEmpty(binding.groups))
                return false; // no groups means “no group to check”—caller will fall back to path-based.

            // Split on ';' and remove any empty entries:
            var groups = binding
                .groups
                .Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries);

            foreach (var raw in groups)
            {
                var trimmed = raw.Trim();
                switch (trimmed)
                {
                    case "Keyboard":
                        if (Keyboard.current != null || Mouse.current != null)
                            return true;
                        break;

                    case "Gamepad":
                        if (Gamepad.current != null)
                            return true;
                        break;

                    default:
                        // If you have other scheme names, handle them here.
                        // If unknown, assume “supported”:
                        return true;
                }
            }

            // None of the listed groups had a matching device.
            return false;
        }

        //
        // 2) If “groups” is empty or didn’t match, fall back to snippet-only (“path”) check.
        //
        private static bool IsBindingSupportedByPath(string path)
        {
            if (string.IsNullOrEmpty(path) || !path.StartsWith("<"))
                return true;

            // Extract token inside angle brackets, e.g. "<Gamepad>"
            int slash = path.IndexOf('/');
            string deviceToken = slash > 0 ? path.Substring(0, slash) : path;

            switch (deviceToken)
            {
                case "<Keyboard>":
                    return Keyboard.current != null;

                case "<Mouse>":
                    return Mouse.current != null;

                case "<Gamepad>":
                    // any gamepad (Xbox, PS, etc.)
                    return Gamepad.current != null;

                default:
                    // if some other layout (Touchscreen, etc.), allow by default:
                    return true;
            }
        }

        //
        // 3) “Master” helper that considers composites vs. single bindings:
        //
        private static bool IsBindingCurrentlySupported(InputAction action, int bindingIndex)
        {
            var binding = action.bindings[bindingIndex];

            // If it’s a composite (e.g. a 2D Vector composite), first attempt “group”:
            if (binding.isComposite)
            {
                // 3a) If the composite itself has a groups string, trust that:
                if (!string.IsNullOrEmpty(binding.groups))
                    return IsBindingSupportedByGroup(binding);

                // 3b) Otherwise, look at each “part” that follows (while isPartOfComposite).
                int nextIndex = bindingIndex + 1;
                bool anyPartSupported = false;
                while (nextIndex < action.bindings.Count && action.bindings[nextIndex].isPartOfComposite)
                {
                    var part = action.bindings[nextIndex];

                    // Try “group” on the part first:
                    if (!string.IsNullOrEmpty(part.groups))
                    {
                        if (IsBindingSupportedByGroup(part))
                        {
                            anyPartSupported = true;
                            break;
                        }
                    }
                    else
                    {
                        // Fall back to path-based check for this part:
                        if (IsBindingSupportedByPath(part.path))
                        {
                            anyPartSupported = true;
                            break;
                        }
                    }

                    nextIndex++;
                }

                return anyPartSupported;
            }

            // If it’s a part of a composite (but somehow you targeted the part directly),
            // just do a path-based check on that one:
            if (binding.isPartOfComposite)
                return IsBindingSupportedByPath(binding.path);

            // Otherwise it’s a “simple” (non-composite) binding:
            //  - If it has groups, do group check
            //  - Else do path check
            if (!string.IsNullOrEmpty(binding.groups))
                return IsBindingSupportedByGroup(binding);

            return IsBindingSupportedByPath(binding.path);
        }

        private void PerformInteractiveRebind(InputAction action, int bindingIndex, bool allCompositeParts = false)
        {
            m_RebindOperation?.Cancel(); // Will null out m_RebindOperation.

            void CleanUp(bool fullRebindFinished)
            {
                // Re-enable navigation if the rebind is completely finished (either cancelled early or the last part bound)
                if (fullRebindFinished && EventSystem.current != null)
                    EventSystem.current.sendNavigationEvents = true;

                m_RebindOperation?.Dispose();
                m_RebindOperation = null;
                action.Enable();
            }

            // Fixes the "Cannot rebind action x while it is enabled" error
            action.Disable();

            // Configure the rebind.
            m_RebindOperation = action.PerformInteractiveRebinding(bindingIndex)
                .OnCancel(operation =>
                {
                    // <<< CHANGED >>>
                    // If the user cancels ANY part of the rebind (single or composite), 
                    // we consider the entire rebind aborted. So we call CleanUp(true).
                    CleanUp(fullRebindFinished: false);

                    m_RebindStopEvent?.Invoke(this, operation);

                    if (m_RebindOverlay != null)
                        m_RebindOverlay.SetActive(false);

                    UpdateBindingDisplay();
                })
                .OnComplete(operation =>
                {
                    // If this was a part of a composite, and there are further parts, do NOT re-enable navigation yet.
                    bool isLastPart = true;
                    if (allCompositeParts)
                    {
                        var nextBindingIndex = bindingIndex + 1;
                        if (nextBindingIndex < action.bindings.Count && action.bindings[nextBindingIndex].isPartOfComposite)
                            isLastPart = false;
                    }

                    if (m_RebindOverlay != null)
                        m_RebindOverlay.SetActive(false);

                    m_RebindStopEvent?.Invoke(this, operation);

                    // Check for duplicates, etc. (existing logic)
                    if (CheckDuplicateBindings(action, bindingIndex, allCompositeParts))
                    {
                        action.RemoveBindingOverride(bindingIndex);
                        // Because we are restarting this same part, we STILL are in the middle of the 
                        // composite rebind; navigation remains off.
                        PerformInteractiveRebind(action, bindingIndex, allCompositeParts);
                        return;
                    }

                    UpdateBindingDisplay();

                    // If there are more parts in a composite, do NOT yet enable navigation; 
                    // just kick off the next part.
                    if (allCompositeParts && !isLastPart)
                    {
                        CleanUp(fullRebindFinished: false); // still not done overall
                        var nextBindingIndex = bindingIndex + 1;
                        if (nextBindingIndex < action.bindings.Count && action.bindings[nextBindingIndex].isPartOfComposite)
                            PerformInteractiveRebind(action, nextBindingIndex, true);
                        return;
                    }

                    // <<< CHANGED >>>
                    // If we reach here, either this was a non-composite bind or it was the last part of a composite.
                    // So we can clean up and re-enable navigation now.
                    CleanUp(fullRebindFinished: true);
                });

            // If it's a part binding, show the name of the part in the UI.
            var partName = default(string);
            if (action.bindings[bindingIndex].isPartOfComposite)
                partName = $"Binding '{action.bindings[bindingIndex].name}'. ";

            // Bring up rebind overlay, if we have one.
            m_RebindOverlay?.SetActive(true);
            if (m_RebindText != null)
            {
                var text = !string.IsNullOrEmpty(m_RebindOperation.expectedControlType)
                    ? $"{partName}Waiting for {m_RebindOperation.expectedControlType} input..."
                    : $"{partName}Waiting for input...";
                m_RebindText.text = text;
            }

            // If we have no rebind overlay and no callback but we have a binding text label,
            // temporarily set the binding text label to "<Waiting>".
            if (m_RebindOverlay == null && m_RebindText == null && m_RebindStartEvent == null && m_BindingText != null)
                m_BindingText.text = "<Waiting...>";

            // Give listeners a chance to act on the rebind starting.
            m_RebindStartEvent?.Invoke(this, m_RebindOperation);

            m_RebindOperation.Start();
        }

        private bool CheckDuplicateBindings(InputAction action, int bindingIndex, bool allCompositeParts = false)
        {
            InputBinding newBinding = action.bindings[bindingIndex];
            string newEffectivePath = newBinding.effectivePath;

            foreach (var globalBinding in action.actionMap.bindings)
            {
                if (globalBinding.action == newBinding.action)
                    continue;

                if (globalBinding.effectivePath == newEffectivePath)
                {
                    return true;
                }
            }

            if (allCompositeParts)
            {
                for (int i = 0; i < bindingIndex; ++i)
                {
                    if (action.bindings[i].effectivePath == newEffectivePath)
                        return true;
                }
            }

            return false;
        }

        protected void OnEnable()
        {
            if (s_RebindActionUIs == null)
                s_RebindActionUIs = new List<RebindActionUI>();
            s_RebindActionUIs.Add(this);
            if (s_RebindActionUIs.Count == 1)
                InputSystem.onActionChange += OnActionChange;
            if (m_DefaultInputActions != null && m_UIInputActionMap == null)
                m_UIInputActionMap = m_DefaultInputActions.FindActionMap("UI");
        }

        protected void OnDisable()
        {
            m_RebindOperation?.Dispose();
            m_RebindOperation = null;

            s_RebindActionUIs.Remove(this);
            if (s_RebindActionUIs.Count == 0)
            {
                s_RebindActionUIs = null;
                InputSystem.onActionChange -= OnActionChange;
            }
        }

        // When the action system re-resolves bindings, we want to update our UI in response. While this will
        // also trigger from changes we made ourselves, it ensures that we react to changes made elsewhere. If
        // the user changes keyboard layout, for example, we will get a BoundControlsChanged notification and
        // will update our UI to reflect the current keyboard layout.
        private static void OnActionChange(object obj, InputActionChange change)
        {
            if (change != InputActionChange.BoundControlsChanged)
                return;

            var action = obj as InputAction;
            var actionMap = action?.actionMap ?? obj as InputActionMap;
            var actionAsset = actionMap?.asset ?? obj as InputActionAsset;

            for (var i = 0; i < s_RebindActionUIs.Count; ++i)
            {
                var component = s_RebindActionUIs[i];
                var referencedAction = component.actionReference?.action;
                if (referencedAction == null)
                    continue;

                if (referencedAction == action ||
                    referencedAction.actionMap == actionMap ||
                    referencedAction.actionMap?.asset == actionAsset)
                    component.UpdateBindingDisplay();
            }
        }

        [Tooltip("Reference to action that is to be rebound from the UI.")]
        [SerializeField]
        private InputActionReference m_Action;

        [SerializeField]
        private string m_BindingId;

        [SerializeField]
        private InputBinding.DisplayStringOptions m_DisplayStringOptions;

        [Tooltip("Text label that will receive the name of the action. Optional. Set to None to have the "
            + "rebind UI not show a label for the action.")]
        [SerializeField]
        private TMP_Text m_ActionLabel;

        [Tooltip("Text label that will receive the current, formatted binding string.")]
        [SerializeField]
        private TMP_Text m_BindingText;

        [Tooltip("Optional UI that will be shown while a rebind is in progress.")]
        [SerializeField]
        private GameObject m_RebindOverlay;

        [Tooltip("Optional text label that will be updated with prompt for user input.")]
        [SerializeField]
        private TMP_Text m_RebindText;

        [Tooltip("Optional reference to default input actions containing the UI action map. The UI action map is "
            + "disabled when rebinding is in progress.")]
        [SerializeField]
        private InputActionAsset m_DefaultInputActions;
        private InputActionMap m_UIInputActionMap;

        [Tooltip("Event that is triggered when the way the binding is display should be updated. This allows displaying "
            + "bindings in custom ways, e.g. using images instead of text.")]
        [SerializeField]
        private UpdateBindingUIEvent m_UpdateBindingUIEvent;

        [Tooltip("Event that is triggered when an interactive rebind is being initiated. This can be used, for example, "
            + "to implement custom UI behavior while a rebind is in progress. It can also be used to further "
            + "customize the rebind.")]
        [SerializeField]
        private InteractiveRebindEvent m_RebindStartEvent;

        [Tooltip("Event that is triggered when an interactive rebind is complete or has been aborted.")]
        [SerializeField]
        private InteractiveRebindEvent m_RebindStopEvent;

        private InputActionRebindingExtensions.RebindingOperation m_RebindOperation;

        private static List<RebindActionUI> s_RebindActionUIs;

        // We want the label for the action name to update in edit mode, too, so
        // we kick that off from here.
        #if UNITY_EDITOR
        protected void OnValidate()
        {
            UpdateActionLabel();
            UpdateBindingDisplay();
        }

        #endif

        private void Start()
        {
            UpdateActionLabel();
            UpdateBindingDisplay();
        }

        private void Update()
        {
            // 1) Resolve the action and binding index just like in StartInteractiveRebind
            if (!ResolveActionAndBinding(out var action, out var bindingIndex))
                return;

            // 2) Grab the InputBinding object
            var binding = action.bindings[bindingIndex];

            // 3) Check whether it’s supported right now
            bool supported = IsBindingCurrentlySupported(action, bindingIndex);

            // 4) Find a Button component on this GameObject (or a parent, if that’s how you set it up)
            var btns = GetComponentsInChildren<Button>();

            foreach (var btn in btns)
            {
                btn.interactable = supported;
            }
        }

        private void UpdateActionLabel()
        {
            if (m_ActionLabel != null)
            {
                var action = m_Action?.action;
                m_ActionLabel.text = action != null ? action.name : string.Empty;
            }
        }

        [Serializable]
        public class UpdateBindingUIEvent : UnityEvent<RebindActionUI, string, string, string>
        {
        }

        [Serializable]
        public class InteractiveRebindEvent : UnityEvent<RebindActionUI, InputActionRebindingExtensions.RebindingOperation>
        {
        }
    }
}
