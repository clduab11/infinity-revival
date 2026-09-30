using System;
using System.Collections.Generic;

namespace Praxen.Game.Domain.Combat
{
    public sealed class EnemyPatternDecision
    {
        public EnemyPattern Pattern { get; }
        public long PreparedAtUs { get; }
        public uint RandomStateBefore { get; }
        public uint RandomStateAfter { get; }
        public int SelectionIndex { get; }

        internal EnemyPatternSelector Owner { get; }
        internal int Generation { get; }
        internal int PatternIndex { get; }

        internal EnemyPatternDecision(EnemyPatternSelector owner, int generation, int patternIndex,
            EnemyPattern pattern, long preparedAtUs, uint randomStateBefore, uint randomStateAfter)
        {
            Owner = owner;
            Generation = generation;
            PatternIndex = patternIndex;
            Pattern = pattern;
            PreparedAtUs = preparedAtUs;
            RandomStateBefore = randomStateBefore;
            RandomStateAfter = randomStateAfter;
            SelectionIndex = generation;
        }
    }

    public sealed class EnemyPatternSelector
    {
        private const uint NormalizedZeroSeed = 0x6D2B79F5u;
        private readonly int _difficultyTier;
        private readonly int _introPatternIndex;
        private readonly long[] _readyAtUs;
        private readonly List<string> _history = new List<string>(2);
        private long _lastCommitTimeUs;

        public EnemyPatternDeck Deck { get; }
        public uint InitialSeed { get; }
        public uint RandomState { get; private set; }
        public int SelectionCount { get; private set; }
        public IReadOnlyList<string> History => Array.AsReadOnly(_history.ToArray());

        public EnemyPatternSelector(EnemyPatternDeck deck, uint seed, int difficultyTier = 0)
        {
            if (deck == null) throw new ArgumentNullException(nameof(deck));
            if (difficultyTier < 0 || difficultyTier > 5)
                throw new ArgumentOutOfRangeException(nameof(difficultyTier));
            Deck = deck;
            InitialSeed = seed == 0 ? NormalizedZeroSeed : seed;
            RandomState = InitialSeed;
            _difficultyTier = difficultyTier;
            _readyAtUs = new long[deck.Patterns.Count];
            _introPatternIndex = FindIntroPatternIndex(deck);
        }

        public bool TryPrepare(long timeUs, EnemyObservation observation,
            out EnemyPatternDecision decision, out long? nextEligibleTimeUs)
        {
            if (timeUs < 0) throw new ArgumentOutOfRangeException(nameof(timeUs));
            decision = null;
            nextEligibleTimeUs = null;
            if (timeUs < _lastCommitTimeUs || SelectionCount == int.MaxValue) return false;
            List<int> candidates = EligibleCandidates(timeUs, observation, out nextEligibleTimeUs);
            if (candidates.Count == 0) return false;
            nextEligibleTimeUs = null;

            uint after = RandomState;
            int patternIndex = SelectionCount == 0 && candidates.Contains(_introPatternIndex)
                ? _introPatternIndex
                : DrawPattern(candidates, observation, ref after);
            decision = new EnemyPatternDecision(this, SelectionCount, patternIndex,
                Deck.Patterns[patternIndex], timeUs, RandomState, after);
            return true;
        }

        public bool CanCommit(EnemyPatternDecision decision, long timeUs)
        {
            return PrevalidateCommit(decision, timeUs, out _);
        }

        public bool TryCommit(EnemyPatternDecision decision, long timeUs)
        {
            if (!PrevalidateCommit(decision, timeUs, out long readyAtUs)) return false;
            _readyAtUs[decision.PatternIndex] = readyAtUs;
            _lastCommitTimeUs = timeUs;
            RandomState = decision.RandomStateAfter;
            SelectionCount++;
            if (_history.Count == 2) _history.RemoveAt(0);
            _history.Add(decision.Pattern.Id);
            return true;
        }

        private List<int> EligibleCandidates(long timeUs, EnemyObservation observation,
            out long? nextEligibleTimeUs)
        {
            var candidates = new List<int>(Deck.Patterns.Count);
            nextEligibleTimeUs = null;
            for (int index = 0; index < Deck.Patterns.Count; index++)
            {
                if (!Deck.Patterns[index].IsEligible(observation, _difficultyTier)) continue;
                long readyAtUs = _readyAtUs[index];
                if (timeUs >= readyAtUs) candidates.Add(index);
                else if (!nextEligibleTimeUs.HasValue || readyAtUs < nextEligibleTimeUs.Value)
                    nextEligibleTimeUs = readyAtUs;
            }
            return candidates;
        }

        private int DrawPattern(List<int> candidates, EnemyObservation observation, ref uint state)
        {
            var weights = new uint[candidates.Count];
            uint total = 0;
            for (int index = 0; index < candidates.Count; index++)
            {
                uint weight = EffectiveWeight(Deck.Patterns[candidates[index]], observation,
                    candidates.Count > 1);
                weights[index] = weight;
                total = checked(total + weight);
            }

            uint draw = DrawBounded(ref state, total);
            for (int index = 0; index < candidates.Count; index++)
            {
                if (draw < weights[index]) return candidates[index];
                draw -= weights[index];
            }
            throw new InvalidOperationException("Enemy pattern weight selection exhausted its range.");
        }

        private uint EffectiveWeight(EnemyPattern pattern, EnemyObservation observation,
            bool suppressImmediateRepeat)
        {
            if (suppressImmediateRepeat && _history.Count > 0 &&
                pattern.Id == _history[_history.Count - 1]) return 0;
            uint weight = (uint)pattern.Weight;
            if (_history.Count == 2 && pattern.Id == _history[0])
                weight = Math.Max(1u, weight / 2);
            if (pattern.PreferredDefenseOutcome.HasValue &&
                pattern.PreferredDefenseOutcome == observation.LastDefenseOutcome)
                weight = checked(weight * 2);
            return weight;
        }

        private bool PrevalidateCommit(EnemyPatternDecision decision, long timeUs, out long readyAtUs)
        {
            readyAtUs = 0;
            if (decision == null || !ReferenceEquals(decision.Owner, this) ||
                decision.Generation != SelectionCount || decision.SelectionIndex != SelectionCount ||
                decision.RandomStateBefore != RandomState || SelectionCount == int.MaxValue ||
                timeUs < 0 || timeUs < decision.PreparedAtUs || timeUs < _lastCommitTimeUs)
                return false;
            int index = decision.PatternIndex;
            if (index < 0 || index >= Deck.Patterns.Count ||
                !ReferenceEquals(decision.Pattern, Deck.Patterns[index]) || timeUs < _readyAtUs[index])
                return false;
            try
            {
                readyAtUs = checked(timeUs + decision.Pattern.CooldownUs);
                return true;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        private static uint DrawBounded(ref uint state, uint bound)
        {
            // Xorshift32 has 2^32 - 1 nonzero states. Translate to zero-based samples
            // and reject the upper tail so every authored weight has equal coverage.
            uint limit = uint.MaxValue - uint.MaxValue % bound;
            uint sample;
            do
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
                sample = state - 1;
            } while (sample >= limit);
            return sample % bound;
        }

        private static int FindIntroPatternIndex(EnemyPatternDeck deck)
        {
            if (deck.IntroPatternId == null) return -1;
            for (int index = 0; index < deck.Patterns.Count; index++)
                if (deck.Patterns[index].Id == deck.IntroPatternId) return index;
            return -1;
        }
    }
}
