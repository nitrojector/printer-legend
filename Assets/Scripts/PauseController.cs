using Printer;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseController : MonoBehaviour
{
    public GameObject[] gameObjectsArray;
    [SerializeField] public GameObject pauseMenuUI;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pauseMenuUI.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {

    }
    
    public void Pause()
    {
        pauseMenuUI.SetActive(true);
        PlayerManager.SetPaused(true);
        Time.timeScale = 0f;
        Debug.Log("Game Paused");
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        PlayerManager.SetPaused(false);
        Time.timeScale = 1f;
        Debug.Log("Game Resumed");
    }

    public void Exit()
    {
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        Debug.Log("Exiting to Main Menu");
    }

   
    
    
}
