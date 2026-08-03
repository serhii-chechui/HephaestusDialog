using System;
using System.Collections.Generic;

namespace WTFGames.Hephaestus.Dialog {
    /// <summary>
    /// Walks a dialog graph. Synchronous and callback-driven (Q7a): it raises <see cref="Line"/> /
    /// <see cref="Choices"/> / <see cref="Ended"/>, and the host resumes it by calling
    /// <see cref="Advance"/> (continue a line) or <see cref="Choose"/> (pick an option). Auto-nodes
    /// (entry/action/condition) are walked without pausing. No async, no engine types.
    /// One runner per conversation (Q7b); it holds no serializable state.
    /// </summary>
    public interface IDialogRunner {
        RunnerState State { get; }

        /// <summary>Id of the node the runner is currently resting on (line/choice/exit), or null.</summary>
        string CurrentNodeId { get; }

        event Action<LineView> Line;
        event Action<IReadOnlyList<ChoiceView>> Choices;
        event Action<DialogResult> Ended;

        /// <summary>Begins walking <paramref name="graph"/> from its entry until the first line/choice/exit.</summary>
        void Start(IDialogGraph graph, IDialogContext context);

        /// <summary>Continues past the current line to its next node. Valid only while awaiting a line.</summary>
        void Advance();

        /// <summary>Selects the option (by its <see cref="ChoiceView.OptionId"/>). Valid only while awaiting a choice.</summary>
        void Choose(int optionId);
    }

    /// <inheritdoc cref="IDialogRunner"/>
    public sealed class DialogRunner : IDialogRunner {
        // Guards against exit-less auto-node cycles (condition/action loops with no line/choice/exit).
        private const int MaxAutoStepsPerTransition = 10000;

        private readonly IDialogRegistry _registry;

        private IDialogGraph _graph;
        private IDialogContext _context;
        private DialogNode _current;
        private List<ChoiceOption> _visibleOptions;

        public RunnerState State { get; private set; } = RunnerState.Idle;

        public string CurrentNodeId => _current?.Id;

        public event Action<LineView> Line;
        public event Action<IReadOnlyList<ChoiceView>> Choices;
        public event Action<DialogResult> Ended;

        public DialogRunner(IDialogRegistry registry) {
            _registry = registry ?? throw new DialogException("A runner requires a registry.");
        }

        public void Start(IDialogGraph graph, IDialogContext context) {
            if (State != RunnerState.Idle) {
                throw new DialogException("This runner has already started; use one runner per conversation.");
            }
            _graph = graph ?? throw new DialogException("Cannot start a null graph.");
            _context = context;
            EnterFrom(graph.EntryId);
        }

        public void Advance() {
            if (State != RunnerState.AwaitingLine) {
                throw new DialogException($"Advance() is only valid while awaiting a line (state: {State}).");
            }
            EnterFrom(((LineNode)_current).Next);
        }

        public void Choose(int optionId) {
            if (State != RunnerState.AwaitingChoice) {
                throw new DialogException($"Choose() is only valid while awaiting a choice (state: {State}).");
            }
            if (optionId < 0 || optionId >= _visibleOptions.Count) {
                throw new DialogException($"Choice option {optionId} is out of range (0..{_visibleOptions.Count - 1}).");
            }
            var option = _visibleOptions[optionId];
            foreach (var action in option.OnSelect) {
                _registry.Execute(action, _context);
            }
            EnterFrom(option.Target);
        }

        // Walks auto-nodes until a line/choice pauses the runner or an exit ends it.
        private void EnterFrom(string nodeId) {
            var id = nodeId;
            for (var step = 0; step < MaxAutoStepsPerTransition; step++) {
                var node = _graph.Find(id);
                if (node == null) {
                    throw new DialogException($"Dialog '{_graph.Id}' references missing node '{id}'.");
                }

                switch (node) {
                    case EntryNode entry:
                        id = entry.Next;
                        continue;

                    case ActionNode actionNode:
                        foreach (var action in actionNode.Actions) {
                            _registry.Execute(action, _context);
                        }
                        id = actionNode.Next;
                        continue;

                    case ConditionNode conditionNode:
                        id = _registry.Evaluate(conditionNode.Condition, _context)
                            ? conditionNode.IfTrue
                            : conditionNode.IfFalse;
                        continue;

                    case LineNode lineNode:
                        foreach (var action in lineNode.OnEnter) {
                            _registry.Execute(action, _context);
                        }
                        _current = lineNode;
                        State = RunnerState.AwaitingLine;
                        Line?.Invoke(new LineView(lineNode.SpeakerId, lineNode.TextKey));
                        return;

                    case ChoiceNode choiceNode:
                        PresentChoices(choiceNode);
                        return;

                    case ExitNode exitNode:
                        _current = exitNode;
                        State = RunnerState.Ended;
                        Ended?.Invoke(new DialogResult(exitNode.ResultTag));
                        return;

                    default:
                        throw new DialogException($"Unhandled node type '{node.Type}' at '{node.Id}'.");
                }
            }

            throw new DialogException(
                $"Auto-node loop guard tripped in dialog '{_graph.Id}' near '{id}' (exit-less condition/action cycle?).");
        }

        private void PresentChoices(ChoiceNode choiceNode) {
            _visibleOptions = new List<ChoiceOption>(choiceNode.Options.Count);
            var views = new List<ChoiceView>(choiceNode.Options.Count);
            foreach (var option in choiceNode.Options) {
                if (_registry.Evaluate(option.Show, _context)) {
                    views.Add(new ChoiceView(_visibleOptions.Count, option.TextKey));
                    _visibleOptions.Add(option);
                }
            }
            _current = choiceNode;
            State = RunnerState.AwaitingChoice;
            Choices?.Invoke(views);
        }
    }
}
