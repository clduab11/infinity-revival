using System;
using Praxen.Game.Domain.Combat;
using UnityEngine;

namespace Praxen.Game.Content.Definitions
{
    [CreateAssetMenu(menuName = "Praxen/Content/Enemy Deck")]
    public sealed class EnemyDeckDefinition : ContentDefinition
    {
        public EnemyArchetype Archetype;
        public string Lesson, ContentRevision, BalanceRevision, IntroPatternId;
        public EnemyPatternDefinition[] Patterns = Array.Empty<EnemyPatternDefinition>();

        public EnemyPatternDeck ToRuntime()
        { Validate().ThrowIfInvalid(); return BuildRuntime(); }
        internal EnemyPatternDeck BuildRuntime()
        {
            var patterns = new EnemyPattern[Patterns.Length];
            for (int i = 0; i < patterns.Length; i++) patterns[i] = Patterns[i].BuildRuntime();
            return new EnemyPatternDeck(Id, Archetype, Lesson, ContentRevision, BalanceRevision, patterns,
                string.IsNullOrEmpty(IntroPatternId) ? null : IntroPatternId);
        }
        internal override void ValidateFields(ContentValidationContext context)
        {
            context.Text(this, "Lesson", Lesson);
            context.Text(this, "ContentRevision", ContentRevision);
            context.Text(this, "BalanceRevision", BalanceRevision);
            context.Require(Patterns != null && Patterns.Length >= 1 && Patterns.Length <= 16, this,
                "Patterns", "A regular deck requires one to sixteen patterns.");
            if (Patterns == null) return;
            bool complete = true;
            for (int i = 0; i < Patterns.Length; i++)
            {
                context.Child(this, "Patterns[" + i + "]", Patterns[i]);
                complete &= Patterns[i] != null && Patterns[i].Steps != null;
                if (Patterns[i] != null && Patterns[i].Steps != null)
                    foreach (var step in Patterns[i].Steps) complete &= step.Attack != null;
            }
            if (complete) context.Attempt(this, "Patterns", () => BuildRuntime());
        }
    }
}
