using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WTFGames.Hephaestus.Dialog.Editor {
    /// <summary>
    /// Reads/writes the editor-only <c>{id}.layout.json</c>:
    /// <c>{ "nodes": { id: { x, y, color? } }, "comments": [ { x, y, text } ] }</c>.
    /// </summary>
    public sealed class DialogLayoutSerializer {
        public string Serialize(DialogLayout layout) {
            var nodes = new JObject();
            foreach (var pair in layout.Nodes) {
                var node = new JObject { ["x"] = pair.Value.X, ["y"] = pair.Value.Y };
                if (!string.IsNullOrEmpty(pair.Value.Color)) {
                    node["color"] = pair.Value.Color;
                }
                nodes[pair.Key] = node;
            }

            var comments = new JArray();
            foreach (var comment in layout.Comments) {
                comments.Add(new JObject {
                    ["x"] = comment.X, ["y"] = comment.Y, ["text"] = comment.Text
                });
            }

            return new JObject { ["nodes"] = nodes, ["comments"] = comments }.ToString(Formatting.Indented);
        }

        public DialogLayout Deserialize(string json) {
            var layout = new DialogLayout();
            JObject root;
            try {
                root = JObject.Parse(json);
            } catch (Exception e) {
                throw new DialogException("Layout JSON is not valid JSON.", e);
            }

            if (root["nodes"] is JObject nodes) {
                foreach (var prop in nodes.Properties()) {
                    if (prop.Value is JObject n) {
                        layout.Nodes[prop.Name] = new NodeLayout(
                            n["x"]?.Value<float>() ?? 0f,
                            n["y"]?.Value<float>() ?? 0f,
                            (string)n["color"]);
                    }
                }
            }

            if (root["comments"] is JArray comments) {
                foreach (var token in comments) {
                    if (token is JObject c) {
                        layout.Comments.Add(new CommentLayout {
                            X = c["x"]?.Value<float>() ?? 0f,
                            Y = c["y"]?.Value<float>() ?? 0f,
                            Text = (string)c["text"]
                        });
                    }
                }
            }

            return layout;
        }
    }
}
