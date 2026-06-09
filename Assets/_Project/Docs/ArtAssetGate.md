# Art Asset Gate

Created: 2026-06-05
Stage: 4.1

## Purpose

This gate defines which art assets may enter formal runtime integration. It exists because the Stage 4.0 transparency audit showed that many legacy `Art2D` assets contain baked white backgrounds or no usable alpha channel. Those assets can be useful as references or temporary prototypes, but they must not be treated as production-ready runtime sprites.

## Source Directories

- `Assets/_Project/ArtDrops/Incoming`: new art drops awaiting inspection.
- `Assets/_Project/ArtDrops/Verified`: art assets that passed gate checks and may be used by later Prefab, Animator, UI, or Scene work.
- `Assets/_Project/ArtDrops/Rejected`: art assets that failed gate checks or are not suitable for runtime use.
- `Assets/_Project/Docs/ArtDropReports`: per-drop inspection reports.

## Hard Blocks

- Do not directly use baked white background assets in formal Prefabs, Animators, UI, or scenes.
- Do not directly use screenshots, preview images, overview images, contact sheets, or sprite sheets that were not explicitly authored and sliced for runtime use.
- Do not put no-alpha player, Boss, enemy, projectile, VFX, or UI icon images directly into Prefabs or scenes.
- Do not use legacy `Assets/Resources/Art2D` white-background assets as formal runtime sprites unless they are first replaced by verified transparent art.
- Do not use auto white-removal outputs as production art unless a human approves the result in a report.

## Allowed Exceptions

- Floor tiles may have no alpha if they are explicitly marked as tile assets.
- Opaque wall, floor, or background tile assets may be accepted only when the report marks them as intentional opaque environment art.
- Prototype-only transparent salvage outputs must remain in test or prototype folders and must not be promoted into formal runtime integration without review.

## Runtime Usage Rules

- World objects must use `SpriteRenderer`.
- UI objects must use `UnityEngine.UI.Image`.
- UI images must have correct `Image Type`, `Preserve Aspect`, `RectTransform`, and `Sliced` settings when a nine-sliced frame is required.
- UI icons must not use sliced borders.
- UI frames, panels, buttons, slots, and bars may use sliced settings only when the sprite border is intentionally authored and verified.
- SpriteRenderer assets must use the correct Sorting Layer and order for their runtime role.

## Alpha And Image Quality Checks

Every new art asset must pass these checks before formal integration:

- The file is not a screenshot.
- The file is not a preview image.
- The file is not an overview image.
- The file is not a contact sheet.
- The file is not a baked checkerboard image.
- The file is not a baked white background image.
- Runtime sprites that need transparency have a real alpha channel.
- Transparent corners are actually transparent when required.
- The image does not contain large unused transparent margins unless the margin is intentional and documented.
- The image does not contain visible white halos on a dark background.
- The image is not visibly overcropped or damaged.
- The intended runtime use is documented.

## Verification Requirements

- All new assets must first enter `Assets/_Project/ArtDrops/Incoming`.
- Codex may only use assets from `Assets/_Project/ArtDrops/Verified` for later formal integration.
- Every drop must receive a report under `Assets/_Project/Docs/ArtDropReports`.
- Every formal integration must have a GameView screenshot or report proving the actual visible result.
- Visual validation must happen in an independent test scene before modifying gameplay scenes.
- `Main.unity` must not be used as an art experiment scene.

## Player Direction Rules

- Do not rotate the full player sprite continuously through 360 degrees to simulate aiming or facing.
- Player characters must use an 8-direction discrete facing system.
- Required directions are `Up`, `Down`, `Left`, `Right`, `UpRight`, `DownRight`, `UpLeft`, and `DownLeft`.
- If full art does not exist for all eight directions, the report must state which directions are real art and which directions are mirrored, reused, or approximate.
- Missing direction art must be recorded as a resource gap, not hidden by continuous rotation.

## Stage 4.1 Decision

Legacy white-background `Art2D` resources are blocked from formal runtime integration. The next approved art step should import a very small Warrior vertical slice using truly transparent PNG files placed in `Assets/_Project/ArtDrops/Incoming`, inspected, and promoted to `Assets/_Project/ArtDrops/Verified` only after passing this gate.
