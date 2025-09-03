using UnityEngine;

public class BestiaryBonusesApplier : MonoBehaviour
{
    private bool _applied;

    public void ApplyForTeam(int team, PieceManager pieceManager)
    {
        if (_applied) return;

        if (GameData.Instance == null || !GameData.Instance.IsBestiaryActiveForCurrentMode())
        {
            Debug.Log("[Bestiary][BonusesApplier] Inactive for current mode.");
            return;
        }

        var mgr = BestiaryManager.Instance;
        if (mgr == null)
        {
            Debug.LogWarning("[Bestiary][BonusesApplier] BestiaryManager == null");
            return;
        }

        var totals = mgr.GetTotals();
        Debug.Log($"[Bestiary][BonusesApplier] Applying totals -> {totals}");

        foreach (var piece in pieceManager.chessPieces)
        {
            if (piece == null || piece.team != team) continue;

            // % do ataku (attack zwykle float)
            if (totals.attackPct > 0f)
                piece.attack *= (1f + totals.attackPct);

            // % do max HP (i current HP) — ZAOKRĄGLAMY DO INT jeśli pola są intami
            if (totals.maxHealthPct > 0f)
            {
                float mul = 1f + totals.maxHealthPct;

                // jeśli masz floaty – też zadziała, bo przypiszemy int->float bez błędu
                int newMax = Mathf.RoundToInt(piece.maxHealth * mul);
                int newCur = Mathf.RoundToInt(piece.health * mul);

                piece.maxHealth = newMax;
                piece.health = Mathf.Min(newCur, newMax);
            }

            // FLAT do ruchu
            if (totals.movementAdd != 0)
            {
                piece.movementRange += totals.movementAdd;
                piece.maxMovementRange += totals.movementAdd;
            }

            if (totals.maxMovementAdd != 0)
                piece.maxMovementRange += totals.maxMovementAdd;

            if (totals.attackRangeAdd != 0)
                piece.attackRange += totals.attackRangeAdd;

            // ✅ widoczność jako INT (działa też gdy field jest float)
            piece.visionRange = Mathf.RoundToInt((piece.attackRange + piece.maxMovementRange) / 2f);
        }

        _applied = true;
    }
}
