using TMPro;
using UnityEngine;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Mono;

namespace Assets.Scripts.Areas.Shared.UI
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
