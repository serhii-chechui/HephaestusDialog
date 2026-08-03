namespace WTFGames.Hephaestus.Dialog {
    /// <summary>
    /// Per-conversation context handed to condition/action handlers. Intentionally minimal in
    /// the core; games may implement it with additional members their handlers cast to. Handlers
    /// get their own dependencies (check evaluator, save system, …) at registration time, so the
    /// context only needs to carry conversation-scoped info.
    /// </summary>
    public interface IDialogContext {
        /// <summary>The id of the dialog currently running.</summary>
        string DialogId { get; }
    }

    /// <summary>Default minimal context.</summary>
    public sealed class DialogContext : IDialogContext {
        public string DialogId { get; }

        public DialogContext(string dialogId) {
            DialogId = dialogId;
        }
    }
}
