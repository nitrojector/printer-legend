using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PrinterEventManager : MonoBehaviour
{
    void Update()
    {
        // Restart scene when R is pressed
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // Quit game when Escape is pressed
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Application.Quit();
        }
    }
}