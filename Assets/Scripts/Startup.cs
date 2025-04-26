using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Startup : MonoBehaviour
{

    public string SampleScene;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        /*if (System.Environment.GetCommandLineArgs().Any(arg => arg == "-port")) {
            Debug.Log("Starting game");
            SceneManager.LoadScene(SampleScene);
        }*/
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
