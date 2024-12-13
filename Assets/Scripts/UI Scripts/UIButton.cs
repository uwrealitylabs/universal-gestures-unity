using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

enum ButtonType
{
    ControlPanel,
    OpenLink,
    PerformFunction,
    None
};

enum State
{
    Active,
    Inactive
}

[CustomEditor(typeof(UIButton))]
public class UIButtonEditor : Editor
{
    public override void OnInspectorGUI()
    {
        SerializedProperty buttonTypeProp = serializedObject.FindProperty("buttonType");
        SerializedProperty uiPanelProp = serializedObject.FindProperty("targetUIPanel");
        SerializedProperty linkProp = serializedObject.FindProperty ("link");
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

        ButtonType buttonType = (ButtonType) buttonTypeProp.enumValueIndex;
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

public class UIButton : MonoBehaviour
{
    [SerializeField]
    private ButtonType buttonType;

    // only if button type controls panel
    [SerializeField]
    private GameObject targetUIPanel;

    // only if the button opens link
    [SerializeField]
    private string link;

    // only if button performs function
    [SerializeField]
    private UnityEvent function;

    // manage if the button controls the state of another button
    [SerializeField]
    private Boolean dictatesOther;

    [SerializeField]
    private UIButton dependentButton;

    // manage if the button has active and inactive sprite states
    [SerializeField]
    private Boolean hasStateSprites;

    [SerializeField]
    private Image myImage;

    [SerializeField]
    private Sprite activeSprite;

    [SerializeField]
    private Sprite inactiveSprite;

    private State state;

    public void Start()
    {
        if (hasStateSprites)
        {
            myImage.sprite = inactiveSprite;
        }
        state = State.Inactive;
    }
    public void ClosePanel()
    {
        Debug.Log("Panel Closed");
        Activate();
        targetUIPanel.SetActive(false);
    }

    public void OpenPanel()
    {
        Activate();
        targetUIPanel.SetActive(true);
    }

    public void Deactivate()
    {
        state = State.Inactive;
        if (hasStateSprites)
        {
            myImage.sprite = inactiveSprite;
        }
    }

    public void Activate()
    {
        state = State.Active;
        if (hasStateSprites)
        {
            myImage.sprite = activeSprite;
        }
        if (dictatesOther)
        {
            dependentButton.Deactivate();
        }
    }

    public void OpenURL()
    {
        Application.OpenURL(link);
        Activate();
    }

    public void PerformFunction()
    {
        function.Invoke();
        if(state == State.Inactive)
        {
            Activate();
        }else if(state == State.Active)
        {
            Deactivate();
        }
    }

    public Boolean HasStateSprites()
    {
        return hasStateSprites;
    }
}
