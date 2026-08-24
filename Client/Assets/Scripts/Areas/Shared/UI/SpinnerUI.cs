using UnityEngine;

namespace Assets.Scripts.Areas.Shared.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SpinnerUI : MonoBehaviour
    {
        private const float _rotationSpeed = 180f;

        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            transform.localRotation = Quaternion.identity;
        }

        private void Update()
        {
            _rectTransform.Rotate(0f, 0f, -_rotationSpeed * Time.unscaledDeltaTime);
        }
    }
}
