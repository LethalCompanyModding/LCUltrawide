using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LCUltrawide;

[BepInPlugin(LCMPluginInfo.PLUGIN_GUID, LCMPluginInfo.PLUGIN_NAME, LCMPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private static ConfigEntry<float> ConfigUIScale { get; set; } = null!;
    private static ConfigEntry<float> ConfigUIAspect { get; set; } = null!;

    //How often the screen size will be checked in seconds
    private const float aspectUpdateTime = 1.0f;

    //Previous aspect ratio update
    private static float prevAspect = 0f;
    private static float prevTime = 0f;

    //Current aspect ratio stored
    private static float currentAspect = 0f;

    //Default Helmet width
    private const float fDefaultHelmetWidth = 0.3628f;

    private static ManualLogSource Log = null!;

    // Modded In-Game Settings keys & save file path
    private const string GamePixelSaveData = "LCUltrawide_GamePixel";
    private const string TerminalPixelSaveData = "LCUltrawide_TerminalPixel";
    private const string SaveFilePath = "LCUltrawide_SaveData";

    // Modded In-Game Settings Values
    private const int DefaultGamePixelation = 49; // vanilla default at 1080p
    private const int DefaultTerminalPixelation = 46; // vanilla default at 1080p
    private static int GamePixelation = DefaultGamePixelation; 
    private static int TerminalPixelation = DefaultTerminalPixelation; 
    private static int _tempGamePixelation = GamePixelation;
    private static int _tempTerminalPixelation = TerminalPixelation;

    private void Awake()
    {
        // Plugin startup logic
        Logger.LogInfo($"Plugin {LCMPluginInfo.PLUGIN_GUID} is loaded with version {LCMPluginInfo.PLUGIN_VERSION}!");

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
            currentAspect = 0;
            prevAspect = 0;
            prevTime = 0;
        };

        Log = Logger;
        Harmony.CreateAndPatchAll(typeof(Plugin));
    }

    private static void SavePixelationSettings()
    {
        ES3.Save(GamePixelSaveData, GamePixelation, SaveFilePath);
        ES3.Save(TerminalPixelSaveData, TerminalPixelation, SaveFilePath);
    }

    private static void ReadPixelationSettings()
    {
        Log.LogDebug($"Getting pixelation settings from {SaveFilePath}");
        
        // get value from save or create save data
        if (ES3.KeyExists(GamePixelSaveData, SaveFilePath))
            GamePixelation = ES3.Load<int>(GamePixelSaveData, SaveFilePath);
        else
        {
            Log.LogDebug("Creating save data for GamePixelation Setting");
            ES3.Save(GamePixelSaveData, GamePixelation, SaveFilePath);
        }    

        // get value from save or create save data
        if (ES3.KeyExists(TerminalPixelSaveData, SaveFilePath))
            TerminalPixelation = ES3.Load<int>(TerminalPixelSaveData, SaveFilePath);
        else
        {
            Log.LogDebug("Creating save data for TerminalPixelation Setting");
            ES3.Save(TerminalPixelSaveData, TerminalPixelation, SaveFilePath);
        }   

        // update temp values to match current values
        _tempGamePixelation = GamePixelation;
        _tempTerminalPixelation = TerminalPixelation;

        Log.LogDebug($"Loaded Values: GamePixelation={GamePixelation}, TerminalPixelation={TerminalPixelation}");
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

        // update game and terminal resolutions for new aspect ratio while maintaining pixelation setting
        UpdateGameResolution(hudManager, newAspect, GamePixelation);
        UpdateTerminalResolution(hudManager, newAspect, TerminalPixelation);

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

    static void UpdateGameResolution(HUDManager hudManager, float aspect, int pixelValue)
    {
        // HUDManager does not exist in main menus
        if (hudManager == null)
            return;

        if (hudManager.playerScreenTexture.texture is not RenderTexture screenTex)
        {
            Log.LogError("Unable to read player screen texture");
            return;
        }

        Log.LogDebug("Setting hudmanager playerScreenTexture.texture to preferred height & width");
        screenTex.Release();
        decimal screenHeight = Screen.height * GetPixelationValue(pixelValue);
        Log.LogDebug($"screenHeight = {screenHeight}");
        screenTex.height = Math.Clamp((int)screenHeight, 16, SystemInfo.GetMaxRenderTextureSize());
        screenTex.width = Math.Clamp((int)(screenTex.height * aspect), 16, SystemInfo.GetMaxRenderTextureSize());
    }

    static void UpdateTerminalResolution(HUDManager hudManager, float aspect, int pixelValue)
    {
        // HUDManager does not exist in main menus
        if (hudManager == null)
            return;

        if (hudManager.terminalScript == null)
        {
            Log.LogError("Unable to read terminal screen texture");
            return;
        }
            
        RenderTexture terminalTexHighRes = hudManager.terminalScript.playerScreenTexHighRes;
        terminalTexHighRes.Release();
        decimal terminalHeight = Screen.height * GetPixelationValue(pixelValue);
        Log.LogDebug($"terminalHeight = {terminalHeight}");
        terminalTexHighRes.height = Math.Clamp((int)terminalHeight, 2, SystemInfo.GetMaxRenderTextureSize());
        terminalTexHighRes.width = Math.Clamp((int)(terminalTexHighRes.height * aspect), 2, SystemInfo.GetMaxRenderTextureSize());
        Log.LogDebug("Setting Terminal playerScreenTexHighRes to preferred height & width");
    }

    [HarmonyPatch(typeof(IngamePlayerSettings), nameof(IngamePlayerSettings.LoadSettingsFromPrefs))]
    [HarmonyPostfix]
    static void LoadSaveSettings()
    {
        // update our values from save
        ReadPixelationSettings();
    }

    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.Start))]
    [HarmonyPostfix]
    static void HUDManagerStart()
    {
        currentAspect = 0;
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
            currentAspect = canvasSize.x / canvasSize.y;

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

    // Patch ResetSettingsToDefault to set our modded settings' default values
    [HarmonyPatch(typeof(IngamePlayerSettings), nameof(IngamePlayerSettings.ResetSettingsToDefault))]
    [HarmonyPostfix]
    static void ResetToDefault()
    {
        // update slider visuals
        GamePixelSlider?.SetValueWithoutNotify(DefaultGamePixelation);
        TerminalPixelSlider?.SetValueWithoutNotify(DefaultTerminalPixelation);

        // set pixelation values to game defaults
        GamePixelation = DefaultGamePixelation;
        TerminalPixelation = DefaultTerminalPixelation;

        // update temp values to our permanent values
        _tempGamePixelation = GamePixelation;
        _tempTerminalPixelation = TerminalPixelation;

        // update modded save file
        SavePixelationSettings();

        // update resolution with new pixelation values
        if (HUDManager.Instance != null)
        {
            UpdateGameResolution(HUDManager.Instance, currentAspect, DefaultGamePixelation);
            UpdateTerminalResolution(HUDManager.Instance, currentAspect, DefaultTerminalPixelation);
        }
            
    }

    // Patch DiscardChangedSettings to discard our changed modded settings
    [HarmonyPatch(typeof(IngamePlayerSettings), nameof(IngamePlayerSettings.DiscardChangedSettings))]
    [HarmonyPostfix]
    static void ResetDiscardedValues()
    {
        // update slider visual to our permanent values
        GamePixelSlider?.SetValueWithoutNotify(GamePixelation);
        TerminalPixelSlider?.SetValueWithoutNotify(TerminalPixelation);

        // update temp values to our permanent values
        _tempGamePixelation = GamePixelation;
        _tempTerminalPixelation = TerminalPixelation;

        // update resolution pixelation with our permanent values
        if (HUDManager.Instance != null)
        {
            UpdateGameResolution(HUDManager.Instance, currentAspect, GamePixelation);
            UpdateTerminalResolution(HUDManager.Instance, currentAspect, TerminalPixelation);
        }

    }

    // Patch SaveChangedSettings to save our changed modded settings to file
    [HarmonyPatch(typeof(IngamePlayerSettings), nameof(IngamePlayerSettings.SaveChangedSettings))]
    [HarmonyPostfix]
    static void SaveChangedSettings()
    {
        // update permanent values to current temp values
        GamePixelation = _tempGamePixelation;
        TerminalPixelation = _tempTerminalPixelation;

        // save values to file
        SavePixelationSettings();

        // update resolution with new pixelation value
        if (HUDManager.Instance != null)
        {
            UpdateGameResolution(HUDManager.Instance, currentAspect, GamePixelation);
            UpdateTerminalResolution(HUDManager.Instance, currentAspect, TerminalPixelation);
        }

    }

    private static Slider? GamePixelSlider { get; set; }
    private static Slider? TerminalPixelSlider { get; set; }

    // This method is used to replace the vanilla pixel resolution setting with our modded settings
    static void UpdateSettingsMenu(Transform settingspanel)
    {
        if (settingspanel == null)
            return;

        var pixelRes = settingspanel.transform.Find("PixelRes").gameObject;
        var brightness = settingspanel.transform.Find("BrightnessSetting").gameObject; // used as our template for the slider

        // modify existing label to be used for Game Pixelation slider
        var textLabel = pixelRes.transform.Find("Label2");
        textLabel.GetComponent<TextMeshProUGUI>().text = "Game Pixelation:";
        textLabel.GetComponent<RectTransform>().anchoredPosition = new(-6.3f, 32f);

        // create secondary label for Terminal Pixelation slider
        var terminalResLabel = UnityEngine.Object.Instantiate(textLabel.gameObject, pixelRes.transform);
        terminalResLabel.GetComponent<TextMeshProUGUI>().text = "Terminal Pixelation:";
        terminalResLabel.GetComponent<RectTransform>().anchoredPosition = new(-6.3f, -2f);

        // Delete original setting objects/components
        UnityEngine.Object.DestroyImmediate(pixelRes.transform.Find("Label").gameObject);
        UnityEngine.Object.DestroyImmediate(pixelRes.transform.Find("Arrow").gameObject);
        UnityEngine.Object.DestroyImmediate(pixelRes.transform.Find("Template").gameObject);
        UnityEngine.Object.DestroyImmediate(pixelRes.GetComponent<TMP_Dropdown>());
        UnityEngine.Object.DestroyImmediate(pixelRes.GetComponent<Image>());
        UnityEngine.Object.DestroyImmediate(pixelRes.GetComponent<SettingsOption>());

        // default value images are first, so that they are shown behind the copied sliders
        var gameDefault = UnityEngine.Object.Instantiate(brightness.transform.Find("Image"), pixelRes.transform);
        gameDefault.GetComponent<RectTransform>().anchoredPosition = new(-10.8f, 15f); // matches up with slider value of 49 (DefaultGamePixelation)
        var terminalDefault = UnityEngine.Object.Instantiate(brightness.transform.Find("Image"), pixelRes.transform);
        terminalDefault.GetComponent<RectTransform>().anchoredPosition = new(-15f, -18f); // matches up with slider value of 46 (DefaultTerminalPixelation)

        // game pixel setting slider
        var gamePixelSliderObj = UnityEngine.Object.Instantiate(brightness.transform.Find("Slider"), pixelRes.transform);
        gamePixelSliderObj.GetComponent<RectTransform>().anchoredPosition = new(-10f, 15f);
        UnityEngine.Object.DestroyImmediate(gamePixelSliderObj.GetComponent<SettingsOption>());
        GamePixelSlider = gamePixelSliderObj.GetComponent<Slider>();
        GamePixelSlider.minValue = 0; // 0% pixeltaion
        GamePixelSlider.maxValue = 99; // 99% pixelation
        GamePixelSlider.value = GamePixelation;
        GamePixelSlider.onValueChanged.RemoveAllListeners();
        GamePixelSlider.onValueChanged.AddListener(GamePixelSliderChanged);

        // terminal pixel setting slider
        var terminalPixelSliderObj = UnityEngine.Object.Instantiate(brightness.transform.Find("Slider"), pixelRes.transform);
        terminalPixelSliderObj.GetComponent<RectTransform>().anchoredPosition = new(-10f, -18f);
        UnityEngine.Object.DestroyImmediate(terminalPixelSliderObj.GetComponent<SettingsOption>());
        TerminalPixelSlider = terminalPixelSliderObj.GetComponent<Slider>();
        TerminalPixelSlider.minValue = 0; // 0% pixeltaion
        TerminalPixelSlider.maxValue = 99; // 99% pixelation
        TerminalPixelSlider.value = TerminalPixelation;
        TerminalPixelSlider.onValueChanged.RemoveAllListeners();
        TerminalPixelSlider.onValueChanged.AddListener(TerminalPixelSliderChanged);

        // slightly shrink x and y scale to fit everything cleanly
        pixelRes.transform.localScale = new(0.95f, 0.95f, 1f);

        // slight reposition 
        pixelRes.GetComponent<RectTransform>().anchoredPosition = new(33f, 67f);

        // adjust indirect lighting setting a bit to give a cleaner look
        settingspanel.transform.Find("AdvancedLighting").GetComponent<RectTransform>().anchoredPosition = new(26f, 28f);
    }

    // QuickMenuManager patch to replace vanilla pixel res setting for the in-game menus with one that has a slider
    [HarmonyPatch(typeof(QuickMenuManager), nameof(QuickMenuManager.Start))]
    [HarmonyPostfix]
    static void QuickMenuChangePixelSetting(QuickMenuManager __instance)
    {
        var settingspanel = __instance?.settingsPanel?.transform;

        // this probably won't happen, but might as well catch it just in case
        if (settingspanel == null)
        {
            Log.LogDebug("Returning from QuickMenuManager Start with no settings panel");
            return;
        }

        UpdateSettingsMenu(settingspanel);
    }

    // MenuManager patch to replace the vanilla pixel res setting for the main menu with one that has a slider
    [HarmonyPatch(typeof(MenuManager), nameof(MenuManager.Awake))]
    [HarmonyPostfix]
    static void MainMenuChangePixelSetting(MenuManager __instance)
    {
        var settingspanel = __instance?.PleaseConfirmChangesSettingsPanel?.transform?.parent;

        // this can be expected on startup
        if (settingspanel == null)
        {
            Log.LogDebug("Returning from MenuManager Awake with no settings panel");
            return;
        }

        UpdateSettingsMenu(settingspanel);
    }

    static void GamePixelSliderChanged(float value)
    {
        // play audio of setting change
        IngamePlayerSettings.Instance.SettingsAudio.PlayOneShot(GameNetworkManager.Instance.buttonTuneSFX);
        
        // update resolution to temp value
        _tempGamePixelation = (int)value;
        
        UpdateGameResolution(HUDManager.Instance, currentAspect, _tempGamePixelation);

        // show pixelation setting change
        IngamePlayerSettings.Instance.SetQuickMenuBGTransparent();
        // update menu visuals to show changes are not permanent until confirmed
        IngamePlayerSettings.Instance.SetChangesNotAppliedTextVisible();
    }

    static void TerminalPixelSliderChanged(float value)
    {
        // play audio of setting change
        IngamePlayerSettings.Instance.SettingsAudio.PlayOneShot(GameNetworkManager.Instance.buttonTuneSFX);

        // update terminal resolution to temp value
        _tempTerminalPixelation = (int)value;
        
        // no need to bother showing the result of temp value, it's not visible while in this menu
    }

    // Converts our setting value 0-99, into a value we can use directly as a resolution modifier
    // returns value between 1 and 0.01 for Pixelation modifier, 1 = no pixelation and 0.01 = maximum pixelation
    // using decimal instead of float to avoid values such as 0.001
    static decimal GetPixelationValue(int settingVal)
    {
        if (IngamePlayerSettings.Instance == null)
            return 1;

        decimal pixelationValue = settingVal;

        pixelationValue /= 100;
        pixelationValue = 1 - pixelationValue;
        Log.LogDebug($"Pixelation Value is {pixelationValue}");
        return pixelationValue;
    }

}
