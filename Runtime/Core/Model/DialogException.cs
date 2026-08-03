using System;

namespace WTFGames.Hephaestus.Dialog {
    /// <summary>Thrown for malformed graphs, unregistered types, and invalid runner transitions.</summary>
    public sealed class DialogException : Exception {
        public DialogException(string message) : base(message) { }
        public DialogException(string message, Exception inner) : base(message, inner) { }
    }
}
