using ParryArena.Arena;
using TMPro;
using UnityEngine;

namespace ParryArena.UI
{
    /// <summary>
    /// In-fight HUD: enemy health (top), player health + stamina (bottom-left),
    /// and a controls hint. Health bars update via events; stamina is polled
    /// because it changes continuously. The enemy stagger meter from the design
    /// lands here once the combat loop exists.
    /// </summary>
    public class ArenaHUD : MonoBehaviour
    {
        PlayerController _player;
        StatBar _playerHealth;
        StatBar _playerStamina;
        StatBar _enemyHealth;

        public void Build(Transform canvas, PlayerController player, EnemyActor enemy)
        {
            _player = player;

            // Enemy: top-centre name + health.
            var enemyBox = UIFactory.CreateAnchoredBox(canvas, "EnemyHUD",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -28f), new Vector2(720f, 64f));
            UIFactory.AddVerticalLayout(enemyBox.gameObject, 6f, TextAnchor.UpperCenter);
            UIFactory.CreateLabel(enemyBox, enemy.Definition.DisplayName, UITheme.SmallSize,
                TextAlignmentOptions.Center, UITheme.Text);
            _enemyHealth = UIFactory.CreateBar(enemyBox, UITheme.EnemyHealth, 18f, 720f);

            // Player: bottom-left health + stamina.
            var playerBox = UIFactory.CreateAnchoredBox(canvas, "PlayerHUD",
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(32f, 32f), new Vector2(440f, 78f));
            UIFactory.AddVerticalLayout(playerBox.gameObject, 8f, TextAnchor.UpperLeft);
            _playerHealth = UIFactory.CreateBar(playerBox, UITheme.Health, 24f, 440f);
            _playerStamina = UIFactory.CreateBar(playerBox, UITheme.Stamina, 14f, 440f);

            // Controls hint.
            var hintBox = UIFactory.CreateAnchoredBox(canvas, "ControlsHint",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 22f), new Vector2(960f, 28f));
            UIFactory.CreateLabel(hintBox,
                "WASD move   •   LMB attack   •   RMB block / tap-parry   •   Space dodge   •   MMB lock-on   •   Esc pause",
                UITheme.SmallSize, TextAlignmentOptions.Center, UITheme.TextMuted);

            // Health bars are event-driven; set the initial fill explicitly since
            // Init() already fired before we subscribed.
            player.Health.Changed += h => _playerHealth.SetFraction(h.Fraction);
            enemy.Health.Changed += h => _enemyHealth.SetFraction(h.Fraction);
            _playerHealth.SetFraction(player.Health.Fraction);
            _enemyHealth.SetFraction(enemy.Health.Fraction);
        }

        void Update()
        {
            if (_player != null && _playerStamina != null)
                _playerStamina.SetFraction(_player.Stamina / _player.MaxStamina);
        }
    }
}
