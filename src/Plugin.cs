using BepInEx;
using BepInEx.Logging;
using flanne;
using flanne.Core;
using HarmonyLib;
using UnityEngine;

namespace Callmore.MoreUI;

[BepInPlugin(
    PluginInfo.PLUGIN_GUID,
    PluginInfo.PLUGIN_NAME,
    PluginInfo.PLUGIN_VERSION
)]
[BepInProcess("MinutesTillDawn.exe")]
public class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource logger;
    readonly Harmony harmony = new Harmony("Callmore.TestMod");

    public void Awake()
    {
        logger = Logger;

        harmony.PatchAll();

        // Plugin startup logic
        logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
    }

    [HarmonyPatch(typeof(GameController), "Start")]
    class GameControllerStartPatch
    {
        static void Prefix(GameController __instance)
        {
            logger.LogInfo("Spawning stat GUI.");
            GameObject obj = new GameObject("StatGUI");
            StatGUI statComponenet = obj.AddComponent<StatGUI>();
            statComponenet.SetGameControllerTarget(ref __instance);

            obj.transform.SetParent(GameObject.Find("HUDPanel").transform, false);
            obj.transform.localScale = Vector3.one;
            obj.transform.position = Vector3.zero;

            // Set rect transform i guess??
            RectTransform rectTransform = obj.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.anchoredPosition = new Vector2(10, -340);
        }
    }
}
