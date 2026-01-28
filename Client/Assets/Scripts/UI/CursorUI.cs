using Assets.Scripts.Shared;
using UnityEngine;

namespace Assets.Scripts.UI
{
    public class CursorUI : MonoSingleton<CursorUI>
    {
        private Texture2D _cursorPointer;
        private object _invoker;

        public void Start()
        {
            _cursorPointer = Resources.Load<Texture2D>($"Textures/CursorPointer");
        }

        public void ShowPointer(object invoker)
        {
            if (_invoker == null)
            {
                _invoker = invoker;
                Cursor.SetCursor(_cursorPointer, Vector2.zero, CursorMode.Auto);
            }
        }

        public void ShowDefault(object invoker)
        {
            if (_invoker == invoker)
            {
                _invoker = null;
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
        }
    }
}
