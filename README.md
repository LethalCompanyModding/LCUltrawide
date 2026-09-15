# LCUltrawide

Lethal Company is locked to 16:9 aspect ratio by default and will add black bars on the sides of ultrawide monitors.  
This mod makes some changes to the games rendering and UI to enable support for any custom resolution and aspect ratio.

## Maintainers

**Original Author**: [Stefan750](https://github.com/stefan750/LCUltrawide)

**Current Maintainer(s)**: [darmuh](https://github.com/darmuh), *You?*

This mod has been adopted by the [Lethal Company Modding community repo](https://github.com/LethalCompanyModding/LCUltrawide) and may be maintained by any willing community member with a github account.

## Features

- Automatically detects monitor aspect ratio and scales the game to fill the entire screen (works even in window mode!)  
- Pixelation Resolution sliders are provided as replacements for the Pixelation Resolution setting.  
- Allows changing of HUD scale and aspect ratio  
- Fixes the inventory slots being slightly misaligned on some monitors  
- Fixes the UI being slightly too large on wider monitors  
- More robust code for the Scanner HUD to ensure correct position of markers  

## Installation

1. Make sure you have [BepInEx](https://thunderstore.io/c/lethal-company/p/BepInEx/BepInExPack/) installed for the game  
2. Download the latest version of the mod from [Thunderstore](https://thunderstore.io/c/lethal-company/p/stefan750/LCUltrawide/) or [GitHub releases](https://github.com/LethalCompanyModding/LCUltrawide/releases/latest)  
3. Navigate to the games install folder (you can right click the game in your Steam library, select "Manage" and then "Browse Local Files" to easily find it)  
4. Copy the BepInEx folder from the downloaded .zip into your game folder making sure the contents end up in the already existing folders  

## Usage  

### Ultrawide Resolution  
By default the mod will take the original game resolution and automatically scale it to fit your monitor.  

### Optional Configurations  
You can customize this mod's settings in it's config file, ``BepInEx/config/LCUltrawide.cfg``  
 - Pixleation Resolution: This mod replaces the base-game Pixelation Resolution in-game setting with it's own modded settings.  
	- The base-game setting (dropdown) is replaced with two new sliders.  
	- The modded settings allow you to completely remove all pixelation and also scale with your monitor resolution.  
	- All the way to the left is 0 pixelation, all the way to the right is maximum pixelation.  
	- Game Pixelation Slider: Changes the player camera's pixelation amount when not in the menus.  
	- Terminal Pixelation Slider: Changes the player camera's pixelation amount when interacting with the terminal.  
	- The settings are set & saved using the game's save system and are persistent between modded profiles so long as the mod is installed.  
	- Furthermore, these modded settings do not contaminate the existing vanilla pixel resolution setting value in your game settings.  
		- Disabling/Enabling the mod will not cause any issues.  
	- The default values for these settings are meant to match the vanilla values for a 1080p monitor.  
 - [UI] Scale: Use this configuration item to modify the scale of UI elements on the screen.  
 - [UI] Aspect Ratio: Use this configuration item to set the aspect ratio for the in-game HUD.  
	- A higher number makes the HUD wider.  
	- (0 = auto, 1.33 = 4:3, 1.77 = 16:9, 2.33 = 21:9, 3.55 = 32:9)  

### Compatibility  
Supports configuration in-game via [Lethal Config](https://thunderstore.io/c/lethal-company/p/AinaVT/LethalConfig/) when it is present.    
