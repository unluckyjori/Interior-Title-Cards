# Interior Title Cards Mod for Lethal Company

This mod displays a title card when players enter the facility interior area in Lethal Company.

## Features

- Displays a title card when entering the facility ("Entering FACILITY")
- Automatically disappears after 3 seconds

![Image](https://i.imgur.com/HbUlh2O.jpeg)

## Customization

### Debug Settings
- **EnableDebugLogging**: Enable detailed debug logging for troubleshooting.  

### Animation Timing
- **TopTextDisplayDuration**: How long the top text displays on screen in seconds.  
- **InteriorTextDisplayDuration**: How long the interior text displays on screen in seconds.  
- **TopTextFadeInDuration**: How long the top text takes to fade in.  
- **TopTextFadeOutDuration**: How long the top text takes to fade out.  
- **InteriorTextFadeInDuration**: How long the interior text takes to fade in.  
- **InteriorTextFadeOutDuration**: How long the interior text takes to fade out.  
- **TopTextStartDelay**: Delay in seconds before the top text starts displaying after entering.  
- **InteriorTextStartDelay**: Delay in seconds before the interior text starts displaying after entering.

### Interior Name Overrides
- **Interior Name**: Override the interior name


### Text Appearance
- **Top text override**: Custom text displayed above the interior name.  
- **TopTextFontSize**: Font size for the top text.  
- **InteriorTextFontSize**: Font size for the interior name text.  
- **TopTextColor**: Color of the top text in hex format.  
- **InteriorTextColor**: Color of the interior name text in hex format.  
- **TopTextFontWeight**: Font weight for the top text.  
- **InteriorTextFontWeight**: Font weight for the interior name text.
- **TopTextPosition**: Position of the interior text as X,Y coordinates.
- **InteriorTextPosition**: Position of the interior text as X,Y coordinates.

### Visual Effects
- **TopTextFadeEnabled**: Enable fade in/out effect for top text.  
- **InteriorTextFadeEnabled**: Enable fade in/out effect for interior text.  

## Custom Images

This mod supports custom images to replace the text in title cards. Images are automatically loaded based on the current dungeon and can be configured to show in various ways.

### Directory Structure

Custom images should be placed in the following directory structure:
```
BepInEx/plugins/InteriorTitleCardsImages/
├── dev/          # Developer-provided images (priority by default)
│   └── DungeonName/
│       ├── InteriorText/
│       │   └── image.png
│       ├── TopText/
│       │   └── image.png
│       └── Combined/
│           └── image.png
└── user/         # User-made images
    └── DungeonName/
        ├── InteriorText/
        │   └── image.png
        ├── TopText/
        │   └── image.png
        └── Combined/
            └── image.png
```

### Image Types

- **InteriorText**: Replaces the interior name text (e.g., "FACILITY")
- **TopText**: Replaces the top text (e.g., "NOW ENTERING...")
- **Combined**: Replaces both text elements with a single image

### Supported Formats

- PNG (recommended for transparency)
- JPG/JPEG
- BMP
- TGA
- GIF

### File Naming

Images can be named in several ways (checked in order of preference):
1. `image.png` (default)
2. `{DungeonName}.png` (e.g., `Facility.png`)
3. `{DungeonName}.png` (underscores removed, e.g., `HauntedMansion.png`)
4. `titlecard.png`
5. `card.png`

### Configuration Options

#### Image Display Settings
- **EnableCustomImages**: Enable or disable all custom image functionality
- **ImageDisplayType**: Choose how images are displayed
  - Top text image only
  - Interior text image only
  - Both separate images
  - Combined image
- **ImageSourceMode**: Control which image sources to use
  - Developer images only
  - User-made images only
  - Both (developer prioritized)
  - Both (user prioritized)

#### Image Positioning
- **TopImagePosition**: Position of top images as X,Y coordinates
- **InteriorImagePosition**: Position of interior images as X,Y coordinates
- **CombinedImagePosition**: Position of combined images as X,Y coordinates

#### Image Sizing
- **TopTextImageWidth/Height**: Dimensions for top text images
- **InteriorTextImageWidth/Height**: Dimensions for interior text images
- **CombinedImageWidth/Height**: Dimensions for combined images
- Set to 0 to use original image dimensions

#### Image Filtering
- **ImageBlacklist**: Comma-separated list of image paths to exclude

### Adding Images to Mod Packages

When creating mod packages that include custom images, include the following folder structure in your package:

```
plugins/
└── InteriorTitleCardsImages/
    └── (DEV OR USER)/
        └── (InteriorName)/
            └── (InteriorText, TopText, Combined)/
                └── image.png
```

**Folder Guidelines:**
- **DEV**: Use for interior mods (mods that add new interiors/dungeons)
- **USER**: Use for user-made content or mods that modify existing interiors
- **InteriorName**: Use the exact dungeon/interior name as it appears in LethalLevelLoader
- **Image Types**: Choose the appropriate subfolder based on what you want to replace:
  - `InteriorText`: Replaces the interior name (e.g., "FACILITY")
  - `TopText`: Replaces the top text (e.g., "NOW ENTERING...")
  - `Combined`: Replaces both text elements with a single image

## Roadmap

- Add custom font

## Contributing

Feel free to submit issues or pull requests. For major changes, please open an issue first to discuss what you would like to change.

## License

This project is licensed under the MIT License.
