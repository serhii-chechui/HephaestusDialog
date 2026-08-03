using NUnit.Framework;
using WTFGames.Hephaestus.Dialog.Editor;

namespace WTFGames.Hephaestus.Dialog.Tests {
    public class DialogLayoutSerializerTests {
        [Test]
        public void RoundTrip_Preserves_Nodes_And_Comments() {
            var layout = new DialogLayout();
            layout.Nodes["n1"] = new NodeLayout(10f, 20f, "#ff0000");
            layout.Nodes["n2"] = new NodeLayout(30f, 40f);
            layout.Comments.Add(new CommentLayout { X = 1f, Y = 2f, Text = "note" });

            var serializer = new DialogLayoutSerializer();
            var restored = serializer.Deserialize(serializer.Serialize(layout));

            Assert.AreEqual(10f, restored.Nodes["n1"].X);
            Assert.AreEqual(20f, restored.Nodes["n1"].Y);
            Assert.AreEqual("#ff0000", restored.Nodes["n1"].Color);
            Assert.AreEqual(30f, restored.Nodes["n2"].X);
            Assert.IsNull(restored.Nodes["n2"].Color);

            Assert.AreEqual(1, restored.Comments.Count);
            Assert.AreEqual("note", restored.Comments[0].Text);
        }
    }
}
