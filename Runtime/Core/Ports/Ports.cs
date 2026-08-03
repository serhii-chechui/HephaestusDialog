using System.Threading.Tasks;

namespace WTFGames.Hephaestus.Dialog {
    /// <summary>
    /// Resolves a localization key to display text for the active locale. The presenter uses it;
    /// missing keys should return a visible fallback (e.g. the key itself) rather than throw.
    /// </summary>
    public interface ILocalizedTextProvider {
        string Resolve(string key);
    }

    /// <summary>
    /// Loads a dialog graph by id (Q10a: async I/O port, plain <see cref="Task"/> so the core stays
    /// engine-pure). The game implements it — e.g. over Addressables — parsing JSON into a graph.
    /// The host awaits the load once, before handing the graph to the synchronous runner.
    /// </summary>
    public interface IDialogRepository {
        Task<IDialogGraph> LoadAsync(string dialogId);
    }

    /// <summary>Loads the per-dialog string table for a locale (async, plain <see cref="Task"/>).</summary>
    public interface IStringTableRepository {
        Task<IStringTable> LoadStringsAsync(string dialogId, string locale);
    }
}
