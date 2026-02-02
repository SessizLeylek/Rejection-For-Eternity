using System.Collections;
using TMPro;
using UnityEngine;

public class SlidingText : MonoBehaviour
{
    public static SlidingText Instance => _instance;
    private static SlidingText _instance;

    [SerializeField] TextMeshPro _textMeshPro;

    private void Awake()
    {
        _instance = this;
    }

    public void ShowSlidingText(string text)
    {
        StopAllCoroutines();
        StartCoroutine(SlideText(text));
    }

    private IEnumerator SlideText(string text)
    {
        Vector3 startPos = new Vector3(0, -3, -8);
        Vector3 endPos = new Vector3(0, 3, -8);

        _textMeshPro.text = text;
        _textMeshPro.transform.position = startPos;

        float startTime = Time.time;
        float elapsedTime = 0;
        while (elapsedTime < 1)
        {
            _textMeshPro.transform.position = Vector3.Lerp(startPos, endPos, OutQuint(elapsedTime));
            
            yield return null;
            elapsedTime = Time.time - startTime;
        }

        _textMeshPro.transform.position = endPos;
        _textMeshPro.text = "";
    }

    private float OutQuint(float t)
    {
        return 1f - Mathf.Pow(1f - t, 5f);
    }
}
