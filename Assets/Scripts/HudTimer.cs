using UnityEngine;
using TMPro;

public class HUDTimer : MonoBehaviour
{
    public TMP_Text timerText;        // the TextMeshPro text that shows the time
    public bool startRunning = true;  // start counting as soon as the scene loads

    private float time;               // total seconds that have passed
    private bool running;

    void Start()
    {
        running = startRunning;
        ShowTime();
    }

    void Update()
    {
        // Time.deltaTime is 0 while the game is frozen (Time.timeScale = 0),
        // so the timer automatically waits during your "HILL ACT 1" title card.
        if (running)
        {
            time += Time.deltaTime;
            ShowTime();
        }
    }

    void ShowTime()
    {
        // Classic Sonic stops at 9:59.99
        float shown = Mathf.Min(time, 599.99f);

        int minutes = (int)(shown / 60f);
        int seconds = (int)(shown % 60f);
        int centiseconds = (int)((shown * 100f) % 100f);

        timerText.text = minutes + ":" + seconds.ToString("00") + "." + centiseconds.ToString("00");
    }

    // ---- You can call these from other scripts ----
    public void StopTimer()  { running = false; }
    public void StartTimer() { running = true; }

    public void ResetTimer()
    {
        time = 0f;
        ShowTime();
    }
}