using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine.UIElements;

namespace WTFGames.Hephaestus.Dialog.Editor {
    /// <summary>
    /// Builds one editor field for a descriptor <see cref="ParamField"/>, bound to a params
    /// dictionary (shared by the condition and action editors). Enum params are stored as the
    /// selected option index; ints/floats as numbers; the rest as strings. A default is seeded when
    /// the key is absent so the emitted spec is complete. <paramref name="onChange"/> runs after edits.
    /// </summary>
    public static class SpecFieldFactory {
        public static VisualElement Make(ParamField field, IDictionary<string, object> @params, Action onChange) {
            switch (field.Kind) {
                case ParamKind.Bool: {
                    EnsureDefault(@params, field.Name, GetBool(@params, field.Name));
                    var toggle = new Toggle(field.Name) { value = GetBool(@params, field.Name) };
                    toggle.RegisterValueChangedCallback(e => { @params[field.Name] = e.newValue; onChange(); });
                    return toggle;
                }
                case ParamKind.Enum: {
                    var options = field.Options?.ToList() ?? new List<string>();
                    if (options.Count == 0) {
                        return new Label($"{field.Name}: (no options)");
                    }
                    var index = GetInt(@params, field.Name);
                    if (index < 0 || index >= options.Count) {
                        index = 0;
                    }
                    EnsureDefault(@params, field.Name, index);
                    var popup = new PopupField<string>(field.Name, options, options[index]);
                    popup.RegisterValueChangedCallback(e => { @params[field.Name] = options.IndexOf(e.newValue); onChange(); });
                    return popup;
                }
                case ParamKind.Int:
                case ParamKind.Float: {
                    EnsureDefault(@params, field.Name,
                        field.Kind == ParamKind.Int ? (object)GetInt(@params, field.Name) : GetFloat(@params, field.Name));
                    var numberField = new TextField(field.Name) { value = GetNumberString(@params, field.Name) };
                    numberField.RegisterValueChangedCallback(e => { SetNumber(@params, field.Name, e.newValue, field.Kind); onChange(); });
                    return numberField;
                }
                default: {
                    EnsureDefault(@params, field.Name, GetString(@params, field.Name));
                    var textField = new TextField(field.Name) { value = GetString(@params, field.Name) };
                    textField.RegisterValueChangedCallback(e => { @params[field.Name] = e.newValue; onChange(); });
                    return textField;
                }
            }
        }

        private static void EnsureDefault(IDictionary<string, object> p, string key, object value) {
            if (!p.ContainsKey(key)) {
                p[key] = value;
            }
        }

        private static bool GetBool(IDictionary<string, object> p, string key)
            => p.TryGetValue(key, out var v) && v is bool b && b;

        private static int GetInt(IDictionary<string, object> p, string key)
            => p.TryGetValue(key, out var v) && v != null ? Convert.ToInt32(v, CultureInfo.InvariantCulture) : 0;

        private static float GetFloat(IDictionary<string, object> p, string key)
            => p.TryGetValue(key, out var v) && v != null ? Convert.ToSingle(v, CultureInfo.InvariantCulture) : 0f;

        private static string GetString(IDictionary<string, object> p, string key)
            => p.TryGetValue(key, out var v) && v != null ? Convert.ToString(v, CultureInfo.InvariantCulture) : "";

        private static string GetNumberString(IDictionary<string, object> p, string key)
            => p.TryGetValue(key, out var v) && v != null ? Convert.ToString(v, CultureInfo.InvariantCulture) : "0";

        private static void SetNumber(IDictionary<string, object> p, string key, string raw, ParamKind kind) {
            if (kind == ParamKind.Int) {
                if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)) {
                    p[key] = i;
                }
            } else if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var f)) {
                p[key] = f;
            }
        }
    }
}
