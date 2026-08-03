using System.Collections.Generic;

namespace WTFGames.Hephaestus.Dialog {
    /// <summary>An immutable dialog graph: an id, an entry node id, and the set of nodes.</summary>
    public interface IDialogGraph {
        string Id { get; }
        string EntryId { get; }
        IReadOnlyList<IDialogNode> Nodes { get; }

        /// <summary>Returns the node with the given id, or null if absent.</summary>
        IDialogNode Find(string nodeId);
    }

    /// <inheritdoc cref="IDialogGraph"/>
    public sealed class DialogGraph : IDialogGraph {
        private readonly Dictionary<string, IDialogNode> _byId;

        public string Id { get; }
        public string EntryId { get; }
        public IReadOnlyList<IDialogNode> Nodes { get; }

        public DialogGraph(string id, string entryId, IReadOnlyList<IDialogNode> nodes) {
            Id = id;
            EntryId = entryId;
            Nodes = nodes ?? System.Array.Empty<IDialogNode>();

            _byId = new Dictionary<string, IDialogNode>(Nodes.Count);
            foreach (var node in Nodes) {
                if (node == null) {
                    continue;
                }
                if (_byId.ContainsKey(node.Id)) {
                    throw new DialogException($"Duplicate node id '{node.Id}' in dialog '{id}'.");
                }
                _byId.Add(node.Id, node);
            }
        }

        public IDialogNode Find(string nodeId) {
            return nodeId != null && _byId.TryGetValue(nodeId, out var node) ? node : null;
        }
    }
}
