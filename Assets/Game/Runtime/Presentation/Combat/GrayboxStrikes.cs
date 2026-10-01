using System.Collections.Generic;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Input;

namespace Praxen.Game.Presentation.Combat
{
    /// <summary>Fixed validation fixtures, not enemy AI or production attack balance.</summary>
    public static class GrayboxStrikes
    {
        public static EnemyStrike Create(long id)
        {
            switch ((id - 1) % 5)
            {
                case 0: return new EnemyStrike(id, DefenseMask.Guard | DefenseMask.Parry | DefenseMask.Dodge,
                    DodgeSide.Left, SwipeDirection.Up, 20, 25);
                case 1: return new EnemyStrike(id, DefenseMask.Guard | DefenseMask.Dodge,
                    DodgeSide.Right, SwipeDirection.Down, 40, 35);
                case 2: return new EnemyStrike(id, DefenseMask.Parry | DefenseMask.Dodge,
                    DodgeSide.Both, SwipeDirection.Left, 0, 30);
                case 3: return new EnemyStrike(id, DefenseMask.Guard,
                    DodgeSide.None, SwipeDirection.Right, 100, 40);
                default: return new EnemyStrike(id, DefenseMask.Parry,
                    DodgeSide.None, SwipeDirection.Down, 0, 20);
            }
        }

        public static string Describe(EnemyStrike strike)
        {
            var defenses = new List<string>();
            if ((strike.AllowedDefenses & DefenseMask.Guard) != 0)
                defenses.Add(HudText.Get("tell.guard"));
            if ((strike.AllowedDefenses & DefenseMask.Parry) != 0)
                defenses.Add(HudText.Format("tell.parry", HudText.Get("direction." + strike.RequiredParryDirection).ToUpperInvariant()));
            if ((strike.AllowedDefenses & DefenseMask.Dodge) != 0)
                defenses.Add(HudText.Format("tell.dodge", HudText.Get("dodge." + strike.SafeDodgeSides)));
            return string.Join("   ", defenses);
        }
    }
}
