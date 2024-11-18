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

        if (GUILayout.Button("Cast Victory Spell"))
        {
            gameInput.CastSpell(Spell.Victory);
        }
        
        if (GUILayout.Button("Cast Palm Spell"))
        {
            gameInput.CastSpell(Spell.Palm);
        }

        if (GUILayout.Button("Cast Call Spell"))
        {
            gameInput.CastSpell(Spell.Call);
        }

    }
}
