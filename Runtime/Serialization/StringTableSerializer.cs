using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WTFGames.Hephaestus.Dialog;

namespace WTFGames.Hephaestus.Dialog.Serialization {
    /// <summary>
    /// Reads/writes a per-dialog string table: <c>{ "locale": "en", "entries": { key: text, ... } }</c>.
    /// </summary>
    public sealed class StringTableSerializer {
        public string Serialize(string locale, IReadOnlyDictionary<string, string> entries) {
            var entryObject = new JObject();
            if (entries != null) {
                foreach (var kv in entries) {
                    entryObject[kv.Key] = kv.Value;
                }
            }
            var root = new JObject {
                ["locale"] = locale,
                ["entries"] = entryObject
            };
            return root.ToString(Formatting.Indented);
        }

        public IStringTable Deserialize(string json) {
            var entries = DeserializeEntries(json, out var locale);
            return new StringTable(locale, entries);
        }

        /// <summary>
        /// Reads the raw {key -> text} entries and the locale — the form authoring tools work with
        /// (the runtime instead uses the <see cref="IStringTable"/> from <see cref="Deserialize"/>).
        /// </summary>
        public Dictionary<string, string> DeserializeEntries(string json, out string locale) {
            JObject root;
            try {
                root = JObject.Parse(json);
            } catch (Exception e) {
                throw new DialogException("String table JSON is not valid JSON.", e);
            }

            locale = (string)root["locale"];
            var entries = new Dictionary<string, string>();
            if (root["entries"] is JObject entryObject) {
                foreach (var prop in entryObject.Properties()) {
                    entries[prop.Name] = (string)prop.Value;
                }
            }
            return entries;
        }
    }
}
