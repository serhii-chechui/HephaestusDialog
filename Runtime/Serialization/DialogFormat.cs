using WTFGames.Hephaestus.Dialog;

namespace WTFGames.Hephaestus.Dialog.Serialization {
    /// <summary>Format identity and current schema version for the on-disk dialog JSON.</summary>
    public static class DialogFormat {
        public const string FormatId = "wtfgames.dialog";
        public const int CurrentSchemaVersion = 1;
    }

    /// <summary>Reads/writes a <see cref="IDialogGraph"/> to the versioned JSON format.</summary>
    public interface IDialogGraphSerializer {
        string Serialize(IDialogGraph graph);
        IDialogGraph Deserialize(string json);
    }
}
