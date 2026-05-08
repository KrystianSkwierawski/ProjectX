using Assets.Scripts.Extensions;
using Assets.Scripts.UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class Merchant : NetworkBehaviour
{
    private const float _npcMaxDistance = 5f;
    private MerchantNpc _npc;

    private void Update()
    {
        if (IsOwner)
        {
            // TODO: cancel button
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                MerchantUI.Instance.Hide();
            }

            CheckNpcClicked();
        }
    }

    private void CheckNpcClicked()
    {
        var mouse = Mouse.current;

        var ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());

        var hover = Physics.Raycast(ray, out RaycastHit hit) && hit.transform.tag == "MerchantNpc";

        if (!hover)
        {
            CursorUI.Instance.ShowDefault();

            return;
        }

        if (hit.transform.IsFarToTarget(transform.gameObject, _npcMaxDistance))
        {
            CursorUI.Instance.ShowDefault();

            return;
        }

        _npc = hit.transform.GetComponent<MerchantNpc>();

        CursorUI.Instance.ShowPointer();

        if (mouse.rightButton.wasPressedThisFrame)
        {
            MerchantUI.Instance.Show(_npc.Offers);
        }
    }
}
