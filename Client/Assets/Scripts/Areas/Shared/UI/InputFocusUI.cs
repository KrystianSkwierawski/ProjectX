using TMPro;
using UnityEngine.EventSystems;

namespace Assets.Scripts.Areas.Shared.UI
{
    public static class InputFocusUI
    {
        public static bool IsAnyInputFocused
        {
            get
            {
                var selectedObject = EventSystem.current?.currentSelectedGameObject;

                return selectedObject != null && selectedObject.GetComponentInParent<TMP_InputField>()?.isFocused == true;
            }
        }
    }
}
