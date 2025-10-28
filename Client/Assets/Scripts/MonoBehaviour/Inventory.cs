using UnityEngine;
using UnityEngine.InputSystem;

public class Inventory : MonoBehaviour
{
    private void Update()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            // TODO: wait?
            ToggleInventory();
        }
    }

    private static void ToggleInventory()
    {
        if (UIManager.Instance.Inventory.activeSelf)
        {
            AudioManager.Instance.PlayOneShot(AudioTypeEnum.InventoryClose, 0.5f);

            UIManager.Instance.Inventory.SetActive(false);
        }
        else
        {
            AudioManager.Instance.PlayOneShot(AudioTypeEnum.InventoryOpen, 0.5f);

            UIManager.Instance.Inventory.SetActive(true);
        }
    }
}
