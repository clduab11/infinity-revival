using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Praxen.Game.Content.Definitions
{
    public sealed class ContentValidationIssue
    {
        public string AssetId { get; }
        public string Field { get; }
        public string Message { get; }
        public ContentValidationIssue(string assetId, string field, string message)
        { AssetId = assetId; Field = field; Message = message; }
        public override string ToString() => AssetId + "." + Field + ": " + Message;
    }

    public sealed class ContentValidationResult
    {
        public IReadOnlyList<ContentValidationIssue> Issues { get; }
        public bool IsValid => Issues.Count == 0;
        internal ContentValidationResult(List<ContentValidationIssue> issues)
            => Issues = Array.AsReadOnly(issues.ToArray());
        public void ThrowIfInvalid()
        {
            if (!IsValid) throw new ContentValidationException(this);
        }
    }

    public sealed class ContentValidationException : InvalidOperationException
    {
        public ContentValidationResult Result { get; }
        public ContentValidationException(ContentValidationResult result)
            : base(string.Join("\n", result.Issues)) => Result = result;
    }

    internal sealed class ContentValidationContext
    {
        private readonly List<ContentValidationIssue> issues = new List<ContentValidationIssue>();
        private readonly Dictionary<string, ContentDefinition> identities =
            new Dictionary<string, ContentDefinition>(StringComparer.Ordinal);
        private readonly HashSet<ContentDefinition> visited =
            new HashSet<ContentDefinition>(new ReferenceComparer());

        internal ContentValidationResult Result => new ContentValidationResult(issues);
        internal void Visit(ContentDefinition definition)
        {
            if (!visited.Add(definition)) return;
            string id = definition.Id;
            Require(id != null && Regex.IsMatch(id, "^[a-z][a-z0-9_-]*(\\.[a-z][a-z0-9_-]*)+$"),
                definition, "Id", "Use an explicit lowercase namespaced ID, independent of asset names.");
            if (!string.IsNullOrEmpty(id))
            {
                if (identities.TryGetValue(id, out var previous) && !ReferenceEquals(previous, definition))
                    Add(definition, "Id", "Another definition uses this stable ID; assign a distinct ID.");
                else identities[id] = definition;
            }
            Require(definition.Version > 0, definition, "Version", "Content version must be positive.");
            Text(definition, "LocalizationKey", definition.LocalizationKey);
            definition.ValidateFields(this);
        }

        internal void Child(ContentDefinition parent, string field, ContentDefinition child)
        {
            if (child == null) Add(parent, field, "Assign the required content reference.");
            else Visit(child);
        }

        internal void Text(ContentDefinition asset, string field, string value)
            => Require(!string.IsNullOrWhiteSpace(value), asset, field, "Supply a nonblank value.");
        internal void Require(bool valid, ContentDefinition asset, string field, string message)
        { if (!valid) Add(asset, field, message); }
        internal void Add(ContentDefinition asset, string field, string message)
            => issues.Add(new ContentValidationIssue(string.IsNullOrWhiteSpace(asset.Id) ? "<missing-id>" : asset.Id,
                field, message));

        internal void Attempt(ContentDefinition asset, string field, Action action)
        {
            try { action(); }
            catch (ArgumentException exception)
            {
                string parameter = exception.ParamName;
                Add(asset, AuthoredField(parameter, field), exception.Message);
            }
            catch (OverflowException) { Add(asset, field, "Timing or damage arithmetic overflows; reduce the authored values."); }
        }

        private static string AuthoredField(string parameter, string fallback)
        {
            switch (parameter)
            {
                case "attackDamage": case "healthDamage": return "Damage";
                case "comboBonusPercent": return "ComboDamageBonus";
                case "attackId": return "Id";
                default: return string.IsNullOrEmpty(parameter) ? fallback :
                    char.ToUpperInvariant(parameter[0]) + parameter.Substring(1);
            }
        }

        private sealed class ReferenceComparer : IEqualityComparer<ContentDefinition>
        {
            public bool Equals(ContentDefinition left, ContentDefinition right) => ReferenceEquals(left, right);
            public int GetHashCode(ContentDefinition value) => RuntimeHelpers.GetHashCode(value);
        }
    }
}
