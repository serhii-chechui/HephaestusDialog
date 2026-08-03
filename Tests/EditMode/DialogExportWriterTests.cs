using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using WTFGames.Hephaestus.Dialog.Editor;
using WTFGames.Hephaestus.Dialog.Serialization;

namespace WTFGames.Hephaestus.Dialog.Tests {
    public class DialogExportWriterTests {
        [Test]
        public void Writes_Three_Named_Files_That_Round_Trip() {
            var dir = Path.Combine(Path.GetTempPath(), "dialog_export_" + Guid.NewGuid().ToString("N"));
            try {
                var graph = new DialogGraph("demo", "e", new IDialogNode[] {
                    new EntryNode("e", "l"),
                    new LineNode("l", "npc", "demo.line", "x"),
                    new ExitNode("x")
                });
                var strings = new Dictionary<string, string> { ["demo.line"] = "Hello there." };
                var layout = new DialogLayout();
                layout.Nodes["l"] = new NodeLayout(120f, 40f);

                var result = new DialogExportWriter().Write(dir, "en", graph, strings, layout);

                Assert.IsTrue(File.Exists(result.DialogPath));
                Assert.IsTrue(File.Exists(result.StringsPath));
                Assert.IsTrue(File.Exists(result.LayoutPath));
                StringAssert.EndsWith("demo.dialog.json", result.DialogPath);
                StringAssert.EndsWith("demo.strings.en.json", result.StringsPath);
                StringAssert.EndsWith("demo.layout.json", result.LayoutPath);

                var reloadedGraph = new DialogGraphSerializer().Deserialize(File.ReadAllText(result.DialogPath));
                Assert.AreEqual("demo", reloadedGraph.Id);
                Assert.IsInstanceOf<LineNode>(reloadedGraph.Find("l"));

                var reloadedStrings = new StringTableSerializer().Deserialize(File.ReadAllText(result.StringsPath));
                Assert.IsTrue(reloadedStrings.TryGet("demo.line", out var text));
                Assert.AreEqual("Hello there.", text);

                var reloadedLayout = new DialogLayoutSerializer().Deserialize(File.ReadAllText(result.LayoutPath));
                Assert.AreEqual(120f, reloadedLayout.Nodes["l"].X);
            } finally {
                if (Directory.Exists(dir)) {
                    Directory.Delete(dir, true);
                }
            }
        }
    }
}
