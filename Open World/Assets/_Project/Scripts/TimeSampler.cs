using TMPro;
using UnityEngine;

public class TimeSampler : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timeText;

    private float startTime;
    private float time;
    private float finishTime;


    public void StartTimer()
    {
        startTime = Time.realtimeSinceStartup;
    }

    public void StopTimer()
    {
        finishTime = Time.realtimeSinceStartup;
        time = finishTime - startTime;
        timeText.text = time.ToString();
    }
}
