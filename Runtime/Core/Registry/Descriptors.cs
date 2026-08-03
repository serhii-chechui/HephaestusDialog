using System.Collections.Generic;

namespace WTFGames.Hephaestus.Dialog {
    /// <summary>The editor-facing type of a parameter field, so a generic inspector can be drawn.</summary>
    public enum ParamKind {
        String,
        Int,
        Float,
        Bool,
        Enum,
        Ref
    }

    /// <summary>Describes one parameter of a condition/action type for the authoring inspector.</summary>
    public sealed class ParamField {
        public string Name { get; }
        public ParamKind Kind { get; }
        public bool Required { get; }
        /// <summary>Allowed values for <see cref="ParamKind.Enum"/>; otherwise null.</summary>
        public IReadOnlyList<string> Options { get; }

        public ParamField(string name, ParamKind kind, bool required = true, IReadOnlyList<string> options = null) {
            Name = name;
            Kind = kind;
            Required = required;
            Options = options;
        }
    }

    /// <summary>Editor metadata for a registered condition type (drawn by the descriptor-driven inspector).</summary>
    public sealed class ConditionDescriptor {
        public string TypeId { get; }
        public string DisplayName { get; }
        public IReadOnlyList<ParamField> Params { get; }

        public ConditionDescriptor(string typeId, string displayName, IReadOnlyList<ParamField> @params = null) {
            TypeId = typeId;
            DisplayName = displayName ?? typeId;
            Params = @params ?? System.Array.Empty<ParamField>();
        }
    }

    /// <summary>Editor metadata for a registered action type.</summary>
    public sealed class ActionDescriptor {
        public string TypeId { get; }
        public string DisplayName { get; }
        public IReadOnlyList<ParamField> Params { get; }

        public ActionDescriptor(string typeId, string displayName, IReadOnlyList<ParamField> @params = null) {
            TypeId = typeId;
            DisplayName = displayName ?? typeId;
            Params = @params ?? System.Array.Empty<ParamField>();
        }
    }
}
