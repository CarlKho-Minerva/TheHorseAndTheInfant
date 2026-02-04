# itch.io WebGL Build Instructions for "The Horse and The Infant"

## STEP 1: Build Settings in Unity

1. **Open Build Settings**: File → Build Settings (Cmd+Shift+B)

2. **Switch to WebGL**:
   - Select "WebGL" in the Platform list
   - Click "Switch Platform" (this may take a few minutes)

3. **Player Settings** (click the button or Edit → Project Settings → Player):

   **Resolution and Presentation:**
   - Default Canvas Width: `960`
   - Default Canvas Height: `540` (16:9 aspect ratio)
   - Run In Background: ✓ Enabled

   **WebGL Template:**
   - Select "ItchIO" (our custom template with CRT frame)

   **Other Settings:**
   - Color Space: Linear (for better visuals)
   - Auto Graphics API: ✓

   **Publishing Settings:**
   - Compression Format: Gzip (best compatibility)
   - Decompression Fallback: ✓ Enabled

4. **Build**:
   - Click "Build"
   - Create a folder called "WebGL_Build"
   - Wait for build to complete

## STEP 2: Upload to itch.io

1. Go to https://itch.io/game/new

2. **Basic Info:**
   - Title: "The Horse and The Infant"
   - Project URL: choose a URL slug
   - Classification: Game
   - Kind of project: HTML

3. **Uploads:**
   - Click "Upload files"
   - Upload the entire WebGL_Build folder as a ZIP
   - Mark it as "This file will be played in the browser"

4. **Embed Options:**
   - Viewport dimensions: 960 x 540
   - ✓ Fullscreen button
   - ✓ Enable scrollbars
   - Mobile friendly: No (WebGL)

5. **Details:**
   - Add your description, screenshots, etc.
   - Genre: Action, Indie, Narrative
   - Tags: pixel-art, retro, crt, medieval, slime

## STEP 3: Test

1. Save your itch.io page
2. Click "View page" to test
3. The game should maintain 16:9 aspect ratio with letterboxing/pillarboxing
4. CRT scanlines will be visible from the in-game PixelationEffect

## Troubleshooting

**Black screen?**
- Make sure "Decompression Fallback" is enabled in Player Settings

**Aspect ratio wrong?**
- The template handles this automatically, but you can adjust TARGET_ASPECT in index.html

**Performance issues?**
- Lower the targetWidth in PixelationEffect (try 320 instead of 480)
- Disable some post-processing effects

## Quick Build Command (Terminal)

You can also build from command line:
```bash
/Applications/Unity/Hub/Editor/[VERSION]/Unity.app/Contents/MacOS/Unity \
  -quit -batchmode \
  -projectPath "/Users/cvk/TheHorseAndTheInfant" \
  -buildTarget WebGL \
  -executeMethod BuildScript.BuildWebGL
```

---
Good luck with the submission! 🎮
