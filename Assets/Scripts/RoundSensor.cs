using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class RoundSensor : MonoBehaviour
{
    [Header("Scene Settings")]
    public string printingSceneName = "PrintingScene";

    private InputAction spaceAction;

    void OnEnable()
    {
        // New Input System: bind Space key
        spaceAction = new InputAction("Start", InputActionType.Button, "<Keyboard>/space");
        spaceAction.Enable();

        // When Space is pressed, trigger scene load
        spaceAction.performed += OnSpacePressed;
    }

    void OnDisable()
    {
        if (spaceAction != null)
        {
            spaceAction.performed -= OnSpacePressed;
            spaceAction.Disable();
        }
    }

    private void OnSpacePressed(InputAction.CallbackContext ctx)
    {
        // Load the printing scene
        SceneManager.LoadScene(printingSceneName);
    }
}