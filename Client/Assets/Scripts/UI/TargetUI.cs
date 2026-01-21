using Assets.Scripts.Shared;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.UI
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
