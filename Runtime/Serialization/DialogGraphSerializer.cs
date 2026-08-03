using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WTFGames.Hephaestus.Dialog;

namespace WTFGames.Hephaestus.Dialog.Serialization {
    /// <summary>
    /// Newtonsoft-based (de)serialization of the dialog graph. Nodes are discriminated by a
    /// <c>type</c> string; conditions/actions serialize as <c>{ type, params }</c> envelopes.
    /// On read, the <c>schemaVersion</c> is checked and migrations are applied (none defined yet;
    /// the hook is <see cref="Migrate"/>).
    /// </summary>
    public sealed class DialogGraphSerializer : IDialogGraphSerializer {
        public string Serialize(IDialogGraph graph) {
            if (graph == null) {
                throw new DialogException("Cannot serialize a null graph.");
            }

            var nodes = new JArray();
            foreach (var node in graph.Nodes) {
                nodes.Add(SerializeNode(node));
            }

            var root = new JObject {
                ["format"] = DialogFormat.FormatId,
                ["schemaVersion"] = DialogFormat.CurrentSchemaVersion,
                ["id"] = graph.Id,
                ["entry"] = graph.EntryId,
                ["nodes"] = nodes
            };
            return root.ToString(Formatting.Indented);
        }

        public IDialogGraph Deserialize(string json) {
            JObject root;
            try {
                root = JObject.Parse(json);
            } catch (Exception e) {
                throw new DialogException("Dialog JSON is not valid JSON.", e);
            }

            var format = (string)root["format"];
            if (format != DialogFormat.FormatId) {
                throw new DialogException($"Unexpected dialog format '{format}' (expected '{DialogFormat.FormatId}').");
            }

            var versionToken = root["schemaVersion"];
            if (versionToken == null || versionToken.Type != JTokenType.Integer) {
                throw new DialogException("Dialog JSON is missing an integer 'schemaVersion'.");
            }
            var version = versionToken.Value<int>();
            if (version > DialogFormat.CurrentSchemaVersion) {
                throw new DialogException(
                    $"Dialog schemaVersion {version} is newer than supported {DialogFormat.CurrentSchemaVersion}.");
            }
            root = Migrate(root, version);

            var id = (string)root["id"];
            var entry = (string)root["entry"];
            if (!(root["nodes"] is JArray nodesArray)) {
                throw new DialogException($"Dialog '{id}' is missing a 'nodes' array.");
            }

            var nodes = new List<IDialogNode>(nodesArray.Count);
            foreach (var nodeToken in nodesArray) {
                nodes.Add(DeserializeNode((JObject)nodeToken));
            }
            return new DialogGraph(id, entry, nodes);
        }

        // Migration hook: bring an older-version root up to CurrentSchemaVersion. No-op today.
        private static JObject Migrate(JObject root, int fromVersion) {
            // for (var v = fromVersion; v < DialogFormat.CurrentSchemaVersion; v++) { ...apply v -> v+1... }
            return root;
        }

        #region Serialize nodes

        private static JObject SerializeNode(IDialogNode node) {
            switch (node) {
                case EntryNode n:
                    return new JObject { ["id"] = n.Id, ["type"] = "entry", ["next"] = n.Next };

                case LineNode n: {
                    var o = new JObject {
                        ["id"] = n.Id, ["type"] = "line",
                        ["speaker"] = n.SpeakerId, ["textKey"] = n.TextKey, ["next"] = n.Next
                    };
                    if (n.OnEnter.Count > 0) {
                        o["onEnter"] = SerializeSpecs(n.OnEnter);
                    }
                    return o;
                }

                case ChoiceNode n: {
                    var options = new JArray();
                    foreach (var opt in n.Options) {
                        var oo = new JObject { ["textKey"] = opt.TextKey, ["target"] = opt.Target };
                        if (opt.Show != null) {
                            oo["show"] = SerializeSpec(opt.Show.Type, opt.Show.Params);
                        }
                        if (opt.OnSelect.Count > 0) {
                            oo["onSelect"] = SerializeSpecs(opt.OnSelect);
                        }
                        options.Add(oo);
                    }
                    return new JObject { ["id"] = n.Id, ["type"] = "choice", ["options"] = options };
                }

                case ConditionNode n:
                    return new JObject {
                        ["id"] = n.Id, ["type"] = "condition",
                        ["condition"] = SerializeSpec(n.Condition?.Type, n.Condition?.Params),
                        ["ifTrue"] = n.IfTrue, ["ifFalse"] = n.IfFalse
                    };

                case ActionNode n: {
                    var o = new JObject { ["id"] = n.Id, ["type"] = "action", ["next"] = n.Next };
                    if (n.Actions.Count > 0) {
                        o["actions"] = SerializeSpecs(n.Actions);
                    }
                    return o;
                }

                case ExitNode n: {
                    var o = new JObject { ["id"] = n.Id, ["type"] = "exit" };
                    if (!string.IsNullOrEmpty(n.ResultTag)) {
                        o["resultTag"] = n.ResultTag;
                    }
                    return o;
                }

                default:
                    throw new DialogException($"Cannot serialize unknown node type '{node?.Type}'.");
            }
        }

        private static JArray SerializeSpecs(IReadOnlyList<ActionSpec> specs) {
            var arr = new JArray();
            foreach (var s in specs) {
                arr.Add(SerializeSpec(s.Type, s.Params));
            }
            return arr;
        }

        private static JObject SerializeSpec(string type, IReadOnlyDictionary<string, object> @params) {
            var paramObject = new JObject();
            if (@params != null) {
                foreach (var kv in @params) {
                    paramObject[kv.Key] = kv.Value == null ? JValue.CreateNull() : JToken.FromObject(kv.Value);
                }
            }
            return new JObject { ["type"] = type, ["params"] = paramObject };
        }

        #endregion

        #region Deserialize nodes

        private static IDialogNode DeserializeNode(JObject o) {
            var id = (string)o["id"];
            var type = (string)o["type"];
            switch (type) {
                case "entry":
                    return new EntryNode(id, (string)o["next"]);

                case "line":
                    return new LineNode(id, (string)o["speaker"], (string)o["textKey"], (string)o["next"],
                        ReadActions(o["onEnter"]));

                case "choice": {
                    var options = new List<ChoiceOption>();
                    if (o["options"] is JArray optionArray) {
                        foreach (var optToken in optionArray) {
                            var opt = (JObject)optToken;
                            options.Add(new ChoiceOption(
                                (string)opt["textKey"], (string)opt["target"],
                                ReadCondition(opt["show"]), ReadActions(opt["onSelect"])));
                        }
                    }
                    return new ChoiceNode(id, options);
                }

                case "condition":
                    return new ConditionNode(id, ReadCondition(o["condition"]),
                        (string)o["ifTrue"], (string)o["ifFalse"]);

                case "action":
                    return new ActionNode(id, ReadActions(o["actions"]), (string)o["next"]);

                case "exit":
                    return new ExitNode(id, (string)o["resultTag"]);

                default:
                    throw new DialogException($"Unknown node type '{type}' for node '{id}'.");
            }
        }

        private static ConditionSpec ReadCondition(JToken token) {
            if (!(token is JObject o)) {
                return null;
            }
            return new ConditionSpec((string)o["type"], ReadParams(o["params"]));
        }

        private static IReadOnlyList<ActionSpec> ReadActions(JToken token) {
            if (!(token is JArray arr)) {
                return Array.Empty<ActionSpec>();
            }
            var list = new List<ActionSpec>(arr.Count);
            foreach (var specToken in arr) {
                var o = (JObject)specToken;
                list.Add(new ActionSpec((string)o["type"], ReadParams(o["params"])));
            }
            return list;
        }

        private static IReadOnlyDictionary<string, object> ReadParams(JToken token) {
            var result = new Dictionary<string, object>();
            if (token is JObject o) {
                foreach (var prop in o.Properties()) {
                    result[prop.Name] = ReadValue(prop.Value);
                }
            }
            return result;
        }

        private static object ReadValue(JToken token) {
            switch (token.Type) {
                case JTokenType.Integer: return token.Value<long>();
                case JTokenType.Float: return token.Value<double>();
                case JTokenType.Boolean: return token.Value<bool>();
                case JTokenType.Null: return null;
                default: return token.Value<string>();
            }
        }

        #endregion
    }
}
