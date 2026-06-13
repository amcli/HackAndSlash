using System.Collections.Generic;
using ParryArena.Core;
using ParryArena.Data;

namespace ParryArena.UI
{
    public class EnemySelectScreen : SelectionScreen<EnemyDefinition>
    {
        protected override string Title => "SELECT OPPONENT";
        protected override string ConfirmLabel => "Start Fight";
        protected override IReadOnlyList<EnemyDefinition> Items => GameContent.Enemies;
        protected override EnemyDefinition CurrentSelection => GameApp.Instance.Session.SelectedEnemy;

        protected override string DisplayName(EnemyDefinition item) => item.DisplayName;
        protected override void Apply(EnemyDefinition item) => GameApp.Instance.Session.SelectedEnemy = item;

        protected override string Describe(EnemyDefinition item) =>
            $"<b>{item.DisplayName}</b>\n{item.Description}\n\n" +
            $"<color=#9AA2AC>Health</color> {item.MaxHealth:0}";
    }
}
