using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace WTFGames.Hephaestus.Dialog.Editor {
    /// <summary>
    /// Descriptor-driven editor for a single <see cref="ConditionSpec"/> (Q5): a type dropdown fed
    /// by <see cref="DialogDescriptorCatalog"/> plus one field per the selected type's ParamField.
    /// Emits the edited spec (or null for "none") via a callback. Enum params are stored as the
    /// selected option index (matching how the game maps them, e.g. stat.check target/method).
    /// Unknown types (no descriptor registered) keep their params untouched for round-trip.
    /// </summary>
    public sealed class ConditionEditorElement : VisualElement {
        private const string NoneLabel = "(none)";

        private readonly Action<ConditionSpec> _onChange;
        private readonly Dictionary<string, object> _params = new Dictionary<string, object>();
        private readonly VisualElement _paramsBox = new VisualElement();

        private string _type;

        public ConditionEditorElement(ConditionSpec initial, Action<ConditionSpec> onChange) {
            _onChange = onChange;
            _type = initial?.Type;
            if (initial != null) {
                foreach (var pair in initial.Params) {
                    _params[pair.Key] = pair.Value;
                }
            }

            var choices = new List<string> { NoneLabel };
            choices.AddRange(DialogDescriptorCatalog.Conditions.Select(d => d.TypeId));
            var current = _type ?? NoneLabel;
            if (current != NoneLabel && !choices.Contains(current)) {
                choices.Add(current); // preserve an unregistered type
            }

            var typeField = new PopupField<string>("Condition", choices, current);
            typeField.RegisterValueChangedCallback(e => OnTypeChanged(e.newValue));
            Add(typeField);
            Add(_paramsBox);

            RebuildParams();
        }

        private void OnTypeChanged(string type) {
            if (type == NoneLabel) {
                _type = null;
            } else {
                _type = type;
            }
            _params.Clear();
            RebuildParams();
            Emit();
        }

        private void RebuildParams() {
            _paramsBox.Clear();
            if (_type == null) {
                return;
            }

            var descriptor = DialogDescriptorCatalog.FindCondition(_type);
            if (descriptor == null) {
                _paramsBox.Add(new Label("(unregistered type — params preserved)"));
                return;
            }

            foreach (var field in descriptor.Params) {
                _paramsBox.Add(SpecFieldFactory.Make(field, _params, Emit));
            }
        }

        private void Emit() {
            _onChange(_type == null ? null : new ConditionSpec(_type, new Dictionary<string, object>(_params)));
        }
    }
}
