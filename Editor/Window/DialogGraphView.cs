using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace WTFGames.Hephaestus.Dialog.Editor {
    /// <summary>
    /// The dialog editing canvas: pan/zoom, selection, a grid; creating nodes, loading a graph for
    /// display/editing, and rebuilding the graph + layout + strings back from its nodes/edges. Kept
    /// behind our own type so the window and editing code depend on this rather than on GraphView
    /// directly (risk-1).
    /// </summary>
    public class DialogGraphView : GraphView {
        private const float NodeWidth = 240f;
        private const float NodeHeight = 150f;

        private int _createdCount;

        public DialogGraphView() {
            style.flexGrow = 1;

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();
        }

        public IEnumerable<DialogNodeView> NodeViews => nodes.ToList().OfType<DialogNodeView>();

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter) {
            var compatible = new List<Port>();
            ports.ForEach(port => {
                if (port != startPort && port.node != startPort.node && port.direction != startPort.direction) {
                    compatible.Add(port);
                }
            });
            return compatible;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt) {
            foreach (NodeType type in Enum.GetValues(typeof(NodeType))) {
                var captured = type;
                evt.menu.AppendAction($"Add Node/{captured}", _ => CreateNode(captured));
            }
            base.BuildContextualMenu(evt);
        }

        public void Clear() {
            foreach (var element in graphElements.ToList()) {
                RemoveElement(element);
            }
        }

        public void LoadGraph(IDialogGraph graph, DialogLayout layout, Func<string, string> resolveText) {
            Clear();
            if (graph == null) {
                return;
            }

            var views = new Dictionary<string, DialogNodeView>();
            var fallbackIndex = 0;
            foreach (var node in graph.Nodes) {
                var view = new DialogNodeView(node.Type, node.Id);
                view.Load(node, resolveText);

                if (layout != null && layout.Nodes.TryGetValue(node.Id, out var placement)) {
                    view.SetPosition(new Rect(placement.X, placement.Y, NodeWidth, NodeHeight));
                } else {
                    view.SetPosition(FallbackPosition(fallbackIndex++));
                }

                AddElement(view);
                views[node.Id] = view;
            }

            foreach (var view in views.Values) {
                foreach (var (port, targetId) in view.LoadedEdges()) {
                    if (!string.IsNullOrEmpty(targetId)
                        && views.TryGetValue(targetId, out var target)
                        && target.InputPort != null) {
                        AddElement(port.ConnectTo(target.InputPort));
                    }
                }
            }
        }

        public IDialogGraph BuildGraph(string dialogId) {
            var nodes = new List<IDialogNode>();
            string entryId = null;
            foreach (var view in NodeViews) {
                if (view.NodeType == NodeType.Entry) {
                    entryId = view.NodeId;
                }
                nodes.Add(view.BuildModel());
            }
            return new DialogGraph(dialogId, entryId, nodes);
        }

        public DialogLayout BuildLayout() {
            var layout = new DialogLayout();
            foreach (var view in NodeViews) {
                var rect = view.GetPosition();
                layout.Nodes[view.NodeId] = new NodeLayout(rect.x, rect.y);
            }
            return layout;
        }

        /// <summary>Collects the authored {key -> text} entries from all nodes.</summary>
        public void CollectStrings(IDictionary<string, string> strings) {
            foreach (var view in NodeViews) {
                view.CollectStrings(strings);
            }
        }

        /// <summary>Highlights the node with the given id (yellow border) and clears the rest. Used by the preview.</summary>
        public void HighlightNode(string id) {
            foreach (var view in NodeViews) {
                var on = id != null && view.NodeId == id;
                var color = on ? new StyleColor(Color.yellow) : new StyleColor(StyleKeyword.Null);
                var width = on ? new StyleFloat(2f) : new StyleFloat(StyleKeyword.Null);
                view.style.borderTopColor = color;
                view.style.borderBottomColor = color;
                view.style.borderLeftColor = color;
                view.style.borderRightColor = color;
                view.style.borderTopWidth = width;
                view.style.borderBottomWidth = width;
                view.style.borderLeftWidth = width;
                view.style.borderRightWidth = width;
            }
        }

        private void CreateNode(NodeType type) {
            var id = Guid.NewGuid().ToString("N").Substring(0, 8);
            var view = new DialogNodeView(type, id);
            view.Load(DefaultModel(type, id), null);
            view.SetPosition(NextCreatePosition());
            AddElement(view);
        }

        private static IDialogNode DefaultModel(NodeType type, string id) {
            switch (type) {
                case NodeType.Entry: return new EntryNode(id, null);
                case NodeType.Line: return new LineNode(id, "", "", null);
                case NodeType.Choice: return new ChoiceNode(id, Array.Empty<ChoiceOption>());
                case NodeType.Condition: return new ConditionNode(id, null, null, null);
                case NodeType.Action: return new ActionNode(id, Array.Empty<ActionSpec>(), null);
                case NodeType.Exit: return new ExitNode(id, "");
                default: throw new DialogException($"Unknown node type '{type}'.");
            }
        }

        private Rect NextCreatePosition() {
            var step = _createdCount++ % 6;
            return new Rect(120f + step * 40f, 120f + step * 40f, NodeWidth, NodeHeight);
        }

        private static Rect FallbackPosition(int index) {
            const int columns = 4;
            var x = 60f + index % columns * (NodeWidth + 60f);
            var y = 60f + index / columns * (NodeHeight + 60f);
            return new Rect(x, y, NodeWidth, NodeHeight);
        }
    }
}
