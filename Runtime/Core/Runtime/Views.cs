namespace WTFGames.Hephaestus.Dialog {
    /// <summary>Where the runner is currently resting.</summary>
    public enum RunnerState {
        Idle,
        AwaitingLine,
        AwaitingChoice,
        Ended
    }

    /// <summary>What the presenter needs to show for a line. Text is a key — the presenter resolves it.</summary>
    public readonly struct LineView {
        public string SpeakerId { get; }
        public string TextKey { get; }

        public LineView(string speakerId, string textKey) {
            SpeakerId = speakerId;
            TextKey = textKey;
        }
    }

    /// <summary>One selectable option. <see cref="OptionId"/> is the index to pass back to <c>Choose</c>.</summary>
    public readonly struct ChoiceView {
        public int OptionId { get; }
        public string TextKey { get; }

        public ChoiceView(int optionId, string textKey) {
            OptionId = optionId;
            TextKey = textKey;
        }
    }

    /// <summary>The outcome reported when a dialog reaches an <see cref="ExitNode"/>.</summary>
    public readonly struct DialogResult {
        public string ResultTag { get; }

        public DialogResult(string resultTag) {
            ResultTag = resultTag;
        }
    }
}
