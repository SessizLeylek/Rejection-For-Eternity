using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class SpeechBubble : MonoBehaviour
{
    public bool IsVisible { get; private set; } = false;

    [SerializeField] SpriteRenderer _bubbleRenderer;
    [SerializeField] TextMeshPro _text;

    private Coroutine _showHideRoutine;
        
    public void SetText(string text)
    {
        _text.SetText(PreprocessText(text));
    }

    public void SetPosition(float height)
    {
        transform.position = new Vector3(0, height, 0);
    }

    public void Show(Action completeCallback = null)
    {
        if (_showHideRoutine != null)
        {
            StopCoroutine(_showHideRoutine);
            _showHideRoutine = null;
        }

        _showHideRoutine = StartCoroutine(ShowHideRoutine(true, completeCallback));

        IsVisible = true;
    }

    public void Hide(Action completeCallback = null)
    {
        _text.SetText("");

        if (_showHideRoutine != null)
        {
            StopCoroutine(_showHideRoutine);
            _showHideRoutine = null;
        }

        _showHideRoutine = StartCoroutine(ShowHideRoutine(false, completeCallback));
    }

    private string PreprocessText(string text)
    {
        // Removes non-ascii characters
        var buffer = new char[text.Length];
        int j = 0;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c >= 32 && c <= 126)
                buffer[j++] = c;
        }

        var ascii = new string(buffer, 0, j);
        return ascii;
    }

    private IEnumerator ShowHideRoutine(bool show, Action completeCallback)
    {
        var startTime = Time.time;
        var elapsedTime = 0f;
        while (elapsedTime < 1)
        {
            yield return null;
            elapsedTime = Time.time - startTime;

            _bubbleRenderer.color = new Color(1, 1, 1, EaseFunctions.EaseOut(show ? elapsedTime : 1 - elapsedTime));
        }

        completeCallback?.Invoke();

        if (!show)
        {
            IsVisible = false;
        }
    }
}
