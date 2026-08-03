using System.Collections.Generic;

namespace WTFGames.Hephaestus.Dialog {
    /// <summary>A resolved set of localization key → text entries for one locale.</summary>
    public interface IStringTable {
        string Locale { get; }
        bool TryGet(string key, out string text);
    }

    /// <summary>In-memory <see cref="IStringTable"/>.</summary>
    public sealed class StringTable : IStringTable {
        private readonly IReadOnlyDictionary<string, string> _entries;

        public string Locale { get; }

        public StringTable(string locale, IReadOnlyDictionary<string, string> entries) {
            Locale = locale;
            _entries = entries ?? new Dictionary<string, string>();
        }

        public bool TryGet(string key, out string text) {
            if (key != null && _entries.TryGetValue(key, out text)) {
                return true;
            }
            text = null;
            return false;
        }
    }
}
