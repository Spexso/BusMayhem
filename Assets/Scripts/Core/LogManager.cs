using UnityEngine;

public static class LogManager
{
    public static void TimerLog(string message)
    {
        Debug.Log($"[Timer] {message}");
    }
}
