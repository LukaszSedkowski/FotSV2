using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PersistentDamageZoneAbility : Ability, ITargetedAbility
{
    [Range(1, 10)] public int radius = 2;
    [Range(1, 20)] public int castRange = 6;

    public float damagePerTurn = 10f;     // obra¿enia co turê
    public int durationTurns = 3;         // ile tur ma staæ strefa

    // Koszty rzutu (analogicznie do AreaAbility)
    public int movementCost = 5;
    public int ldLevelCost = 0;
    public int ldHealthCost = 0;

    private ChessPieces _user;
    private readonly List<Vector2Int> _rangeCells = new();
    private readonly List<Vector2Int> _previewCells = new();

    public PersistentDamageZoneAbility(string name, Sprite icon, int radius = 2, int castRange = 6, float dpt = 10f, int duration = 3)
        : base(name, icon, null)
    {
        this.radius = radius;
        this.castRange = castRange;
        this.damagePerTurn = dpt;
        this.durationTurns = duration;
    }

    // ---------- ITargetedAbility ----------
    public void StartTargeting(AbilityContext ctx, ChessPieces user)
    {
        _user = user;
        _rangeCells.Clear();
        _previewCells.Clear();

        var from = new Vector2Int(user.currentX, user.currentY);
        ForEachInCircle(from, castRange, ctx.tile, pos =>
        {
            TintCell(ctx, pos, Color.cyan); // zasiêg rzutu
            _rangeCells.Add(pos);
        });
    }

    public void UpdatePreview(AbilityContext ctx, Vector2Int hoverCell)
    {
        // wyczyœæ poprzedni preview
        foreach (var pos in _previewCells)
            TintCell(ctx, pos, _rangeCells.Contains(pos) ? Color.cyan : Color.white);
        _previewCells.Clear();

        if (!InCastRange(hoverCell)) return;

        // nowy podgl¹d strefy (czerwony)
        ForEachInCircle(hoverCell, radius, ctx.tile, pos =>
        {
            TintCell(ctx, pos, Color.red);
            _previewCells.Add(pos);
        });
    }

    public void Cast(AbilityContext ctx, Vector2Int targetCell)
    {
        if (_user == null) { Cancel(ctx); return; }
        if (!InCastRange(targetCell)) { Debug.Log("[Zone] Poza zasiêgiem."); return; }

        // Koszty
        _user.movementRange = Mathf.Max(0, _user.movementRange - movementCost);
        _user.lightDarkness.ChangeLightDarkLevel(ldLevelCost);
        _user.ChangeLightDarkHealth(ldHealthCost);

        // Wyznacz komórki strefy
        var zoneCells = new List<Vector2Int>();
        ForEachInCircle(targetCell, radius, ctx.tile, pos => zoneCells.Add(pos));

        // Zarejestruj strefê i pokoloruj na czerwono
        var zem = UnityEngine.Object.FindAnyObjectByType<ZoneEffectManager>();
        if (zem != null)
        {
            float finalDmg = damagePerTurn * _user.GetBonus(_user.elementType);
            zem.AddDamageZone(zoneCells, durationTurns, finalDmg, Color.red);
            Debug.Log($"[Zone] Strefa obra¿eñ: {zoneCells.Count} pól, {finalDmg}/turê przez {durationTurns} tury.");
        }
        else
        {
            // awaryjnie tylko kolorujemy (gdyby ktoœ zapomnia³ dodaæ managera do sceny)
            foreach (var c in zoneCells) TintCell(ctx, c, Color.red);
            Debug.LogWarning("[Zone] Brak ZoneEffectManager w scenie – strefa nie bêdzie zadawaæ obra¿eñ co turê.");
        }

        // Odœwie¿enia jak po rzucie czaru
        ctx.fow.UpdateFogOfWar(_user.currentX, _user.currentY, ctx.pieces.chessPieces);
        ctx.fow.UpdatePieceVisibility(ctx.pieces.chessPieces);
        ctx.highlight.HighlightPossibleMoves(_user);
        ctx.turn.CheckGameOver();

        Cancel(ctx);
    }

    public void Cancel(AbilityContext ctx)
    {
        foreach (var pos in _previewCells) TintCell(ctx, pos, _rangeCells.Contains(pos) ? Color.cyan : Color.white);
        foreach (var pos in _rangeCells) TintCell(ctx, pos, Color.white);
        _previewCells.Clear();
        _rangeCells.Clear();
        _user = null;
        ctx.board.OnAbilityTargetingFinished();
    }

    // ---------- helpers ----------
    private bool InCastRange(Vector2Int cell)
    {
        if (_user == null) return false;
        var from = new Vector2Int(_user.currentX, _user.currentY);
        return Vector2Int.Distance(from, cell) <= castRange;
    }

    private void ForEachInCircle(Vector2Int center, int r, TileManager tile, Action<Vector2Int> action)
    {
        int minX = Mathf.Max(0, center.x - r);
        int maxX = Mathf.Min(TileManager.Tile_Count_X - 1, center.x + r);
        int minY = Mathf.Max(0, center.y - r);
        int maxY = Mathf.Min(TileManager.Tile_Count_Y - 1, center.y + r);

        for (int x = minX; x <= maxX; x++)
            for (int y = minY; y <= maxY; y++)
            {
                var p = new Vector2Int(x, y);
                if (Vector2Int.Distance(center, p) <= r)
                    action(p);
            }
    }

    private void TintCell(AbilityContext ctx, Vector2Int pos, Color col)
    {
        var mr = ctx.tile.tiles[pos.x, pos.y].GetComponent<MeshRenderer>();
        if (mr != null) mr.material.color = col;
    }
}
