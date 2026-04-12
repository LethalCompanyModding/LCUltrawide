# LCUltrawide Changelog

## v1.3.0 (v81 update)
- Updated for v81
	- Ignoring the new Pixelation setting since it has the ability to override this mod's changes.
- Removed the deprecated width config option as well as the config width option.
- Added new Resolution Modifier config options to replace the removed config options and in-game Pixelation setting
- Tidied up references to use less ``GameObject.Find`` in favor of existing cached references and performing transform crawls from them.
- Added several Debugging log messages.
- Updated readme to remove previous maintainers. Added myself (darmuh) although I do not plan to be an especially active maintainer.
- Updated readme with new information regarding configuration options

## v1.2.2

- Adds support for Lethal Config

## v1.2.1

- Completely ignore the width config option now rather than just warn

## v1.2.0

- Deprecated the width config option
- Output a message when ignoring a client's width option
- Updated to the latest game assembly

## v1.1.3

- Added client side tag to thunderstore package
- Downgraded an error to a warning since the method still completes

## v1.1.2

- Compiled against latest game version
- Added minor safety checks for null refs

### 1.1.1

- Fixed UI elements being too far from screen edge by default
- Fixed aspect ratio not applying correctly when rejoining games
- Fixed aspect ratio sometimes updating even if window size has not changed
- Made helmet slightly less intrusive on wider monitors

### 1.1.0

- Auto detect aspect ratio and adjust to window size changes in real time
- Improved UI scaling consistency across different aspect ratios
- Helmet visor model is now scaled with aspect ratio
- Fixed wrong aspect ratio when using the Terminal
- Moved hooks from PlayerControllerB to HUDManager
- Numerous other internal changes to support automatic aspect ratio adjustment

### 1.0.0

- Support for setting a custom resolution and aspect ratio in config file
- Support for changing UI scale and aspect ratio
