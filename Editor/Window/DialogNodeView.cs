using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace WTFGames.Hephaestus.Dialog.Editor {
    /// <summary>
    /// A GraphView node representing one dialog node. Holds the node's content, exposes inline
    /// editors for the simple fields (speaker, localization key + authored text, exit tag, choice
    /// options), and rebuilds the immutable model node from its content + the wired edges. Condition
    /// and action editing (descriptor-driven) is round-tripped but not yet editable here (3d-2).
    /// Kept as our own type so a future GraphView/GTF port touches this layer only (risk-1).
    /// </summary>
    public class DialogNodeView : Node {
        private sealed class OutputSlot {
            public Port Port;
            public string LoadedTargetId;
            // Choice-option content.
            public string OptionTextKey;
            public string OptionText;
            public ConditionSpec OptionShow;
            public IReadOnlyList<ActionSpec> OptionOnSelect = Array.Empty<ActionSpec>();
        }

        public string NodeId { get; set; }
        public NodeType NodeType { get; }
        public Port InputPort { get; private set; }

        private string _speakerId = "";
        private string _textKey = "";
        private string _text = "";
        private string _resultTag = "";
        private ConditionSpec _condition;
        private IReadOnlyList<ActionSpec> _onEnter = Array.Empty<ActionSpec>();
        private IReadOnlyList<ActionSpec> _actions = Array.Empty<ActionSpec>();

        private readonly List<OutputSlot> _outputs = new List<OutputSlot>();
        private VisualElement _optionsContainer;

        public DialogNodeView(NodeType nodeType, string nodeId) {
            NodeType = nodeType;
            NodeId = nodeId;
            title = nodeType.ToString();
        }

        /// <summary>Captures content, builds ports, and adds the inline editors. <paramref name="resolveText"/> gives the authored text for a key.</summary>
        public void Load(IDialogNode node, Func<string, string> resolveText) {
            if (!(node is EntryNode)) {
                InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
                InputPort.portName = "In";
                inputContainer.Add(InputPort);
            }

            switch (node) {
                case EntryNode entry:
                    AddOutput("Next", entry.Next);
                    break;
                case LineNode line:
                    _speakerId = line.SpeakerId ?? "";
                    _textKey = line.TextKey ?? "";
                    _text = resolveText?.Invoke(line.TextKey) ?? "";
                    _onEnter = line.OnEnter;
                    AddOutput("Next", line.Next);
                    break;
                case ActionNode action:
                    _actions = action.Actions;
                    AddOutput("Next", action.Next);
                    break;
                case ConditionNode condition:
                    _condition = condition.Condition;
                    AddOutput("True", condition.IfTrue);
                    AddOutput("False", condition.IfFalse);
                    break;
                case ChoiceNode choice:
                    foreach (var option in choice.Options) {
                        AddOutput(option.TextKey, option.Target, option.TextKey,
                            resolveText?.Invoke(option.TextKey) ?? "", option.Show, option.OnSelect);
                    }
                    break;
                case ExitNode exit:
                    _resultTag = exit.ResultTag ?? "";
                    break;
            }

            BuildInlineEditors();
            RefreshExpandedState();
            RefreshPorts();
        }

        public IEnumerable<(Port port, string targetId)> LoadedEdges() {
            foreach (var slot in _outputs) {
                yield return (slot.Port, slot.LoadedTargetId);
            }
        }

        public IDialogNode BuildModel() {
            switch (NodeType) {
                case NodeType.Entry:
                    return new EntryNode(NodeId, TargetOf(_outputs[0]));
                case NodeType.Line:
                    return new LineNode(NodeId, _speakerId, _textKey, TargetOf(_outputs[0]), _onEnter);
                case NodeType.Action:
                    return new ActionNode(NodeId, _actions, TargetOf(_outputs[0]));
                case NodeType.Condition:
                    return new ConditionNode(NodeId, _condition, TargetOf(_outputs[0]), TargetOf(_outputs[1]));
                case NodeType.Choice:
                    var options = _outputs
                        .Select(s => new ChoiceOption(s.OptionTextKey, TargetOf(s), s.OptionShow, s.OptionOnSelect))
                        .ToList();
                    return new ChoiceNode(NodeId, options);
                case NodeType.Exit:
                    return new ExitNode(NodeId, _resultTag);
                default:
                    throw new DialogException($"Cannot build unknown node type '{NodeType}'.");
            }
        }

        /// <summary>Writes this node's authored ({key -> text}) entries into <paramref name="strings"/>.</summary>
        public void CollectStrings(IDictionary<string, string> strings) {
            if (NodeType == NodeType.Line && !string.IsNullOrEmpty(_textKey)) {
                strings[_textKey] = _text ?? "";
            }
            if (NodeType == NodeType.Choice) {
                foreach (var slot in _outputs) {
                    if (!string.IsNullOrEmpty(slot.OptionTextKey)) {
                        strings[slot.OptionTextKey] = slot.OptionText ?? "";
                    }
                }
            }
        }

        #region Inline editors

        private void BuildInlineEditors() {
            switch (NodeType) {
                case NodeType.Line:
                    extensionContainer.Add(MakeField("Speaker", _speakerId, v => _speakerId = v));
                    extensionContainer.Add(MakeField("Key", _textKey, v => _textKey = v));
                    extensionContainer.Add(MakeMultilineField("Text", _text, v => _text = v));
                    extensionContainer.Add(new ActionListEditorElement("On Enter", _onEnter, list => _onEnter = list));
                    break;
                case NodeType.Exit:
                    extensionContainer.Add(MakeField("Result Tag", _resultTag, v => _resultTag = v));
                    break;
                case NodeType.Condition:
                    extensionContainer.Add(new ConditionEditorElement(_condition, spec => _condition = spec));
                    break;
                case NodeType.Action:
                    extensionContainer.Add(new ActionListEditorElement("Actions", _actions, list => _actions = list));
                    break;
                case NodeType.Choice:
                    _optionsContainer = new VisualElement();
                    extensionContainer.Add(_optionsContainer);
                    foreach (var slot in _outputs) {
                        _optionsContainer.Add(OptionEditor(slot));
                    }
                    var addButton = new Button(AddOption) { text = "+ Option" };
                    extensionContainer.Add(addButton);
                    break;
            }
        }

        private VisualElement OptionEditor(OutputSlot slot) {
            var box = new VisualElement { style = { marginBottom = 4 } };
            box.Add(MakeField("Key", slot.OptionTextKey, v => { slot.OptionTextKey = v; slot.Port.portName = v; }));
            box.Add(MakeMultilineField("Text", slot.OptionText, v => slot.OptionText = v));
            box.Add(new ConditionEditorElement(slot.OptionShow, spec => slot.OptionShow = spec));
            box.Add(new ActionListEditorElement("On Select", slot.OptionOnSelect, list => slot.OptionOnSelect = list));
            box.Add(new Button(() => RemoveOption(slot)) { text = "− Remove option" });
            return box;
        }

        private void AddOption() {
            var slot = AddOutput("new.option", null, "new.option", "");
            _optionsContainer.Add(OptionEditor(slot));
            RefreshPorts();
            RefreshExpandedState();
        }

        private void RemoveOption(OutputSlot slot) {
            foreach (var edge in slot.Port.connections.ToList()) {
                edge.output?.Disconnect(edge);
                edge.input?.Disconnect(edge);
                edge.RemoveFromHierarchy();
            }
            outputContainer.Remove(slot.Port);
            _outputs.Remove(slot);
            RebuildOptionEditors();
            RefreshPorts();
            RefreshExpandedState();
        }

        private void RebuildOptionEditors() {
            _optionsContainer.Clear();
            foreach (var slot in _outputs) {
                _optionsContainer.Add(OptionEditor(slot));
            }
        }

        private static TextField MakeField(string label, string value, Action<string> onChange) {
            var field = new TextField(label) { value = value };
            field.RegisterValueChangedCallback(e => onChange(e.newValue));
            return field;
        }

        private static TextField MakeMultilineField(string label, string value, Action<string> onChange) {
            var field = new TextField(label) { value = value, multiline = true };
            field.RegisterValueChangedCallback(e => onChange(e.newValue));
            return field;
        }

        #endregion

        private OutputSlot AddOutput(string portName, string loadedTargetId, string optionTextKey = null,
            string optionText = null, ConditionSpec optionShow = null, IReadOnlyList<ActionSpec> optionOnSelect = null) {
            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            port.portName = portName;
            outputContainer.Add(port);
            var slot = new OutputSlot {
                Port = port,
                LoadedTargetId = loadedTargetId,
                OptionTextKey = optionTextKey,
                OptionText = optionText,
                OptionShow = optionShow,
                OptionOnSelect = optionOnSelect ?? Array.Empty<ActionSpec>()
            };
            _outputs.Add(slot);
            return slot;
        }

        private static string TargetOf(OutputSlot slot) {
            foreach (var edge in slot.Port.connections) {
                if (edge.input?.node is DialogNodeView target) {
                    return target.NodeId;
                }
            }
            return null;
        }
    }
}
