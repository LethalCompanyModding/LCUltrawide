using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace LCUltrawide;

[BepInPlugin(LCMPluginInfo.PLUGIN_GUID, LCMPluginInfo.PLUGIN_NAME, LCMPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private static ConfigEntry<float> GameCamResMultiplier { get; set; } = null!;
    private static ConfigEntry<float> TerminalResMultiplier { get; set; } = null!;
    private static ConfigEntry<float> ConfigUIScale { get; set; } = null!;
    private static ConfigEntry<float> ConfigUIAspect { get; set; } = null!;

    //How often the screen size will be checked in seconds
    private const float aspectUpdateTime = 1.0f;

    //Previous aspect ratio update
    private static float prevAspect = 0f;
    private static float prevTime = 0f;

    //Default Helmet width
    private const float fDefaultHelmetWidth = 0.3628f;

    //Default Screen Texture Heights
    //Might require change with game updates
    private const int defaultScreenTexHeight = 520;
    private const int defaultTerminalTexHighResHeight = 580;
    private static ManualLogSource Log = null!;
    
    private void Awake()
    {
        // Plugin startup logic
        Logger.LogInfo($"Plugin {LCMPluginInfo.PLUGIN_GUID} is loaded!");

        GameCamResMultiplier = Config.Bind(
            "Resolution Override",
            "Gameplay Camera Resolution Multiplier", 
            1f,
            new ConfigDescription(
            """
            Use this to up or downscale your game camera rendering resolution.
            The game's default gameplay camera rendering resolution of 860x520 is multiplied by the value in this configuration item.
            Game default value: 1
            WARNING: Increasing the multiplier is more costly on your PC's Hardware. Use with caution!
            """,
            new AcceptableValueRange<float>(0.1f, 4f)));

        TerminalResMultiplier = Config.Bind(
            "Resolution Override", 
            "Terminal Resolution Multiplier", 
            1f,
            new ConfigDescription(
            """
            Use this to up or downscale your terminal rendering resolution.
            The game's default terminal rendering resolution of 960x580 is multiplied by the value in this configuration item.
            Game default value: 1
            WARNING: Increasing the multiplier is more costly on your PC's Hardware. Use with caution!
            """,
            new AcceptableValueRange<float>(0.1f, 4f)));

        ConfigUIScale = Config.Bind(
            "UI", 
            "Scale", 
            1f, 
            new ConfigDescription(
            """
            Changes the size of UI elements on the screen.
            Game default value: 1
            """,
            new AcceptableValueRange<float>(0.5f, 1.5f)));

        ConfigUIAspect = Config.Bind(
            "UI", 
            "AspectRatio", 
            0f, 
            """
            Changes the aspect ratio of the in-game HUD, a higher number makes the HUD wider.
            (0 = auto, 1.33 = 4:3, 1.77 = 16:9, 2.33 = 21:9, 3.55 = 32:9)
            """
            );

        Config.SettingChanged += (s, e) =>
        {
            //Update resolution and UI
            prevAspect = 0;
            prevTime = 0;
        };

        Log = Logger;
        Harmony.CreateAndPatchAll(typeof(Plugin));
    }

    public static void ChangeAspectRatio(float newAspect)
    {
        Log.LogDebug($"ChangeAspectRatio - {newAspect}");
        HUDManager hudManager = HUDManager.Instance;

        //Change camera render texture resolution

        if (hudManager is null)
        {
            Log.LogError("Unable to access hudManager");
            return;
        }

        if (hudManager.playerScreenTexture.texture is not RenderTexture screenTex)
        {
            Log.LogError("Unable to read player screen texture");
            return;
        }

        Log.LogDebug("Setting hudmanager playerScreenTexture.texture to preferred height & width");
        screenTex.Release();
        screenTex.height = Convert.ToInt32(defaultScreenTexHeight * GameCamResMultiplier.Value);
        screenTex.width = Convert.ToInt32(screenTex.height * newAspect);

        // terminal resolution
        if (hudManager.terminalScript != null)
        {
            RenderTexture terminalTexHighRes = hudManager.terminalScript.playerScreenTexHighRes;
            terminalTexHighRes.Release();
            terminalTexHighRes.height = Convert.ToInt32(defaultTerminalTexHighResHeight * TerminalResMultiplier.Value);
            terminalTexHighRes.width = Convert.ToInt32(terminalTexHighRes.height * newAspect);
            Log.LogDebug("Setting Terminal playerScreenTexHighRes to preferred height & width");
        }

        Camera? camera = GameNetworkManager.Instance?.localPlayerController?.gameplayCamera;

        if (camera is null)
            Log.LogWarning("Unable to acquire Game Camera, not resetting aspect ratio");
        else
            Log.LogDebug("Resetting gameplayCamera aspect");

        camera?.ResetAspect();

        //Correct aspect ratio for camera view
        Transform? panelTransform = hudManager.playerScreenTexture.transform.parent?.parent;

        //skipcq: CS-R1136
        if (panelTransform != null && panelTransform.TryGetComponent(out AspectRatioFitter arf))
        {
            arf.aspectRatio = newAspect;
            Log.LogDebug($"Updating UI/Canvas/Panel AspectRatioFitter aspectRatio to {newAspect}");
        }

        //Change UI scale
        Transform? canvasTransform = panelTransform?.parent;

        //skipcq: CS-R1136
        if (canvasTransform != null && canvasTransform.gameObject.TryGetComponent(out CanvasScaler canvasScaler))
        {
            float refHeight = 500 / ConfigUIScale.Value;
            float refWidth = refHeight * newAspect;
            canvasScaler.referenceResolution = new Vector2(refWidth, refHeight);
            Log.LogDebug("Updating UI/Canvas CanvasScaler to preferred height/width");
        }

        //Change HUD aspect ratio
        GameObject? hudObject = hudManager.HUDContainer;

        //skipcq: CS-R1136
        if (hudObject != null && hudObject.TryGetComponent(out AspectRatioFitter arf2))
        {
            arf2.aspectRatio = ConfigUIAspect.Value > 0 ? ConfigUIAspect.Value : newAspect;
            Log.LogDebug("Updating HUDManager HUDContainer AspectRatioFitter to preferred height/width");
        }

        //Fix stretched HUD elements
        GameObject? uiCameraObject = hudManager.UICamera?.gameObject;

        //skipcq: CS-R1136
        if (uiCameraObject != null && uiCameraObject.TryGetComponent(out Camera uiCamera))
        {
            uiCamera.fieldOfView = Math.Min(106 / (ConfigUIAspect.Value > 0 ? ConfigUIAspect.Value : newAspect), 60);
            Log.LogDebug("Updating Systems/UI/UICamera field of view to fix stretched HUD elements");
        }

        //Fix Inventory position
        GameObject? inventoryObject = hudManager.Inventory.canvasGroup.gameObject;

        //skipcq: CS-R1136
        if (inventoryObject != null && inventoryObject.TryGetComponent(out RectTransform rectTransform))
        {
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            Log.LogDebug("Updating HUDManager Inventory position for preferred resolution");
        }

        //Scale up width of helmet model
        Transform? helmetTransform = hudManager.helmetGoop.transform.parent?.parent;

        //skipcq: CS-R1136
        if (helmetTransform != null)
        {
            Vector3 helmetScale = helmetTransform.localScale;
            // Helmet width is good up until an aspect ratio of 2.3~
            helmetScale.x = fDefaultHelmetWidth * Math.Max(newAspect / 2.3f, 1);
            helmetTransform.localScale = helmetScale;
            Log.LogDebug("Updating PlayerHUDHelmetModel transform scale width for preferred resolution");
        }
    }

    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.Start))]
    [HarmonyPostfix]
    static void HUDManagerStart()
    {
        prevAspect = 0;
        prevTime = 0;
    }

    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.Update))]
    [HarmonyPostfix]
    static void HUDManagerUpdate(HUDManager __instance)
    {
        //Check screen aspect ratio and update resolution and UI if it changed
        if (Time.time > (prevTime + aspectUpdateTime))
        {
            Vector2 canvasSize = __instance.playerScreenTexture.canvas.renderingDisplaySize;
            float currentAspect = canvasSize.x / canvasSize.y;

            //Use approximate equals because '==' operator sometimes causes issues with floating point numbers
            if (!Mathf.Approximately(currentAspect, prevAspect))
            {
                ChangeAspectRatio(currentAspect);
                prevAspect = currentAspect;

                Log.LogDebug("New Aspect Ratio: " + currentAspect);
            }

            prevTime = Time.time;
        }
    }

    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.UpdateScanNodes))]
    [HarmonyPostfix]
    static void HUDManagerUpdateScanNodes(PlayerControllerB playerScript, HUDManager __instance, Dictionary<RectTransform, ScanNodeProperties> ___scanNodes)
    {
        //Correct UI marker positions for scanned objects
        RectTransform[] scanElements = __instance.scanElements;

        GameObject playerScreen = __instance.playerScreenTexture.gameObject;
        if (!playerScreen.TryGetComponent(out RectTransform screenTransform))
        {
            return;
        }
        Rect rect = screenTransform.rect;

        for (int i = 0; i < scanElements.Length; i++)
        {
            if (___scanNodes.TryGetValue(scanElements[i], out ScanNodeProperties scanNode))
            {
                Vector3 viewportPos = playerScript.gameplayCamera.WorldToViewportPoint(scanNode.transform.position);
                scanElements[i].anchoredPosition = new Vector2(rect.xMin + rect.width * viewportPos.x, rect.yMin + rect.height * viewportPos.y);
            }
        }
    }

    // This patch ignores applying the pixelresolution setting that was added in v80
    [HarmonyPatch(typeof(IngamePlayerSettings), nameof(IngamePlayerSettings.SetPixelResolution))]
    [HarmonyPrefix]
    static bool SettingsPixelRes(int value)
    {
        Log.LogDebug($"NOT setting PixelResolution to [{value}]");
        return false;
    }

}
