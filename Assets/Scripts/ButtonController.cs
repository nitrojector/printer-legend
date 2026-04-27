using Config;
using Printer;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonController : MonoBehaviour
{
    [SerializeField] private GameObject nextButton;

    private void Start()
    {
        if (nextButton != null)
        {
            bool hasNext = LevelManager.CurrentLevelIndex + 1 < LevelSequenceConfig.Instance.Levels.Count;
            nextButton.SetActive(hasNext);
        }
    }

    public void NextPrint()
    {
        LevelManager.AdvanceLevelIndex();
        SceneManager.LoadScene("Printing", LoadSceneMode.Single);
    }

    public void Gallery()
    {
        // Gallery saving not yet implemented
    }

    public void Exit()
    {
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }
}
