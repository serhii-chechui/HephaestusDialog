using System;
using System.Collections.Generic;
using System.Linq;

namespace WTFGames.Hephaestus.Dialog.Editor {
    /// <summary>
    /// Stub <see cref="IDialogRegistry"/> for the in-editor preview (B2). Conditions resolve to the
    /// author-controlled <see cref="ConditionsPass"/> toggle instead of running the real handlers
    /// (which need the game); actions are appended to <see cref="ActionLog"/> instead of executing.
    /// Lets the author click through branches without entering Play mode.
    /// </summary>
    public sealed class PreviewRegistry : IDialogRegistry {
        public bool ConditionsPass = true;
        public readonly List<string> ActionLog = new List<string>();

        public void RegisterCondition(string typeId, IConditionHandler handler, ConditionDescriptor descriptor) { }
        public void RegisterAction(string typeId, IActionHandler handler, ActionDescriptor descriptor) { }

        public bool IsConditionRegistered(string typeId) => true;
        public bool IsActionRegistered(string typeId) => true;

        public bool Evaluate(ConditionSpec spec, IDialogContext context) => spec == null || ConditionsPass;

        public void Execute(ActionSpec spec, IDialogContext context) {
            if (spec != null) {
                ActionLog.Add(Describe(spec));
            }
        }

        public IReadOnlyCollection<ConditionDescriptor> ConditionDescriptors => Array.Empty<ConditionDescriptor>();
        public IReadOnlyCollection<ActionDescriptor> ActionDescriptors => Array.Empty<ActionDescriptor>();

        private static string Describe(ActionSpec spec) {
            var args = string.Join(", ", spec.Params.Select(p => $"{p.Key}={p.Value}"));
            return $"{spec.Type}({args})";
        }
    }
}
