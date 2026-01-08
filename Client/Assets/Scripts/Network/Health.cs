using Assets.Scripts.Mono;
using Assets.Scripts.Shared;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Network
{
    public class Health : NetworkBehaviour
    {
        public float Value { get; private set; } = 100;

        public void DealDamage(float damage, string token, ulong clientId)
        {
            Value -= damage;
            Debug.Log($"Object damaged. Damage: {damage}, CurrentValue: {Value}");

            if (Value <= 0)
            {
                Debug.Log("Object killed");

                HideTargetCanvasClientRpc();

                CombatManager.Instance.OnKillEvent.Invoke(new KillEventModel
                {
                    ClientId = clientId,
                    ClientToken = token,
                    GameObject = gameObject
                });

                return;
            }

            UpdateTargetCanvasClientRpc(Value);

            return;
        }

        [ClientRpc]
        private void HideTargetCanvasClientRpc()
        {
            UIManager.Instance.Target.SetActive(false);
        }

        [ClientRpc]
        private void UpdateTargetCanvasClientRpc(float value)
        {
            Debug.Log("Updating target UI");

            Value = value;
            UIManager.Instance.TargetHealthPointsText.text = Value.ToString();
        }

        public override void OnNetworkDespawn()
        {
            Value = 100;
            base.OnNetworkDespawn();
        }
    }
}