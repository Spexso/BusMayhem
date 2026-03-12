using UnityEngine;
using UnityEngine.InputSystem;

public class StartScreenUI : MonoBehaviour
{
    // Fields
    private BusMayhemInputActions actions;
    private bool inputReceived;

    // Methods
    private void Awake()
    {
        actions = new BusMayhemInputActions();
    }

    private void OnEnable()
    {
        actions.StartScene.Tap.performed += OnTap;
        actions.StartScene.Enable();
    }

    private void OnDisable()
    {
        actions.StartScene.Tap.performed -= OnTap;
        actions.StartScene.Disable();
    }

    private void OnTap(InputAction.CallbackContext context)
    {
        if (inputReceived)
            return;

        inputReceived = true;
        SceneLoader.LoadGameplayScene();
    }
}