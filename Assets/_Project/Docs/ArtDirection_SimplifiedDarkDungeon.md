# Art Direction Decision

Created: 2026-06-05
Stage: 4.2R

## Decision

The project art direction is reset from high-detail dark fantasy painted character art to:

**Simplified Dark Dungeon Top-Down Style**

The goal is not single-image concept-art detail. The goal is a shippable Unity 2D game art pipeline that prioritizes:

- Unity-ready runtime assets.
- True transparent PNG files.
- Small-size readability.
- Maintainable character actions and facing directions.
- Consistent style across player characters, enemies, Bosses, UI, and environment.
- No baked white backgrounds, white halos, checkerboard backgrounds, collages, overview sheets, or pasted-image feeling.
- Faster progress toward a complete playable game.

## Problems With The Previous Direction

- High-detail character images often generated baked white, gray, or preview backgrounds.
- Automatic white-background removal is prototype-only; it leaves edge halos and is not reliable enough for production assets.
- Large painted character images are hard to scale consistently in Unity.
- Full multi-direction animation sets are too expensive to maintain when each frame is high-detail.
- Codex cannot turn unsuitable PNG files into production art by placing them into Unity.
- New art must be generated correctly first, then admitted through the Art Asset Gate before formal Unity integration.

## New Style Definition

**Simplified Dark Dungeon Top-Down Style** means:

- Unity 2D top-down.
- Slight 3/4 overhead view.
- Simplified dark dungeon style.
- Dark fantasy but readable.
- Clean silhouette.
- Compact body shape.
- Low-to-mid detail.
- Strong profession identity.
- No realistic painting complexity.
- No poster illustration.
- No concept art page.
- No background.
- True transparent PNG.
- Tight crop.
- Small transparent margin.
- One asset per image.
- Suitable for Unity SpriteRenderer or UI Image.

## Player Character Rules

- Player characters should be simplified but professionally readable.
- Do not use a complex realistic illustration style.
- Direction, weapon, and action must remain readable at small gameplay size.
- Warrior: blue-gray armor, round shield, one-handed sword.
- Archer: green cloak, light armor, longbow.
- Mage: blue-purple robe, staff, small magic spark accents.
- Character proportions may be slightly game-like, but not extreme chibi and not pixel-art mosaic.
- Each character must be practical for discrete 8-direction facing.

## Enemy Rules

- Clear silhouettes.
- Function readable at a glance.
- Few but effective animation frames.
- Strong color blocks.
- No excessive fine detail.

## Boss Rules

- Large clear silhouette.
- Concentrated signature features.
- Phase changes may use glow, cracks, color intensity, and light particles.
- Do not use poster-style Boss illustrations as runtime sprites.

## UI Rules

- Simple, dark, readable.
- Avoid large complex illustrated backgrounds.
- Frames must be nine-slice friendly when needed.
- Icons must remain readable at 64x64.

## Environment Rules

- Use modular Tiles, Props, Doors, and Interactables.
- Do not paste large background images into scenes as the main environment.
- Floors, walls, doors, braziers, chests, pillars, and altars should be produced and integrated separately.
- Scene quality should come from modular composition, not one large background image.

## Pipeline Decision

Legacy white-background `Assets/Resources/Art2D` assets remain blocked from formal runtime integration. Future art should enter:

`Assets/_Project/ArtDrops/Incoming`

Only assets that pass inspection may move to:

`Assets/_Project/ArtDrops/Verified`

Codex may only perform formal Prefab, Animator, UI, or Scene integration using assets from `Verified`.
