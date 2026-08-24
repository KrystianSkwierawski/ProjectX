using System;
using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Areas.Shared.Mono;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.Areas.Shared.UI
{
    public sealed class LoadingScreenUI : MonoSingleton<LoadingScreenUI>
    {
        private readonly List<LoadingScope> _activeScopes = new List<LoadingScope>();
        private readonly StringBuilder _messageBuilder = new StringBuilder();

        private GameObject _view;
        private TMP_Text _message;

        protected override bool PersistBetweenScenes => false;

        protected override void Awake()
        {
            base.Awake();

            CacheViewReferences();

            RefreshView();
        }

        public IDisposable Show(string message)
        {
            return AcquireScope(message);
        }

        private void CacheViewReferences()
        {
            var view = transform.Find("View");

            _view = view.gameObject;
            _message = view.Find("Content/Message").GetComponent<TMP_Text>();
        }

        private IDisposable AcquireScope(string message)
        {
            var scope = new LoadingScope(this, message ?? string.Empty);

            _activeScopes.Add(scope);

            RefreshView();

            return scope;
        }

        private void ReleaseScope(LoadingScope scope)
        {
            if (_activeScopes.Remove(scope))
            {
                RefreshView();
            }
        }

        private void RefreshView()
        {
            var isVisible = _activeScopes.Count > 0;

            _view.SetActive(isVisible);

            if (!isVisible)
            {
                _message.text = string.Empty;
                return;
            }

            _messageBuilder.Clear();

            for (var i = 0; i < _activeScopes.Count; i++)
            {
                if (i > 0)
                {
                    _messageBuilder.AppendLine();
                }

                _messageBuilder.Append(_activeScopes[i].Message);
            }

            _message.text = _messageBuilder.ToString();
        }

        protected override void OnDestroy()
        {
            foreach (var scope in _activeScopes)
            {
                scope.Detach();
            }

            _activeScopes.Clear();

            base.OnDestroy();
        }

        private sealed class LoadingScope : IDisposable
        {
            private LoadingScreenUI _owner;

            public LoadingScope(LoadingScreenUI owner, string message)
            {
                _owner = owner;
                Message = message;
            }

            public string Message { get; }

            public void Dispose()
            {
                if (_owner == null)
                {
                    return;
                }

                var owner = _owner;

                _owner = null;

                owner.ReleaseScope(this);
            }

            public void Detach()
            {
                _owner = null;
            }
        }
    }
}
