using Printer;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseController : MonoBehaviour
{
    [SerializeField] public GameObject pauseMenuUI;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }
    
    public void Pause()
    {
        pauseMenuUI.SetActive(true);
        GameManager.Instance.SetPaused(true);
        Time.timeScale = 0f;
        Debug.Log("Game Paused");
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        GameManager.Instance.SetPaused(false);
        Time.timeScale = 1f;
        Debug.Log("Game Resumed");
    }

    public void Exit()
    {
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        Debug.Log("Exiting to Main Menu");
    }
}
