# Next AI Prompt

You are taking over a Unity project named `bagsys`. Your task is to continue improving the player module and the two hotkey-driven UI systems without breaking the legacy backpack/inventory logic.

## Current State
- The player already supports mouse-facing rotation on the ground plane.
- Left-click combat already switches by equipped weapon type:
  - Dagger / MainHand behaves like melee.
  - Wand behaves like ranged projectile.
- B opens a runtime backpack/status panel.
- E opens the character/equipment panel.
- Runtime bootstrapping already creates the required Play-mode objects when needed.
- The project still uses the legacy inventory system as the source of truth.
- The new UI panels are overlays and summaries, not full replacements for the old drag/drop inventory.

## What Already Works
- Player movement, facing, and attack switching are in place.
- The backpack panel shows:
  - current coins
  - current picked item
  - occupied slot count
  - item category totals
  - a short item list with quality coloring
- The character panel shows:
  - total stats
  - base stats plus equipment bonus split
  - current weapon and weapon type
  - attack mode
  - per-slot equipment details
- Play mode verification has already confirmed that the runtime panels exist.

## Important Constraints
- Do not break the legacy inventory, drag/drop, save/load, or equipment slot logic.
- Prefer small, additive changes.
- Keep the runtime fallback panels working even if legacy Knapsack/CharacterPanel objects are absent.
- Avoid touching unrelated dungeon/combat systems unless needed for the player UI or hotkey flow.
- Remember the project uses a custom `Material` item class, so Unity materials must be explicitly qualified when needed.

## What Needs Improvement Next
Focus on making the two UI systems more interactive and useful.

### Backpack / B panel
Good next steps:
- Turn the summary panel into a more actionable item list.
- Add recent pickup highlighting.
- Add rarity/quality emphasis.
- Add a cleaner readout for full/partial stacks and category totals.
- Optionally add click-to-inspect behavior if it can be done without disturbing the old inventory logic.

### Character / E panel
Good next steps:
- Add stronger visual comparison for base stats vs equipment bonuses.
- Highlight changed values with color or formatting.
- Show when the equipped weapon changes the attack mode.
- Optionally provide equip/unequip actions from the panel only if it can reuse the legacy item flow safely.

## Suggested Evaluation Criteria
When assessing the current game, judge:
- whether the player UI is understandable in Play mode
- whether B/E are clearly separated and useful
- whether the weapon-type switching feels obvious to the player
- whether the panels improve decision-making without clutter
- whether the legacy inventory remains stable

## Helpful File References
- [Assets/Scripts/UI/RuntimeBackpackPanel.cs](Assets/Scripts/UI/RuntimeBackpackPanel.cs)
- [Assets/Scripts/Inventory/CharacterPanel.cs](Assets/Scripts/Inventory/CharacterPanel.cs)
- [Assets/Scripts/InventoryManager.cs](Assets/Scripts/InventoryManager.cs)
- [Assets/Scripts/Bootstrap/GameBootstrapper.cs](Assets/Scripts/Bootstrap/GameBootstrapper.cs)
- [Assets/Scripts/Player/PlayerController.cs](Assets/Scripts/Player/PlayerController.cs)
- [Assets/Scripts/Player/PlayerAttack.cs](Assets/Scripts/Player/PlayerAttack.cs)

## Output I Want From You
1. Evaluate the current gameplay/UI state.
2. Identify the highest-value next improvement.
3. Produce a concrete next-step prompt for Copilot.
4. Keep the recommendation focused and implementation-aware.

## If You Need a One-Line Prompt
Use this:
"Evaluate the current Unity player/inventory UI state and propose the highest-value next implementation step, keeping the legacy inventory system intact while improving B/E panel usability and clarity."