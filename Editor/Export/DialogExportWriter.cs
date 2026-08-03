using System.Collections.Generic;
using System.IO;
using WTFGames.Hephaestus.Dialog.Serialization;

namespace WTFGames.Hephaestus.Dialog.Editor {
    /// <summary>Paths of the files written by an export.</summary>
    public sealed class DialogExportResult {
        public string DialogPath { get; }
        public string StringsPath { get; }
        public string LayoutPath { get; }

        public DialogExportResult(string dialogPath, string stringsPath, string layoutPath) {
            DialogPath = dialogPath;
            StringsPath = stringsPath;
            LayoutPath = layoutPath;
        }
    }

    /// <summary>
    /// Writes the three per-dialog files (Q8) into a directory: <c>{id}.dialog.json</c> (runtime
    /// graph+logic), <c>{id}.strings.{locale}.json</c> (localization), and <c>{id}.layout.json</c>
    /// (editor-only). Addressables-agnostic — marking the runtime files Addressable (Q10c) is a
    /// game-side concern, done on import.
    /// </summary>
    public sealed class DialogExportWriter {
        private readonly IDialogGraphSerializer _graphSerializer;
        private readonly StringTableSerializer _stringTableSerializer;
        private readonly DialogLayoutSerializer _layoutSerializer;

        public DialogExportWriter(
            IDialogGraphSerializer graphSerializer = null,
            StringTableSerializer stringTableSerializer = null,
            DialogLayoutSerializer layoutSerializer = null) {
            _graphSerializer = graphSerializer ?? new DialogGraphSerializer();
            _stringTableSerializer = stringTableSerializer ?? new StringTableSerializer();
            _layoutSerializer = layoutSerializer ?? new DialogLayoutSerializer();
        }

        public DialogExportResult Write(
            string directory,
            string locale,
            IDialogGraph graph,
            IReadOnlyDictionary<string, string> strings,
            DialogLayout layout) {

            if (graph == null) {
                throw new DialogException("Cannot export a null graph.");
            }
            if (string.IsNullOrEmpty(directory)) {
                throw new DialogException("Export directory is not set.");
            }
            Directory.CreateDirectory(directory);

            var id = graph.Id;
            var dialogPath = Path.Combine(directory, $"{id}.dialog.json");
            var stringsPath = Path.Combine(directory, $"{id}.strings.{locale}.json");
            var layoutPath = Path.Combine(directory, $"{id}.layout.json");

            File.WriteAllText(dialogPath, _graphSerializer.Serialize(graph));
            File.WriteAllText(stringsPath, _stringTableSerializer.Serialize(locale, strings));
            File.WriteAllText(layoutPath, _layoutSerializer.Serialize(layout ?? new DialogLayout()));

            return new DialogExportResult(dialogPath, stringsPath, layoutPath);
        }
    }
}
