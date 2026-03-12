using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class EndScreenUI : MonoBehaviour
{
    // Fields
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button retryButton;

    // Methods
    private void Start()
    {
        GameResult result = LevelManager.GetLastGameResult();

        if (result == GameResult.Win)
        {
            resultText.text = "Level Complete, success!";
            nextLevelButton.gameObject.SetActive(true);
            retryButton.gameObject.SetActive(false);
        }
        else if (result == GameResult.FailWaitingAreaFull)
        {
            resultText.text = "No More Space, failed";
            nextLevelButton.gameObject.SetActive(false);
            retryButton.gameObject.SetActive(true);
        }
        else
        {
            resultText.text = "Time Is Up!, failed";
            nextLevelButton.gameObject.SetActive(false);
            retryButton.gameObject.SetActive(true);
        }

        nextLevelButton.onClick.AddListener(OnNextLevelClicked);
        retryButton.onClick.AddListener(OnRetryClicked);
    }

    private void OnNextLevelClicked()
    {
        SceneLoader.LoadGameplayScene();
    }

    private void OnRetryClicked()
    {
        SceneLoader.LoadGameplayScene();
    }

    private void OnDestroy()
    {
        nextLevelButton.onClick.RemoveListener(OnNextLevelClicked);
        retryButton.onClick.RemoveListener(OnRetryClicked);
    }
}