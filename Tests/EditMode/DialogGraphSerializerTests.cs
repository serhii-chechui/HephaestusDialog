using System.Collections.Generic;
using NUnit.Framework;
using WTFGames.Hephaestus.Dialog;
using WTFGames.Hephaestus.Dialog.Serialization;

namespace WTFGames.Hephaestus.Dialog.Tests {
    public class DialogGraphSerializerTests {
        private DialogGraphSerializer _serializer;

        [SetUp]
        public void SetUp() => _serializer = new DialogGraphSerializer();

        // A graph exercising every node type and both spec kinds, with int/string/bool params.
        private static IDialogGraph SampleGraph() {
            return new DialogGraph("swamp_witch", "n0", new IDialogNode[] {
                new EntryNode("n0", "line1"),
                new LineNode("line1", "npc.witch", "k.greet", "c1",
                    new[] { new ActionSpec("camera.shot", new Dictionary<string, object> { ["vcam"] = "witch_cu" }) }),
                new ChoiceNode("c1", new[] {
                    new ChoiceOption("k.help", "act1", new ConditionSpec("stat.check",
                        new Dictionary<string, object> { ["stat"] = "strength", ["min"] = 6 })),
                    new ChoiceOption("k.leave", "ex", onSelect: new[] {
                        new ActionSpec("flag.set", new Dictionary<string, object> { ["flag"] = "left", ["value"] = true })
                    })
                }),
                new ActionNode("act1", new[] { new ActionSpec("quest.start", new Dictionary<string, object> { ["id"] = "q1" }) }, "gate"),
                new ConditionNode("gate", new ConditionSpec("flag.get", new Dictionary<string, object> { ["flag"] = "left" }), "ex", "ex"),
                new ExitNode("ex", "done")
            });
        }

        [Test]
        public void RoundTrip_Is_Stable() {
            var json1 = _serializer.Serialize(SampleGraph());
            var graph2 = _serializer.Deserialize(json1);
            var json2 = _serializer.Serialize(graph2);
            Assert.AreEqual(json1, json2);
        }

        [Test]
        public void Deserialize_Preserves_Structure() {
            var json = _serializer.Serialize(SampleGraph());
            var graph = _serializer.Deserialize(json);

            Assert.AreEqual("swamp_witch", graph.Id);
            Assert.AreEqual("n0", graph.EntryId);

            var line = (LineNode)graph.Find("line1");
            Assert.AreEqual("npc.witch", line.SpeakerId);
            Assert.AreEqual("k.greet", line.TextKey);
            Assert.AreEqual(1, line.OnEnter.Count);
            Assert.AreEqual("camera.shot", line.OnEnter[0].Type);

            var choice = (ChoiceNode)graph.Find("c1");
            Assert.AreEqual(2, choice.Options.Count);
            Assert.AreEqual("stat.check", choice.Options[0].Show.Type);
            Assert.IsNull(choice.Options[1].Show);
        }

        [Test]
        public void Params_Preserve_Their_Types_Across_RoundTrip() {
            var graph = _serializer.Deserialize(_serializer.Serialize(SampleGraph()));

            var showParams = ((ChoiceNode)graph.Find("c1")).Options[0].Show.Params;
            Assert.AreEqual("strength", showParams.GetString("stat"));
            Assert.AreEqual(6, showParams.GetInt("min"));

            var onSelect = ((ChoiceNode)graph.Find("c1")).Options[1].OnSelect[0].Params;
            Assert.AreEqual("left", onSelect.GetString("flag"));
            Assert.IsTrue(onSelect.GetBool("value"));
        }

        [Test]
        public void Newer_SchemaVersion_Throws() {
            var json = _serializer.Serialize(SampleGraph())
                .Replace("\"schemaVersion\": 1", "\"schemaVersion\": 999");
            Assert.Throws<DialogException>(() => _serializer.Deserialize(json));
        }

        [Test]
        public void Unknown_NodeType_Throws() {
            const string json = "{ \"format\": \"wtfgames.dialog\", \"schemaVersion\": 1, \"id\": \"d\", " +
                                "\"entry\": \"n0\", \"nodes\": [ { \"id\": \"n0\", \"type\": \"bogus\" } ] }";
            Assert.Throws<DialogException>(() => _serializer.Deserialize(json));
        }

        [Test]
        public void Wrong_Format_Throws() {
            const string json = "{ \"format\": \"something.else\", \"schemaVersion\": 1, \"id\": \"d\", " +
                                "\"entry\": \"n0\", \"nodes\": [] }";
            Assert.Throws<DialogException>(() => _serializer.Deserialize(json));
        }

        [Test]
        public void Malformed_Json_Throws() {
            Assert.Throws<DialogException>(() => _serializer.Deserialize("{ this is not json"));
        }

        [Test]
        public void Duplicate_NodeIds_Throw() {
            const string json = "{ \"format\": \"wtfgames.dialog\", \"schemaVersion\": 1, \"id\": \"d\", " +
                                "\"entry\": \"n0\", \"nodes\": [ " +
                                "{ \"id\": \"n0\", \"type\": \"entry\", \"next\": \"x\" }, " +
                                "{ \"id\": \"n0\", \"type\": \"exit\" } ] }";
            Assert.Throws<DialogException>(() => _serializer.Deserialize(json));
        }
    }
}
