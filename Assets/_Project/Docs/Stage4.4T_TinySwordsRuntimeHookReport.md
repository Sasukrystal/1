# Stage 4.4T Tiny Swords Runtime Hook Report

## Scope

Stage 4.4T connects the Tiny Swords Warrior visual prefab to the current ModernRogue 2D runtime player path.

Main.unity was not modified.

## Modified Files

- Assets/Scripts/TinySwordsPlayerVisual2D.cs
- Assets/Scripts/ModernRogue/ModernRogueBootstrapper.cs
- Assets/Resources/ArtIntegration/Player/Player_TinySwordsWarrior_Art.prefab
- Assets/_Project/Scenes/TinySwordsWarriorRuntimeHookTest.unity
- Assets/Screenshots/stage4_4T_tiny_swords_runtime_hook_gameview.png
- Assets/Screenshots/stage4_4T_tiny_swords_visual_closeup_gameview.png
- Assets/_Project/Docs/Stage4.4T_TinySwordsRuntimeHookReport.md

## Resources Prefab Copy

Created a controlled Resources copy:

- Source: Assets/_Project/ArtIntegration/Prefabs/Player/Player_TinySwordsWarrior_Art.prefab
- Runtime path: Assets/Resources/ArtIntegration/Player/Player_TinySwordsWarrior_Art.prefab
- Load call: Resources.Load<GameObject>("ArtIntegration/Player/Player_TinySwordsWarrior_Art")

The Resources copy references the Stage 4.3T Tiny Swords Warrior sprites and AnimatorController. It does not copy old AI Warrior art, automatic white-removal test art, or the full Tiny Swords source pack.

## Runtime Visual Bridge

Created TinySwordsPlayerVisual2D as a runtime-only visual bridge.

Responsibilities:

- Loads and instantiates the Tiny Swords Warrior Resources prefab.
- Parents the visual instance under the runtime Player object.
- Caches Animator and SpriteRenderer components from the visual instance.
- Disables Rigidbody2D and Collider2D components on the visual instance so it cannot affect gameplay physics.
- Sets SpriteRenderer sortingLayerName to Player and sortingOrder to 0.
- Keeps the root Player Rigidbody2D, Collider2D, attack logic, and movement logic unchanged.
- Scales the visual instance to 2.1x for MVP readability because the Tiny Swords character occupies only a small part of each 192x192 frame.

## Rotation Handling

PlayerController2D root rotation is intentionally preserved because current attack and projectile logic use transform.right.

The visible Tiny Swords instance does not inherit the root object's continuous mouse-facing rotation. Each LateUpdate applies:

- visualInstance.transform.localPosition = Vector3.zero
- visualInstance.transform.localRotation = Quaternion.Inverse(transform.rotation)
- visualInstance.transform.localScale = Vector3.one

Runtime verification showed:

- Player root z rotation: about 314 degrees
- TinySwordsWarriorRuntimeVisual world z rotation: 0 degrees

This confirms the gameplay root can rotate while the visible Sprite remains upright instead of spinning 360 degrees.

## Left / Right Facing

Facing is reduced to the current MVP requirement:

- Right / Side: original Tiny Swords Warrior animation, SpriteRenderer.flipX = false
- Left: SpriteRenderer.flipX = true when player transform.right.x is negative
- Up / Down / Diagonal: not implemented in this stage

No 360 degree visual rotation is used for facing.

## Animation Driving

TinySwordsPlayerVisual2D drives the existing TinySwordsWarrior_Art Animator:

- Idle / Run: Animator bool IsMoving from Rigidbody2D.velocity
- Attack1: Animator trigger Attack from PlayerAttack2D.LastAttackTime
- Attack fallback: Input.GetMouseButtonDown(0), only for visual trigger safety
- Guard: Animator bool Guard from RunProgressionSystem.IsShielding
- Guard fallback: Input.GetMouseButton(1), only for visual trigger safety
- Attack2: Animator parameter/state kept in the controller but not bound to input in this stage

No PlayerAttack2D gameplay logic was modified.

## Old Visual Handling

ModernRogueBootstrapper now removes or disables old runtime player visual layers during 2D player conversion:

- SKCharacterVisual2D is removed if present.
- SKPlayerVisual child is disabled if present.
- RuntimeCharacterVisual is removed if present.
- ProceduralCharacterAnimator is removed if present.
- RuntimeArtBinder is removed if present.
- RuntimeStylizedVisual child is disabled if present.
- PlayerClassVfx2D is preserved.

The root Player SpriteRenderer remains hidden with alpha 0 and disabled.

## Test Scene

Created:

- Assets/_Project/Scenes/TinySwordsWarriorRuntimeHookTest.unity

The scene contains:

- ModernRogueBootstrapperHost for runtime hook verification.
- VisualProofCamera for close-up visual verification.
- VisualProof_TinySwordsWarrior, a direct Tiny Swords prefab instance used only to verify sprite readability, transparency, and framing.

It does not modify Main.unity and does not contain new enemy, boss, UI, or gameplay content.

## GameView Verification

Screenshot:

- Assets/Screenshots/stage4_4T_tiny_swords_runtime_hook_gameview.png
- Assets/Screenshots/stage4_4T_tiny_swords_visual_closeup_gameview.png

Observed:

- Runtime Player has TinySwordsPlayerVisual2D.
- Runtime TinySwordsWarriorRuntimeVisual is instantiated.
- Runtime VisualRoot uses Tiny Swords Warrior sprites and TinySwordsWarrior_Art.controller.
- SKCharacterVisual2D is absent.
- SKPlayerVisual is absent.
- RuntimeCharacterVisual component is absent.
- RuntimeStylizedVisual remains as an inactive legacy child.
- No Unity Console errors were present during verification.

Important correction:

- The wide runtime screenshot still shows older generated room/UI content created by existing runtime bootstrap systems.
- The wide runtime screenshot is too dark and should not be treated as final visual acceptance.
- The close-up visual screenshot is the relevant proof for Tiny Swords Warrior sprite quality: the Warrior is readable, transparent, and has no baked white background, white box, or checkerboard background.
- Stage 4.4T did not modify or clean environment/UI content.

## Main.unity

Main.unity was not modified.

## Unity Console

Unity Console Error count: 0

## Git Status At Report Time

Expected Stage 4.4T changes are uncommitted. This stage should be reviewed before committing.

## Submit Recommendation

Recommended to submit Stage 4.4T after user review if the GameView result is acceptable.

## Fix2 Pass

Date: 2026-06-06

### Failure Summary

The first runtime hook pass still failed visual acceptance. The visible Tiny Swords Warrior existed, but the controlled runtime Player was still coupled to older runtime visuals and root rotation behavior.

Observed failures:

- WASD movement could feel stuck or dependent on mouse direction because PlayerController2D continuously rotated the Player Rigidbody2D to face the mouse.
- The visible red rotating bar came from the Player world-space FloatingHealthBar, not from Tiny Swords.
- The attack flash/white-box risk came from PlayerAttack2D spawning the old slash feedback path, which could use old Art2D VFX or generated fallback visuals.
- The Tiny Swords runtime child was also being disabled by ModernRogueBootstrapper.HideLegacyPlayerVisualChildren until this fix excluded TinySwordsWarriorRuntimeVisual from legacy visual hiding.

### Script Changes

Modified scripts:

- Assets/Scripts/ModernRogue/PlayerController2D.cs
- Assets/Scripts/ModernRogue/PlayerAttack2D.cs
- Assets/Scripts/TinySwordsPlayerVisual2D.cs
- Assets/Scripts/ModernRogue/ModernRogueBootstrapper.cs

No Main.unity changes were made.

No combat damage, cooldown, hit radius, drop, backpack, shop, or stat values were changed.

### Movement And Aim Fix

PlayerController2D no longer rotates the Player root object toward the mouse.

New behavior:

- Player root rotation is held at 0 degrees.
- Rigidbody2D.freezeRotation is enabled.
- Mouse aim is stored as PlayerController2D.AimDirection.
- Dodge fallback direction uses AimDirection when there is no movement input.

This keeps WASD movement in world space and avoids rotating the Player collider/visual hierarchy.

### Attack Direction Fix

PlayerAttack2D no longer depends on transform.right for Warrior melee direction.

New behavior:

- PlayerAttack2D reads PlayerController2D.AimDirection.
- Melee hit center, angle checks, and projectile direction use AimDirection.
- Attack damage, cooldown, radius, and arc angle are unchanged.

### Attack White-Box Fix

PlayerAttack2D no longer calls SpawnSlashFeedback for Warrior melee.

Reason:

- The old slash feedback could display old Art2D slash sprites or generated fallback visuals.
- Stage 4.4T uses Tiny Swords Attack animation as the accepted Warrior attack visual.
- It is acceptable in this stage to have no extra slash VFX rather than a white-box slash.

### Red Rotating Bar Fix

Source:

- Bagsys.RogueLike.FloatingHealthBar on the runtime Player.
- It creates a world-space Canvas child named FloatingHealthBar.

Fix:

- ModernRogueBootstrapper removes FloatingHealthBar from the runtime Player.
- It also removes any child named FloatingHealthBar.
- HUD systems are not modified.

### Tiny Swords Visual Fix

TinySwordsPlayerVisual2D now:

- Loads Resources.Load<GameObject>("ArtIntegration/Player/Player_TinySwordsWarrior_Art").
- Instantiates TinySwordsWarriorRuntimeVisual under the runtime Player.
- Forces the visual instance active.
- Keeps localPosition at (0, 0, -1).
- Keeps localRotation identity because Player root no longer rotates.
- Keeps localScale at 2.1 for MVP readability.
- Sets SpriteRenderer sortingLayerName to Player.
- Sets SpriteRenderer sortingOrder to 120.
- Uses PlayerController2D.AimDirection.x for flipX:
  - AimDirection.x >= 0: flipX false
  - AimDirection.x < 0: flipX true

### Runtime Verification

Observed in Play Mode:

- Player root exists.
- Player root rotation is 0 degrees.
- Player has TinySwordsPlayerVisual2D.
- TinySwordsWarriorRuntimeVisual exists and is a Player child.
- VisualRoot SpriteRenderer is enabled.
- VisualRoot SpriteRenderer is visible.
- VisualRoot sprite path: Assets/_Project/ArtDrops/Incoming/TinySwordsWarrior/Warrior_Idle.png
- SpriteRenderer color alpha: 1
- SpriteRenderer sorting layer/order: Player / 120
- Animator controller path: Assets/_Project/ArtIntegration/AnimatorControllers/Player/TinySwordsWarrior_Art.controller
- SKPlayerVisual is absent.
- SKCharacterVisual2D is absent.
- FloatingHealthBar is absent.
- MeleeArcSlashSprite2D is absent.
- MeleeArcSlash2D is absent.

### Screenshot

Fix2 GameView screenshot:

- Assets/Screenshots/stage4_4T_tiny_swords_runtime_hook_gameview_fix2.png

The screenshot shows the controlled runtime Player using the Tiny Swords Warrior visual. The red rotating FloatingHealthBar is gone.

Note: The existing generated room still contains old white-background environment/room artwork. That is not fixed in Stage 4.4T-Fix2 because this pass is limited to the Player runtime visual, movement, red bar, and attack white-box feedback.

### Unity Console

Unity Console Error count: 0

### Git Status

Stage 4.4T and Fix2 changes remain uncommitted for user review.
