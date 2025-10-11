using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrap : MonoBehaviour
{
    private void Start()
    {
#if UNITY_EDITOR
        SceneManager.LoadScene("MainScene", LoadSceneMode.Single);
        Debug.Log("MainScene Loaded");

        SceneManager.LoadScene("ClientScene", LoadSceneMode.Additive);
        Debug.Log("ClientScene Loaded");

        SceneManager.LoadScene("EnvironmentScene", LoadSceneMode.Additive);
        Debug.Log("EnvironmentScene Loaded");
#elif UNITY_SERVER && !UNITY_EDITOR
        SceneManager.LoadScene("MainScene", LoadSceneMode.Single);
        Debug.Log("MainScene Loaded");

        SceneManager.LoadScene("ServerScene", LoadSceneMode.Additive);
        Debug.Log("ServerScene Loaded");
#endif
    }
}
