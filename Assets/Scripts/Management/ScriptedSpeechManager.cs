using System;
using System.Collections;
using UnityEngine;

public class ScriptedSpeechManager : MonoBehaviour
{
    [SerializeField] SpeechBubble _speechBubble;
    [SerializeField] AudioSource _whistleSource;


    public void StartScript(ScriptedSpeech speech, Action onComplete = null)
    {
        _speechBubble.SetPosition(speech.BubbleHeight);
        _speechBubble.Show();
        StartCoroutine(DisplaySpeech(speech, onComplete));
    }

    public void Whistle()
    {
        _whistleSource.Play();
    }

    private IEnumerator DisplaySpeech(ScriptedSpeech speech, Action onComplete)
    {

        var speechOrder = 0;
        while (speechOrder < speech.Dialogs.Length)
        {
            var splittedText = speech.Dialogs[speechOrder].Split(" ");
            var waitCoupleSeconds = new WaitForSeconds(Mathf.Min(0.1f, 0.5f / splittedText.Length));
            string constructedString = string.Empty;

            for (int i = 0; i < splittedText.Length; i++)
            {
                constructedString += splittedText[i] + " ";
                _speechBubble.SetText(constructedString);

                yield return waitCoupleSeconds;
            }

            while(!Input.GetMouseButtonDown(0)) yield return null;
            speechOrder++;
        }

        _speechBubble.Hide();
        onComplete?.Invoke();
    }

    public struct ScriptedSpeech
    {
        public float BubbleHeight;
        public string[] Dialogs;

        public ScriptedSpeech(float height, params string[] dialogs)
        {
            BubbleHeight = height;
            Dialogs = dialogs;
        }
    }
}

