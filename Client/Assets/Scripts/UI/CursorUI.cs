using System.Runtime.CompilerServices;
using Assets.Scripts.Shared;
using UnityEngine;

namespace Assets.Scripts.UI
{
    public class CursorUI : MonoSingleton<CursorUI>
    {
        private Texture2D _cursorPointer;
        private string _caller;

        public void Start()
        {
            _cursorPointer = Resources.Load<Texture2D>($"Icons/CursorPointer");
        }

        public void ShowPointer([CallerFilePath] string caller = "")
        {
            if (_caller == null)
            {
                _caller = caller;
                Cursor.SetCursor(_cursorPointer, Vector2.zero, CursorMode.Auto);
            }
        }

        public void ShowDefault([CallerFilePath] string caller = "")
        {
            if (_caller == caller)
            {
                _caller = null;
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
        }
    }
}
