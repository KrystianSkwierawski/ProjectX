using System;
using System.Collections.Generic;
using Assets.Scripts.Enums;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Assets.Scripts.Shared
{
    public class TranslateManager : Singleton<TranslateManager>
    {
        private IDictionary<TranslateKeyEnum, string> _cache = new Dictionary<TranslateKeyEnum, string>();

        private JObject _object;

        public TranslateManager()
        {
            var asset = Resources.Load<TextAsset>($"i18n/{UserManager.Instance.Language}");

            var json = asset.ToString();

            _object = JObject.Parse(json);
        }

        public string GetByKey(string key)
        {
            if (Enum.TryParse<TranslateKeyEnum>(key, out var enumKey))
            {
                return GetByKey(enumKey);
            }

            return string.Empty;
        }

        public string GetByKey(TranslateKeyEnum key)
        {
            if (_cache.TryGetValue(key, out var result))
            {
                return result;
            }

            var language = UserManager.Instance.Language;

            var keyString = key.ToString();

            var token = _object.SelectToken(keyString);

            if (token == null)
            {
                Debug.LogWarning($"Not found translate. Key: {keyString}, Value: {token}, Language: {language}");

                return string.Empty;
            }

            Debug.Log($"Found translate. Key: {keyString}, Value: {token}, Language: {language}");

            result = token.ToString();

            _cache.Add(key, result);

            return result;
        }
    }
}
