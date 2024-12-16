using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum ButtonType
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
        if (state == State.Inactive)
        {
            Activate();
        }
        else if (state == State.Active)
        {
            Deactivate();
        }
    }

    public Boolean HasStateSprites()
    {
        return hasStateSprites;
    }
}
