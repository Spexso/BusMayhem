using UnityEngine.SceneManagement;

public static class SceneLoader
{
    private const string StartScene = "StartScene";
    private const string GameplayScene = "GameplayScene";
    private const string EndScene = "EndScene";

    public static void LoadStartScene()
    {
        SceneManager.LoadScene(StartScene);
    }

    public static void LoadGameplayScene()
    {
        SceneManager.LoadScene(GameplayScene);
    }

    public static void LoadEndScene()
    {
        SceneManager.LoadScene(EndScene);
    }
}