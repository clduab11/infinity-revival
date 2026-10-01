using System;
using Praxen.Game.Domain.Combat;

namespace Praxen.Game.Domain.Content
{
    public sealed class EncounterContentSnapshot
    {
        public WeaponSnapshot Weapon { get; }
        public CombatOffenseTuning Offense { get; }
        public CombatDefenseTuning Defense { get; }
        public EnemyPatternDeck EnemyDeck { get; }

        public EncounterContentSnapshot(WeaponSnapshot weapon, CombatOffenseTuning offense,
            CombatDefenseTuning defense, EnemyPatternDeck enemyDeck)
        {
            Weapon = weapon ?? throw new ArgumentNullException(nameof(weapon));
            Offense = offense ?? throw new ArgumentNullException(nameof(offense));
            Defense = defense ?? throw new ArgumentNullException(nameof(defense));
            EnemyDeck = enemyDeck ?? throw new ArgumentNullException(nameof(enemyDeck));
        }
    }
}
