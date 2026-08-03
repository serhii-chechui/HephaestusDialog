namespace WTFGames.Hephaestus.Dialog {
    /// <summary>The typed kinds of node in a dialog graph.</summary>
    public enum NodeType {
        Entry,
        Line,
        Choice,
        Condition,
        Action,
        Exit
    }

    /// <summary>A single node in a dialog graph. Nodes are referenced by string <see cref="Id"/>.</summary>
    public interface IDialogNode {
        string Id { get; }
        NodeType Type { get; }
    }

    /// <summary>Base class for the concrete node types; carries the id and kind.</summary>
    public abstract class DialogNode : IDialogNode {
        public string Id { get; }
        public abstract NodeType Type { get; }

        protected DialogNode(string id) {
            Id = id ?? throw new DialogException("A node id cannot be null.");
        }
    }
}
