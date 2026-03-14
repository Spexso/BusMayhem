using UnityEngine;
using System.Collections;
using TMPro;

public class HouseController : MonoBehaviour
{
    // Fields
    [SerializeField] private AudioSource soundPlayer;
    [SerializeField] private AudioClip noFreeTileSound;
    [SerializeField] private AudioClip popStickmanSound;
    [SerializeField] private SpriteRenderer noFreeTileIcon;
    [SerializeField] private TextMeshPro remainingStickmanCount;


    private float iconScaleUpDuration = 0.15f;
    private float iconHoldDuration = 0.5f;
    private float iconScaleDownDuration = 0.15f;

    private int gridX;
    private int gridY;
    private StickmanColor[] stickmanQueue;
    private int queueIndex;

    private static readonly int[] neighbourDirX = { 0, 1, 0, -1 };
    private static readonly int[] neighbourDirY = { -1, 0, 1, 0 };

    public bool IsEmpty => queueIndex >= stickmanQueue.Length;

    // Methods
    public void Initialize(int x, int y, StickmanColor[] queue)
    {
        gridX = x;
        gridY = y;
        stickmanQueue = queue;
        queueIndex = 0;

        if (remainingStickmanCount)
            remainingStickmanCount.text = stickmanQueue.Length.ToString();
    }

    public void PlayStickmanPopSound()
    {
        soundPlayer.PlayOneShot(popStickmanSound);
    }

    public void PlayNoFreeTileSound()
    {
        soundPlayer.PlayOneShot(noFreeTileSound);
    }

    public void OnTapped()
    {
        if (IsEmpty)
            return;

        Vector2Int? freeNeighbour = FindFreeNeighbour();

        if (freeNeighbour == null)
        {
            OnAllNeighboursBlocked();
            return;
        }

        StickmanColor colorToSpawn = stickmanQueue[queueIndex];
        queueIndex++;

        GridManager.Instance?.SpawnStickmanAtCell(freeNeighbour.Value.x, freeNeighbour.Value.y, colorToSpawn);
        
        PlayStickmanPopSound();
        if (remainingStickmanCount != null)
            remainingStickmanCount.text = (stickmanQueue.Length - queueIndex).ToString();

        if (IsEmpty)
        {
            GridCell myCell = GridManager.Instance != null ? GridManager.Instance?.GetCell(gridX, gridY) : null;

            if (myCell != null)
                myCell.ClearHouse();

            Destroy(gameObject);
        }
    }

    private Vector2Int? FindFreeNeighbour()
    {
        for (int i = 0; i < 4; i++)
        {
            int nx = gridX + neighbourDirX[i];
            int ny = gridY + neighbourDirY[i];

            GridCell cell = GridManager.Instance?.GetCell(nx, ny);

            if (cell == null)
                continue;

            if (!cell.IsOccupied)
                return new Vector2Int(nx, ny);
        }

        return null;
    }

    private IEnumerator NoFreeTileIconCoroutine()
    {
        Vector3 fullScale = Vector3.one * 2.0f;

        noFreeTileIcon.gameObject.SetActive(true);
        noFreeTileIcon.transform.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < iconScaleUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / iconScaleUpDuration);
            noFreeTileIcon.transform.localScale = Vector3.Lerp(Vector3.zero, fullScale, t);
            yield return null;
        }

        noFreeTileIcon.transform.localScale = fullScale;
        yield return new WaitForSeconds(iconHoldDuration);

        elapsed = 0f;
        while (elapsed < iconScaleDownDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / iconScaleDownDuration);
            noFreeTileIcon.transform.localScale = Vector3.Lerp(fullScale, Vector3.zero, t);
            yield return null;
        }

        noFreeTileIcon.transform.localScale = Vector3.zero;
        noFreeTileIcon.gameObject.SetActive(false);
    }

    private void OnAllNeighboursBlocked()
    {
        // Handle blocked feedback here
        StartCoroutine(NoFreeTileIconCoroutine());
        PlayNoFreeTileSound();

        Debug.Log("All neighbours are blocked for this house!");
    }
}