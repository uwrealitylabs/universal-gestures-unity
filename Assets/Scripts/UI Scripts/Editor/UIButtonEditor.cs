using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


[CustomEditor(typeof(UIButton))]
public class UIButtonEditor : Editor
{
    public override void OnInspectorGUI()
    {
        SerializedProperty buttonTypeProp = serializedObject.FindProperty("buttonType");
        SerializedProperty uiPanelProp = serializedObject.FindProperty("targetUIPanel");
        SerializedProperty linkProp = serializedObject.FindProperty("link");
        SerializedProperty functionProp = serializedObject.FindProperty("function");

        SerializedProperty dictatesOtherProp = serializedObject.FindProperty("dictatesOther");
        SerializedProperty dependentButtonProp = serializedObject.FindProperty("dependentButton");

        SerializedProperty hasStateSpritesProp = serializedObject.FindProperty("hasStateSprites");
        SerializedProperty myImageProp = serializedObject.FindProperty("myImage");
        SerializedProperty activeSpriteProp = serializedObject.FindProperty("activeSprite");
        SerializedProperty inactiveSpriteProp = serializedObject.FindProperty("inactiveSprite");

        EditorGUILayout.PropertyField(buttonTypeProp, new GUIContent("Button Type"));
        EditorGUILayout.PropertyField(dictatesOtherProp, new GUIContent("Dictates Other?"));
        EditorGUILayout.PropertyField(hasStateSpritesProp, new GUIContent("Has State Sprites?"));

        ButtonType buttonType = (ButtonType)buttonTypeProp.enumValueIndex;
        Boolean dictatesOther = dictatesOtherProp.boolValue;
        Boolean hasStateSprites = hasStateSpritesProp.boolValue;

        switch (buttonType)
        {
            case ButtonType.ControlPanel:
                EditorGUILayout.PropertyField(uiPanelProp, new GUIContent("Target UI Panel"));
                break;

            case ButtonType.OpenLink:
                EditorGUILayout.PropertyField(linkProp, new GUIContent("Link"));
                break;

            case ButtonType.PerformFunction:
                EditorGUILayout.PropertyField(functionProp, new GUIContent("On Activation"));
                break;
        }

        if (dictatesOther)
        {
            EditorGUILayout.PropertyField(dependentButtonProp, new GUIContent("Dependent Button"));
        }

        if (hasStateSprites)
        {
            EditorGUILayout.PropertyField(myImageProp, new GUIContent("Image"));
            EditorGUILayout.PropertyField(activeSpriteProp, new GUIContent("Active Sprite"));
            EditorGUILayout.PropertyField(inactiveSpriteProp, new GUIContent("Inactive Sprite"));
        }

        serializedObject.ApplyModifiedProperties();
    }
}
