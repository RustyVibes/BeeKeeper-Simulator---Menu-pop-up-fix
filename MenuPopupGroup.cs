using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace MainMenuPopupFix
{
    /// <summary>
    /// One popup on the main menu. <see cref="Toggle"/> is set when the menu button that opens it
    /// drives the popup through its onValueChanged event; closing that toggle lets the game hide the
    /// popup and update the button's own visual state.
    /// </summary>
    internal sealed class MenuPopup
    {
        internal GameObject Go;
        internal Toggle Toggle;
        internal bool WasActive;
    }

    /// <summary>
    /// Keeps all popups that are direct children of the main menu panel mutually exclusive.
    ///
    /// The game's own buttons only ever open their own popup: Settings and Join Game are Toggles
    /// wired to SetActive, and Play Game toggles the save-slots panel through UIMainMenu. None of
    /// them closes the others, so two popups can be visible at once. This component watches for a
    /// popup becoming active and closes the rest in the same frame.
    /// </summary>
    internal sealed class MenuPopupGroup : MonoBehaviour
    {
        private readonly List<MenuPopup> _popups = new List<MenuPopup>();
        private bool _initialised;

        internal void Initialise(GameObject root)
        {
            if (_initialised || root == null)
                return;

            _initialised = true;

            var byObject = new Dictionary<GameObject, MenuPopup>();

            // Every direct child whose name contains "Popup": LoadGamePopup, JoinLobbyPopup and
            // SettingsPopup (1) in this version of the menu.
            for (int i = 0; i < root.transform.childCount; i++)
            {
                var child = root.transform.GetChild(i).gameObject;
                if (child.name.IndexOf("popup", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                var popup = new MenuPopup { Go = child, WasActive = child.activeSelf };
                _popups.Add(popup);
                byObject[child] = popup;
            }

            // Find the buttons that drive a popup through a persistent onValueChanged call
            // (Join Game and Settings). The target is the popup GameObject and the method SetActive.
            foreach (var toggle in root.GetComponentsInChildren<Toggle>(true))
            {
                var changed = toggle.onValueChanged;
                int count = changed.GetPersistentEventCount();

                for (int i = 0; i < count; i++)
                {
                    if (!string.Equals(changed.GetPersistentMethodName(i), "SetActive",
                            StringComparison.Ordinal))
                        continue;

                    if (!(changed.GetPersistentTarget(i) is GameObject target))
                        continue;

                    if (byObject.TryGetValue(target, out var popup))
                        popup.Toggle = toggle;
                }
            }

            if (_popups.Count == 0)
            {
                MainMenuPopupFixPlugin.Log.LogWarning(
                    $"Main menu popup group found no popups under '{root.name}'; nothing will be closed.");
                return;
            }

            int toggleCount = 0;
            foreach (var popup in _popups)
            {
                if (popup.Toggle != null)
                    toggleCount++;
            }

            MainMenuPopupFixPlugin.Log.LogInfo(
                $"Main menu popup group ready: {_popups.Count} popup(s), {toggleCount} driven by a toggle.");
        }

        private void LateUpdate()
        {
            try
            {
                if (!_initialised)
                    return;

                // Keep the menu button's selected visual in step with the popup. The popup can be
                // closed by its own Back button without going through the Toggle, which would
                // otherwise leave the button looking selected.
                foreach (var popup in _popups)
                {
                    if (popup.Go == null || popup.Toggle == null)
                        continue;

                    bool active = popup.Go.activeSelf;
                    if (popup.Toggle.isOn != active)
                        popup.Toggle.isOn = active;
                }

                MenuPopup opened = null;

                // Find the popup that just turned on. Popups close by themselves when their button is
                // clicked again, so only the false -> true edge needs handling.
                foreach (var popup in _popups)
                {
                    if (popup.Go == null)
                        continue;

                    bool active = popup.Go.activeSelf;
                    if (active && !popup.WasActive)
                        opened = popup;

                    popup.WasActive = active;
                }

                if (opened == null)
                    return;

                foreach (var other in _popups)
                {
                    if (other == opened || other.Go == null || !other.Go.activeSelf)
                        continue;

                    // A toggle-driven popup is closed through its toggle so the button's selected
                    // visual clears with it. The save-slots panel has no toggle, so it is hidden
                    // directly.
                    if (other.Toggle != null && other.Toggle.isOn)
                        other.Toggle.isOn = false;

                    if (other.Go.activeSelf)
                        other.Go.SetActive(false);

                    other.WasActive = false;

                    MainMenuPopupFixPlugin.Log.LogInfo(
                        $"Closed '{other.Go.name}' because '{opened.Go.name}' opened.");
                }
            }
            catch (Exception e)
            {
                MainMenuPopupFixPlugin.Log.LogError($"Main menu popup update failed: {e}");
            }
        }
    }

    [HarmonyPatch(typeof(UIMainMenu), "Start")]
    internal static class UIMainMenuStartPatch
    {
        private static AccessTools.FieldRef<UIMainMenu, GameObject> _loadGamePopupOf;
        private static bool _fieldResolved;
        private static bool _fieldMissing;

        private static void Postfix(UIMainMenu __instance)
        {
            try
            {
                if (!TryGetLoadGamePopup(__instance, out var loadGamePopup))
                    return;

                var panel = loadGamePopup.transform.parent;
                if (panel == null)
                {
                    MainMenuPopupFixPlugin.Log.LogWarning(
                        "Main menu save-slots popup has no parent panel; popup group not installed.");
                    return;
                }

                if (panel.GetComponent<MenuPopupGroup>() != null)
                    return;

                var group = panel.gameObject.AddComponent<MenuPopupGroup>();
                group.Initialise(panel.gameObject);
            }
            catch (Exception e)
            {
                MainMenuPopupFixPlugin.Log.LogError($"Main menu popup group failed: {e}");
            }
        }

        // Resolved once: a game update that renames the field must warn once and stay disabled.
        // A throwing static initialiser is cached by the runtime and rethrown on every menu load.
        private static bool TryGetLoadGamePopup(UIMainMenu menu, out GameObject popup)
        {
            popup = null;

            if (!_fieldResolved)
            {
                _fieldResolved = true;

                if (AccessTools.Field(typeof(UIMainMenu), "loadGameUIPopup") == null)
                {
                    _fieldMissing = true;
                    MainMenuPopupFixPlugin.Log.LogWarning(
                        "Main menu has no 'loadGameUIPopup' field; popup group not installed.");
                }
                else
                {
                    _loadGamePopupOf =
                        AccessTools.FieldRefAccess<UIMainMenu, GameObject>("loadGameUIPopup");
                }
            }

            if (_fieldMissing || _loadGamePopupOf == null)
                return false;

            popup = _loadGamePopupOf(menu);
            if (popup == null)
            {
                MainMenuPopupFixPlugin.Log.LogWarning(
                    "Main menu has no save-slots popup; popup group not installed.");
                return false;
            }

            return true;
        }
    }
}
