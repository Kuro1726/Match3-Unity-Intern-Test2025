# Match3 Unity - Gameplay Rewrite

This project replaces the original swap-based match-3 gameplay with a layered tile-and-tray game.

## Core Gameplay

1. Tap an unblocked item on the board to move it into the five-cell tray.
2. Items cannot return to the board in standard Play mode.
3. Exactly three identical items are cleared automatically.
4. Clear every item to win.
5. Filling all five tray cells without a match causes a loss.

Items on higher layers block overlapping items below them. Blocked items are darkened and become selectable as soon as their blockers leave the board.

## Game Modes

- **Play:** Standard manual gameplay.
- **Autoplay:** Searches for a complete winning sequence, then performs each move with a 0.5-second delay. The search respects blockers, matches, and tray capacity.
- **Auto Lose:** Finds five selectable items without creating a triple, then plays that losing sequence with a 0.5-second delay.
- **Time Attack:** Gives the player 60 seconds to clear the board. A full tray does not cause a loss; tapping a tray item returns it to its original board cell. The timer pauses with the game.

Time Attack wins when no board items remain and loses when the timer reaches zero while items remain.

## Board Layout Tool

Board layouts are stored in `BoardLayoutSO` assets and edited through the custom Inspector.

The tool provides:

- A colored board preview and per-layer tabs.
- Half-cell position snapping.
- Item and layer creation/removal.
- Automatic board-bound clamping.
- Validation before Assign or asset saving.

A valid layout must:

- Stay inside the configured board bounds.
- Have no overlapping items on the same layer.
- Contain all seven fish types.
- Have a total count divisible by three for every type across the complete board. Individual layers do not need divisible-by-three counts.

The current default board is 4 x 6 with 48 items:

- Layer 0: 24
- Layer 1: 15
- Layer 2: 8
- Layer 3: 1

## Configuration and Animation

`GameSettings` exposes the tray size, Time Attack duration, item background appearance, movement timing/easing, punch effect, and clear animation settings. Inspector tooltips describe each field.

DOTween is used for:

- Board-to-tray movement.
- Tray-to-board return movement in Time Attack.
- Tray compaction.
- Scaling matched items to zero.

## Main Files

- `Assets/Scripts/Board/Board.cs`
- `Assets/Scripts/Board/BoardLayoutSO.cs`
- `Assets/Scripts/Board/Cell.cs`
- `Assets/Scripts/Board/Item.cs`
- `Assets/Scripts/Controllers/BoardController.cs`
- `Assets/Scripts/Controllers/GameManager.cs`
- `Assets/Scripts/GameSettings.cs`
- `Assets/Scripts/Editor/BoardLayoutSOEditor.cs`
- `Assets/Scenes/Game.unity`

The runtime and editor scripts compile successfully with Unity 2020.3.38f1, and the updated scene loads without missing Time Attack button references.
