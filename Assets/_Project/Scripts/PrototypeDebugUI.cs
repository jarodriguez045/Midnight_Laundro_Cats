using UnityEngine;

public class PrototypeDebugUI : MonoBehaviour
{
    [SerializeField] Health playerHealth;
    [SerializeField] PlayerMovementController movementController;
    [SerializeField] PlayerSwingController swingController;
    [SerializeField] PlayerWallClimbController climbController;
    [SerializeField] PlayerCombatController combatController;
    [SerializeField] Vector2 screenPosition = new Vector2(18f, 18f);
    [SerializeField] Vector2 panelSize = new Vector2(560f, 240f);
    [SerializeField] int fontSize = 18;

    GUIStyle _boxStyle;
    GUIStyle _labelStyle;

    void OnGUI()
    {
        EnsureStyles();

        Rect rect = new Rect(screenPosition.x, screenPosition.y, panelSize.x, panelSize.y);
        Color previous = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0f, 0f, 0f, 0.65f);
        GUI.Box(rect, GUIContent.none, _boxStyle);
        GUI.backgroundColor = previous;

        float x = rect.x + 12f;
        float y = rect.y + 10f;
        float line = fontSize + 6f;

        GUI.Label(new Rect(x, y, rect.width - 24f, line), "Midnight Laundro Cats — Prototype", _labelStyle);
        y += line;

        string health = playerHealth != null
            ? $"Health: {playerHealth.CurrentHealth:0}/{playerHealth.MaxHealth:0}" + (playerHealth.IsDead ? " (DEAD)" : string.Empty)
            : "Health: n/a";
        GUI.Label(new Rect(x, y, rect.width - 24f, line), health, _labelStyle);
        y += line;

        string move = movementController != null
            ? $"Move: speed {movementController.CurrentSpeed:0.0} | grounded {movementController.IsGrounded} | run {movementController.IsRunning} | locked {movementController.IsControlLocked}"
            : "Move: n/a";
        GUI.Label(new Rect(x, y, rect.width - 24f, line), move, _labelStyle);
        y += line;

        string swing = swingController != null
            ? $"Swing: {(swingController.IsSwinging ? $"ON len {swingController.RopeLength:0.0}" : "off")} (RMB attach/release, Q/E reel)"
            : "Swing: n/a";
        GUI.Label(new Rect(x, y, rect.width - 24f, line), swing, _labelStyle);
        y += line;

        string climb = climbController != null
            ? $"Climb: {(climbController.IsClimbing ? "ON" : "off")} (F toggle, Jump to leap off)"
            : "Climb: n/a";
        GUI.Label(new Rect(x, y, rect.width - 24f, line), climb, _labelStyle);
        y += line;

        string combat = combatController != null
            ? $"Combat: ready {combatController.IsAttackReady} cooldown {combatController.AttackCooldownRemaining:0.00}s (LMB)"
            : "Combat: n/a";
        GUI.Label(new Rect(x, y, rect.width - 24f, line), combat, _labelStyle);
        y += line;

        GUI.Label(new Rect(x, y, rect.width - 24f, line), "WASD move | Shift run | Space jump | Esc unlock cursor", _labelStyle);
    }

    void EnsureStyles()
    {
        if (_labelStyle != null)
        {
            return;
        }

        _boxStyle = new GUIStyle(GUI.skin.box);
        _boxStyle.normal.background = Texture2D.whiteTexture;

        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            normal = { textColor = Color.white },
            richText = false
        };
    }
}
