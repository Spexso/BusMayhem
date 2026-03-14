using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class StartScreenUI : MonoBehaviour
{
    // Fields
    [SerializeField] private RawImage logoImage;
    [SerializeField] private GameObject clickButton;
    [SerializeField] private AudioSource menuMusic;
    private BusMayhemInputActions actions;
    private bool inputReceived;

    // Methods
    private void Awake()
    {
        actions = new BusMayhemInputActions();

        logoImage.gameObject.SetActive(false);
        clickButton.gameObject.SetActive(false);
    }

    private void Start()
    {
        if (menuMusic)
            menuMusic.Play();

        if (logoImage == null)
        {
            Debug.LogError("[StartScreenUI] Logo image reference is missing.");
            EnableInput();
            logoImage.gameObject.SetActive(true);
            clickButton.gameObject.SetActive(true);
            return;
        }

        UIScalePanel scalePanel = logoImage.GetComponent<UIScalePanel>();
        if (scalePanel == null)
        {
            Debug.LogError("[StartScreenUI] UIScalePanel component not found on logo GameObject.");
            logoImage.gameObject.SetActive(true);
            clickButton.gameObject.SetActive(true);
            EnableInput();
            return;
        }

        scalePanel.Show(() =>
        {
            if (clickButton == null)
            {
                Debug.LogError("[StartScreenUI] clickButton reference is missing.");
                EnableInput();
            }
            else
            {
                UIScalePanel scaleclickButton = clickButton.GetComponent<UIScalePanel>();
                if (scalePanel == null)
                {
                    Debug.LogError("[StartScreenUI] UIScalePanel component not found on logo GameObject.");
                    clickButton.gameObject.SetActive(true);
                    EnableInput();
                }

                scaleclickButton.Show(() => EnableInput());
            }
            EnableInput();
        });

    }

    private void OnEnable()
    {
        actions.StartScene.Tap.performed += OnTap;
    }

    private void OnDisable()
    {
        actions.StartScene.Tap.performed -= OnTap;
        actions.StartScene.Disable();

        if (menuMusic)
            menuMusic.Stop();
    }

    private void EnableInput()
    {
        actions.StartScene.Enable();
    }

    private void OnTap(InputAction.CallbackContext context)
    {
        if (inputReceived)
            return;

        inputReceived = true;
        SceneLoader.LoadGameplayScene();
    }
}