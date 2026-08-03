using System.Collections.Generic;

namespace WTFGames.Hephaestus.Dialog {
    /// <summary>
    /// Authored description of a condition as data: a registered <see cref="Type"/> id plus a
    /// bag of <see cref="Params"/>. The core never evaluates it directly — the game registers a
    /// handler for the type via <see cref="IDialogRegistry"/>. Serializes as
    /// <c>{ "type": "...", "params": { ... } }</c>.
    /// </summary>
    public sealed class ConditionSpec {
        public string Type { get; }
        public IReadOnlyDictionary<string, object> Params { get; }

        public ConditionSpec(string type, IReadOnlyDictionary<string, object> @params = null) {
            Type = type;
            Params = @params ?? EmptyParams;
        }

        internal static readonly IReadOnlyDictionary<string, object> EmptyParams =
            new Dictionary<string, object>();
    }

    /// <summary>Authored description of an action as data. Same envelope as <see cref="ConditionSpec"/>.</summary>
    public sealed class ActionSpec {
        public string Type { get; }
        public IReadOnlyDictionary<string, object> Params { get; }

        public ActionSpec(string type, IReadOnlyDictionary<string, object> @params = null) {
            Type = type;
            Params = @params ?? ConditionSpec.EmptyParams;
        }
    }
}
