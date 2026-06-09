# Stage 4.6D - Dungeon Crawl BaseRoom Runtime Hook Report

## Modified Files

* `Assets/Scripts/DungeonCrawlBaseRoomRuntimeVisual.cs`
* `Assets/Scripts/ModernRogue/SoulKnightDirector.cs`
* `Assets/Resources/ArtIntegration/Environment/DungeonCrawlBaseRoom/BaseRoom_Visual.prefab`
* `Assets/Screenshots/stage4_6D_dungeon_crawl_base_room_runtime_hook.png`
* `Assets/_Project/Docs/Stage4.6D_DungeonCrawlBaseRoomRuntimeHookReport.md`

Unity `.meta` files were generated for the new runtime script, Resources folders, Resources prefab, screenshot, and report.

## Resources Prefab Copy

Created Resources-loadable prefab copy:

* `Assets/Resources/ArtIntegration/Environment/DungeonCrawlBaseRoom/BaseRoom_Visual.prefab`

Runtime load path:

* `Resources.Load<GameObject>("ArtIntegration/Environment/DungeonCrawlBaseRoom/BaseRoom_Visual")`

The Resources prefab is a copy of the accepted Stage 4.5D-Build visual prefab. It does not copy the full Dungeon Crawl source package and does not reference the ProjectUtumno atlas images. Its sprite dependencies remain the selected PNG files under `Assets/_Project/ArtDrops/Incoming/DungeonCrawlBaseRoom/`.

## Runtime Loading

`DungeonCrawlBaseRoomRuntimeVisual.EnsureForLobby(currentRoot.transform)` is called immediately after `SoulKnightDungeonBuilder.BuildLobby(...)` in `SoulKnightDirector.LoadLobby()`.

The bridge creates a child root named:

* `DungeonCrawlBaseRoomRuntimeVisual`

It instantiates:

* `BaseRoom_Visual_RuntimeInstance`

The runtime visual is placed at `(0, -1.3, 0)` under `SoulKnight_Lobby`, so the existing Player spawn remains inside the new Dungeon Crawl room visual without moving the Player spawn or changing gameplay.

## Old Base Visual Source

The old lobby/base visuals are generated at runtime by `SoulKnightDungeonBuilder.BuildLobby(...)` and `AddLobbyArchitecture(...)`.

Observed old visual sources include:

* `LobbyFloorArt` using `Environment/Tiles/Floor_Lobby`
* `LobbyMainCarpet` using `Environment/Lobby/Lobby_Carpet`
* `PortalDais` using `Environment/Tiles/Floor_Lobby`
* `LeftAlcoveFloor` / `RightAlcoveFloor` using `Environment/Tiles/Floor_Lobby`
* `ShopCounterArt` using `Environment/Lobby/Lobby_ShopCounter`
* `TrainingDummyArt` using `Environment/Lobby/Lobby_TrainingDummy`
* `PortalArt` using `Environment/Lobby/Lobby_Portal`
* `SmallWorldLabel/Text` UI labels such as class statue / shop / training / quest labels

These are old runtime Art2D lobby visuals, not the new Dungeon Crawl BaseRoom art.

## Disabled Old Visual Components

The bridge disables only visual components under `SoulKnight_Lobby`, excluding the new Dungeon Crawl runtime visual root:

* `Renderer` components, including old `SpriteRenderer` visuals.
* `Graphic` components, including old world-space `Text` and `Image` labels.

Runtime audit examples:

* `SoulKnight_Lobby/LobbyFloorArt` remained active, but its `SpriteRenderer.enabled` was `false`.
* `SoulKnight_Lobby/DungeonPortal/PortalArt` remained active, but its `SpriteRenderer.enabled` was `false`.
* `SoulKnight_Lobby/SmallWorldLabel/Text` remained active, but its UI graphic output was disabled.

## Preserved Gameplay Objects

Gameplay objects are not deleted and are not set inactive by the bridge. Their scripts, colliders, and triggers remain in place.

Preserved categories include:

* `SoulKnightChest`
* `SoulKnightPortal`
* `LobbyClassStatueInteractable`
* `LobbyWeaponForgeInteractable`
* Portal colliders / trigger pads
* Chest / shop / training / quest / entrance logic objects

## Constraint Checks

* Modified `Main.unity`: No.
* Modified gameplay values / combat numbers / drops / backpack / shop / quest logic: No.
* Modified `PlayerController2D.cs`: No.
* Modified `PlayerAttack2D.cs`: No.
* Modified `TinySwordsPlayerVisual2D.cs`: No.
* Used old AI Art2D environment image as main base visual: No.
* Used ProjectUtumno large atlas image: No.
* Copied full Dungeon Crawl raw package: No.
* Changed enemies / Boss / Archer / Mage / UI system: No.

## Player Verification

Runtime Play Mode audit confirmed:

* Player still has `ModernRogue.PlayerController2D`.
* Player still has `ModernRogue.PlayerAttack2D`.
* Player still has `ModernRogue.TinySwordsPlayerVisual2D`.
* Player root rotation remained near zero, not a 360-degree spinning root.
* `PlayerController2D.AimDirection` was present and nonzero.
* `PlayerAttack2D` remained present and uses the Stage 4.4T direction path.
* Tiny Swords runtime visual was visible in the GameView screenshot.
* The old red world-space health bar did not reappear.
* The old slash white-box visual did not reappear in the checked GameView state.

Manual keyboard input was not automated by this report; component state and visual runtime behavior were checked in Play Mode.

## BaseRoom Verification

Runtime GameView showed:

* New Dungeon Crawl BaseRoom visible in Play Mode.
* Dungeon Crawl tiled floor and wall boundary visible.
* Door, portal, chest, vendor/shop, training, quest, torch/lamp/altar/crate visual areas visible.
* Tiny Swords Warrior player visible inside the room.
* Old lobby/base large floor art and old white-background interactable visuals were not visible.
* The result was not a small patch over the old base; it replaced the main base visual.

Old world-space label UI initially appeared, then was classified as old lobby visual and disabled through `Graphic` components under `SoulKnight_Lobby`.

## Screenshot

* GameView screenshot: `Assets/Screenshots/stage4_6D_dungeon_crawl_base_room_runtime_hook.png`

## Verification State

* Unity Console Error count: 0 at report creation time.
* Current `git status` at report creation time contains only Stage 4.6D runtime hook files and their Unity `.meta` files.

## Recommendation

Recommended next step: review the runtime screenshot, then commit Stage 4.6D if accepted. Future stages can align gameplay trigger positions with the new Dungeon Crawl visual markers if desired, but this stage intentionally only replaces the main base visual display.

## Fix1 - Runtime BaseRoom Polish

Fix1 continued Stage 4.6D without committing.

### Room Size / Layout

* Rebuilt `BaseRoom_Visual.prefab` from roughly 20x12 tiles to 26x16 tiles.
* Updated both prefab copies:
  * `Assets/_Project/ArtIntegration/Prefabs/Environment/DungeonCrawlBaseRoom/BaseRoom_Visual.prefab`
  * `Assets/Resources/ArtIntegration/Environment/DungeonCrawlBaseRoom/BaseRoom_Visual.prefab`
* The runtime still loads the Resources prefab via:
  * `Resources.Load<GameObject>("ArtIntegration/Environment/DungeonCrawlBaseRoom/BaseRoom_Visual")`
* The floor remains individual 32x32 tile SpriteRenderer objects, not one large image.
* Wall boundary remains individual Dungeon Crawl wall tiles.
* Player stays inside the room, center/lower-center.

### Camera Adjustment

* Lobby camera framing was adjusted in `SoulKnightDirector.SetupCamera(...)`.
* Lobby only:
  * `orthographicSize` changed from `14.2` to `15.6`.
  * camera offset changed to `(0, 2.1, 0)` so the larger room fits better.
* Non-lobby stages keep the previous `14.2` size and zero offset.
* `LoopCameraFollow` gained an offset overload while preserving the existing `Configure(target)` behavior.

### White Frame / Legacy Visual Cleanup

White frame sources found in Play Mode:

* Old Lobby wall `BoxCollider2D` objects:
  * `SoulKnight_Lobby/NorthWall`
  * `SoulKnight_Lobby/SouthWall`
  * `SoulKnight_Lobby/WestWall`
  * `SoulKnight_Lobby/EastWall`
* Old `DungeonPortal` rectangular pad `BoxCollider2D`.
* Old root scene `Canvas`, separate from `SoulKnightOverlayCanvas`, containing legacy UI panels / white-background UI art.
* GameView Gizmos can still draw enabled Collider2D outlines in the Unity editor. These are editor overlays, not runtime art. The remaining important trigger colliders are circular and aligned to the new icons.

Fixes:

* Disabled old wall `BoxCollider2D` components at runtime.
* Disabled old `DungeonPortal` rectangular `BoxCollider2D` at runtime.
* Kept old `DungeonPortal` circular trigger enabled.
* Disabled old root `Canvas` and its `GraphicRaycaster`, `Graphic`, and child `Renderer` outputs at runtime.
* Kept `SoulKnightOverlayCanvas` untouched.
* Continued disabling old Lobby `Renderer` and `Graphic` components under `SoulKnight_Lobby`, excluding the new Dungeon Crawl visual root.

### Core Interactable Visuals

Portal:

* Visual: `portal.png`
* Runtime visual node: `Portal_Visual`
* Runtime gameplay trigger moved to `(9.25, 3.45)`.
* Old portal logic object `DungeonPortal` remains active.
* Old rectangular pad collider disabled; circular portal trigger remains enabled.

Class change statue:

* New resources copied:
  * `Dungeon Crawl Stone Soup Full/dungeon/statues/statue_ancient_hero.png`
  * `Dungeon Crawl Stone Soup Full/dungeon/statues/pedestal.png`
* Runtime visual nodes:
  * `ClassChangeStatue_Visual`
  * `ClassChangePedestal_Visual`
* Runtime gameplay trigger `职业雕像` moved to `(0, 2.6)`.
* Existing `LobbyClassStatueInteractable` remains active.
* Existing interaction method remains right-click while near the trigger.

Weapon shop:

* New resources copied:
  * `Dungeon Crawl Stone Soup Full/dungeon/shops/shop_weapon.png`
  * `Dungeon Crawl Stone Soup Full/item/weapon/ancient_sword.png`
  * `Dungeon Crawl Stone Soup Full/item/weapon/golden_sword.png`
* Runtime visual nodes:
  * `WeaponShop_Visual`
  * `WeaponShopSword_Visual`
  * `WeaponShopGoldenSword_Visual`
* Runtime gameplay trigger `职业武器铺` moved to `(-9.25, 1.45)`.
* Existing `LobbyWeaponForgeInteractable` remains active.
* Existing interaction method remains right-click while near the trigger.

Additional new resource:

* `Dungeon Crawl Stone Soup Full/dungeon/statues/statue_sword.png`

All new PNGs were copied into:

* `Assets/_Project/ArtDrops/Incoming/DungeonCrawlBaseRoom/`

Import settings follow Stage 4.5D-Build:

* Texture Type: Sprite
* Sprite Mode: Single
* Alpha Is Transparency: true
* Generate Mip Maps: false
* Filter Mode: Point
* Compression: None
* Pixels Per Unit: 32

### Fix1 Verification

* Modified `Main.unity`: No.
* Modified gameplay values / shop / class-change / drops / backpack / quest logic: No.
* Player remains Tiny Swords Warrior.
* `PlayerController2D`, `PlayerAttack2D`, and `TinySwordsPlayerVisual2D` remain present in Play Mode.
* Player root rotation remained near zero.
* Old world-space health bar did not return.
* Old slash white-box visual did not return in checked state.
* Old root `Canvas` was disabled; `SoulKnightOverlayCanvas` remains available.
* No ProjectUtumno atlas or full raw Dungeon Crawl package was imported.

### Fix1 Screenshot

* GameView screenshot: `Assets/Screenshots/stage4_6D_dungeon_crawl_base_room_runtime_hook_fix1.png`

The Fix1 screenshot shows the larger Dungeon Crawl room, Tiny Swords Warrior player, visible portal, visible class-change statue, visible weapon shop, and no obvious old white-frame/white-background base visuals in the captured GameView.

### Remaining Notes

* If Unity GameView `Gizmos` is enabled, the editor may draw collider outlines for the remaining active circular interactable triggers. These are editor overlays, not game art.
* Old UI cleanup was intentionally narrow. A broader UI pass should be handled in a later stage.
* Existing class statue and weapon shop interaction still uses the pre-existing right-click interaction model while near the trigger. Portal remains a walk-in trigger.
