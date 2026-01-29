using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Assets.Scripts.Shared
{
    public class TranslateManager : Singleton<TranslateManager>
    {
        private IDictionary<string, string> _cache = new Dictionary<string, string>();

        private JObject _object;

        public TranslateManager()
        {
            var asset = Resources.Load<TextAsset>($"i18n/{UserManager.Instance.Language}");

            var json = asset.ToString();

            _object = JObject.Parse(json);
        }

        public string GetByKey(string key)
        {
            if (_cache.TryGetValue(key, out var result))
            {
                return result;
            }

            var language = UserManager.Instance.Language;

            var token = _object.SelectToken(key);

            if (token == null)
            {
                Debug.LogWarning($"Not found translate. Key: {key}, Value: {token}, Language: {language}");

                return string.Empty;
            }

            Debug.Log($"Found translate. Key: {key}, Value: {token}, Language: {language}");

            result = token.ToString();

            _cache.Add(key, result);

            return result;
        }
    }
}
