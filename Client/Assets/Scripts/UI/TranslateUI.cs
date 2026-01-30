using Assets.Scripts.Enums;
using Assets.Scripts.Shared;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.UI
{
    public class TranslateUI : MonoBehaviour
    {
        [SerializeField]
        private TranslateKeyEnum _key;

        public void Start()
        {
            gameObject.GetComponent<TextMeshProUGUI>().text = TranslateManager.Instance.GetByKey(_key);
        }
    }
}
