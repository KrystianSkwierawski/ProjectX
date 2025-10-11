using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    void Start()
    {
#if UNITY_EDITOR
        SceneManager.LoadScene("ClientScene", LoadSceneMode.Additive);

        Debug.Log("ClientScene Loaded");

        SceneManager.LoadScene("EnvironmentScene", LoadSceneMode.Additive);

        Debug.Log("EnvironmentScene Loaded");
#elif UNITY_SERVER && !UNITY_EDITOR
        SceneManager.LoadScene("ServerScene", LoadSceneMode.Additive);

        Debug.Log("ServerScene Loaded");
#endif
    }
}
