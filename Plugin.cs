using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MainMenuPopupFix
{
    /// <summary>
    /// Standalone plugin: makes the main menu's popups (Save Slots, Settings, Join Game) mutually
    /// exclusive, so opening one closes the previously opened one instead of stacking them.
    /// </summary>
    [BepInPlugin(Guid, "Main Menu Popup Fix", Version)]
    public class MainMenuPopupFixPlugin : BaseUnityPlugin
    {
        public const string Guid = "com.rustyvibes.beekeepersim.mainmenupopupfix";
        public const string Version = "1.0.1";

        internal static ManualLogSource Log;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            _harmony = new Harmony(Guid);
            try
            {
                _harmony.PatchAll(typeof(MainMenuPopupFixPlugin).Assembly);
                Log.LogInfo(
                    $"Main Menu Popup Fix {Version} loading — Unity {Application.unityVersion}, game v{Application.version}.");
            }
            catch (Exception e)
            {
                Log.LogError($"Main menu popup patch failed: {e}");
            }
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
