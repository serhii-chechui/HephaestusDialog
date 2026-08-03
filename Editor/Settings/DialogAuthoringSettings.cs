using UnityEditor;
using UnityEngine;

namespace WTFGames.Hephaestus.Dialog.Editor {
    /// <summary>
    /// Project-level authoring settings (Q10b): where the editor writes the exported dialog files,
    /// and the default locale of the string tables it authors. Committed as a single asset so the
    /// path is shared across the team. The default is generic (<c>Assets/Dialogs</c>); a consuming
    /// project points it at its own folder.
    /// </summary>
    public sealed class DialogAuthoringSettings : ScriptableObject {
        [SerializeField]
        [Tooltip("Project-relative folder the editor exports dialog files into.")]
        private string authoringDirectory = "Assets/Dialogs";

        [SerializeField]
        [Tooltip("Default locale of the string table authored alongside the graph.")]
        private string defaultLocale = "en";

        public string AuthoringDirectory => authoringDirectory;
        public string DefaultLocale => defaultLocale;

        /// <summary>Finds the single committed settings asset, or null if none exists yet.</summary>
        public static DialogAuthoringSettings Find() {
            var guids = AssetDatabase.FindAssets("t:DialogAuthoringSettings");
            if (guids.Length == 0) {
                return null;
            }
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<DialogAuthoringSettings>(path);
        }
    }
}
