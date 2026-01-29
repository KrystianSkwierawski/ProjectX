using Assets.Scripts.Shared;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.UI
{
    public class TranslateUI : MonoBehaviour
    {
        [SerializeField]
        private string _key; // TODO: enum?

        public void Start()
        {
            if (string.IsNullOrWhiteSpace(_key))
            {
                return;
            }

            gameObject.GetComponent<TextMeshProUGUI>().text = TranslateManager.Instance.GetByKey(_key);
        }
    }
}
