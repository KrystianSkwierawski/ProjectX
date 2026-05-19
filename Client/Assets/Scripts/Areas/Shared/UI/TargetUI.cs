using TMPro;
using UnityEngine;
using Assets.Scripts.Areas.Shared.Mono;

namespace Assets.Scripts.Areas.Shared.UI
{
    public class TargetUI : MonoSingleton<TargetUI>
    {
        #region GameObject

        public GameObject TargetCanvas { get; private set; }

        public GameObject Target { get; private set; }


        #endregion

        #region TextMesh

        public TextMeshProUGUI TargetNameText { get; private set; }

        public TextMeshProUGUI TargetHealthPointsText { get; private set; }

        #endregion

        public void Start()
        {
            TargetCanvas = GameObject.Find("TargetCanvas");

            Target = TargetCanvas.transform.Find("Target").gameObject;

            TargetNameText = TargetCanvas.transform.Find("Target/Name").GetComponent<TextMeshProUGUI>();
            TargetHealthPointsText = TargetCanvas.transform.Find("Target/HealthPoints").GetComponent<TextMeshProUGUI>();
        }

        public void SetTarget(string name, string health)
        {
            Target.SetActive(true);
            TargetNameText.text = name;
            TargetHealthPointsText.text = health;
        }
    }
}
