using UnityEngine;
using UnityEngine.InputSystem;

public class Dead : MonoBehaviour
{
    void Update()
    {
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            Debug.Log("Player is dead");
            var canvs = GameObject.Find("DeadCanvas");
            var image = canvs.transform.Find("Image").gameObject;
            image.SetActive(!image.activeSelf);
        }
    }
}
