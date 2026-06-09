# Stage 4.5D - Dungeon Crawl 32x32 BaseRoom Plan

## Decision

* BaseRoom scene resources will use Dungeon Crawl 32x32 / supplemental candidate assets.
* Tiny Swords continues to be used for the Warrior character.
* Dungeon Crawl is only used first for base / dungeon environment / props / interactables validation.
* Do not import the full resource package at once.
* Do not use the large ProjectUtumno atlas images as first-stage scene textures.

## Source Location

* Original downloaded packages have been moved to:
  `_ExternalArtSources/DungeonCrawl32/RawDownloaded/`

## Source License

* Dungeon Crawl 32x32 tiles: OpenGameArt, CC0 / Public Domain.
* Dungeon Crawl 32x32 tiles supplemental: OpenGameArt, CC0 / Public Domain.
* Recommended optional project credits: Dungeon Crawl Stone Soup tile contributors / OpenGameArt page.

## Resource Audit Summary

* Approximate PNG count in RawDownloaded: 12087.
* Focused audit count:
  * `Dungeon Crawl Stone Soup Full`: 6029 PNG files.
  * `Dungeon Crawl Stone Soup Supplemental`: 3016 PNG files.
  * `crawl-tiles Oct-5-2010`: 3039 PNG files.
* Individual 32x32 PNG files are present and are the preferred first-stage source.
* Large atlas images are present and should be avoided for this stage:
  * `_ExternalArtSources/DungeonCrawl32/RawDownloaded/DungeonCrawl_ProjectUtumnoTileset (1).png`
  * `_ExternalArtSources/DungeonCrawl32/RawDownloaded/ProjectUtumno_full.png`
  * `_ExternalArtSources/DungeonCrawl32/RawDownloaded/ProjectUtumno_supplemental.png`
* Full / Supplemental include dungeon, effect, emissaries, gui, item, misc, monster, and player categories.
* Dungeon subfolders include floor, wall, doors, gateways, shops, altars, statues, traps, vaults, water, and related prop categories.
* The source set is sufficient for a BaseRoom MVP using individual 32x32 PNGs.

## BaseRoom MVP Scope

* 20x12 or similar size base room.
* Stone floor tile.
* Wall boundary.
* Main exit / portal.
* Chest.
* Vendor/shop visual.
* Training dummy or weapon rack.
* Quest board / sign.
* Torch / lamp.
* Player spawn point.

## Candidate Assets

| Category | SourcePath | SuggestedRole | Size | AlphaCheck | Single PNG or Sheet | Visually Suitable for Dark Dungeon BaseRoom | RecommendedSortingLayer |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Stone floor | `_ExternalArtSources/DungeonCrawl32/RawDownloaded/Dungeon Crawl Stone Soup Full/dungeon/floor/crypt_10.png` | Base floor tile | 32x32 | hasAlpha=false; corner alpha=255,255,255,255 | Single PNG | Yes; dark crypt floor suitable for the base room ground | Environment_Floor |
| Stone floor accent | `_ExternalArtSources/DungeonCrawl32/RawDownloaded/Dungeon Crawl Stone Soup Full/dungeon/floor/cobble_blood_1_new.png` | Optional damaged floor / blood accent decal | 32x32 | hasAlpha=false; corner alpha=255,255,255,255 | Single PNG | Yes, but use sparingly as an accent | Environment_Floor_Decal |
| Stone wall | `_ExternalArtSources/DungeonCrawl32/RawDownloaded/Dungeon Crawl Stone Soup Full/dungeon/wall/brick_dark_0.png` | Wall boundary tile | 32x32 | hasAlpha=true; corner alpha=255,255,255,255 | Single PNG | Yes; strong dark dungeon wall candidate | Environment_Wall |
| Stone wall variant | `_ExternalArtSources/DungeonCrawl32/RawDownloaded/Dungeon Crawl Stone Soup Full/dungeon/wall/brick_gray_0.png` | Alternate wall boundary tile | 32x32 | hasAlpha=true; corner alpha=255,255,255,255 | Single PNG | Yes; useful for visual variation | Environment_Wall |
| Door / gate | `_ExternalArtSources/DungeonCrawl32/RawDownloaded/Dungeon Crawl Stone Soup Full/dungeon/doors/closed_door.png` | Main room door or nonfunctional visual door | 32x32 | hasAlpha=true; corner alpha=255,255,255,255 | Single PNG | Yes; readable as a dungeon door | Environment_Interactable |
| Portal / exit | `_ExternalArtSources/DungeonCrawl32/RawDownloaded/Dungeon Crawl Stone Soup Full/dungeon/gateways/portal.png` | Main exit / portal visual | 32x32 | hasAlpha=true; corner alpha=0,0,0,0 | Single PNG | Yes; clear magical exit marker | Environment_Interactable |
| Stairs | `_ExternalArtSources/DungeonCrawl32/RawDownloaded/Dungeon Crawl Stone Soup Full/dungeon/gateways/rock_stairs_up.png` | Alternative exit marker | 32x32 | hasAlpha=true; corner alpha=255,0,255,255 | Single PNG | Yes; usable if portal is too magical for the first room | Environment_Interactable |
| Chest | `_ExternalArtSources/DungeonCrawl32/RawDownloaded/Dungeon Crawl Stone Soup Full/dungeon/chest_2_closed.png` | Storage chest prop | 32x32 | hasAlpha=true; corner alpha=0,0,0,0 | Single PNG | Yes; clear interactable prop | Props |
| Vendor / shop | `_ExternalArtSources/DungeonCrawl32/RawDownloaded/Dungeon Crawl Stone Soup Full/dungeon/shops/shop_general.png` | Vendor/shop visual marker | 32x32 | hasAlpha=true; corner alpha=0,0,0,0 | Single PNG | Yes; good MVP proxy for a merchant station | Props |
| Training dummy | `_ExternalArtSources/DungeonCrawl32/RawDownloaded/Dungeon Crawl Stone Soup Full/monster/statues/training_dummy_new.png` | Training dummy prop | 32x32 | hasAlpha=true; corner alpha=0,0,0,0 | Single PNG | Yes; directly matches training area need | Props |
| Quest board / sign proxy | `_ExternalArtSources/DungeonCrawl32/RawDownloaded/Dungeon Crawl Stone Soup Full/item/scroll/blank_paper.png` | Notice paper / quest board proxy, likely combined with a backing board later | 32x32 | hasAlpha=true; corner alpha=0,0,0,0 | Single PNG | Partial; readable as notice paper, but may need a backing prop in Build stage | Props |
| Torch / lamp | `_ExternalArtSources/DungeonCrawl32/RawDownloaded/Dungeon Crawl Stone Soup Full/dungeon/wall/torches/torch_0.png` | Wall torch prop | 32x32 | hasAlpha=true; corner alpha=0,0,0,0 | Single PNG | Yes; good dungeon lighting marker | Props_Light |
| Altar / shrine | `_ExternalArtSources/DungeonCrawl32/RawDownloaded/Dungeon Crawl Stone Soup Full/dungeon/altars/altar_base.png` | Shrine / altar prop | 32x32 | hasAlpha=true; corner alpha=0,0,0,0 | Single PNG | Yes; optional magic station candidate | Props |
| Crate / storage | `_ExternalArtSources/DungeonCrawl32/RawDownloaded/Dungeon Crawl Stone Soup Full/dungeon/large_box.png` | Crate / storage decoration | 32x32 | hasAlpha=true; corner alpha=0,0,0,0 | Single PNG | Yes; simple room dressing | Props |
| Magic lamp | `_ExternalArtSources/DungeonCrawl32/RawDownloaded/Dungeon Crawl Stone Soup Full/item/misc/magic_lamp.png` | Lamp / magic prop | 32x32 | hasAlpha=true; corner alpha=0,0,0,0 | Single PNG | Yes; useful as decorative light or shop prop | Props_Light |

## Do Not Do

* Do not modify `Main.unity`.
* Do not connect gameplay.
* Do not import the full resource package.
* Do not use a full tileset image as a background.
* Do not use old AI white-background environment images.
* Do not break the Tiny Swords Warrior Player integration.
* Do not use the large ProjectUtumno atlas images as first-stage resources.

## Next Build Stage Plan

Stage 4.5D-Build:

* Copy selected PNG files to `Assets/_Project/ArtDrops/Incoming/DungeonCrawlBaseRoom/`.
* Configure Unity Import Settings.
* Slice only if necessary.
* Create `BaseRoom_Visual.prefab`.
* Create `DungeonCrawlBaseRoomVisualTest.unity`.
* Place the Tiny Swords Warrior as a scale reference.
* Capture screenshots for visual verification.
* Do not modify `Main.unity`.
