using System.Collections.Generic;

namespace WTFGames.Hephaestus.Dialog {
    /// <summary>The graph's start. Auto-walked: the runner immediately follows <see cref="Next"/>.</summary>
    public sealed class EntryNode : DialogNode {
        public string Next { get; }
        public override NodeType Type => NodeType.Entry;

        public EntryNode(string id, string next) : base(id) {
            Next = next;
        }
    }

    /// <summary>
    /// One spoken line by <see cref="SpeakerId"/>. Fires <see cref="OnEnter"/> actions on entry,
    /// shows the line (text resolved from <see cref="TextKey"/> by the presenter), then waits for
    /// <c>Advance()</c> and follows <see cref="Next"/>.
    /// </summary>
    public sealed class LineNode : DialogNode {
        public string SpeakerId { get; }
        public string TextKey { get; }
        public IReadOnlyList<ActionSpec> OnEnter { get; }
        public string Next { get; }
        public override NodeType Type => NodeType.Line;

        public LineNode(string id, string speakerId, string textKey, string next,
            IReadOnlyList<ActionSpec> onEnter = null) : base(id) {
            SpeakerId = speakerId;
            TextKey = textKey;
            Next = next;
            OnEnter = onEnter ?? System.Array.Empty<ActionSpec>();
        }
    }

    /// <summary>A single player option inside a <see cref="ChoiceNode"/>.</summary>
    public sealed class ChoiceOption {
        public string TextKey { get; }
        /// <summary>Optional gate. When null (or a registered pass), the option is shown.</summary>
        public ConditionSpec Show { get; }
        public IReadOnlyList<ActionSpec> OnSelect { get; }
        public string Target { get; }

        public ChoiceOption(string textKey, string target, ConditionSpec show = null,
            IReadOnlyList<ActionSpec> onSelect = null) {
            TextKey = textKey;
            Target = target;
            Show = show;
            OnSelect = onSelect ?? System.Array.Empty<ActionSpec>();
        }
    }

    /// <summary>Presents the player options whose <see cref="ChoiceOption.Show"/> gate passes.</summary>
    public sealed class ChoiceNode : DialogNode {
        public IReadOnlyList<ChoiceOption> Options { get; }
        public override NodeType Type => NodeType.Choice;

        public ChoiceNode(string id, IReadOnlyList<ChoiceOption> options) : base(id) {
            Options = options ?? System.Array.Empty<ChoiceOption>();
        }
    }

    /// <summary>Auto-branch: routes to <see cref="IfTrue"/> or <see cref="IfFalse"/> on the condition.</summary>
    public sealed class ConditionNode : DialogNode {
        public ConditionSpec Condition { get; }
        public string IfTrue { get; }
        public string IfFalse { get; }
        public override NodeType Type => NodeType.Condition;

        public ConditionNode(string id, ConditionSpec condition, string ifTrue, string ifFalse) : base(id) {
            Condition = condition;
            IfTrue = ifTrue;
            IfFalse = ifFalse;
        }
    }

    /// <summary>Auto-node: fires its <see cref="Actions"/> then follows <see cref="Next"/>. No UI.</summary>
    public sealed class ActionNode : DialogNode {
        public IReadOnlyList<ActionSpec> Actions { get; }
        public string Next { get; }
        public override NodeType Type => NodeType.Action;

        public ActionNode(string id, IReadOnlyList<ActionSpec> actions, string next) : base(id) {
            Actions = actions ?? System.Array.Empty<ActionSpec>();
            Next = next;
        }
    }

    /// <summary>A terminal node. Ends the dialog and reports the optional <see cref="ResultTag"/>.</summary>
    public sealed class ExitNode : DialogNode {
        public string ResultTag { get; }
        public override NodeType Type => NodeType.Exit;

        public ExitNode(string id, string resultTag = null) : base(id) {
            ResultTag = resultTag;
        }
    }
}
