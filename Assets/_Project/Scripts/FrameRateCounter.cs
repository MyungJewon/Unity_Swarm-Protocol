using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FrameRateCounter : MonoBehaviour
{
    [Header("UI Reference")]
    public TMP_Text fpsText;

    float deltaTime = 0.0f;

    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

        if (fpsText != null)
        {
            float msec = deltaTime * 1000.0f;
            float fps = 1.0f / deltaTime;
            
            fpsText.text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);

            if (fps < 30) fpsText.color = Color.red;
            else if (fps < 55) fpsText.color = Color.yellow;
            else fpsText.color = Color.green;
        }
    }
}