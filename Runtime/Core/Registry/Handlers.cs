using System.Collections.Generic;

namespace WTFGames.Hephaestus.Dialog {
    /// <summary>
    /// Game-supplied evaluator for one condition type id (e.g. <c>stat.check</c>). Reads the
    /// authored params and the runtime context and returns pass/fail.
    /// </summary>
    public interface IConditionHandler {
        bool Evaluate(IReadOnlyDictionary<string, object> @params, IDialogContext context);
    }

    /// <summary>Game-supplied effect for one action type id (e.g. <c>flag.set</c>).</summary>
    public interface IActionHandler {
        void Execute(IReadOnlyDictionary<string, object> @params, IDialogContext context);
    }
}
