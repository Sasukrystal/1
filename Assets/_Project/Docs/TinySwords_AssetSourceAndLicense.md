# Tiny Swords Asset Source And License

Created: 2026-06-05
Stage: 4.2T

## Source And License Record

- Asset name: Tiny Swords
- Author: Pixel Frog
- Source: itch.io Tiny Swords download page
- User-confirmed usage: personal and commercial projects allowed
- Modifications allowed: yes
- Credit: not required but welcome
- Redistribution restriction: do not redistribute, resell, or repackage the asset files themselves
- Project usage note: this Unity project may use the art in the game, but must not redistribute the raw asset pack as a standalone asset package
- Recommended credit if attribution is added later: Tiny Swords by Pixel Frog

## Stage 4.2T Audit Scope

Audited paths:

- `Assets/Tiny Swords/`
- `Assets/Tiny Swords.rar`

This stage only audits the files. It does not move, delete, slice, import, or modify any Tiny Swords asset.

## File Counts

- Total files in audited scope, including `Assets/Tiny Swords.rar`: 754
- PNG files: 607
- Aseprite files: 54
- Zip files: 4
- Rar files: 1
- Unity `.meta` files currently present in `Assets/Tiny Swords`: 0
- Other files, mostly `.DS_Store`: 88
- Directories under `Assets/Tiny Swords`: 143
- Nested compressed packages exist: yes
  - `Assets/Tiny Swords.rar`
  - `Assets/Tiny Swords/Tiny Swords/Tiny RPG Character Asset Pack -Demo Soldier_Orc.zip`
  - `Assets/Tiny Swords/Tiny Swords/Tiny RPG Character Asset Pack v1.02 -Free Soldier_Orc.zip`
  - `Assets/Tiny Swords/Tiny Swords/Tiny RPG Character Asset Pack v1.03b -Free Soldier_Orc.zip`
  - `Assets/Tiny Swords/Tiny Swords/Tiny Swords01/Factions/Knights/Troops/Archer/Archer + Bow.zip`

## Main Directory Structure

- `Assets/Tiny Swords/Tiny Swords/Tiny Swords01`
  - `Deco`
  - `Effects`
  - `Factions/Goblins`
  - `Factions/Knights`
  - `Resources`
  - `Terrain`
  - `UI`
- `Assets/Tiny Swords/Tiny Swords/Tiny Swords02`
  - `Buildings`
  - `Particle FX`
  - `Terrain`
  - `UI Elements`
  - `Units`

## Classification Summary

Approximate file classification by path and file name:

- Warrior / Human Units: 116
- Archer: 51
- Mage / Monk / Caster: 23
- Enemies: 48
- Buildings: 67
- Environment and Terrain: 136
- UI: 176
- VFX: 15
- Unknown or source/control files: 121

Notes:

- `Tiny Swords01` includes Knights and Goblins faction assets, terrain, UI, VFX, resources, and decorative props.
- `Tiny Swords02` includes units by color faction, buildings, terrain tilesets, particle FX, and UI elements.
- Many unknown files are `.DS_Store` or source/support files and should not enter runtime integration.

## Warrior Asset Audit

### Primary Recommended Warrior Source

`Assets/Tiny Swords/Tiny Swords/Tiny Swords01/Factions/Knights/Troops/Warrior/Blue/Warrior_Blue.png`

- Type: sprite sheet
- Size: 1152x1536
- Likely grid: 6 columns x 8 rows, 192x192 cells
- Approximate frame count: 48 cells
- Alpha: RGBA with transparent corners
- Baked white background: no
- Baked gray background: no
- Baked checkerboard background: no
- Existing Unity `.meta` slicing: no
- Suitability: best candidate for Player Warrior because it visually contains multi-direction Warrior poses in one sheet
- Direction support: visually appears to support multiple directions, likely including side, down-facing, up-facing, and diagonal-like poses
- Action support: visually appears to include idle/ready and attack poses; run/walk interpretation requires manual frame review after slicing
- Hit/Hurt: not clearly present
- Death: not clearly present in this sheet
- Block/Shield: shield is present in the Warrior design; a dedicated block animation is not clearly isolated in this sheet
- Dodge/Roll: not clearly present

### Secondary Warrior Source

`Assets/Tiny Swords/Tiny Swords/Tiny Swords02/Units/Blue Units/Warrior/`

Files:

- `Warrior_Idle.png` | 1536x192 | 8 frames
- `Warrior_Run.png` | 1152x192 | 6 frames
- `Warrior_Attack1.png` | 768x192 | 4 frames
- `Warrior_Attack2.png` | 768x192 | 4 frames
- `Warrior_Guard.png` | 1152x192 | 6 frames

Properties:

- Type: horizontal sprite sheets
- Alpha: RGBA with transparent corners
- Baked white background: no
- Baked gray background: no
- Baked checkerboard background: no
- Existing Unity `.meta` slicing: no
- Suitability: good for a side/right-facing Warrior prototype and action validation
- Direction support: mostly one visual side/right direction; left can be mirrored
- Idle: yes
- Run/Walk: yes
- Attack: yes, two attack variants
- Hit/Hurt: not found in Warrior folder
- Death: not found in Warrior folder
- Block/Shield: yes, `Warrior_Guard.png`
- Dodge/Roll: not found in Warrior folder

### Directional Reference Source

`Assets/Tiny Swords/Tiny Swords/Tiny Swords02/Units/Blue Units/Lancer/`

This is not the Warrior, but it is useful as a direction system reference because it includes named directional sheets:

- `Lancer_Down_Attack.png`
- `Lancer_Down_Defence.png`
- `Lancer_DownRight_Attack.png`
- `Lancer_DownRight_Defence.png`
- `Lancer_Right_Attack.png`
- `Lancer_Right_Defence.png`
- `Lancer_Up_Attack.png`
- `Lancer_Up_Defence.png`
- `Lancer_UpRight_Attack.png`
- `Lancer_UpRight_Defence.png`
- `Lancer_Idle.png`
- `Lancer_Run.png`

Lancer should not replace Warrior without approval, but it shows how Tiny Swords encodes multi-direction unit assets.

## PNG Alpha Sampling

Sampled files:

- `Tiny Swords02/Units/Blue Units/Warrior/Warrior_Idle.png`
- `Tiny Swords02/Units/Blue Units/Warrior/Warrior_Run.png`
- `Tiny Swords02/Units/Blue Units/Warrior/Warrior_Attack1.png`
- `Tiny Swords02/Units/Blue Units/Warrior/Warrior_Attack2.png`
- `Tiny Swords02/Units/Blue Units/Warrior/Warrior_Guard.png`
- `Tiny Swords01/Factions/Knights/Troops/Warrior/Blue/Warrior_Blue.png`
- `Tiny Swords02/Terrain/Tileset/Tilemap_color1.png`
- `Tiny Swords02/Particle FX/Fire_01.png`
- `Tiny Swords01/Effects/Fire/Fire.png`

Results:

- All sampled PNG files have alpha channels.
- All sampled PNG files have transparent corners.
- No sampled PNG showed baked white, gray, or checkerboard background at the corners.
- Warrior, unit, VFX, and UI assets are generally suitable for SpriteRenderer or UI Image after proper slicing/import.
- Terrain tileset assets are suitable for Tilemap or SpriteRenderer workflows after proper slicing/import.
- Most unit PNGs are sprite sheets, not single-frame sprites, and require explicit Unity slicing before animation work.

## Handling Recommendations For Source And Archive Files

- Do not directly submit `.rar`, `.zip`, or `.aseprite` source files as part of a formal game runtime resource commit unless explicitly approved.
- Keep compressed packages and Aseprite sources outside formal runtime integration.
- Recommended later cleanup: move `Assets/Tiny Swords.rar`, nested `.zip` files, `.aseprite` files, and `.DS_Store` files outside Unity `Assets`, or add a targeted `.gitignore` rule after user approval.
- Do not redistribute, resell, or repackage Tiny Swords raw asset files.
- Use only curated runtime PNGs and generated Unity metadata in the actual game integration commits.

## Stage 4.2T Decision

Tiny Swords is suitable as the new simplified art base for this Unity project.

Recommended Warrior direction:

1. Use `Tiny Swords01/Factions/Knights/Troops/Warrior/Blue/Warrior_Blue.png` as the primary candidate for a multi-direction Player Warrior slice.
2. Use `Tiny Swords02/Units/Blue Units/Warrior/` as a secondary candidate for side/right-facing action validation.
3. Do not use old AI white-background `Art2D` Warrior resources for formal Warrior integration.
4. Do not use Stage 4.0 auto-transparent salvage outputs for formal Warrior integration.
5. Before creating Prefabs, AnimatorControllers, or AnimationClips, perform a dedicated slicing/import stage for the chosen Tiny Swords Warrior sheet.
