using System.Collections.Generic;
using UnityEngine;

public class ZoneEffectManager : MonoBehaviour
{
    public class Zone
    {
        public HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
        public int turnsLeft;
        public float valuePerTurn;   // dmg albo heal per turê
        public bool isHeal;          // true = leczenie, false = obra¿enia
        public int ownerTeam;        // kto „posiada” strefê (u¿ywane przy leczeniu sojuszników)
        public Color color;
    }

    private readonly List<Zone> _zones = new List<Zone>();

    public void AddDamageZone(IEnumerable<Vector2Int> cells, int durationTurns, float damagePerTurn, Color color)
    {
        var z = new Zone { turnsLeft = durationTurns, valuePerTurn = damagePerTurn, color = color, isHeal = false };
        foreach (var c in cells) z.cells.Add(c);
        _zones.Add(z);

        var hm = FindAnyObjectByType<HighlightManager>();
        if (hm != null) hm.AddZoneCells(z.cells, isDamage: true);
    }

    // NOWE: strefa leczenia (zaznaczana na zielono w HighlightManager)
    public void AddHealZone(IEnumerable<Vector2Int> cells, int durationTurns, float healPerTurn, int ownerTeam, Color color)
    {
        var z = new Zone { turnsLeft = durationTurns, valuePerTurn = healPerTurn, color = color, isHeal = true, ownerTeam = ownerTeam };
        foreach (var c in cells) z.cells.Add(c);
        _zones.Add(z);

        var hm = FindAnyObjectByType<HighlightManager>();
        if (hm != null) hm.AddZoneCells(z.cells, isDamage: false);
    }

    // Wywo³uj raz na zmianê tury
    public void TickAndApply()
    {
        if (_zones.Count == 0) return;

        var board = FindAnyObjectByType<ChessBoard>();
        if (board == null || board.pieceManager == null || board.tileManager == null) return;

        var pieces = board.pieceManager.chessPieces;

        foreach (var zone in _zones)
        {
            foreach (var cell in zone.cells)
            {
                var p = pieces[cell.x, cell.y];
                if (p == null) continue;

                if (zone.isHeal)
                {
                    // Leczymy TYLKO sojuszników w³aœciciela strefy
                    if (p.team == zone.ownerTeam)
                    {
                        float before = p.health;
                        p.health = Mathf.Min(p.health + zone.valuePerTurn, p.maxHealth);
                        // (opcjonalny log)
                        // Debug.Log($"[Zone-Heal] +{p.health - before} HP na ({cell.x},{cell.y})");
                    }
                }
                else
                {
                    // Obra¿enia dla ka¿dego stoj¹cego na polu
                    p.health -= zone.valuePerTurn;
                    if (p.health <= 0)
                    {
                        Destroy(p.gameObject);
                        pieces[cell.x, cell.y] = null;
                        Debug.Log($"[Zone-Dmg] Pionek na ({cell.x},{cell.y}) zgin¹³ od strefy.");
                    }
                }
            }

            zone.turnsLeft--;
        }

        // sprz¹tniecie wygas³ych stref
        for (int i = _zones.Count - 1; i >= 0; i--)
        {
            if (_zones[i].turnsLeft <= 0)
            {
                RestoreZoneTiles(_zones[i]);
                _zones.RemoveAt(i);
            }
        }
    }

    private void RestoreZoneTiles(Zone z)
    {
        var hm = FindAnyObjectByType<HighlightManager>();
        if (hm != null)
        {
            // isDamage == !isHeal
            hm.RemoveZoneCells(z.cells, isDamage: !z.isHeal);
            hm.ResetTileColors();
        }
    }
}
