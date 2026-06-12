using System.Collections.Generic;
using ParryArena.Core;
using ParryArena.Data;

namespace ParryArena.UI
{
    public class LoadoutSelectScreen : SelectionScreen<LoadoutDefinition>
    {
        protected override string Title => "SELECT LOADOUT";
        protected override string ConfirmLabel => "Confirm";
        protected override IReadOnlyList<LoadoutDefinition> Items => GameContent.Loadouts;
        protected override LoadoutDefinition CurrentSelection => GameApp.Instance.Session.SelectedLoadout;

        protected override string DisplayName(LoadoutDefinition item) => item.DisplayName;
        protected override void Apply(LoadoutDefinition item) => GameApp.Instance.Session.SelectedLoadout = item;

        protected override string Describe(LoadoutDefinition item) =>
            $"<b>{item.DisplayName}</b>\n{item.Description}\n\n" +
            $"<color=#9AA2AC>Health</color> {item.MaxHealth:0}    " +
            $"<color=#9AA2AC>Stamina</color> {item.MaxStamina:0}    " +
            $"<color=#9AA2AC>Damage</color> {item.AttackDamage:0}";
    }
}
