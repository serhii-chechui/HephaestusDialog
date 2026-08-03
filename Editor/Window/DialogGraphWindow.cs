using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using WTFGames.Hephaestus.Dialog.Serialization;

namespace WTFGames.Hephaestus.Dialog.Editor {
    /// <summary>
    /// The dialog graph editor window (Phase 3). Create nodes (right-click), edit their content
    /// inline, wire edges, and export back through the validator + writer. The descriptor-driven
    /// condition/action editing and the in-editor preview land in the next slices.
    /// </summary>
    public class DialogGraphWindow : EditorWindow {
        private DialogGraphView _graphView;
        private TextField _idField;

        private string _dialogId = "";
        private string _directory;
        private string _locale = "en";
        private Dictionary<string, string> _strings = new Dictionary<string, string>();

        [MenuItem("Window/WTFGames Dialog/Graph Editor")]
        public static void Open() {
            var window = GetWindow<DialogGraphWindow>();
            window.titleContent = new GUIContent("Dialog Graph");
            window.minSize = new Vector2(720f, 480f);
        }

        private void CreateGUI() {
            var toolbar = new Toolbar();
            toolbar.Add(new ToolbarButton(LoadDialog) { text = "Load…" });
            toolbar.Add(new ToolbarButton(SaveDialog) { text = "Save…" });
            toolbar.Add(new ToolbarButton(NewDialog) { text = "New" });
            toolbar.Add(new ToolbarButton(() => _graphView?.Clear()) { text = "Clear" });

            _idField = new TextField { value = _dialogId, style = { minWidth = 160 } };
            _idField.RegisterValueChangedCallback(e => _dialogId = e.newValue);
            toolbar.Add(new Label(" Id: "));
            toolbar.Add(_idField);

            rootVisualElement.Add(toolbar);

            var body = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1 } };
            _graphView = new DialogGraphView { style = { flexGrow = 1 } };
            body.Add(_graphView);
            body.Add(new DialogPreviewPanel(_graphView));
            rootVisualElement.Add(body);
        }

        private void NewDialog() {
            _graphView.Clear();
            _strings.Clear();
            _dialogId = "new_dialog";
            _idField.value = _dialogId;
            _directory = null;
        }

        private void LoadDialog() {
            var path = EditorUtility.OpenFilePanel("Open dialog JSON", Application.dataPath, "json");
            if (string.IsNullOrEmpty(path)) {
                return;
            }

            try {
                var graph = new DialogGraphSerializer().Deserialize(File.ReadAllText(path));
                _dialogId = graph.Id;
                _idField.value = _dialogId;
                _directory = Path.GetDirectoryName(path);
                LoadStrings();
                _graphView.LoadGraph(graph, LoadLayout(), ResolveText);
            } catch (System.Exception e) {
                EditorUtility.DisplayDialog("Load failed", e.Message, "OK");
            }
        }

        private void SaveDialog() {
            if (string.IsNullOrEmpty(_dialogId)) {
                EditorUtility.DisplayDialog("Set an id", "Enter a dialog id in the toolbar first.", "OK");
                return;
            }

            var graph = _graphView.BuildGraph(_dialogId);
            _strings.Clear();
            _graphView.CollectStrings(_strings);

            var report = DialogValidator.Validate(graph, new StringTable(_locale, _strings));
            if (!report.CanExport) {
                EditorUtility.DisplayDialog("Cannot export — fix these first", FormatBlocks(report), "OK");
                return;
            }

            var directory = EditorUtility.SaveFolderPanel("Export dialog to folder",
                string.IsNullOrEmpty(_directory) ? Application.dataPath : _directory, "");
            if (string.IsNullOrEmpty(directory)) {
                return;
            }

            new DialogExportWriter().Write(directory, _locale, graph, _strings, _graphView.BuildLayout());
            _directory = directory;
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Exported", $"Wrote '{_dialogId}' to:\n{directory}", "OK");
        }

        private string ResolveText(string key) => _strings.TryGetValue(key, out var text) ? text : "";

        private void LoadStrings() {
            _strings = new Dictionary<string, string>();
            _locale = "en";
            if (string.IsNullOrEmpty(_directory)) {
                return;
            }
            var stringsPath = Path.Combine(_directory, $"{_dialogId}.strings.{_locale}.json");
            if (File.Exists(stringsPath)) {
                _strings = new StringTableSerializer().DeserializeEntries(File.ReadAllText(stringsPath), out var locale);
                if (!string.IsNullOrEmpty(locale)) {
                    _locale = locale;
                }
            }
        }

        private DialogLayout LoadLayout() {
            if (string.IsNullOrEmpty(_directory)) {
                return null;
            }
            var layoutPath = Path.Combine(_directory, $"{_dialogId}.layout.json");
            return File.Exists(layoutPath)
                ? new DialogLayoutSerializer().Deserialize(File.ReadAllText(layoutPath))
                : null;
        }

        private static string FormatBlocks(ValidationReport report) {
            var builder = new StringBuilder();
            foreach (var issue in report.Issues) {
                if (issue.Severity == ValidationSeverity.Block) {
                    builder.AppendLine("• " + issue);
                }
            }
            return builder.ToString();
        }
    }
}
