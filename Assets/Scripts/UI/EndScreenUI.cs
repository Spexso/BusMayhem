using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Manages the end screen user interface for the game, displaying the outcome of the level and providing options for
/// the player to proceed to the next level or retry.
/// </summary>
public class EndScreenUI : MonoBehaviour
{
    // Fields
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI resultLongText;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private GameObject rewardPanel;
    [SerializeField] private GameObject panel;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip successSound;
    [SerializeField] private AudioClip failSound;
    [SerializeField] private TextMeshProUGUI coinsText;
    [SerializeField] private CoinFlyEffect gainCoinsEffect;

    private void Awake()
    {
        panel.gameObject.SetActive(false);
        nextLevelButton.gameObject.SetActive(false);
        retryButton.gameObject.SetActive(false);

        if (coinsText)
            coinsText.text = (PlayerPrefs.GetInt("Coins", 0) - 10).ToString();
    }

    // Methods
    private void Start()
    {
        GameResult result = LevelManager.GetLastGameResult();
        if (panel == null)
        {
            Debug.LogError("[EndScreenUI] panel reference is missing.");
            panel.gameObject.SetActive(false);
        }

        UIScalePanel scalePanel = panel.GetComponent<UIScalePanel>();
        if (scalePanel == null)
        {
            Debug.LogError("[EndScreenUI] panel reference is missing.");
            panel.gameObject.SetActive(false);
        }
        else
        {
            scalePanel.Show(() =>
            {
                if (result == GameResult.Win)
                {
                    resultText.text = "Success";
                    resultLongText.text = "Level cleared!";
                    nextLevelButton.gameObject.SetActive(true);
                    retryButton.gameObject.SetActive(false);
                    rewardPanel.gameObject.SetActive(true);

                    if (audioSource)
                        audioSource.PlayOneShot(successSound);

                    gainCoinsEffect.Play(() =>
                    {
                        if (coinsText)
                            coinsText.text = PlayerPrefs.GetInt("Coins", 0).ToString();
                    });
                }
                else if (result == GameResult.FailWaitingAreaFull)
                {
                    resultText.text = "Fail";
                    resultLongText.text = "No More Space!";
                    nextLevelButton.gameObject.SetActive(false);
                    retryButton.gameObject.SetActive(true);
                    rewardPanel.gameObject.SetActive(false);

                    if (audioSource)
                        audioSource.PlayOneShot(failSound);
                }
                else
                {
                    resultText.text = "Fail";
                    resultLongText.text = "Time Is Up!";
                    nextLevelButton.gameObject.SetActive(false);
                    retryButton.gameObject.SetActive(true);
                    rewardPanel.gameObject.SetActive(false);

                    if (audioSource)
                        audioSource.PlayOneShot(failSound);
                }

                nextLevelButton.onClick.AddListener(OnNextLevelClicked);
                retryButton.onClick.AddListener(OnRetryClicked);
            });
        }
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