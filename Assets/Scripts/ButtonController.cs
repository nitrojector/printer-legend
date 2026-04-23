using UnityEngine;
using UnityEngine.SceneManagement;


public class ButtonController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void PictureMode()
    {
        SceneManager.LoadScene("Printing", LoadSceneMode.Single);
        Debug.Log("Picture Mode");
    }

    public void Gallery()
    {
        SceneManager.LoadScene("Printing", LoadSceneMode.Single);
        Debug.Log("Gallery");
    }
    
     public void Exit()
    {
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        Debug.Log("Exiting to Main Menu");
    }
}
