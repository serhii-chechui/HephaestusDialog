using System.Collections.Generic;

namespace WTFGames.Hephaestus.Dialog.Editor {
    /// <summary>
    /// Edit-time supplier of the condition/action descriptors a game registers at runtime, so the
    /// GraphView editor can render type pickers + param fields without referencing the game. A
    /// consuming project implements this in an editor assembly; <see cref="DialogDescriptorCatalog"/>
    /// discovers implementations via TypeCache. Implementations must have a public parameterless ctor.
    /// </summary>
    public interface IDialogDescriptorSource {
        IEnumerable<ConditionDescriptor> GetConditionDescriptors();
        IEnumerable<ActionDescriptor> GetActionDescriptors();
    }
}
