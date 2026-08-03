using System.Collections.Generic;

namespace WTFGames.Hephaestus.Dialog.Editor {
    /// <summary>Editor-only placement of one node on the canvas.</summary>
    public sealed class NodeLayout {
        public float X { get; set; }
        public float Y { get; set; }
        public string Color { get; set; }

        public NodeLayout() { }

        public NodeLayout(float x, float y, string color = null) {
            X = x;
            Y = y;
            Color = color;
        }
    }

    /// <summary>Editor-only free-floating comment on the canvas.</summary>
    public sealed class CommentLayout {
        public float X { get; set; }
        public float Y { get; set; }
        public string Text { get; set; }
    }

    /// <summary>
    /// Editor-only presentation data for a dialog graph (node positions/colors, comments). Written
    /// as the sidecar <c>{id}.layout.json</c> that the runtime never reads (Q8), so the runtime
    /// <c>{id}.dialog.json</c> stays pure logic while the editor still remembers the canvas.
    /// </summary>
    public sealed class DialogLayout {
        public Dictionary<string, NodeLayout> Nodes { get; } = new Dictionary<string, NodeLayout>();
        public List<CommentLayout> Comments { get; } = new List<CommentLayout>();
    }
}
