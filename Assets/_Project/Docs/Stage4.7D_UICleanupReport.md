# Stage 4.7D UI Cleanup Report

## Scope

Stage 4.7D cleans runtime UI visuals that were visibly polluting the Dungeon Crawl BaseRoom view. This stage does not modify `Main.unity`, combat values, backpack data, shop logic, drops, quest logic, enemies, bosses, or class gameplay.

## Modified Files

- `Assets/Scripts/ModernRogue/SoulKnightDirector.cs`
- `Assets/Scripts/ModernRogue/UIManager.cs`
- `Assets/Scripts/ModernRogue/NewBackpackPanel.cs`
- `Assets/Scripts/ModernRogue/NewCharacterPanel.cs`
- `Assets/Scripts/ModernRogue/RuntimeUIVisuals.cs`
- `Assets/Scripts/DungeonCrawlBaseRoomRuntimeVisual.cs`
- `Assets/Scripts/InventoryManager.cs`
- `Assets/Screenshots/stage4_7D_ui_cleanup_gameview.png`

## Old UI Objects Located

- `SoulKnightOverlayCanvas/SoulKnightHud`
  - Previously loaded `Assets/Resources/Art2D/UI/HUD/HUD_TopBar_Frame`.
  - Replaced with runtime pure-color UI image.
- `SoulKnightOverlayCanvas/SoulKnightCombatPanel`
  - Previously loaded `Assets/Resources/Art2D/UI/HUD/HUD_PlayerStatus_Frame`.
  - Replaced with runtime pure-color UI image.
- `SoulKnightOverlayCanvas/*条`
  - Previously loaded old Art2D HP/MP/EXP bar back and fill sprites.
  - Replaced with runtime pure-color filled `Image` bars.
- `Canvas/KnapsackPanel`, `Canvas/CharacterPanel`, `Canvas/ChestPanel`, `Canvas/VendorPanel`, `Canvas/ForgePanel`, `Canvas/ToolTip`
  - Main scene legacy UI objects with white-background/old frame pollution.
  - Runtime BaseRoom bridge keeps legacy root `Canvas` disabled and now refreshes this cleanup in `LateUpdate`.
- `UnifiedRoguePanelRoot`, `BackpackTabContent`, `CharacterTabContent`, `CoreTabContent`, `TreasureTabContent`
  - New runtime panels previously still referenced Art2D panel/button/slot sprites.
  - Replaced with runtime pure-color panel/slot/button visuals.

## Disabled / Replaced Visuals

- Removed runtime dependencies on old Art2D HUD sprites in `SoulKnightDirector`.
- Removed runtime dependencies on old Art2D panel/button/slot sprites in:
  - `UIManager`
  - `NewBackpackPanel`
  - `NewCharacterPanel`
- `DungeonCrawlBaseRoomRuntimeVisual` continues disabling legacy `Canvas`, `GraphicRaycaster`, child `Graphic`, and child `Renderer` components at runtime.

## New UI Generation

- Added `RuntimeUIVisuals`, a tiny runtime helper that creates a 1x1 white sprite and applies colors through Unity UI `Image`.
- HUD panels, HP/MP/EXP bars, popup panels, inventory slots, equipment slots, tab buttons, core slots, and treasure cards now use generated pure-color UI.
- No new UI image resources were imported.
- No screenshots or ProjectUtumno sheets are used.

## Original Logic Preservation

- HP, mana/shield, XP, gold, level, attack, defense, item slots, equipment, core slots, and treasure data still read from existing runtime data models.
- `InventoryManager.ToggleKnapsackPanel()` now prefers `ModernRogue.UIManager.OpenTab(0)` when available, falling back to old panels if the modern UI manager is not present.
- `InventoryManager.ToggleCharacterPanel()` now prefers `ModernRogue.UIManager.OpenTab(1)` when available, falling back to old panels if the modern UI manager is not present.
- No item data, stat values, inventory data, shop data, drop data, or quest data were changed.

## Main / Gameplay Safety

- Modified `Main.unity`: No.
- Modified gameplay values: No.
- Modified combat logic: No.
- Modified backpack data logic: No.
- Modified shop/drop/quest logic: No.
- Imported new resources: No.

## Verification

- BaseRoom still displays the Dungeon Crawl room in Play Mode.
- Tiny Swords Warrior player remains visible in the room.
- Left-top HUD no longer shows the old white-background Art2D long frame.
- Left-bottom HUD no longer shows the old white-background Art2D frame.
- Runtime UI object inspection confirmed `SoulKnightHud`, `SoulKnightCombatPanel`, and HUD bars use runtime sprites rather than Art2D asset paths.
- Unity Console Error count: 0.

## Remaining Issues / Notes

- The legacy Main scene UI objects still exist as scene objects for fallback compatibility, but their `Canvas` and child graphics are disabled at runtime by the BaseRoom visual bridge.
- MCP `execute_code` currently fails on a hidden-character compiler issue, so runtime object scanning was done through scene resources and component reads.
- Some UIManager runtime initialization could not be observed through MCP object search during this session, likely due current Enter Play Mode/domain reload behavior. Code-level routing and visual cleanup are in place, and the old UI fallback remains available if the modern UI manager is absent.

## Screenshot

- `Assets/Screenshots/stage4_7D_ui_cleanup_gameview.png`

## Git Status

- Pending local changes for Stage 4.7D. Do not commit until manual review.

## Recommendation

Stage 4.7D is ready for manual Play Mode review. If the user confirms the popup panels no longer show old white/Art2D frames after a fresh Play Mode run, this stage can be committed.

## Fix1 - Modal Canvas Layering

### Issue

Manual review found that when Backpack, Character, Core, or Treasure panels were opened, the Dungeon Crawl BaseRoom world sprites rendered over the modal panels. The cause was the Stage 4.7D cleanup temporarily moving runtime UI canvases to `ScreenSpaceCamera` so MCP camera screenshots could include UI. That put modal UI and BaseRoom sprites into the same camera/depth render path, allowing world sprites to visually cover modal UI.

### Fix

- `SoulKnightOverlayCanvas`
  - Final render mode: `ScreenSpaceOverlay`
  - Final sorting order: `100`
  - World camera: `null`
  - Role: HUD canvas
- `PickupTipPanel`
  - Added nested `Canvas` + `GraphicRaycaster`
  - Final sorting order: `200`
  - Role: interaction prompt canvas
- `ModernRogueUICanvas`
  - Final render mode: `ScreenSpaceOverlay`
  - `overrideSorting = true`
  - Final sorting order: `500`
  - World camera: `null`
  - Role: Backpack / Character / Core / Treasure modal canvas
- `UIManager.EnsureModernCanvas()`
  - Now configures both newly-created and pre-existing `ModernRogueUICanvas` objects through the same modal canvas setup.
- `UIManager.BuildPanels()`
  - Now refreshes Backpack, Character, Core, and Treasure panels immediately after each panel is built.
- `UIManager.ShowTab()`
  - Now refreshes the active tab whenever a tab is opened or switched.
- `NewBackpackPanel`, `NewCharacterPanel`, `NewCorePanel`, and `NewTreasurePanel`
  - Build paths now refresh after all child UI references are created, fixing empty modal pages caused by `OnEnable` firing before `Build()` completed.
- Tab/content click handling
  - Content backgrounds no longer participate in raycasts.
  - Tab label text no longer blocks button raycasts.
  - `EnsureEventSystem()` now ensures an existing EventSystem has a `StandaloneInputModule`.

### BaseRoom Sorting

- `DungeonCrawlBaseRoomRuntimeVisual` and `BaseRoom_Visual_RuntimeInstance` remain normal world objects.
- BaseRoom root layer remains `Default`.
- BaseRoom remains outside UI sorting layers.
- No camera movement or BaseRoom scaling was used to hide the issue.

### Safety

- Modified `Main.unity`: No.
- Modified gameplay logic: No.
- Modified backpack data logic: No.
- Modified combat/shop/drop/quest values: No.
- Imported Golden UI / PSD assets: No.
- Imported new UI resources: No.

### Verification

- Unity Console Error count: 0.
- MCP object inspection confirmed `SoulKnightOverlayCanvas` is `ScreenSpaceOverlay`, `sortingOrder = 100`, `worldCamera = null`.
- `ModernRogueUICanvas` code path now forces `ScreenSpaceOverlay`, `overrideSorting = true`, and `sortingOrder = 500`, so BaseRoom world sprites cannot render over modal UI.
- Empty modal page issue fixed by refreshing each panel after `Build()` and again on tab switch.
- Tab switching issue hardened by disabling raycast targets on content backgrounds and tab text.
- MCP camera screenshots do not include Screen Space Overlay UI; manual GameView review should be used for final visual confirmation of modal layering.

### Fix1 Screenshot

- `Assets/Screenshots/stage4_7D_ui_cleanup_fix1_modal_layering.png`

### Fix1 Git Status

- Pending local Stage 4.7D / Fix1 changes. Do not commit until manual review.

### Recommendation

Recommend manual Play Mode review with GameView Gizmos Off. If Backpack, Character, Core, and Treasure panels now stay above BaseRoom and player sprites, Stage 4.7D is suitable for commit.

## Fix1b - Top HUD Clearance

Manual review found the compact top-left HUD still overlapped the modal tab row. The top status HUD was then fully hidden because the left-bottom combat HUD already carries player status.

- Removed creation of the top-left `SoulKnightHud` visual.
- Runtime cleanup disables any existing `SoulKnightHud` child `Graphic` components, including stale red bars and blue accent lines from hot-reloaded Play Mode objects.
- Kill count and Boss essence text are no longer shown in the top-left overlay, preventing overlap with modal tabs.
- Unity Console Error count: 0.

## Fix1c - Legacy RunHud Red Bar and Tab Clicks

Manual review found a remaining large red bar in the top-left. This was identified as the legacy `RunHud` created by `GameFeelDirector`, not `SoulKnightHud`.

- `DungeonCrawlBaseRoomRuntimeVisual` now disables `GameFeelDirector`.
- `DungeonCrawlBaseRoomRuntimeVisual` now disables all child `Graphic` components under `RunHud` every frame while the BaseRoom runtime visual is active.
- No gameplay stats or combat logic were changed.
- Modal tab switching remains mouse-based only.
- Tab buttons now have both normal `Button.onClick` and explicit `EventTriggerType.PointerClick` callbacks to switch tabs reliably.
- No keyboard `1-4` tab shortcuts are used.
- Unity Console Error count: 0.
