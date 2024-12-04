using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Enemy : MonoBehaviour
{
    private WizardGameManager _gameManager;
    private Transform _targetTransform;
    private bool _isDead = false;
    [SerializeField] private float _speed;
    [SerializeField] private float _bobSpeed;
    [SerializeField] private float _bobDistance;

    [SerializeField] private GameObject deathEffectPrefab;
    [SerializeField] private List<GameObject> spellPrefabs;
    public List<Spell> spells;
    private List<GameObject> spellIcons = new List<GameObject>();

    public void Setup(WizardGameManager gameManager, Transform target)
    {
        _gameManager = gameManager;
        _targetTransform = target;

        // spawn position
        transform.localPosition = UnityEngine.Random.insideUnitSphere * 3;
        transform.localPosition = new Vector3(transform.localPosition.x, Mathf.Abs(transform.localPosition.y)+1, -Mathf.Abs(transform.localPosition.z));

        // generate spell
        int numSpellTypes = Enum.GetNames(typeof(Spell)).Length;
        int numSpells = UnityEngine.Random.Range(1, 4);
        for (int i = 0; i < numSpells; i++) spells.Add((Spell)UnityEngine.Random.Range(0, numSpellTypes));
        regenerateSpellIcons();
    }

    void Update()
    {
        // update enemy position and rotation
        transform.LookAt(_targetTransform);
        transform.Translate(Vector3.forward * Time.deltaTime * _speed);
        transform.Translate(Vector3.up * Mathf.Sin(Time.time * _bobSpeed) * _bobDistance, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!_isDead && other.gameObject.tag == "Player") _gameManager.EndGame();
    }

    public void ReceiveSpell(Spell spell)
    {
        if (spells[0] == spell)
        {
            spells.RemoveAt(0);
            regenerateSpellIcons();
        }

        // death
        if (spells.Count == 0)
        {
            _isDead = true;
            Instantiate(deathEffectPrefab, this.transform.position, Quaternion.identity, this.transform);
            this.transform.localScale = Vector3.zero;
            Destroy(this.gameObject, 2);
        }
    }

    private void regenerateSpellIcons()
    {
        // destroy old icons
        foreach (GameObject icon in spellIcons) Destroy(icon);
        spellIcons.Clear();

        // generate new icons
        for(int i = 0; i < spells.Count; i++)
        {
            GameObject spellObj = Instantiate(spellPrefabs[(int)spells[i]], this.transform);
            spellIcons.Add(spellObj);

            float x = -(i - (spells.Count-1)/2f) * 0.5f;
            spellObj.transform.localPosition = new Vector3(x, 0.9f, 0.3f);
        }
    }
}
