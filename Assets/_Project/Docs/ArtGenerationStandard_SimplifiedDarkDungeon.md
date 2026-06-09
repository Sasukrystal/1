# Art Generation Standard - Simplified Dark Dungeon

Created: 2026-06-05
Stage: 4.2R

## Universal Generation Rules

- Every generated runtime asset must be an independent PNG.
- One image contains exactly one asset.
- Characters, enemies, Bosses, icons, props, VFX, projectiles, and UI icons must use a true transparent background.
- Four corners must be alpha 0.
- No white background.
- No gray background.
- No checkerboard background.
- No baked preview background.
- No opaque rectangle.
- No white halo.
- No background plate.
- No shadow base unless shadow is explicitly the requested asset.
- No environment background.
- No text, labels, file names, paths, numbers, or watermarks.
- No overview image.
- No collage.
- No contact sheet.
- No sprite sheet.
- No preview board.
- Tight crop.
- Minimal transparent margin.
- Subject fills most of the canvas while staying fully visible.
- No cropped body parts, weapon, shield, or effects.
- Suitable for Unity SpriteRenderer or UI Image.

## Style Rules

- Simplified dark dungeon style.
- Top-down slight 3/4 overhead view.
- Hand-painted but simplified.
- Clean silhouette.
- Reduced detail compared with realistic concept art.
- Strong readability at small scale.
- Unified color palette:
  - dark stone
  - muted blue
  - steel gray
  - bone white
  - sickly green
  - ember orange
  - dark purple
- Avoid high-detail portrait illustration.
- Avoid realistic photo.
- Avoid 3D render.
- Avoid pixel art unless explicitly requested later.

## Character Direction Rules

- Do not simulate facing by rotating the entire sprite 360 degrees.
- Use discrete 8-direction visual logic:
  - Right
  - UpRight
  - Up
  - UpLeft
  - Left
  - DownLeft
  - Down
  - DownRight
- First production pass may create:
  - Right
  - UpRight
  - DownRight
  - Up
  - Down
- Left, UpLeft, and DownLeft may be mirrored in Unity from Right, UpRight, and DownRight.
- Any mirrored directions must be documented clearly.

## Production Priority

1. Transparent background correctness.
2. Single asset per PNG.
3. Tight crop and no huge empty border.
4. Character/object consistency.
5. Unity runtime usability.
6. Small-size readability.
7. Artistic polish.

If artistic detail conflicts with transparency, consistency, or Unity runtime usability, remove that detail.

## Import Baseline

Use this baseline for generated character, enemy, Boss, prop, projectile, VFX, and icon PNG files unless a later stage explicitly overrides it:

- Texture Type: Sprite
- Sprite Mode: Single
- Alpha Is Transparency: true
- sRGB: true
- Generate Mip Maps: false
- Filter Mode: Bilinear
- Compression: None
- Pixels Per Unit: 100
- Mesh Type: Tight for characters, enemies, Bosses, props, icons, VFX, and projectiles
- Pivot: Center

UI frames, panels, slots, and bars may use Full Rect and Sprite Border only after explicit UI review.
