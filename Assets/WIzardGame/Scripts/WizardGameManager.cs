using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum Spell
{
    Victory,
    Palm,
    Call
}

public class WizardGameManager : MonoBehaviour
{
    private bool _inGame = false;
    private int _score;
    [SerializeField] private TextMeshPro gameText;
    [SerializeField] private Transform player;

    // ENEMY MANAGEMENT
    List<Enemy> _enemies = new List<Enemy>();
    [SerializeField] private Transform enemyConatiner;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float enemySpawnTime = 5f;
    private float enemySpawnCounter;

    private void Start()
    {
        if (player.tag != "Player") Debug.LogError("Player object needs to have 'Player' tag");
    }

    void Update()
    {
        if (_inGame)
        {
            // enemy spawning
            enemySpawnCounter += Time.deltaTime;
            if (enemySpawnCounter > enemySpawnTime)
            {
                enemySpawnCounter = 0;

                Enemy enemy = Instantiate(enemyPrefab, enemyConatiner).GetComponent<Enemy>();
                _enemies.Add(enemy);
                enemy.Setup(this, player);
            }
        } 
    }

    public void CastSpell(Spell spell)
    {
        foreach (Enemy enemy in _enemies) enemy.ReceiveSpell(spell);

        // kill enemies
        for(int i = _enemies.Count-1; i >= 0; i--)
        {
            if (_enemies[i].spells.Count == 0)
            {
                Destroy(_enemies[i].gameObject);
                _enemies.RemoveAt(i);
                _score++;
            }
        }
    }

    public void StartGame()
    {
        Debug.Log("Game Started!");
        _score = 0;
        gameText.text = "Game Started";
        _inGame = true;
    }

    public void EndGame()
    {
        Debug.Log("Game Ended! Score: " + _score);
        gameText.text = "Game Ended";

        // clean up enemies
        foreach (Enemy enemy in _enemies) Destroy(enemy.gameObject);
        _enemies.Clear();

        _inGame = false;
    }
}
