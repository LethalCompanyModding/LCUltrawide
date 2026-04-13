# LCUltrawide

Lethal Company is locked to 16:9 aspect ratio by default and will add black bars on the sides of ultrawide monitors.  
This mod makes some changes to the games rendering and UI to enable support for any custom resolution and aspect ratio.

*NOTE: The "Pixelation" setting added in v80 is ignored in favor of this mod's resolution modifiers. (see Optional Configurations section)*

## Maintainers

**Original Author**: [Stefan750](https://github.com/stefan750/LCUltrawide)

**Current Maintainer(s)**: [darmuh](https://github.com/darmuh), *You?*

This mod has been adopted by the [Lethal Company Modding community repo](https://github.com/LethalCompanyModding/LCUltrawide) and may be maintained by any willing community member with a github account.

## Features

- Automatically detects monitor aspect ratio and scales the game to fill the entire screen (works even in window mode!)
- Increase the games rendering resolution values for better visibility (at the cost of performance)
- Decrease the games rendering resolution values for better performance (at the cost of visibility)
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
 - [Resolution Override] Gameplay Camera Resolution Multiplier: Up or Downscale your gameplay camera resolution with this modifier.
	- The default gameplay camera rendering resolution of ``860x520`` is multiplied by the value in this configuration item.
	- The rendering resolution is also modified to fit your monitor's aspect ratio.
	- WARNING: Increasing the multiplier is more costly on your PC's Hardware. Use with caution!
 - [Resolution Override] Terminal Resolution Multiplier: Up or Downscale your terminal camera resolution with this modifier.
	- The default terminal camera rendering resolution of ``960x580`` is multiplied by the value in this configuration item.
	- The rendering resolution is also modified to fit your monitor's aspect ratio.
	- WARNING: Increasing the multiplier is more costly on your PC's Hardware. Use with caution! 
 - [UI] Scale: Use this configuration item to modify the scale of UI elements on the screen.
 - [UI] Aspect Ratio: Use this configuration item to set the aspect ratio for the in-game HUD.
	- A higher number makes the HUD wider.
	- (0 = auto, 1.33 = 4:3, 1.77 = 16:9, 2.33 = 21:9, 3.55 = 32:9)

### Compatibility
Supports configuration in-game via [Lethal Config](https://thunderstore.io/c/lethal-company/p/AinaVT/LethalConfig/) when it is present.    
