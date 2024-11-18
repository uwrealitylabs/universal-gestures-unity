using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    private WizardGameManager _gameManager;
    private Transform _targetTransform;
    [SerializeField] private float _speed;

    [SerializeField] public List<Spell> spells;
    [SerializeField] private List<GameObject> spellPrefabs;
    [SerializeField] private List<GameObject> spellIcons;


    public void Setup(WizardGameManager gameManager, Transform target)
    {
        _gameManager = gameManager;
        _targetTransform = target;

        // spawn position
        transform.position = Random.insideUnitSphere * 3;
        transform.position = new Vector3(transform.position.x, Mathf.Abs(transform.position.y)+1, -Mathf.Abs(transform.position.z));

        // generate spell
        int numSpells = Random.Range(1, 4);
        for (int i = 0; i < numSpells; i++) spells.Add((Spell)Random.Range(0, 3));
        regenerateSpellIcons();
    }

    void Update()
    {
        // update enemy position and rotation
        transform.LookAt(_targetTransform);
        transform.Translate(Vector3.forward * Time.deltaTime * _speed);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player") _gameManager.EndGame();
    }

    public void ReceiveSpell(Spell spell)
    {
        if (spells[0] == spell)
        {
            spells.RemoveAt(0);
            regenerateSpellIcons();
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
