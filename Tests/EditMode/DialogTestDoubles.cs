using System.Collections.Generic;
using WTFGames.Hephaestus.Dialog;

namespace WTFGames.Hephaestus.Dialog.Tests {
    /// <summary>Action handler that appends its <c>tag</c> param to a shared log (asserts ordering).</summary>
    internal sealed class LogActionHandler : IActionHandler {
        private readonly List<string> _log;
        public LogActionHandler(List<string> log) => _log = log;
        public void Execute(IReadOnlyDictionary<string, object> @params, IDialogContext context) {
            _log.Add(@params.GetString("tag"));
        }
    }

    /// <summary>Condition handler that returns its <c>value</c> param — deterministic gating.</summary>
    internal sealed class ConstConditionHandler : IConditionHandler {
        public bool Evaluate(IReadOnlyDictionary<string, object> @params, IDialogContext context) {
            return @params.GetBool("value");
        }
    }

    /// <summary>Records everything the runner asked to present.</summary>
    internal sealed class FakePresenter : IDialogPresenter {
        public readonly List<LineView> Lines = new List<LineView>();
        public readonly List<IReadOnlyList<ChoiceView>> ChoiceSets = new List<IReadOnlyList<ChoiceView>>();
        public int ClearCount;

        public void ShowLine(LineView line) => Lines.Add(line);
        public void ShowChoices(IReadOnlyList<ChoiceView> choices) => ChoiceSets.Add(choices);
        public void Clear() => ClearCount++;
    }

    internal static class TestRegistry {
        public const string LogAction = "log";
        public const string ConstCondition = "const";

        public static DialogRegistry Build(List<string> actionLog) {
            var registry = new DialogRegistry();
            registry.RegisterAction(LogAction, new LogActionHandler(actionLog),
                new ActionDescriptor(LogAction, "Log", new[] { new ParamField("tag", ParamKind.String) }));
            registry.RegisterCondition(ConstCondition, new ConstConditionHandler(),
                new ConditionDescriptor(ConstCondition, "Const", new[] { new ParamField("value", ParamKind.Bool) }));
            return registry;
        }

        public static ActionSpec Log(string tag) =>
            new ActionSpec(LogAction, new Dictionary<string, object> { ["tag"] = tag });

        public static ConditionSpec Const(bool value) =>
            new ConditionSpec(ConstCondition, new Dictionary<string, object> { ["value"] = value });
    }
}
