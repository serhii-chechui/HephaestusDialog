using System.Collections.Generic;

namespace WTFGames.Hephaestus.Dialog {
    /// <summary>
    /// The open registry of condition/action types (Q5). The game registers a handler and its
    /// editor descriptor together (one call — keeps runtime and editor in sync). The runner
    /// evaluates/executes specs through this; the editor reads descriptors from it.
    /// A null spec is a no-op: conditions pass, actions do nothing.
    /// </summary>
    public interface IDialogRegistry {
        void RegisterCondition(string typeId, IConditionHandler handler, ConditionDescriptor descriptor);
        void RegisterAction(string typeId, IActionHandler handler, ActionDescriptor descriptor);

        bool IsConditionRegistered(string typeId);
        bool IsActionRegistered(string typeId);

        bool Evaluate(ConditionSpec spec, IDialogContext context);
        void Execute(ActionSpec spec, IDialogContext context);

        IReadOnlyCollection<ConditionDescriptor> ConditionDescriptors { get; }
        IReadOnlyCollection<ActionDescriptor> ActionDescriptors { get; }
    }

    /// <inheritdoc cref="IDialogRegistry"/>
    public sealed class DialogRegistry : IDialogRegistry {
        private readonly Dictionary<string, IConditionHandler> _conditionHandlers = new Dictionary<string, IConditionHandler>();
        private readonly Dictionary<string, IActionHandler> _actionHandlers = new Dictionary<string, IActionHandler>();
        private readonly Dictionary<string, ConditionDescriptor> _conditionDescriptors = new Dictionary<string, ConditionDescriptor>();
        private readonly Dictionary<string, ActionDescriptor> _actionDescriptors = new Dictionary<string, ActionDescriptor>();

        public void RegisterCondition(string typeId, IConditionHandler handler, ConditionDescriptor descriptor) {
            Require(typeId, handler, descriptor, descriptor?.TypeId);
            if (_conditionHandlers.ContainsKey(typeId)) {
                throw new DialogException($"Condition type '{typeId}' is already registered.");
            }
            _conditionHandlers.Add(typeId, handler);
            _conditionDescriptors.Add(typeId, descriptor);
        }

        public void RegisterAction(string typeId, IActionHandler handler, ActionDescriptor descriptor) {
            Require(typeId, handler, descriptor, descriptor?.TypeId);
            if (_actionHandlers.ContainsKey(typeId)) {
                throw new DialogException($"Action type '{typeId}' is already registered.");
            }
            _actionHandlers.Add(typeId, handler);
            _actionDescriptors.Add(typeId, descriptor);
        }

        public bool IsConditionRegistered(string typeId) => typeId != null && _conditionHandlers.ContainsKey(typeId);
        public bool IsActionRegistered(string typeId) => typeId != null && _actionHandlers.ContainsKey(typeId);

        public bool Evaluate(ConditionSpec spec, IDialogContext context) {
            if (spec == null) {
                return true; // no gate = pass
            }
            if (!_conditionHandlers.TryGetValue(spec.Type, out var handler)) {
                throw new DialogException($"Unregistered condition type '{spec.Type}'.");
            }
            return handler.Evaluate(spec.Params, context);
        }

        public void Execute(ActionSpec spec, IDialogContext context) {
            if (spec == null) {
                return;
            }
            if (!_actionHandlers.TryGetValue(spec.Type, out var handler)) {
                throw new DialogException($"Unregistered action type '{spec.Type}'.");
            }
            handler.Execute(spec.Params, context);
        }

        public IReadOnlyCollection<ConditionDescriptor> ConditionDescriptors => _conditionDescriptors.Values;
        public IReadOnlyCollection<ActionDescriptor> ActionDescriptors => _actionDescriptors.Values;

        private static void Require(string typeId, object handler, object descriptor, string descriptorTypeId) {
            if (string.IsNullOrEmpty(typeId)) {
                throw new DialogException("A registration type id cannot be null or empty.");
            }
            if (handler == null) {
                throw new DialogException($"Handler for '{typeId}' cannot be null.");
            }
            if (descriptor == null) {
                throw new DialogException($"Descriptor for '{typeId}' cannot be null.");
            }
            if (descriptorTypeId != typeId) {
                throw new DialogException($"Descriptor TypeId '{descriptorTypeId}' does not match registration id '{typeId}'.");
            }
        }
    }
}
