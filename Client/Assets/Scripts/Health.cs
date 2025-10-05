using TMPro;
using Unity.Netcode;
using UnityEngine;

public class Health : NetworkBehaviour
{
    public float Value { get; private set; } = 100;

    private GameObject _targetCanvas;

    private void Start()
    {
        if (IsClient)
        {
            _targetCanvas = GameObject.Find("TargetCanvas");
        }
    }
    public bool DealDamage(float damage)
    {
        Value -= damage;
        Debug.Log($"Object damaged. Damage: {damage}, CurrentValue: {Value}");


        if (Value <= 0)
        {
            Debug.Log("Object killed");

            // send to db

            HideTargetCanvasClientRpc();

            gameObject.GetComponent<NetworkObject>().Despawn();

            return true;
        }

        UpdateTargetCanvasClientRpc(Value);

        return true;
    }

    [ClientRpc]
    private void HideTargetCanvasClientRpc()
    {
        _targetCanvas.transform.Find("Target").gameObject.SetActive(false);
    }

    [ClientRpc]
    private void UpdateTargetCanvasClientRpc(float value)
    {
        Debug.Log("Updating health UI");

        Value = value;
        _targetCanvas.transform.Find("Target/HealthPoints").GetComponent<TextMeshProUGUI>().text = value.ToString();
    }
}
