using UnityEngine;
using UnityEngine.SceneManagement;

public class BGMController : MonoBehaviour
{
    public string[] stopInScenes; // list of scene names where BGM should stop

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (string stopScene in stopInScenes)
        {
            if (scene.name == stopScene)
            {
                GetComponent<AudioSource>().Stop();
                Destroy(gameObject); // fully remove BGM so it won't return
                break;
            }
        }
    }
}
