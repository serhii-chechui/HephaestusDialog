using System.Collections.Generic;

namespace WTFGames.Hephaestus.Dialog {
    /// <summary>
    /// The view port (Q6): the runner tells a presenter what to show. The presenter resolves
    /// <c>TextKey</c>s (via <see cref="ILocalizedTextProvider"/>) and, on player input, drives the
    /// session's <see cref="DialogSession.Advance"/> / <see cref="DialogSession.Choose"/>. The
    /// Hephaestus UI is one implementation; tests use a fake.
    /// </summary>
    public interface IDialogPresenter {
        void ShowLine(LineView line);
        void ShowChoices(IReadOnlyList<ChoiceView> choices);
        void Clear();
    }

    /// <summary>
    /// Wires a <see cref="IDialogRunner"/> to an <see cref="IDialogPresenter"/>: forwards the
    /// runner's line/choice/ended callbacks to the presenter, and exposes Start/Advance/Choose so
    /// the presenter can drive the flow back. A thin convenience over the runner's events.
    /// </summary>
    public sealed class DialogSession {
        private readonly IDialogRunner _runner;
        private readonly IDialogPresenter _presenter;

        public DialogSession(IDialogRunner runner, IDialogPresenter presenter) {
            _runner = runner ?? throw new DialogException("A session requires a runner.");
            _presenter = presenter ?? throw new DialogException("A session requires a presenter.");
            _runner.Line += _presenter.ShowLine;
            _runner.Choices += _presenter.ShowChoices;
            _runner.Ended += OnEnded;
        }

        public RunnerState State => _runner.State;

        public void Start(IDialogGraph graph, IDialogContext context) => _runner.Start(graph, context);
        public void Advance() => _runner.Advance();
        public void Choose(int optionId) => _runner.Choose(optionId);

        private void OnEnded(DialogResult result) => _presenter.Clear();
    }
}
