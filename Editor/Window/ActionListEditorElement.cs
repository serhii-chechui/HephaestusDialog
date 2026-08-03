using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace WTFGames.Hephaestus.Dialog.Editor {
    /// <summary>
    /// Descriptor-driven editor for a list of <see cref="ActionSpec"/> (e.g. a line's OnEnter, an
    /// ActionNode's actions, a choice option's OnSelect). Each entry is a type dropdown (from the
    /// registered action types) + its param fields, with add/remove. Emits the whole list via a
    /// callback. Unregistered types are preserved for round-trip.
    /// </summary>
    public sealed class ActionListEditorElement : VisualElement {
        private sealed class Entry {
            public string Type;
            public Dictionary<string, object> Params = new Dictionary<string, object>();
        }

        private readonly Action<IReadOnlyList<ActionSpec>> _onChange;
        private readonly List<Entry> _entries = new List<Entry>();
        private readonly VisualElement _listBox = new VisualElement();

        public ActionListEditorElement(string title, IReadOnlyList<ActionSpec> initial,
            Action<IReadOnlyList<ActionSpec>> onChange) {
            _onChange = onChange;
            if (initial != null) {
                foreach (var action in initial) {
                    _entries.Add(new Entry {
                        Type = action.Type,
                        Params = new Dictionary<string, object>(action.Params)
                    });
                }
            }

            Add(new Label(title));
            Add(_listBox);
            Add(new Button(AddEntry) { text = "+ Action" });
            Rebuild();
        }

        private void Rebuild() {
            _listBox.Clear();
            foreach (var entry in _entries) {
                _listBox.Add(EntryEditor(entry));
            }
        }

        private VisualElement EntryEditor(Entry entry) {
            var box = new VisualElement { style = { marginBottom = 4, marginLeft = 8 } };

            var choices = DialogDescriptorCatalog.Actions.Select(d => d.TypeId).ToList();
            if (choices.Count == 0 && entry.Type == null) {
                box.Add(new Label("(no action types registered)"));
                box.Add(new Button(() => { _entries.Remove(entry); Rebuild(); Emit(); }) { text = "− Remove action" });
                return box;
            }
            if (entry.Type == null) {
                entry.Type = choices[0];
            } else if (!choices.Contains(entry.Type)) {
                choices.Add(entry.Type); // preserve an unregistered type
            }

            var typeField = new PopupField<string>("Action", choices, entry.Type);
            typeField.RegisterValueChangedCallback(e => {
                entry.Type = e.newValue;
                entry.Params.Clear();
                Rebuild();
                Emit();
            });
            box.Add(typeField);

            var descriptor = DialogDescriptorCatalog.FindAction(entry.Type);
            if (descriptor == null) {
                box.Add(new Label("(unregistered — params preserved)"));
            } else {
                foreach (var field in descriptor.Params) {
                    box.Add(SpecFieldFactory.Make(field, entry.Params, Emit));
                }
            }

            box.Add(new Button(() => { _entries.Remove(entry); Rebuild(); Emit(); }) { text = "− Remove action" });
            return box;
        }

        private void AddEntry() {
            var firstType = DialogDescriptorCatalog.Actions.Select(d => d.TypeId).FirstOrDefault();
            _entries.Add(new Entry { Type = firstType });
            Rebuild();
            Emit();
        }

        private void Emit() {
            var specs = _entries
                .Where(e => !string.IsNullOrEmpty(e.Type))
                .Select(e => new ActionSpec(e.Type, new Dictionary<string, object>(e.Params)))
                .ToList();
            _onChange(specs);
        }
    }
}
