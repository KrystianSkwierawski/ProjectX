using Unity.Netcode;
using UnityEngine;

public class Health : NetworkBehaviour
{
    public float _value = 100;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(!IsServer)
        {
            return;
        }

        if (_value <= 0)
        {
            Debug.Log("Object killed");

            // send to db

            // todo: spawn/despawn it later
            DestroyOnClientRpc();
            Destroy(gameObject);
        }
    }

    [ClientRpc]
    void DestroyOnClientRpc()
    {
        Destroy(gameObject);
    }

    public bool DealDamage(float damage)
    {
        _value -= damage;
        Debug.Log($"Object damaged. Damage: {damage}, CurrentValue: {_value}");

        return true;
    }
}
