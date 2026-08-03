using System;
using System.Collections.Generic;
using System.Globalization;

namespace WTFGames.Hephaestus.Dialog {
    /// <summary>
    /// Typed readers over a spec's param bag. Values arrive boxed (int/long/double/bool/string,
    /// e.g. from JSON), so these convert defensively rather than casting — handler code stays clean.
    /// </summary>
    public static class SpecParams {
        public static bool Has(this IReadOnlyDictionary<string, object> p, string key) {
            return p != null && p.ContainsKey(key);
        }

        public static string GetString(this IReadOnlyDictionary<string, object> p, string key, string fallback = null) {
            return TryGet(p, key, out var v) && v != null ? Convert.ToString(v, CultureInfo.InvariantCulture) : fallback;
        }

        public static int GetInt(this IReadOnlyDictionary<string, object> p, string key, int fallback = 0) {
            return TryGet(p, key, out var v) && v != null ? Convert.ToInt32(v, CultureInfo.InvariantCulture) : fallback;
        }

        public static float GetFloat(this IReadOnlyDictionary<string, object> p, string key, float fallback = 0f) {
            return TryGet(p, key, out var v) && v != null ? Convert.ToSingle(v, CultureInfo.InvariantCulture) : fallback;
        }

        public static bool GetBool(this IReadOnlyDictionary<string, object> p, string key, bool fallback = false) {
            if (!TryGet(p, key, out var v) || v == null) {
                return fallback;
            }
            if (v is bool b) {
                return b;
            }
            if (v is string s) {
                return s == "1" || bool.TryParse(s, out var parsed) && parsed;
            }
            return Convert.ToInt64(v, CultureInfo.InvariantCulture) != 0;
        }

        private static bool TryGet(IReadOnlyDictionary<string, object> p, string key, out object value) {
            if (p != null && key != null && p.TryGetValue(key, out value)) {
                return true;
            }
            value = null;
            return false;
        }
    }
}
