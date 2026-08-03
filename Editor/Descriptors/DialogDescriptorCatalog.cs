using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace WTFGames.Hephaestus.Dialog.Editor {
    /// <summary>
    /// Aggregates the condition/action descriptors from every <see cref="IDialogDescriptorSource"/>
    /// in the project (found via TypeCache), so the editor can present the registered types. Cached;
    /// call <see cref="Refresh"/> after changing sources (or a domain reload clears it).
    /// </summary>
    public static class DialogDescriptorCatalog {
        private static List<ConditionDescriptor> _conditions;
        private static List<ActionDescriptor> _actions;

        public static IReadOnlyList<ConditionDescriptor> Conditions {
            get { EnsureLoaded(); return _conditions; }
        }

        public static IReadOnlyList<ActionDescriptor> Actions {
            get { EnsureLoaded(); return _actions; }
        }

        public static void Refresh() {
            _conditions = null;
            _actions = null;
        }

        public static ConditionDescriptor FindCondition(string typeId) {
            foreach (var descriptor in Conditions) {
                if (descriptor.TypeId == typeId) {
                    return descriptor;
                }
            }
            return null;
        }

        public static ActionDescriptor FindAction(string typeId) {
            foreach (var descriptor in Actions) {
                if (descriptor.TypeId == typeId) {
                    return descriptor;
                }
            }
            return null;
        }

        [MenuItem("Window/WTFGames Dialog/Log Registered Types")]
        private static void LogRegisteredTypes() {
            var conditions = string.Join(", ", ConditionTypeIds());
            var actions = string.Join(", ", ActionTypeIds());
            Debug.Log($"[Dialog] Conditions: [{conditions}]  Actions: [{actions}]");
        }

        private static IEnumerable<string> ConditionTypeIds() {
            foreach (var descriptor in Conditions) {
                yield return descriptor.TypeId;
            }
        }

        private static IEnumerable<string> ActionTypeIds() {
            foreach (var descriptor in Actions) {
                yield return descriptor.TypeId;
            }
        }

        private static void EnsureLoaded() {
            if (_conditions != null) {
                return;
            }
            _conditions = new List<ConditionDescriptor>();
            _actions = new List<ActionDescriptor>();

            foreach (var type in TypeCache.GetTypesDerivedFrom<IDialogDescriptorSource>()) {
                if (type.IsAbstract || type.IsInterface) {
                    continue;
                }
                try {
                    var source = (IDialogDescriptorSource)Activator.CreateInstance(type);
                    _conditions.AddRange(source.GetConditionDescriptors());
                    _actions.AddRange(source.GetActionDescriptors());
                } catch (Exception e) {
                    Debug.LogError($"[Dialog] Descriptor source '{type.FullName}' failed: {e.Message}");
                }
            }
        }
    }
}
