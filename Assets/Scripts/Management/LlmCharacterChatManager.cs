using LLMUnity;
using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LlmCharacterChatManager : MonoBehaviour
{
    [SerializeField] LLMCharacter playerCharacter;
    [SerializeField] LLMCharacter targetCharacter;
    [SerializeField] SpeechBubble bubble;
    [SerializeField] LlmLoadInfo llmLoadInfo;

    private readonly string _playerPromptTemplate = "Respond like a Human! Form a {0} sentence about {1} in a {2} manner.";
    private readonly string _targetPromptTemplate = "Respond exactly like a {2}! Generate a response to this in a {1} manner: {0}.";
    private string _lastPlayerResponse = "Hello!";
    private string _lastTargetResponse = "Hello!";

    public void VoicePlayer(string[] structs, string[] topics, string[] tones, Action completionCallback = null)
    {
        playerCharacter.ClearChat();
        var prompt = string.Format(_playerPromptTemplate, string.Join(", ", structs), string.Join(", ", topics), string.Join(", ", tones));
        _ = playerCharacter.Chat(prompt, (response) => { 
            bubble.SetText(response);
            _lastPlayerResponse = response;
        }, () => completionCallback());

        bubble.SetText("");
        bubble.SetPosition(-1.5f);
    }

    public void VoiceTarget(string[] tones, Action completionCallback = null)
    {
        targetCharacter.ClearChat();
        var prompt = string.Format(_targetPromptTemplate, _lastPlayerResponse, string.Join(", ", tones), TargetCharacterManager.Instance.CurrentCharacterName); //$"Respond as a {TargetCharacterManager.Instance.CurrentCharacterName}: {_lastPlayerResponse}";
        _ = targetCharacter.Chat(prompt, (response) => {
            bubble.SetText(response);
            _lastTargetResponse = response;
        }, () => completionCallback());

        bubble.SetText("");
        bubble.SetPosition(3.5f);
    }

    public void RandomizeSeeds()
    {
        playerCharacter.seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        targetCharacter.seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
    }

    public bool AreModelsReady()
    {
        return playerCharacter.llm.started && targetCharacter.llm.started;
    }

    private float _lastLlmFailMessageSent = 0f;

    private void Update()
    {
        if ( llmLoadInfo.InfoIsShown && AreModelsReady() )
        {
            llmLoadInfo.HidePanel();
        }

        if (playerCharacter.llm.failed && (Time.time - _lastLlmFailMessageSent) > 1.25f)
        {
            _lastLlmFailMessageSent = Time.time;
            SlidingText.Instance.ShowSlidingText("LLM Model Failed!");
        }
    }

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoad;

        WaitLlmSetup();
    }

    private void OnSceneLoad(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"New Scene Loaded: {scene.name}");

        if (scene.name == "EndingScene")
        {
            SceneManager.sceneLoaded -= OnSceneLoad;
            Destroy(gameObject);
        }
    }

    private async void WaitLlmSetup()
    {
        await LLM.WaitUntilModelSetup(llmLoadInfo.UpdateLoadProgress);

        llmLoadInfo.UpdateLoadProgress(1f);
    }
}


[Serializable]
public class LlmLoadInfo
{
    [SerializeField] GameObject LoadInfoPanel;
    [SerializeField] TextMeshPro ProgressText;

    public bool InfoIsShown => LoadInfoPanel.activeSelf;

    public void UpdateLoadProgress(float loadProgress)
    {
        ProgressText.SetText($"{loadProgress*100f:000.00}%");
    }

    public void HidePanel()
    {
        LoadInfoPanel.SetActive(false);
    }
}