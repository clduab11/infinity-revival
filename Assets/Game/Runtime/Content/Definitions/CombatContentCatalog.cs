using System;
using System.Collections.Generic;
using Praxen.Game.Domain.Combat;
using Praxen.Game.Domain.Content;
using UnityEngine;

namespace Praxen.Game.Content.Definitions
{
    [CreateAssetMenu(menuName = "Praxen/Content/Combat Catalog")]
    public sealed class CombatContentCatalog : ContentDefinition
    {
        public WeaponDefinition[] Weapons = Array.Empty<WeaponDefinition>();
        public EnemyDeckDefinition[] EnemyDecks = Array.Empty<EnemyDeckDefinition>();
        public DefenseDefinition Defense;
        public CameraProfileDefinition Camera;

        public EncounterContentSnapshot ToRuntime(EnemyArchetype archetype, string weaponId)
        {
            Validate().ThrowIfInvalid();
            var definition = FindWeapon(weaponId);
            EnemyDeckDefinition selectedDeck = null;
            foreach (var deck in EnemyDecks) if (deck.Archetype == archetype) selectedDeck = deck;
            if (selectedDeck == null) throw new ArgumentException("No authored deck for the requested archetype.", nameof(archetype));
            var weapon = definition.BuildRuntime();
            return new EncounterContentSnapshot(weapon, weapon.ToOffenseTuning(), Defense.BuildRuntime(), selectedDeck.BuildRuntime());
        }

        public WeaponDefinition GetWeapon(string id)
        { Validate().ThrowIfInvalid(); return FindWeapon(id); }

        private WeaponDefinition FindWeapon(string id)
        {
            foreach (var weapon in Weapons) if (string.Equals(weapon.Id, id, StringComparison.Ordinal)) return weapon;
            throw new ArgumentException("No authored weapon with this stable ID.", nameof(id));
        }

        internal override void ValidateFields(ContentValidationContext context)
        {
            context.Child(this, "Defense", Defense);
            context.Child(this, "Camera", Camera);
            context.Require(Weapons != null && Weapons.Length > 0, this, "Weapons", "A catalog requires weapons.");
            if (Weapons != null)
                for (int i = 0; i < Weapons.Length; i++) context.Child(this, "Weapons[" + i + "]", Weapons[i]);
            context.Require(EnemyDecks != null && EnemyDecks.Length > 0, this, "EnemyDecks", "A catalog requires enemy decks.");
            if (EnemyDecks == null) return;
            var archetypes = new Dictionary<EnemyArchetype, EnemyDeckDefinition>();
            for (int i = 0; i < EnemyDecks.Length; i++)
            {
                var deck = EnemyDecks[i];
                context.Child(this, "EnemyDecks[" + i + "]", deck);
                if (deck == null) continue;
                if (archetypes.TryGetValue(deck.Archetype, out var prior) && !ReferenceEquals(prior, deck))
                    context.Add(this, "EnemyDecks", "Each archetype must resolve to one distinct deck.");
                else archetypes[deck.Archetype] = deck;
            }
        }
    }
}
