using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class GameSettings : ScriptableObject
{
    [Header("Board")]
    [Tooltip("The BoardLayoutSO used when a new game starts. Assigning a different asset changes the active level layout.")]
    public BoardLayoutSO BoardLayout;

    [Tooltip("Board width in cells. This also defines the valid horizontal range in the layout editor.")]
    public int BoardSizeX = 5;

    [Tooltip("Board height in cells. This also controls the tray's vertical position.")]
    public int BoardSizeY = 5;

    [Tooltip("Number of generated layers. The layout editor updates this value when layers are added or removed.")]
    public int BoardLayerCount = 2;

    [Tooltip("Legacy match setting. The current tray gameplay always clears exactly three identical items.")]
    public int MatchesMin = 3;

    [Tooltip("Maximum number of items the bottom tray can contain before the player loses.")]
    public int BottomCellCount = 5;

    [Header("Item Background")]
    [Tooltip("Scale of the cellBackground that moves with each item. Values above 1 make the background larger and visually thicker.")]
    [Range(1f, 1.25f)]
    public float ItemBackgroundScale = 1.08f;

    [Tooltip("Opacity of the background attached to each moving item tile.")]
    [Range(0.25f, 1f)]
    public float ItemBackgroundOpacity = 1f;

    [Header("Item Animation")]
    [Tooltip("Seconds used when an item moves from the board to the bottom tray.")]
    [Range(0.1f, 1f)]
    public float ItemMoveDuration = 0.45f;

    [Tooltip("DOTween ease used for board-to-tray movement. OutBounce gives a clear bounce at the destination.")]
    public Ease ItemMoveEase = Ease.OutBounce;

    [Tooltip("Additional scale punch applied while the complete item tile moves. Set to 0 to disable it.")]
    [Range(0f, 0.4f)]
    public float ItemMovePunchScale = 0.14f;

    [Tooltip("Number of oscillations used by the scale punch. Higher values produce more shaking.")]
    [Range(1, 10)]
    public int ItemMovePunchVibrato = 2;

    [Tooltip("Elasticity of the scale punch. 0 is rigid and 1 is highly elastic.")]
    [Range(0f, 1f)]
    public float ItemMovePunchElasticity = 0.55f;

    [Tooltip("Seconds used to scale three matching item tiles fully to zero.")]
    [Range(0.05f, 0.75f)]
    public float ItemClearDuration = 0.18f;

    [Tooltip("DOTween ease used by the scale-to-zero clear animation.")]
    public Ease ItemClearEase = Ease.InBack;

    [Header("Legacy Level Conditions")]
    [Tooltip("Move limit used by the legacy move-based level mode.")]
    public int LevelMoves = 16;

    [Tooltip("Time limit in seconds used by the legacy timer-based level mode.")]
    public float LevelTime = 30f;

    [Tooltip("Seconds of inactivity before the legacy hint system can run.")]
    public float TimeForHint = 5f;
}
