﻿using BepInEx;
using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using BepInEx.Configuration;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using BepInEx.Logging;

namespace LCUltrawide;

[BepInPlugin(LCMPluginInfo.PLUGIN_GUID, LCMPluginInfo.PLUGIN_NAME, LCMPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private static ConfigEntry<int> configResW;
    private static ConfigEntry<int> configResH;

    private static ConfigEntry<float> configUIScale;
    private static ConfigEntry<float> configUIAspect;
    internal static ManualLogSource Log;
#pragma warning restore CS8618

    //How often the screen size will be checked in seconds
    private const float aspectUpdateTime = 1.0f;

    private static bool aspectAutoDetect = false;

    //Previous aspect ratio update
    private static float prevAspect = 0f;
    private static float prevTime = 0f;

    //Default Helmet width
    private const float fDefaultHelmetWidth = 0.3628f;

    private void Awake()
    {
        // Plugin startup logic
        Logger.LogInfo($"Plugin {LCMPluginInfo.PLUGIN_GUID} is loaded!");

        configResW = Config.Bind("Resolution Override", "Width", 0, "Horizontal rendering resolution override.\nIf set to 0, the resolution will be automatically adjusted to fit your monitors aspect ratio.\nGame default value: 860");
        configResH = Config.Bind("Resolution Override", "Height", 0, "Vertical rendering resolution override.\nIf set to 0, the original resolution will be used.\nGame default value: 520");

        configUIScale = Config.Bind("UI", "Scale", 1f, "Changes the size of UI elements on the screen.");
        configUIAspect = Config.Bind("UI", "AspectRatio", 0f, "Changes the aspect ratio of the in-game HUD, a higher number makes the HUD wider.\n(0 = auto, 1.33 = 4:3, 1.77 = 16:9, 2.33 = 21:9, 3.55 = 32:9)");

        aspectAutoDetect = configResW.Value <= 0;

        Log = Logger;

        Harmony.CreateAndPatchAll(typeof(Plugin));
    }

    public static void ChangeAspectRatio(float newAspect)
    {
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

        screenTex.Release();
        screenTex.height = configResH.Value > 0 ? configResH.Value : screenTex.height;
        screenTex.width = configResW.Value > 0 ? configResW.Value : Convert.ToInt32(screenTex.height * newAspect);

        //Change terminal camera render texture resolution
        GameObject terminalObject = GameObject.Find("TerminalScript");

        //skipcq: CS-R1136
        if (terminalObject != null && terminalObject.TryGetComponent(out Terminal terminal))
        {
            RenderTexture terminalTexHighRes = terminal.playerScreenTexHighRes;
            terminalTexHighRes.Release();
            terminalTexHighRes.height = configResH.Value > 0 ? configResH.Value : terminalTexHighRes.height;
            terminalTexHighRes.width = configResW.Value > 0 ? configResW.Value : Convert.ToInt32(terminalTexHighRes.height * newAspect);

        }

        Camera? camera = GameNetworkManager.Instance?.localPlayerController?.gameplayCamera;

        if (camera is null)
        {
            Log.LogError("Camera is null, unable to reset aspect");
        }

        camera?.ResetAspect();

        //Correct aspect ratio for camera view
        GameObject panelObject = GameObject.Find("Systems/UI/Canvas/Panel");

        //skipcq: CS-R1136
        if (panelObject != null && panelObject.TryGetComponent(out AspectRatioFitter arf))
        {
            arf.aspectRatio = newAspect;
        }

        //Change UI scale
        GameObject canvasObject = GameObject.Find("Systems/UI/Canvas");

        //skipcq: CS-R1136
        if (canvasObject != null && canvasObject.TryGetComponent(out CanvasScaler canvasScaler))
        {
            float refHeight = 500 / configUIScale.Value;
            float refWidth = refHeight * newAspect;
            canvasScaler.referenceResolution = new Vector2(refWidth, refHeight);
        }

        //Change HUD aspect ratio
        GameObject hudObject = hudManager.HUDContainer;

        //skipcq: CS-R1136
        if (hudObject != null && hudObject.TryGetComponent(out AspectRatioFitter arf2))
        {
            arf2.aspectRatio = configUIAspect.Value > 0 ? configUIAspect.Value : newAspect;
        }

        //Fix stretched HUD elements
        GameObject uiCameraObject = GameObject.Find("Systems/UI/UICamera");

        //skipcq: CS-R1136
        if (uiCameraObject != null && uiCameraObject.TryGetComponent(out Camera uiCamera))
        {
            uiCamera.fieldOfView = Math.Min(106 / (configUIAspect.Value > 0 ? configUIAspect.Value : newAspect), 60);
        }

        //Fix Inventory position
        GameObject inventoryObject = hudManager.Inventory.canvasGroup.gameObject;

        //skipcq: CS-R1136
        if (inventoryObject != null && inventoryObject.TryGetComponent(out RectTransform rectTransform))
        {
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
        }

        //Scale up width of helmet model
        GameObject helmetModel = GameObject.Find("PlayerHUDHelmetModel");

        //skipcq: CS-R1136
        if (helmetModel != null && helmetModel.TryGetComponent<Transform>(out Transform transform))
        {
            Vector3 helmetScale = transform.localScale;
            // Helmet width is good up until an aspect ratio of 2.3~
            helmetScale.x = fDefaultHelmetWidth * Math.Max(newAspect / 2.3f, 1);
            transform.localScale = helmetScale;
        }
    }

    [HarmonyPatch(typeof(HUDManager), "Start")]
    [HarmonyPostfix]
    static void HUDManagerStart(HUDManager __instance)
    {
        if (!aspectAutoDetect)
        {
            ChangeAspectRatio(1.77f);
        }

        prevAspect = 0;
        prevTime = 0;
    }

    [HarmonyPatch(typeof(HUDManager), "Update")]
    [HarmonyPostfix]
    static void HUDManagerUpdate(HUDManager __instance)
    {
        //Check screen aspect ratio and update resolution and UI if it changed
        if (aspectAutoDetect && Time.time > (prevTime + aspectUpdateTime))
        {
            Vector2 canvasSize = __instance.playerScreenTexture.canvas.renderingDisplaySize;
            float currentAspect = canvasSize.x / canvasSize.y;

            //Use approximate equals because '==' operator sometimes causes issues with floating point numbers
            if (!Mathf.Approximately(currentAspect, prevAspect))
            {
                ChangeAspectRatio(currentAspect);
                prevAspect = currentAspect;

                Debug.Log("New Aspect Ratio: " + currentAspect);
            }

            prevTime = Time.time;
        }
    }

    [HarmonyPatch(typeof(HUDManager), "UpdateScanNodes")]
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

}
