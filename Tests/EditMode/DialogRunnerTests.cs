using System.Collections.Generic;
using NUnit.Framework;
using WTFGames.Hephaestus.Dialog;

namespace WTFGames.Hephaestus.Dialog.Tests {
    public class DialogRunnerTests {
        private List<string> _log;
        private DialogRegistry _registry;

        [SetUp]
        public void SetUp() {
            _log = new List<string>();
            _registry = TestRegistry.Build(_log);
        }

        private DialogRunner NewRunner() => new DialogRunner(_registry);
        private static DialogContext Ctx() => new DialogContext("test");

        [Test]
        public void Linear_Entry_Line_Exit_Walks_And_Ends() {
            var graph = new DialogGraph("d", "n0", new IDialogNode[] {
                new EntryNode("n0", "n1"),
                new LineNode("n1", "npc", "k.line", "n2"),
                new ExitNode("n2", "done")
            });

            var runner = NewRunner();
            LineView? line = null;
            DialogResult? ended = null;
            runner.Line += l => line = l;
            runner.Ended += r => ended = r;

            runner.Start(graph, Ctx());
            Assert.AreEqual(RunnerState.AwaitingLine, runner.State);
            Assert.AreEqual("k.line", line.Value.TextKey);
            Assert.AreEqual("npc", line.Value.SpeakerId);

            runner.Advance();
            Assert.AreEqual(RunnerState.Ended, runner.State);
            Assert.IsTrue(ended.HasValue);
            Assert.AreEqual("done", ended.Value.ResultTag);
        }

        [Test]
        public void Choice_Hides_Options_Whose_Gate_Fails() {
            var graph = new DialogGraph("d", "n0", new IDialogNode[] {
                new EntryNode("n0", "c1"),
                new ChoiceNode("c1", new[] {
                    new ChoiceOption("opt.shown", "ex"),
                    new ChoiceOption("opt.hidden", "ex", TestRegistry.Const(false))
                }),
                new ExitNode("ex")
            });

            var runner = NewRunner();
            IReadOnlyList<ChoiceView> choices = null;
            runner.Choices += c => choices = c;

            runner.Start(graph, Ctx());
            Assert.AreEqual(RunnerState.AwaitingChoice, runner.State);
            Assert.AreEqual(1, choices.Count);
            Assert.AreEqual("opt.shown", choices[0].TextKey);
            Assert.AreEqual(0, choices[0].OptionId);
        }

        [Test]
        public void Actions_Fire_In_Order_OnEnter_Then_OnSelect() {
            var graph = new DialogGraph("d", "n0", new IDialogNode[] {
                new EntryNode("n0", "a1"),
                new ActionNode("a1", new[] { TestRegistry.Log("action-node") }, "line"),
                new LineNode("line", "npc", "k", "c1", new[] { TestRegistry.Log("on-enter") }),
                new ChoiceNode("c1", new[] {
                    new ChoiceOption("opt", "ex", onSelect: new[] { TestRegistry.Log("on-select") })
                }),
                new ExitNode("ex")
            });

            var runner = NewRunner();
            runner.Start(graph, Ctx());
            CollectionAssert.AreEqual(new[] { "action-node", "on-enter" }, _log);

            runner.Advance();   // past the line into the choice
            runner.Choose(0);
            CollectionAssert.AreEqual(new[] { "action-node", "on-enter", "on-select" }, _log);
            Assert.AreEqual(RunnerState.Ended, runner.State);
        }

        [Test]
        public void ConditionNode_Routes_On_Its_Condition() {
            IDialogNode[] Build(bool value) => new IDialogNode[] {
                new EntryNode("n0", "cond"),
                new ConditionNode("cond", TestRegistry.Const(value), "t", "f"),
                new ExitNode("t", "true-path"),
                new ExitNode("f", "false-path")
            };

            var trueRunner = NewRunner();
            DialogResult? trueResult = null;
            trueRunner.Ended += r => trueResult = r;
            trueRunner.Start(new DialogGraph("d", "n0", Build(true)), Ctx());
            Assert.AreEqual("true-path", trueResult.Value.ResultTag);

            var falseLog = new List<string>();
            var falseRunner = new DialogRunner(TestRegistry.Build(falseLog));
            DialogResult? falseResult = null;
            falseRunner.Ended += r => falseResult = r;
            falseRunner.Start(new DialogGraph("d", "n0", Build(false)), Ctx());
            Assert.AreEqual("false-path", falseResult.Value.ResultTag);
        }

        [Test]
        public void ExitlessAutoCycle_Trips_The_Loop_Guard() {
            var graph = new DialogGraph("d", "n0", new IDialogNode[] {
                new EntryNode("n0", "loop"),
                new ConditionNode("loop", TestRegistry.Const(true), "loop", "loop")
            });

            var runner = NewRunner();
            Assert.Throws<DialogException>(() => runner.Start(graph, Ctx()));
        }

        [Test]
        public void Choose_OutOfRange_Throws() {
            var graph = new DialogGraph("d", "n0", new IDialogNode[] {
                new EntryNode("n0", "c1"),
                new ChoiceNode("c1", new[] { new ChoiceOption("opt", "ex") }),
                new ExitNode("ex")
            });

            var runner = NewRunner();
            runner.Start(graph, Ctx());
            Assert.Throws<DialogException>(() => runner.Choose(3));
        }

        [Test]
        public void Advance_While_Awaiting_Choice_Throws() {
            var graph = new DialogGraph("d", "n0", new IDialogNode[] {
                new EntryNode("n0", "c1"),
                new ChoiceNode("c1", new[] { new ChoiceOption("opt", "ex") }),
                new ExitNode("ex")
            });

            var runner = NewRunner();
            runner.Start(graph, Ctx());
            Assert.Throws<DialogException>(() => runner.Advance());
        }

        [Test]
        public void MissingNode_Reference_Throws() {
            var graph = new DialogGraph("d", "n0", new IDialogNode[] {
                new EntryNode("n0", "does-not-exist")
            });

            var runner = NewRunner();
            Assert.Throws<DialogException>(() => runner.Start(graph, Ctx()));
        }

        [Test]
        public void Session_Drives_Presenter_And_Clears_On_End() {
            var graph = new DialogGraph("d", "n0", new IDialogNode[] {
                new EntryNode("n0", "n1"),
                new LineNode("n1", "npc", "k.line", "n2"),
                new ExitNode("n2")
            });

            var presenter = new FakePresenter();
            var session = new DialogSession(NewRunner(), presenter);

            session.Start(graph, Ctx());
            Assert.AreEqual(1, presenter.Lines.Count);
            Assert.AreEqual("k.line", presenter.Lines[0].TextKey);

            session.Advance();
            Assert.AreEqual(1, presenter.ClearCount);
        }
    }
}
