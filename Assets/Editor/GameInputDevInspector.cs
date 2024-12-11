using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GameInputDev))]
public class GameInputDevInspector : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GameInputDev gameInput = (GameInputDev)target;

        if (GUILayout.Button("Start Game"))
        {
            gameInput.StartGame();
        }

        if (GUILayout.Button("Cast Heart Spell"))
        {
            gameInput.CastSpell(Spell.Heart);
            gameInput.StartGame();
        }

        if (GUILayout.Button("Cast Diamond Spell"))
        {
            gameInput.CastSpell(Spell.Diamond);
        }
    }
}
