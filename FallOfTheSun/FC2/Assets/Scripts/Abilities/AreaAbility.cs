using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AreaAbility : Ability, ITargetedAbility
{
    [Range(1, 10)] public int radius = 2;        // promień rażenia (okrąg)
    [Range(1, 20)] public int castRange = 6;     // zasięg rzutu od pozycji użytkownika
    public float baseDamage = 25f;               // bazowe obrażenia AoE

    // Koszty
    public int movementCost = 5;
    public int ldLevelCost = 0;
    public int ldHealthCost = 0;

    // Zasady trafiania
    public bool affectAllies = false;
    public bool includeSelf = false;
    public bool damageFalloff = false;   // dmg maleje z dystansem od środka

    // Stan localny (tylko podczas celowania)
    private ChessPieces _user;
    private readonly List<Vector2Int> _rangeCells = new();
    private readonly List<Vector2Int> _previewCells = new();

    public AreaAbility(string name, Sprite icon, int radius = 2, int castRange = 6, float dmg = 25f)
        : base(name, icon, null)
    {
        this.radius = radius;
        this.castRange = castRange;
        this.baseDamage = dmg;
    }

    // ------------- ITargetedAbility -------------
    public void StartTargeting(AbilityContext ctx, ChessPieces user)
    {
        _user = user;
        _rangeCells.Clear();
        _previewCells.Clear();

        // Podświetl zasięg rzutu (cyan)
        var from = new Vector2Int(user.currentX, user.currentY);
        ForEachInCircle(from, castRange, ctx.tile, pos =>
        {
            TintCell(ctx, pos, Color.cyan);
            _rangeCells.Add(pos);
        });
    }

    public void UpdatePreview(AbilityContext ctx, Vector2Int hoverCell)
    {
        // wyczyść poprzedni preview (magenta → cyan/white)
        foreach (var pos in _previewCells)
            TintCell(ctx, pos, _rangeCells.Contains(pos) ? Color.cyan : Color.white);
        _previewCells.Clear();

        if (!InCastRange(hoverCell))
        {
            // kursor poza zasięgiem – nic nie rysujemy (zostaje tylko cyan)
            return;
        }

        // nowy podgląd (magenta)
        ForEachInCircle(hoverCell, radius, ctx.tile, pos =>
        {
            TintCell(ctx, pos, Color.magenta);
            _previewCells.Add(pos);
        });
    }

    public void Cast(AbilityContext ctx, Vector2Int targetCell)
    {
        if (_user == null) { Cancel(ctx); return; }
        if (!InCastRange(targetCell)) { Debug.Log("[AoE] Poza zasięgiem rzutu."); return; }

        // Koszty
        _user.movementRange = Mathf.Max(0, _user.movementRange - movementCost);
        _user.lightDarkness.ChangeLightDarkLevel(ldLevelCost);
        _user.ChangeLightDarkHealth(ldHealthCost);

        // Obrażenia w promieniu
        ForEachInCircle(targetCell, radius, ctx.tile, pos =>
        {
            var target = ctx.pieces.chessPieces[pos.x, pos.y];
            if (target == null) return;
            if (target == _user && !includeSelf) return;
            if (!affectAllies && target.team == _user.team) return;

            float dist = Vector2Int.Distance(targetCell, pos);
            float dmg = baseDamage * _user.GetBonus(_user.elementType);
            if (damageFalloff) dmg = Mathf.Max(0, dmg - dist * 3f);

            target.health -= dmg;
            Debug.Log($"[AoE] {_user.type} zadał {dmg} dmg na ({pos.x},{pos.y}). HP celu: {target.health}");

            if (target.health <= 0f)
            {
                // bezpiecznie usuń pionek (spójnie z AttackManager)
                UnityEngine.Object.Destroy(target.gameObject);
                ctx.pieces.chessPieces[pos.x, pos.y] = null;
            }
        });

        // Refreshy i zakończenie
        ctx.fow.UpdateFogOfWar(_user.currentX, _user.currentY, ctx.pieces.chessPieces);
        ctx.fow.UpdatePieceVisibility(ctx.pieces.chessPieces);
        ctx.highlight.HighlightPossibleMoves(_user);
        ctx.turn.CheckGameOver();

        Cancel(ctx);
    }

    public void Cancel(AbilityContext ctx)
    {
        // wyczyść magentę i zasięg (cyan)
        foreach (var pos in _previewCells) TintCell(ctx, pos, _rangeCells.Contains(pos) ? Color.cyan : Color.white);
        foreach (var pos in _rangeCells) TintCell(ctx, pos, Color.white);
        _previewCells.Clear();
        _rangeCells.Clear();
        _user = null;
        ctx.board.OnAbilityTargetingFinished();
    }

    // ------------- helpers -------------
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
public struct AbilityContext
{
    public ChessBoard board;
    public TileManager tile;
    public PieceManager pieces;
    public FogOfWarManager fow;
    public HighlightManager highlight;
    public TurnManager turn;

    public AbilityContext(ChessBoard b)
    {
        board = b;
        tile = b.tileManager;
        pieces = b.pieceManager;
        fow = b.fogOfWarManager;
        highlight = b.highlightManager;
        turn = b.turnManager;
    }
}
public interface ITargetedAbility
{
    void StartTargeting(AbilityContext ctx, ChessPieces user);
    void UpdatePreview(AbilityContext ctx, Vector2Int hoverCell);
    void Cast(AbilityContext ctx, Vector2Int targetCell);
    void Cancel(AbilityContext ctx);
}
