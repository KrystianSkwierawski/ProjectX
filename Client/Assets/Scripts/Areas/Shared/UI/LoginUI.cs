using System;
using System.Text.RegularExpressions;
using Assets.Scripts.Areas.Shared.Mono;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Areas.Shared.UI
{
    public sealed class LoginUI : MonoSingleton<LoginUI>
    {
        private const int _maxCredentialLength = 256;
        private const int _minPasswordLength = 6;
        private const float _spinnerSpeed = 180f;
        private const string _eyeIcon = "\uf06e";
        private const string _eyeSlashIcon = "\uf070";

        private static readonly Regex _emailRegex = new Regex(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        #region Temporary development email support

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static readonly Regex _developmentEmailRegex = new Regex(
            @"^[^@\s]+@localhost$",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
#endif

        #endregion

        private GameObject _formView;
        private GameObject _loadingView;
        private RectTransform _spinner;
        private TMP_InputField _emailInput;
        private TMP_InputField _passwordInput;
        private Image _emailBorder;
        private Image _passwordBorder;
        private Button _loginButton;
        private Button _passwordVisibilityButton;
        private TMP_Text _passwordVisibilityIcon;
        private TMP_Text _emailError;
        private TMP_Text _passwordError;
        private TMP_Text _generalError;

        private bool _isLoading;
        private bool _isPasswordVisible;
        private bool _isEmailFocused;
        private bool _isPasswordFocused;

        public event Action<string, string> LoginRequested;

        protected override bool PersistBetweenScenes => false;

        protected override void Awake()
        {
            base.Awake();

            CacheViewReferences();
            BindEvents();
            ConfigureView();
        }

        private void Update()
        {
            if (_isLoading)
            {
                _spinner.Rotate(0f, 0f, -_spinnerSpeed * Time.unscaledDeltaTime);
            }
        }

        private void CacheViewReferences()
        {
            var card = GameObject.Find("LoginCard").transform.Find("Card");
            var formView = card.Find("FormView");
            var emailBorder = formView.Find("EmailInputBorder");
            var passwordBorder = formView.Find("PasswordInputBorder");
            var passwordInput = passwordBorder.Find("PasswordInput");
            var passwordVisibility = passwordInput.Find("PasswordVisibility");
            var loadingView = card.Find("LoadingView");

            _formView = formView.gameObject;
            _loadingView = loadingView.gameObject;
            _spinner = loadingView.Find("Spinner").GetComponent<RectTransform>();
            _emailInput = emailBorder.Find("EmailInput").GetComponent<TMP_InputField>();
            _passwordInput = passwordInput.GetComponent<TMP_InputField>();
            _emailBorder = emailBorder.GetComponent<Image>();
            _passwordBorder = passwordBorder.GetComponent<Image>();
            _loginButton = formView.Find("LoginButton").GetComponent<Button>();
            _passwordVisibilityButton = passwordVisibility.GetComponent<Button>();
            _passwordVisibilityIcon = passwordVisibility.Find("Icon").GetComponent<TMP_Text>();
            _emailError = formView.Find("EmailError").GetComponent<TMP_Text>();
            _passwordError = formView.Find("PasswordError").GetComponent<TMP_Text>();
            _generalError = formView.Find("GeneralError").GetComponent<TMP_Text>();
        }

        private void BindEvents()
        {
            _loginButton.onClick.AddListener(Submit);
            _passwordVisibilityButton.onClick.AddListener(TogglePasswordVisibility);
            _emailInput.onValueChanged.AddListener(HandleEmailChanged);
            _passwordInput.onValueChanged.AddListener(HandlePasswordChanged);
            _emailInput.onSelect.AddListener(HandleEmailSelected);
            _emailInput.onDeselect.AddListener(HandleEmailDeselected);
            _passwordInput.onSelect.AddListener(HandlePasswordSelected);
            _passwordInput.onDeselect.AddListener(HandlePasswordDeselected);
            _emailInput.onSubmit.AddListener(HandleEmailSubmitted);
            _passwordInput.onSubmit.AddListener(HandlePasswordSubmitted);
        }

        private void ConfigureView()
        {
            _emailInput.characterLimit = _maxCredentialLength;
            _passwordInput.characterLimit = _maxCredentialLength;
            _passwordInput.contentType = TMP_InputField.ContentType.Password;
            _passwordVisibilityIcon.text = _eyeIcon;
            _passwordInput.ForceLabelUpdate();

            ClearErrors();
            SetLoading(false);

            _emailInput.Select();
        }

        public void PrefillDevelopmentCredentials(string email, string password)
        {
            _emailInput.SetTextWithoutNotify(email);
            _passwordInput.SetTextWithoutNotify(password);
            _emailInput.ForceLabelUpdate();
            _passwordInput.ForceLabelUpdate();
        }

        public void SetLoading(bool isLoading)
        {
            _isLoading = isLoading;
            _formView.SetActive(!isLoading);
            _loadingView.SetActive(isLoading);
            _loginButton.interactable = !isLoading;

            if (!isLoading)
            {
                _spinner.localRotation = Quaternion.identity;
            }
        }

        public void ShowRequestError(string message)
        {
            SetLoading(false);
            SetGeneralError(message);
        }

        private void Submit()
        {
            if (_isLoading || !TryGetValidatedCredentials(out var email, out var password))
            {
                return;
            }

            LoginRequested?.Invoke(email, password);
        }

        private bool TryGetValidatedCredentials(out string email, out string password)
        {
            email = _emailInput.text.Trim();
            password = _passwordInput.text;
            _emailInput.SetTextWithoutNotify(email);

            ClearErrors();

            var isEmailValid = ValidateEmail(email);
            var isPasswordValid = ValidatePassword(password);
            return isEmailValid && isPasswordValid;
        }

        private bool ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                SetFieldError(_emailError, _emailBorder, _isEmailFocused, "Email is required.");
                return false;
            }

            if (email.Length > _maxCredentialLength)
            {
                SetFieldError(_emailError, _emailBorder, _isEmailFocused, "Email must be 256 characters or fewer.");
                return false;
            }

            if (!IsEmailValid(email))
            {
                SetFieldError(_emailError, _emailBorder, _isEmailFocused, "Enter a valid email address.");
                return false;
            }

            return true;
        }

        private bool ValidatePassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                SetFieldError(_passwordError, _passwordBorder, _isPasswordFocused, "Password is required.");
                return false;
            }

            if (password.Length < _minPasswordLength)
            {
                SetFieldError(_passwordError, _passwordBorder, _isPasswordFocused, "Password must contain at least 6 characters.");
                return false;
            }

            if (password.Length > _maxCredentialLength)
            {
                SetFieldError(_passwordError, _passwordBorder, _isPasswordFocused, "Password must be 256 characters or fewer.");
                return false;
            }

            return true;
        }

        private static bool IsEmailValid(string email)
        {
            if (_emailRegex.IsMatch(email))
            {
                return true;
            }

            #region Temporary development email support

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return _developmentEmailRegex.IsMatch(email);
#else
            return false;
#endif

            #endregion
        }

        private void TogglePasswordVisibility()
        {
            _isPasswordVisible = !_isPasswordVisible;
            _passwordInput.contentType = _isPasswordVisible
                ? TMP_InputField.ContentType.Standard
                : TMP_InputField.ContentType.Password;
            _passwordVisibilityIcon.text = _isPasswordVisible ? _eyeSlashIcon : _eyeIcon;
            _passwordInput.ForceLabelUpdate();
        }

        private void ClearErrors()
        {
            SetFieldError(_emailError, _emailBorder, _isEmailFocused, string.Empty);
            SetFieldError(_passwordError, _passwordBorder, _isPasswordFocused, string.Empty);
            SetGeneralError(string.Empty);
        }

        private static void SetFieldError(TMP_Text errorText, Image border, bool isFocused, string message)
        {
            var hasError = !string.IsNullOrEmpty(message);
            errorText.text = message;
            errorText.gameObject.SetActive(hasError);
            border.color = hasError ? ColorUI.Error : isFocused ? ColorUI.Accent : ColorUI.Border;
        }

        private void SetGeneralError(string message)
        {
            var hasError = !string.IsNullOrEmpty(message);
            _generalError.text = message;
            _generalError.gameObject.SetActive(hasError);
        }

        private void HandleEmailChanged(string _)
        {
            SetFieldError(_emailError, _emailBorder, _isEmailFocused, string.Empty);
            SetGeneralError(string.Empty);
        }

        private void HandlePasswordChanged(string _)
        {
            SetFieldError(_passwordError, _passwordBorder, _isPasswordFocused, string.Empty);
            SetGeneralError(string.Empty);
        }

        private void HandleEmailSelected(string _)
        {
            _isEmailFocused = true;
            SetFieldError(_emailError, _emailBorder, true, _emailError.text);
        }

        private void HandleEmailDeselected(string _)
        {
            _isEmailFocused = false;
            SetFieldError(_emailError, _emailBorder, false, _emailError.text);
        }

        private void HandlePasswordSelected(string _)
        {
            _isPasswordFocused = true;
            SetFieldError(_passwordError, _passwordBorder, true, _passwordError.text);
        }

        private void HandlePasswordDeselected(string _)
        {
            _isPasswordFocused = false;
            SetFieldError(_passwordError, _passwordBorder, false, _passwordError.text);
        }

        private void HandleEmailSubmitted(string _)
        {
            _passwordInput.Select();
            _passwordInput.ActivateInputField();
        }

        private void HandlePasswordSubmitted(string _)
        {
            Submit();
        }

        protected override void OnDestroy()
        {
            _loginButton.onClick.RemoveListener(Submit);
            _passwordVisibilityButton.onClick.RemoveListener(TogglePasswordVisibility);
            _emailInput.onValueChanged.RemoveListener(HandleEmailChanged);
            _passwordInput.onValueChanged.RemoveListener(HandlePasswordChanged);
            _emailInput.onSelect.RemoveListener(HandleEmailSelected);
            _emailInput.onDeselect.RemoveListener(HandleEmailDeselected);
            _passwordInput.onSelect.RemoveListener(HandlePasswordSelected);
            _passwordInput.onDeselect.RemoveListener(HandlePasswordDeselected);
            _emailInput.onSubmit.RemoveListener(HandleEmailSubmitted);
            _passwordInput.onSubmit.RemoveListener(HandlePasswordSubmitted);

            base.OnDestroy();
        }
    }
}
