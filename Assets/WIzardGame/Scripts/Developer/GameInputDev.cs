using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameInputDev : MonoBehaviour
{
    [SerializeField] WizardGameManager gameManager;
    [SerializeField] private UnityEvent<Spell> castSpell;
    
    public void CastSpell(Spell spell)
    {
        castSpell.Invoke(spell);
    }

    public void StartGame()
    {
        gameManager.StartGame();
    }
}
