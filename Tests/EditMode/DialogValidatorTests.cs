using System.Collections.Generic;
using NUnit.Framework;
using WTFGames.Hephaestus.Dialog;
using WTFGames.Hephaestus.Dialog.Editor;

namespace WTFGames.Hephaestus.Dialog.Tests {
    public class DialogValidatorTests {
        private static DialogGraph Graph(string entryId, params IDialogNode[] nodes)
            => new DialogGraph("d", entryId, nodes);

        private static bool HasBlockFor(ValidationReport report, string nodeId) {
            foreach (var issue in report.Issues) {
                if (issue.Severity == ValidationSeverity.Block && issue.NodeId == nodeId) {
                    return true;
                }
            }
            return false;
        }

        private static bool HasWarning(ValidationReport report) {
            foreach (var issue in report.Issues) {
                if (issue.Severity == ValidationSeverity.Warning) {
                    return true;
                }
            }
            return false;
        }

        [Test]
        public void Well_Formed_Graph_Has_No_Blocks() {
            var report = DialogValidator.Validate(Graph("e",
                new EntryNode("e", "l"),
                new LineNode("l", "npc", "k.l", "x"),
                new ExitNode("x")));

            Assert.IsTrue(report.CanExport);
            Assert.IsEmpty(report.Issues);
        }

        [Test]
        public void Missing_Entry_Blocks() {
            var report = DialogValidator.Validate(Graph("l",
                new LineNode("l", "npc", "k", "x"),
                new ExitNode("x")));

            Assert.IsFalse(report.CanExport);
        }

        [Test]
        public void Two_Entries_Block() {
            var report = DialogValidator.Validate(Graph("e1",
                new EntryNode("e1", "x"),
                new EntryNode("e2", "x"),
                new ExitNode("x")));

            Assert.IsFalse(report.CanExport);
        }

        [Test]
        public void Edge_To_Missing_Node_Blocks() {
            var report = DialogValidator.Validate(Graph("e",
                new EntryNode("e", "l"),
                new LineNode("l", "npc", "k", "nowhere")));

            Assert.IsTrue(HasBlockFor(report, "l"));
            Assert.IsFalse(report.CanExport);
        }

        [Test]
        public void Dead_End_Null_Edge_Blocks() {
            var report = DialogValidator.Validate(Graph("e",
                new EntryNode("e", "l"),
                new LineNode("l", "npc", "k", null)));

            Assert.IsTrue(HasBlockFor(report, "l"));
        }

        [Test]
        public void Empty_Choice_Blocks() {
            var report = DialogValidator.Validate(Graph("e",
                new EntryNode("e", "c"),
                new ChoiceNode("c", new ChoiceOption[0])));

            Assert.IsTrue(HasBlockFor(report, "c"));
        }

        [Test]
        public void Unregistered_Types_Block_When_Known_Sets_Provided() {
            var graph = Graph("e",
                new EntryNode("e", "c"),
                new ChoiceNode("c", new[] {
                    new ChoiceOption("k.a", "x", new ConditionSpec("stat.check")),
                    new ChoiceOption("k.b", "x", new ConditionSpec("mystery.gate"))
                }),
                new ExitNode("x"));

            var known = new HashSet<string> { "stat.check" };
            var report = DialogValidator.Validate(graph, null, known, new HashSet<string>());

            Assert.IsFalse(report.CanExport, "mystery.gate is not registered");
        }

        [Test]
        public void Registered_Types_Do_Not_Block() {
            var graph = Graph("e",
                new EntryNode("e", "c"),
                new ChoiceNode("c", new[] { new ChoiceOption("k.a", "x", new ConditionSpec("stat.check")) }),
                new ExitNode("x"));

            var report = DialogValidator.Validate(graph, null,
                new HashSet<string> { "stat.check" }, new HashSet<string>());

            Assert.IsTrue(report.CanExport);
        }

        [Test]
        public void Unreachable_Node_Warns_But_Does_Not_Block() {
            var report = DialogValidator.Validate(Graph("e",
                new EntryNode("e", "x"),
                new ExitNode("x"),
                new LineNode("orphan", "npc", "k", "x")));

            Assert.IsTrue(report.CanExport, "unreachable is a warning, not a block");
            Assert.IsTrue(HasWarning(report));
        }

        [Test]
        public void Missing_Localized_Text_Warns() {
            var graph = Graph("e",
                new EntryNode("e", "l"),
                new LineNode("l", "npc", "k.present", "x"),
                new ExitNode("x"));

            var strings = new StringTable("en", new Dictionary<string, string>()); // empty -> key missing
            var report = DialogValidator.Validate(graph, strings);

            Assert.IsTrue(report.CanExport);
            Assert.IsTrue(HasWarning(report));
        }

        [Test]
        public void Present_Localized_Text_Does_Not_Warn() {
            var graph = Graph("e",
                new EntryNode("e", "l"),
                new LineNode("l", "npc", "k.present", "x"),
                new ExitNode("x"));

            var strings = new StringTable("en", new Dictionary<string, string> { ["k.present"] = "Hello" });
            var report = DialogValidator.Validate(graph, strings);

            Assert.IsFalse(HasWarning(report));
        }
    }
}
