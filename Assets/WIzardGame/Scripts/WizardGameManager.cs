using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum Spell
{
    Heart,
    Diamond
}

public class WizardGameManager : MonoBehaviour
{
    private bool _inGame = false;
    private int _score;
    private Transform player;
    [SerializeField] private TextMeshPro gameText;
    [SerializeField] private GameObject startGameButton;

    // ENEMY MANAGEMENT
    List<Enemy> _enemies = new List<Enemy>();
    [SerializeField] private Transform enemyConatiner;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float enemySpawnTime = 5f;
    private float enemySpawnCounter;

    [SerializeField] private Material offMaterial;
    [SerializeField] private Material onMaterial;
    [SerializeField] private GameObject heartBox;
    [SerializeField] private GameObject diamondBox;

    private void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
        //if (_player.tag != "Player") Debug.LogError("Player object needs to have 'Player' tag");
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

    public void CastHeartSpell()
    {
        CastSpell(Spell.Heart);
        heartBox.transform.Rotate(new Vector3(20, 0, 0));
    }

    public void CastDiamondSpell()
    {
        CastSpell(Spell.Diamond);
        diamondBox.transform.Rotate(new Vector3(20, 0, 0));
    }

    public void CastSpell(Spell spell)
    {
        foreach (Enemy enemy in _enemies) enemy.ReceiveSpell(spell);

        // remove dead enemies
        for(int i = _enemies.Count-1; i >= 0; i--)
        {
            if (_enemies[i].spells.Count == 0)
            {
                _enemies.RemoveAt(i);
                _score++;
            }
        }
    }

    public void StartGame()
    {
        if (_inGame) return;

        Debug.Log("Game Started!");
        _score = 0;
        gameText.text = "Game Started";
        _inGame = true;
        startGameButton.SetActive(false);

        Debug.Log(startGameButton.active);
        Debug.Log(startGameButton.activeInHierarchy);
        Debug.Log(startGameButton.activeSelf);
    }

    public void EndGame()
    {
        Debug.Log("Game Ended! Score: " + _score);
        gameText.text = "Game Ended";

        // clean up enemies
        foreach (Enemy enemy in _enemies) Destroy(enemy.gameObject);
        _enemies.Clear();

        _inGame = false;
        startGameButton.SetActive(true);
    }
}
