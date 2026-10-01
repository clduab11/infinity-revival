using UnityEngine;

namespace Praxen.Game.Content.Definitions
{
    public abstract class ContentDefinition : ScriptableObject
    {
        public string Id;
        public int Version = 1;
        public string LocalizationKey;

        public virtual ContentValidationResult Validate()
        {
            var context = new ContentValidationContext();
            context.Visit(this);
            return context.Result;
        }

        internal abstract void ValidateFields(ContentValidationContext context);
    }
}
