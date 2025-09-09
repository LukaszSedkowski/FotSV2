using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class HealingAreaAbility : Ability, ITargetedAbility
{
    [Range(1, 10)] public int radius = 2;        // promieñ leczenia (okr¹g)
    [Range(1, 20)] public int castRange = 6;     // zasiêg rzutu od pozycji u¿ytkownika
    public float baseHeal = 25f;                 // bazowe leczenie AoE

    // Koszty (spójne z AreaAbility)
    public int movementCost = 5;
    public int ldLevelCost = 0;
    public int ldHealthCost = 0;

    // Zasady dzia³ania
    public bool affectAllies = true;   // leczymy sojuszników
    public bool includeSelf = true;    // i (domyœlnie) tak¿e siebie
    public bool healFalloff = false;   // leczenie maleje z dystansem?

    // Stan lokalny podczas celowania
    private ChessPieces _user;
    private readonly List<Vector2Int> _rangeCells = new();
    private readonly List<Vector2Int> _previewCells = new();

    public HealingAreaAbility(string name, Sprite icon, int radius = 2, int castRange = 6, float heal = 25f)
        : base(name, icon, null)
    {
        this.radius = radius;
        this.castRange = castRange;
        this.baseHeal = heal;
    }

    // ---------- ITargetedAbility ----------
    public void StartTargeting(AbilityContext ctx, ChessPieces user)
    {
        _user = user;
        _rangeCells.Clear();
        _previewCells.Clear();

        // Podœwietl zasiêg rzutu (cyan)
        var from = new Vector2Int(user.currentX, user.currentY);
        ForEachInCircle(from, castRange, ctx.tile, pos =>
        {
            TintCell(ctx, pos, Color.cyan);
            _rangeCells.Add(pos);
        });
    }

    public void UpdatePreview(AbilityContext ctx, Vector2Int hoverCell)
    {
        // wyczyœæ poprzedni preview (zielony  cyan/white)
        foreach (var pos in _previewCells)
            TintCell(ctx, pos, _rangeCells.Contains(pos) ? Color.cyan : Color.white);
        _previewCells.Clear();

        if (!InCastRange(hoverCell))
            return;

        // nowy podgl¹d (zielony)
        ForEachInCircle(hoverCell, radius, ctx.tile, pos =>
        {
            TintCell(ctx, pos, Color.green);
            _previewCells.Add(pos);
        });
    }

    public void Cast(AbilityContext ctx, Vector2Int targetCell)
    {
        if (_user == null) { Cancel(ctx); return; }
        if (!InCastRange(targetCell)) { Debug.Log("[AoE-Heal] Poza zasiêgiem rzutu."); return; }

        // Koszty
        _user.movementRange = Mathf.Max(0, _user.movementRange - movementCost);
        _user.lightDarkness.ChangeLightDarkLevel(ldLevelCost);
        _user.ChangeLightDarkHealth(ldHealthCost);

        // Leczenie w promieniu
        ForEachInCircle(targetCell, radius, ctx.tile, pos =>
        {
            var target = ctx.pieces.chessPieces[pos.x, pos.y];
            if (target == null) return;
            if (target == _user && !includeSelf) return;
            if (affectAllies && target.team != _user.team) return; // leczy tylko sojuszników

            float dist = Vector2Int.Distance(targetCell, pos);
            float heal = baseHeal * _user.GetBonus(_user.elementType);
            if (healFalloff) heal = Mathf.Max(0, heal - dist * 3f);

            float before = target.health;
            target.health = Mathf.Min(target.health + heal, target.maxHealth);
            float applied = target.health - before;

            if (applied > 0f)
                Debug.Log($"[AoE-Heal] {_user.type} uleczy³ {applied} HP na ({pos.x},{pos.y}). HP celu: {target.health}/{target.maxHealth}");
        });

        // Odœwie¿enia i finisz (spójnie z AreaAbility)
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
