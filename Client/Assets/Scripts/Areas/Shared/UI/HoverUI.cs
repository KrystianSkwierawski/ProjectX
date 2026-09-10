using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Assets.Scripts.Areas.Shared.Subscriptions;

namespace Assets.Scripts.Areas.Shared.UI
{
    public class HoverUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const float _previewMargin = 8f;

        private string _key;
        private RectTransform _preview;
        private Vector2 _previewDefaultAnchoredPosition;

        public void Start()
        {
            _key = gameObject.GetInstanceID().ToString();
            _preview = transform.Find("Preview") as RectTransform;

            if (_preview != null)
            {
                _previewDefaultAnchoredPosition = _preview.anchoredPosition;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (gameObject.TryGetComponent<Button>(out var button) && !button.interactable)
            {
                return;
            }

            CursorUI.Instance.ShowPointer();

            OnPointerEnterSubscription.Instance.Invoke(_key, new OnPointerEnterEvent());
            ClampPreviewToCanvas();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            CursorUI.Instance.ShowDefault();

            OnPointerExitSubscription.Instance.Invoke(_key, new OnPointerExitSubscriptionEvent());
        }

        public void OnDisable()
        {
            CursorUI.Instance?.ShowDefault();

            OnPointerExitSubscription.Instance.Invoke(_key, new OnPointerExitSubscriptionEvent());
        }

        private void ClampPreviewToCanvas()
        {
            if (_preview == null || !_preview.gameObject.activeInHierarchy)
            {
                return;
            }

            _preview.anchoredPosition = _previewDefaultAnchoredPosition;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_preview);
            Canvas.ForceUpdateCanvases();

            var rootCanvas = _preview.GetComponentInParent<Canvas>()?.rootCanvas;

            if (rootCanvas == null || rootCanvas.transform is not RectTransform canvasRect || _preview.parent is not RectTransform parentRect)
            {
                return;
            }

            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRect, _preview);
            var safeRect = canvasRect.rect;
            var correction = Vector2.zero;
            var minimumX = safeRect.xMin + _previewMargin;
            var maximumX = safeRect.xMax - _previewMargin;
            var minimumY = safeRect.yMin + _previewMargin;
            var maximumY = safeRect.yMax - _previewMargin;

            correction.x = CalculateCorrection(bounds.min.x, bounds.max.x, minimumX, maximumX);
            correction.y = CalculateCorrection(bounds.min.y, bounds.max.y, minimumY, maximumY);

            if (correction.sqrMagnitude <= 0f)
            {
                return;
            }

            var worldCorrection = canvasRect.TransformVector(new Vector3(correction.x, correction.y, 0f));
            var parentCorrection = parentRect.InverseTransformVector(worldCorrection);
            _preview.anchoredPosition += new Vector2(parentCorrection.x, parentCorrection.y);
        }

        private static float CalculateCorrection(float contentMinimum, float contentMaximum, float safeMinimum, float safeMaximum)
        {
            if (contentMaximum - contentMinimum > safeMaximum - safeMinimum)
            {
                return (safeMinimum + safeMaximum - contentMinimum - contentMaximum) * 0.5f;
            }

            if (contentMinimum < safeMinimum)
            {
                return safeMinimum - contentMinimum;
            }

            return contentMaximum > safeMaximum ? safeMaximum - contentMaximum : 0f;
        }
    }
}
