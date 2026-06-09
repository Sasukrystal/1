# Tiny Swords Warrior Asset Map

Generated: 2026-06-05 21:14:15
Stage: 4.3T

## Source To Import Mapping
- Source: `_ExternalArtSources/TinySwords/Tiny Swords/Tiny Swords/Tiny Swords02/Units/Blue Units/Warrior/Warrior_Idle.png`
  - Imported: `Assets/_Project/ArtDrops/Incoming/TinySwordsWarrior/Warrior_Idle.png`
  - Action: Idle
  - Frames: 8
  - Sheet size: 1536x192
  - Slice size: 192x192
- Source: `_ExternalArtSources/TinySwords/Tiny Swords/Tiny Swords/Tiny Swords02/Units/Blue Units/Warrior/Warrior_Run.png`
  - Imported: `Assets/_Project/ArtDrops/Incoming/TinySwordsWarrior/Warrior_Run.png`
  - Action: Run
  - Frames: 6
  - Sheet size: 1152x192
  - Slice size: 192x192
- Source: `_ExternalArtSources/TinySwords/Tiny Swords/Tiny Swords/Tiny Swords02/Units/Blue Units/Warrior/Warrior_Attack1.png`
  - Imported: `Assets/_Project/ArtDrops/Incoming/TinySwordsWarrior/Warrior_Attack1.png`
  - Action: Attack1
  - Frames: 4
  - Sheet size: 768x192
  - Slice size: 192x192
- Source: `_ExternalArtSources/TinySwords/Tiny Swords/Tiny Swords/Tiny Swords02/Units/Blue Units/Warrior/Warrior_Attack2.png`
  - Imported: `Assets/_Project/ArtDrops/Incoming/TinySwordsWarrior/Warrior_Attack2.png`
  - Action: Attack2
  - Frames: 4
  - Sheet size: 768x192
  - Slice size: 192x192
- Source: `_ExternalArtSources/TinySwords/Tiny Swords/Tiny Swords/Tiny Swords02/Units/Blue Units/Warrior/Warrior_Guard.png`
  - Imported: `Assets/_Project/ArtDrops/Incoming/TinySwordsWarrior/Warrior_Guard.png`
  - Action: Guard
  - Frames: 6
  - Sheet size: 1152x192
  - Slice size: 192x192

## Action Mapping
- Idle: `Warrior_Idle.png`
- Run: `Warrior_Run.png`
- Attack1: `Warrior_Attack1.png`
- Attack2: `Warrior_Attack2.png`
- Guard: `Warrior_Guard.png`

## Current Direction Support
- Right: original Tiny Swords02 Warrior side/right sheets.
- Left: mirror Right using SpriteRenderer.flipX in a later direction-control stage.
- Up: deferred for MVP; not required in the current Warrior art pass.
- Down: deferred for MVP; not required in the current Warrior art pass.
- Diagonal: deferred for MVP; not required in the current Warrior art pass.

## MVP Direction Decision
- Stage 4.3T accepts side/right Warrior animation as the current MVP solution.
- Left direction should be produced later by mirroring the right-facing sprites with SpriteRenderer.flipX.
- Up, Down, and diagonal directions are intentionally not implemented in this stage and are not blocking the Warrior MVP.
- Do not simulate missing directions by rotating the entire SpriteRenderer 360 degrees.
- If richer direction support is needed later, first audit `Tiny Swords01/Factions/Knights/Troops/Warrior/Blue/Warrior_Blue.png` or add a dedicated supplemental resource.

## Missing Actions And Directions
- Hit/Hurt animation is not present in the selected Warrior subset.
- Death animation is not present in the selected Warrior subset.
- Dodge/Roll animation is not present in the selected Warrior subset.
- Full 8-direction Warrior animation is not implemented in this stage and is not required for the current MVP.

## Notes
- Do not use old AI white-background Warrior assets.
- Do not use Stage 4.0 auto-transparent salvage assets as formal Warrior art.
- Do not simulate direction by rotating the whole sprite 360 degrees.
- Current priority is stable display, no white background, playable animation clips, and gameplay-ready integration.
