using TMPro;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI fpsText;
    [SerializeField] private float updateInterval = 0.25f;

    private float timer;
    private int frames;

    private void Update()
    {
        frames++;
        timer += Time.unscaledDeltaTime;

        if (timer >= updateInterval)
        {
            float fps = frames / timer;
            float ms = (timer / frames) * 1000f;

            fpsText.text = $"FPS: {fps:F0}\nFrame: {ms:F2} ms";

            frames = 0;
            timer = 0f;
        }
    }
}