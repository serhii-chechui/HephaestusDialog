using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WTFGames.Hephaestus.Dialog.Editor {
    /// <summary>
    /// In-editor headless preview (B2): builds the graph from the canvas and walks it with the pure
    /// runner and a <see cref="PreviewRegistry"/> stub — conditions follow a "Conditions pass"
    /// toggle, actions are logged. Shows the current line, clickable choices, the action log, and
    /// highlights the active node on the canvas. No Play mode, no game.
    /// </summary>
    public sealed class DialogPreviewPanel : VisualElement {
        private readonly DialogGraphView _graphView;
        private readonly PreviewRegistry _registry = new PreviewRegistry();

        private readonly Label _speaker = new Label();
        private readonly Label _line = new Label();
        private readonly Button _continue;
        private readonly VisualElement _choices = new VisualElement();
        private readonly Label _status = new Label();
        private readonly Label _log = new Label();

        private Dictionary<string, string> _strings = new Dictionary<string, string>();
        private DialogRunner _runner;

        public DialogPreviewPanel(DialogGraphView graphView) {
            _graphView = graphView;

            style.width = 320;
            style.borderLeftWidth = 1;
            style.borderLeftColor = new Color(0f, 0f, 0f, 0.3f);
            style.paddingLeft = 6;
            style.paddingRight = 6;
            style.paddingTop = 4;

            Add(new Label("Preview") { style = { unityFontStyleAndWeight = FontStyle.Bold } });
            Add(new Button(Play) { text = "▶ Play from Entry" });

            var conditionsPass = new Toggle("Conditions pass") { value = true };
            conditionsPass.RegisterValueChangedCallback(e => _registry.ConditionsPass = e.newValue);
            Add(conditionsPass);

            _speaker.style.unityFontStyleAndWeight = FontStyle.Bold;
            _line.style.whiteSpace = WhiteSpace.Normal;
            _line.style.marginBottom = 6;
            Add(_speaker);
            Add(_line);

            _continue = new Button(Advance) { text = "Continue ▶" };
            Add(_continue);
            Add(_choices);
            Add(_status);

            Add(new Label("Action log:") { style = { marginTop = 8, unityFontStyleAndWeight = FontStyle.Bold } });
            _log.style.whiteSpace = WhiteSpace.Normal;
            Add(_log);

            ResetView();
        }

        private void Play() {
            _strings = new Dictionary<string, string>();
            _graphView.CollectStrings(_strings);

            IDialogGraph graph;
            try {
                graph = _graphView.BuildGraph("preview");
            } catch (Exception e) {
                _status.text = "Build error: " + e.Message;
                return;
            }

            _registry.ActionLog.Clear();
            ResetView();

            _runner = new DialogRunner(_registry);
            _runner.Line += OnLine;
            _runner.Choices += OnChoices;
            _runner.Ended += OnEnded;

            try {
                _runner.Start(graph, new DialogContext("preview"));
            } catch (Exception e) {
                _status.text = "Run error: " + e.Message;
                _graphView.HighlightNode(null);
            }
        }

        private void OnLine(LineView view) {
            _speaker.text = view.SpeakerId ?? "";
            _line.text = Resolve(view.TextKey);
            _choices.Clear();
            _continue.style.display = DisplayStyle.Flex;
            AfterStep();
        }

        private void OnChoices(IReadOnlyList<ChoiceView> views) {
            _continue.style.display = DisplayStyle.None;
            _choices.Clear();
            foreach (var view in views) {
                var optionId = view.OptionId;
                _choices.Add(new Button(() => Choose(optionId)) { text = Resolve(view.TextKey) });
            }
            AfterStep();
        }

        private void OnEnded(DialogResult result) {
            _continue.style.display = DisplayStyle.None;
            _choices.Clear();
            _status.text = string.IsNullOrEmpty(result.ResultTag) ? "Ended." : $"Ended ({result.ResultTag}).";
            AfterStep();
        }

        private void Advance() {
            try {
                _runner?.Advance();
            } catch (Exception e) {
                _status.text = e.Message;
            }
        }

        private void Choose(int optionId) {
            try {
                _runner?.Choose(optionId);
            } catch (Exception e) {
                _status.text = e.Message;
            }
        }

        private void AfterStep() {
            _graphView.HighlightNode(_runner?.CurrentNodeId);
            _log.text = string.Join("\n", _registry.ActionLog);
        }

        private void ResetView() {
            _speaker.text = "";
            _line.text = "";
            _status.text = "";
            _log.text = "";
            _choices.Clear();
            _continue.style.display = DisplayStyle.None;
        }

        private string Resolve(string key) {
            if (string.IsNullOrEmpty(key)) {
                return "";
            }
            return _strings.TryGetValue(key, out var text) && !string.IsNullOrEmpty(text) ? text : $"[[{key}]]";
        }
    }
}
