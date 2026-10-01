using System;
using System.Text.RegularExpressions;

namespace Praxen.Game.Domain.Content
{
    internal static class ContentIdentity
    {
        internal static void Validate(string id, int version, string localizationKey)
        {
            if (id == null || !Regex.IsMatch(id, "^[a-z][a-z0-9_-]*(\\.[a-z][a-z0-9_-]*)+$"))
                throw new ArgumentException("Use an explicit lowercase namespaced content ID.", nameof(id));
            if (version <= 0) throw new ArgumentOutOfRangeException(nameof(version));
            if (string.IsNullOrWhiteSpace(localizationKey))
                throw new ArgumentException("A localization key is required.", nameof(localizationKey));
        }
    }
}
