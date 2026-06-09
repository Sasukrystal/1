# Stage 4.0 Asset Transparency Salvage Report

Generated: 2026-06-05

## Resource Partition Strategy

- SafeRuntimeSprites: allowed for direct use after normal Unity import verification.
- BakedWhiteBackground: forbidden for direct scene, prefab, or UI use until repaired or regenerated.
- SuspectedPreviewOrContactSheet: forbidden for runtime use.
- NeedsManualReview: not used until manually confirmed.

## Test Method

- Original PNG files were not overwritten, moved, or deleted.
- White background was detected by flood fill from image borders.
- Near-white connected background pixels were converted to alpha 0 with light edge feathering.
- Output files are written to `Assets/_Project/ArtProcessed/TransparentTest/`.

## Results

| Original | Processed | Alpha | Corners Transparent | Subject Retained | White Edge Risk | Prototype OK | Regenerate Recommended | Next Stage |
| --- | --- | --- | --- | ---: | --- | --- | --- | --- |
| `Assets/Resources/Art2D/Player_Warrior.png` | `Assets/_Project/ArtProcessed/TransparentTest/Player_Warrior_TransparentTest.png` | `yes` | `yes` | `0.230` | `no` | `yes` | `no` | `yes` |
| `Assets/Resources/Art2D/Player_Archer.png` | `Assets/_Project/ArtProcessed/TransparentTest/Player_Archer_TransparentTest.png` | `yes` | `yes` | `0.149` | `yes` | `no` | `yes` | `yes` |
| `Assets/Resources/Art2D/Player_Mage.png` | `Assets/_Project/ArtProcessed/TransparentTest/Player_Mage_TransparentTest.png` | `yes` | `yes` | `0.192` | `no` | `yes` | `no` | `yes` |
| `Assets/Resources/Art2D/Enemy_Slime.png` | `Assets/_Project/ArtProcessed/TransparentTest/Enemy_Slime_TransparentTest.png` | `yes` | `yes` | `0.101` | `no` | `yes` | `no` | `yes` |
| `Assets/Resources/Art2D/Boss_Titan.png` | `Assets/_Project/ArtProcessed/TransparentTest/Boss_Titan_TransparentTest.png` | `yes` | `yes` | `0.419` | `no` | `yes` | `no` | `yes` |
| `Assets/Resources/Art2D/UI/Icons/Icon_Gold.png` | `Assets/_Project/ArtProcessed/TransparentTest/Icon_Gold_TransparentTest.png` | `yes` | `yes` | `0.240` | `no` | `yes` | `no` | `yes` |
| `Assets/Resources/Art2D/Projectile_Arrow.png` | `Assets/_Project/ArtProcessed/TransparentTest/Projectile_Arrow_TransparentTest.png` | `yes` | `yes` | `0.028` | `no` | `yes` | `no` | `yes` |
| `Assets/Resources/Art2D/Environment/Interactables/Chest_Common_Closed.png` | `Assets/_Project/ArtProcessed/TransparentTest/Chest_Common_Closed_TransparentTest.png` | `yes` | `yes` | `0.206` | `no` | `yes` | `no` | `yes` |

## Per-Asset Notes

### Assets/Resources/Art2D/Player_Warrior.png

- Processed: `Assets/_Project/ArtProcessed/TransparentTest/Player_Warrior_TransparentTest.png`
- Alpha generated: yes
- Corner alpha values: `[0, 0, 0, 0]`
- Subject pixel retention ratio: `0.230`
- Removed background ratio: `0.783`
- Residual white pixel ratio: `0.043`
- Severe miscut detected: `no`
- Visible white edge risk: `no`
- Recommended for prototype: `yes`
- Recommended to regenerate: `no`
- Can enter next stage: `yes`

### Assets/Resources/Art2D/Player_Archer.png

- Processed: `Assets/_Project/ArtProcessed/TransparentTest/Player_Archer_TransparentTest.png`
- Alpha generated: yes
- Corner alpha values: `[0, 0, 0, 0]`
- Subject pixel retention ratio: `0.149`
- Removed background ratio: `0.865`
- Residual white pixel ratio: `0.115`
- Severe miscut detected: `no`
- Visible white edge risk: `yes`
- Recommended for prototype: `no`
- Recommended to regenerate: `yes`
- Can enter next stage: `yes`

### Assets/Resources/Art2D/Player_Mage.png

- Processed: `Assets/_Project/ArtProcessed/TransparentTest/Player_Mage_TransparentTest.png`
- Alpha generated: yes
- Corner alpha values: `[0, 0, 0, 0]`
- Subject pixel retention ratio: `0.192`
- Removed background ratio: `0.820`
- Residual white pixel ratio: `0.002`
- Severe miscut detected: `no`
- Visible white edge risk: `no`
- Recommended for prototype: `yes`
- Recommended to regenerate: `no`
- Can enter next stage: `yes`

### Assets/Resources/Art2D/Enemy_Slime.png

- Processed: `Assets/_Project/ArtProcessed/TransparentTest/Enemy_Slime_TransparentTest.png`
- Alpha generated: yes
- Corner alpha values: `[0, 0, 0, 0]`
- Subject pixel retention ratio: `0.101`
- Removed background ratio: `0.904`
- Residual white pixel ratio: `0.006`
- Severe miscut detected: `no`
- Visible white edge risk: `no`
- Recommended for prototype: `yes`
- Recommended to regenerate: `no`
- Can enter next stage: `yes`

### Assets/Resources/Art2D/Boss_Titan.png

- Processed: `Assets/_Project/ArtProcessed/TransparentTest/Boss_Titan_TransparentTest.png`
- Alpha generated: yes
- Corner alpha values: `[0, 0, 0, 0]`
- Subject pixel retention ratio: `0.419`
- Removed background ratio: `0.599`
- Residual white pixel ratio: `0.021`
- Severe miscut detected: `no`
- Visible white edge risk: `no`
- Recommended for prototype: `yes`
- Recommended to regenerate: `no`
- Can enter next stage: `yes`

### Assets/Resources/Art2D/UI/Icons/Icon_Gold.png

- Processed: `Assets/_Project/ArtProcessed/TransparentTest/Icon_Gold_TransparentTest.png`
- Alpha generated: yes
- Corner alpha values: `[0, 0, 0, 0]`
- Subject pixel retention ratio: `0.240`
- Removed background ratio: `0.768`
- Residual white pixel ratio: `0.010`
- Severe miscut detected: `no`
- Visible white edge risk: `no`
- Recommended for prototype: `yes`
- Recommended to regenerate: `no`
- Can enter next stage: `yes`

### Assets/Resources/Art2D/Projectile_Arrow.png

- Processed: `Assets/_Project/ArtProcessed/TransparentTest/Projectile_Arrow_TransparentTest.png`
- Alpha generated: yes
- Corner alpha values: `[0, 0, 0, 0]`
- Subject pixel retention ratio: `0.028`
- Removed background ratio: `0.979`
- Residual white pixel ratio: `0.025`
- Severe miscut detected: `no`
- Visible white edge risk: `no`
- Recommended for prototype: `yes`
- Recommended to regenerate: `no`
- Can enter next stage: `yes`

### Assets/Resources/Art2D/Environment/Interactables/Chest_Common_Closed.png

- Processed: `Assets/_Project/ArtProcessed/TransparentTest/Chest_Common_Closed_TransparentTest.png`
- Alpha generated: yes
- Corner alpha values: `[0, 0, 0, 0]`
- Subject pixel retention ratio: `0.206`
- Removed background ratio: `0.802`
- Residual white pixel ratio: `0.002`
- Severe miscut detected: `no`
- Visible white edge risk: `no`
- Recommended for prototype: `yes`
- Recommended to regenerate: `no`
- Can enter next stage: `yes`

## Recommendation

- Prototype-acceptable test outputs: `7` of `8`.
- Regeneration recommended: `1` of `8`.
- Do not perform full automatic batch processing until these samples are visually approved in a dark-background sandbox.
- Critical character and animation assets should ideally be regenerated as true transparent-background sprites rather than repaired after the fact.


## GameView Validation

- Validation scene: `Assets/_Project/Scenes/ArtTransparencyTest.unity`
- Screenshot: `Assets/Screenshots/stage4_asset_transparency_test_gameview.png`
- Result: large baked white rectangles are removed in the processed samples.
- Remaining issue: several assets still show visible white edge halos on a dark background, especially character silhouettes and high-contrast magic effects.
- Scale issue: original 1254x1254 generated art is much too large for direct placement at default SpriteRenderer scale; future use requires explicit PPU/scale rules.
- Production recommendation: do not full-batch these into final runtime art without manual visual approval. Critical player, enemy, boss, and UI art should be regenerated with true transparent backgrounds whenever possible.
