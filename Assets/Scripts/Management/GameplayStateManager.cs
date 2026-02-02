using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayStateManager : MonoBehaviour
{
    public static GameplayStateManager Instance { get; private set; }

    public enum GameplayState
    {
        PlayersTurn,
        GeneratedSpeech,
        ScriptedSpeech,
    }

    public GameplayState State { get; private set; } = GameplayState.ScriptedSpeech;
    public TargetEmotionalStatus TargetEmotionalStatus = new();

    public int TalkCount = 0;
    public bool NoCardsLeft = false;

    [SerializeField] SpeechBubble _speechBubble;
    [SerializeField] LlmCharacterChatManager _chatManager;
    [SerializeField] ScriptedSpeechManager _scriptedSpeechManager;
    [SerializeField] TimeTravelManager _timeTravelManager;
    [SerializeField] UiButton[] _gameplayButtons;
    [SerializeField] Transform _gameplayButtonsParent;

    public void RequestStateChange(GameplayState newState)
    {
        State = newState;
        switch (newState)
        {
            case GameplayState.PlayersTurn:

                if (NoCardsLeft)
                {
                    FinishGame();
                    break;
                }

                SetGameplayButtonsActive(true);
                break;

            case GameplayState.GeneratedSpeech:

                SetGameplayButtonsActive(false);
                break;

        }
    }

    public void PushTurnEndValues(string[] structs, string[] topics, string[] tones, bool preventRejection = false, int megaCharmCount = 0, bool forceLose = false)
    {
        RequestStateChange(GameplayState.GeneratedSpeech);
        _chatManager.RandomizeSeeds();

        TalkCount++;

        TargetEmotionalStatus.ListenAndUpdate(structs, topics, tones);
        while (megaCharmCount-- > 0) TargetEmotionalStatus.MegaCharm();

        _speechBubble.Show();
        _chatManager.VoicePlayer(structs, topics, tones, () =>
        {
            StartCoroutine(WaitNextInput( () => {

                var conversationStatus = TargetEmotionalStatus.CheckEndConditions(out var endSentences);

                bool shouldContinue = conversationStatus == TargetEmotionalStatus.ConversationStatus.Continues;

                if (!shouldContinue && preventRejection && conversationStatus != TargetEmotionalStatus.ConversationStatus.Proposed)
                {
                    shouldContinue = true;
                    SlidingText.Instance.ShowSlidingText("Rejection Prevented!");
                }

                if (shouldContinue && !forceLose)
                {
                    var targetMood = TargetEmotionalStatus.EmotionalStatus;

                    _chatManager.VoiceTarget(
                    targetMood.Length > 0 ? targetMood : new string[] { "normal" }, () =>
                    {
                        StartCoroutine(WaitNextInput(() =>
                        {
                            _speechBubble.Hide();
                            RequestStateChange(GameplayState.PlayersTurn);
                        }));
                    });
                }
                else
                {
                    Debug.Log($"failed {conversationStatus.ToString()}");

                    Action finalAction;
                    if (conversationStatus == TargetEmotionalStatus.ConversationStatus.Proposed)
                    {
                        Debug.Log("MARRIAGE PROPOSAL!");
                        finalAction = () => {

                            StartCoroutine(WaitNextInput(() =>
                            {
                                EndingScene.Ending = TargetCharacterManager.Instance.CurrentCharacterName switch
                                {
                                    "Modern" => EndingScene.EndingType.Modern,
                                    "Caveman" => EndingScene.EndingType.Caveman,
                                    "Viking" => EndingScene.EndingType.Viking,
                                    "Janissary" => EndingScene.EndingType.Janissary,
                                    "Cyborg" => EndingScene.EndingType.Cyborg,
                                    _ => throw new Exception("Invalid Character Name!"),
                                };

                                SceneManager.LoadScene("EndingScene");
                            }));
                        };
                    }
                    else
                    {
                        finalAction = () =>
                        {
                            _timeTravelManager.Travel();
                            TalkCount = 0;
                        };
                    }

                    var fallbackSentences = new string[] { "OH NO!", "SOMETHING UNEXPECTED OCCURED WITH FAIL SENTENCES! " };

                    _scriptedSpeechManager.StartScript(new ScriptedSpeechManager.ScriptedSpeech(3.5f, endSentences ?? fallbackSentences),
                            () => StartCoroutine(WaitNextInput(() =>
                            {
                                _speechBubble.Hide();
                                finalAction();
                            })));

                    TargetCharacterManager.Instance.StopHeartsEmitting();
                }
            } ));
        });
    }

    public void FinishGame(bool hideButtons = false)
    {
        if (hideButtons)
        {
            SetGameplayButtonsActive(false);
        }

        SlidingText.Instance.ShowSlidingText("You Have No Cards To Play!");
        StartCoroutine(WaitNextInput(_timeTravelManager.FinishGame));
    }

    private void Start()
    {
        TargetCharacterManager.Instance.ShowHideCharacter(false);
        StartCoroutine(WaitLlmLoad(() => _scriptedSpeechManager.StartScript(new ScriptedSpeechManager.ScriptedSpeech(0,
            "AHH!",
            "I have to find the most gorgeous man in this entire dump.",
            "I have to play the perfect cards to lure him."
            ), () =>
            {
                TargetCharacterManager.Instance.ShowHideCharacter(true);
                _scriptedSpeechManager.Whistle();
                _scriptedSpeechManager.StartScript(new ScriptedSpeechManager.ScriptedSpeech(0,
                    "GODDAMN IT!",
                    "He looks like he could close a multi-million dollar deal...",
                    "...and ruin my life stability in the same perfectly tailored suit.",
                    "I need that high-functioning, alpha-level structure in my personal life immediately."
                    ), () =>
                    {
                        RequestStateChange(GameplayState.PlayersTurn);
                    });
            })));
    }

    private void Awake()
    {
        Instance = this;
    }

    private void SetGameplayButtonsActive(bool active)
    {
        StartCoroutine(MoveGameplayButtonsTo(active));

        foreach (var button in _gameplayButtons)
        {
            button.Interactable = active;
        }
    }

    IEnumerator MoveGameplayButtonsTo(bool inView)
    {
        Vector3 targetedPosition = new Vector3(0, inView ? 0 : -2.5f, 0);
        
        float startTime = Time.time;
        while (true)
        {
            yield return null;
            float elapsedTime = Time.time - startTime;
            if (elapsedTime > 0.5f) break;

            _gameplayButtonsParent.position = Vector3.Lerp(_gameplayButtonsParent.position, targetedPosition, elapsedTime * 2);
        }
    }

    private IEnumerator WaitNextInput(Action onInputCallback)
    {
        while (!Input.GetMouseButtonDown(0)) yield return null;

        onInputCallback();
    }

    private IEnumerator WaitLlmLoad(Action onLoadCallback)
    {
        var sleep = new WaitForSeconds(1f);

        while (!_chatManager.AreModelsReady()) yield return sleep;

        onLoadCallback();
    }
}

public class TargetEmotionalStatus
{
    private float _progress, _trust, _safety, _curiosity, _attachment, 
        _attraction, _respect, _authenticity, _tension, _compatibility;

    public TargetEmotionalStatus()
    {
        ResetStatus();
    }

    public void ResetStatus()
    {
        var character = TargetCharacterManager.Instance;
        if (!character) return;

        var characterName = character.CurrentCharacterName;
        SetStatsForCharacter(characterName);

        ApplyDeltaSigmoid(ref _progress, UnityEngine.Random.Range(-0.1f, 0.1f), _targetStatWeights[characterName]["progress"]);
        ApplyDeltaSigmoid(ref _trust, UnityEngine.Random.Range(-0.1f, 0.1f), _targetStatWeights[characterName]["trust"]);
        ApplyDeltaSigmoid(ref _safety, UnityEngine.Random.Range(-0.1f, 0.1f), _targetStatWeights[characterName]["safety"]);
        ApplyDeltaSigmoid(ref _curiosity, UnityEngine.Random.Range(-0.1f, 0.1f), _targetStatWeights[characterName]["curiosity"]);
        ApplyDeltaSigmoid(ref _attachment, UnityEngine.Random.Range(-0.1f, 0.1f), _targetStatWeights[characterName]["attachment"]);
        ApplyDeltaSigmoid(ref _attraction, UnityEngine.Random.Range(-0.1f, 0.1f), _targetStatWeights[characterName]["attraction"]);
        ApplyDeltaSigmoid(ref _respect, UnityEngine.Random.Range(-0.1f, 0.1f), _targetStatWeights[characterName]["respect"]);
        ApplyDeltaSigmoid(ref _authenticity, UnityEngine.Random.Range(-0.1f, 0.1f), _targetStatWeights[characterName]["authenticity"]);
        ApplyDeltaSigmoid(ref _tension, UnityEngine.Random.Range(-0.1f, 0.1f), _targetStatWeights[characterName]["tension"]);
        ApplyDeltaSigmoid(ref _compatibility, UnityEngine.Random.Range(-0.1f, 0.1f), _targetStatWeights[characterName]["compatibility"]);

    }

    /// <summary>
    /// Listens the conversation and updates the mood
    /// </summary>
    public void ListenAndUpdate(string[] structs, string[] topics, string[] tones)
    {
        var character = TargetCharacterManager.Instance;
        if (!character) return;

        var characterName = character.CurrentCharacterName;

        // calculate topics score
        float topicScore = 1f;
        foreach (var t in topics)
        {
            topicScore *= 1.25f * UnityEngine.Random.Range(1f, 2.2f) * _targetTopicWeights[characterName][t];
        }

        // calculate sentence score
        var structsNew = structs.Length > 0 ? structs : new string[1] { "Filler" };
        foreach (var sentenceStruct in structsNew)
        {
            // progress scoring
            float generalScoring = 0f;
            if (sentenceStruct == "Greeting")
            {
                generalScoring = 2f * (0.5f - _progress);
            }
            else if (sentenceStruct == "Casual Opener")
            {
                generalScoring = 0.7f - _progress;
            }
            else if (sentenceStruct == "Direct Opener")
            {
                generalScoring = 2.5f * (0.25f - _progress);
            }
            ApplyGeneralScore(generalScoring / topicScore);
            ApplyDeltaSigmoid(ref _progress, .1f, _targetStatWeights[characterName]["progress"]);

            // other scores
            float w;
            switch (sentenceStruct)
            {
                case "Greeting":
                    ApplyDeltaSigmoid(ref _safety, .3f * topicScore, _targetStatWeights[characterName]["safety"]);
                    break;

                case "Casual Opener":
                    ApplyDeltaSigmoid(ref _curiosity, .4f * topicScore, _targetStatWeights[characterName]["curiosity"]);
                    break;

                case "Direct Opener":
                    ApplyDeltaSigmoid(ref _respect, .5f * topicScore, _targetStatWeights[characterName]["respect"]);
                    ApplyDeltaSigmoid(ref _tension, .3f * topicScore, _targetStatWeights[characterName]["tension"]);
                    break;

                case "Light Compliment":
                    ApplyDeltaSigmoid(ref _attraction, .4f * topicScore, _targetStatWeights[characterName]["attraction"]);
                    ApplyDeltaSigmoid(ref _safety, .3f * topicScore, _targetStatWeights[characterName]["safety"]);
                    break;

                case "Bold Compliment":
                    ApplyDeltaSigmoid(ref _attraction, .7f * topicScore, _targetStatWeights[characterName]["attraction"]);
                    ApplyDeltaSigmoid(ref _tension, .6f * topicScore, _targetStatWeights[characterName]["tension"]);
                    ApplyDeltaSigmoid(ref _safety, -.2f / topicScore, _targetStatWeights[characterName]["safety"]);
                    break;

                case "Small Talk":
                    w = 1 + tones.OccurenceOf("Polite", "Calm") * 0.3f
                        - tones.OccurenceOf("Serious", "Depressed") * 0.2f;
                    ApplyDeltaSigmoid(ref _authenticity, .2f *w * topicScore, _targetStatWeights[characterName]["authenticity"]);
                    ApplyDeltaSigmoid(ref _curiosity, -.2f /w / topicScore, _targetStatWeights[characterName]["curiosity"]);
                    break;

                case "Personal Question":
                    ApplyDeltaSigmoid(ref _curiosity, .6f * topicScore, _targetStatWeights[characterName]["curiosity"]);
                    ApplyDeltaSigmoid(ref _trust, .4f * topicScore, _targetStatWeights[characterName]["trust"]);
                    ApplyDeltaSigmoid(ref _safety, -.1f / topicScore, _targetStatWeights[characterName]["safety"]);
                    break;

                case "Tease":
                    w = 1 + tones.OccurenceOf("Playful", "Flirty") * .5f
                        + tones.OccurenceOf("Confident") * .3f
                        - tones.OccurenceOf("Sincere", "Depressed") * .4f
                        - tones.OccurenceOf("Aggressive") * .6f;
                    ApplyDeltaSigmoid(ref _tension, .7f *w * topicScore, _targetStatWeights[characterName]["tension"]);
                    ApplyDeltaSigmoid(ref _attraction, .4f *w * topicScore, _targetStatWeights[characterName]["attraction"]);
                    ApplyDeltaSigmoid(ref _trust, -.2f /w / topicScore, _targetStatWeights[characterName]["trust"]);
                    break;

                case "Emotional Disclosure":
                    w = 1 + tones.OccurenceOf("Sincere", "Supportive") * .5f
                        + tones.OccurenceOf("Warm", "Calm") * .3f
                        - tones.OccurenceOf("Sarcastic", "Playful") * .3f
                        - tones.OccurenceOf("Aggressive", "Detached") * .5f;
                    ApplyDeltaSigmoid(ref _trust, .7f *w * topicScore, _targetStatWeights[characterName]["trust"]);
                    ApplyDeltaSigmoid(ref _attachment, .5f *w * topicScore, _targetStatWeights[characterName]["attachment"]);
                    ApplyDeltaSigmoid(ref _tension, -.2f / w / topicScore, _targetStatWeights[characterName]["tension"]);
                    break;

                case "Validation":
                    w = 1 + tones.OccurenceOf("Supportive") * .5f
                        + tones.OccurenceOf("Warm") * .3f
                        - tones.OccurenceOf("Detached") * .4f;
                    ApplyDeltaSigmoid(ref _safety, .7f *w * topicScore, _targetStatWeights[characterName]["safety"]);
                    ApplyDeltaSigmoid(ref _trust, .4f *w * topicScore, _targetStatWeights[characterName]["trust"]);
                    break;

                case "Follow-up":
                    ApplyDeltaSigmoid(ref _respect, .3f * topicScore, _targetStatWeights[characterName]["respect"]);
                    break;

                case "Flirt":
                    w = 1 + tones.OccurenceOf("Flirty") * .5f
                        + tones.OccurenceOf("Confident") * .3f
                        - tones.OccurenceOf("Shy") * .4f;
                    ApplyDeltaSigmoid(ref _attraction, .6f *w * topicScore, _targetStatWeights[characterName]["attraction"]);
                    ApplyDeltaSigmoid(ref _tension, .6f *w * topicScore, _targetStatWeights[characterName]["tension"]);
                    break;

                case "Respect":
                    ApplyDeltaSigmoid(ref _respect, .7f * topicScore, _targetStatWeights[characterName]["respect"]);
                    ApplyDeltaSigmoid(ref _trust, .3f * topicScore, _targetStatWeights[characterName]["trust"]);
                    break;

                case "Apology":
                    w = 1 + tones.OccurenceOf("Sincere") * .6f
                        + tones.OccurenceOf("Calm") * 3f
                        - tones.OccurenceOf("Sarcastic") * .5f;
                    ApplyDeltaSigmoid(ref _trust, .6f *w * topicScore, _targetStatWeights[characterName]["trust"]);
                    ApplyDeltaSigmoid(ref _safety, .5f *w * topicScore, _targetStatWeights[characterName]["safety"]);
                    ApplyDeltaSigmoid(ref _respect, -.1f / w / topicScore, _targetStatWeights[characterName]["respect"]);
                    break;

                case "Ask-Out":
                    w = 1 + tones.OccurenceOf("Confident") * .5f
                        + tones.OccurenceOf("Flirty") * .3f
                        - tones.OccurenceOf("Shy", "Uncertain") * .4f
                        - tones.OccurenceOf("Aggressive") * .6f;
                    ApplyDeltaSigmoid(ref _tension, .8f *w * topicScore, _targetStatWeights[characterName]["tension"]);
                    ApplyDeltaSigmoid(ref _attraction, .6f *w * topicScore, _targetStatWeights[characterName]["attraction"]);
                    ApplyDeltaSigmoid(ref _safety, -.3f / w / topicScore, _targetStatWeights[characterName]["safety"]);
                    break;

                case "Harrasment":
                    ApplyDeltaSigmoid(ref _trust, -.9f * topicScore, _targetStatWeights[characterName]["trust"]);
                    ApplyDeltaSigmoid(ref _safety, -.9f * topicScore, _targetStatWeights[characterName]["safety"]);
                    ApplyDeltaSigmoid(ref _respect, -.7f * topicScore, _targetStatWeights[characterName]["respect"]);
                    break;

                case "Contradiction":
                    ApplyDeltaSigmoid(ref _respect, .3f * topicScore, _targetStatWeights[characterName]["respect"]);
                    ApplyDeltaSigmoid(ref _trust, -.4f * topicScore, _targetStatWeights[characterName]["trust"]);
                    break;

                case "Mockery":
                    w = 1 + tones.OccurenceOf("Sarcastic") * .4f
                        + tones.OccurenceOf("Playful") * .2f
                        - tones.OccurenceOf("Sincere", "Supportive") * .6f;
                    ApplyDeltaSigmoid(ref _tension, .5f *w * topicScore, _targetStatWeights[characterName]["tension"]);
                    ApplyDeltaSigmoid(ref _trust, -.6f / w / topicScore, _targetStatWeights[characterName]["trust"]);
                    ApplyDeltaSigmoid(ref _respect, -.6f / w / topicScore, _targetStatWeights[characterName]["respect"]);
                    break;

                case "Fun-fact":
                    ApplyDeltaSigmoid(ref _curiosity, .6f * topicScore, _targetStatWeights[characterName]["curiosity"]);
                    ApplyDeltaSigmoid(ref _respect, .2f * topicScore, _targetStatWeights[characterName]["respect"]);
                    break;

                case "Mourning":
                    ApplyDeltaSigmoid(ref _safety, .6f * topicScore, _targetStatWeights[characterName]["safety"]);
                    ApplyDeltaSigmoid(ref _attachment, .4f * topicScore, _targetStatWeights[characterName]["attachment"]);
                    ApplyDeltaSigmoid(ref _tension, -.3f / topicScore, _targetStatWeights[characterName]["tension"]);
                    break;
            }
        }

        Debug.Log($"[MOOD] Progress: {_progress} for Talk Count: {GameplayStateManager.Instance.TalkCount}");
        UpdateEmotionalStatus();
    }

    /// <summary>
    /// Moves all stats closer to win condition
    /// </summary>
    public void MegaCharm()
    {
        void AdjustValue(ref float target, float value)
        {
            target = 0.15f * (value - target) + target;
        }
        
        AdjustValue(ref _progress, .66f);
        AdjustValue(ref _trust, .85f);
        AdjustValue(ref _safety, .8f);
        AdjustValue(ref _curiosity, .6f);
        AdjustValue(ref _attachment, .6f);
        AdjustValue(ref _attraction, .8f);
        AdjustValue(ref _respect, .8f);
        AdjustValue(ref _authenticity, .8f);
        AdjustValue(ref _tension, .2f);
        AdjustValue(ref _compatibility, .8f);
    }

    public enum ConversationStatus
    {
        Continues = 0, 
        Proposed = 1 << 3,
        CutOff = 2 << 3,
        NoSpark = 3 << 3, UnsafeFeeling, LostRespect, Mismatch, TooFast,
        TimeRanOut = 4 << 3, EnergyDrained, ComfortPause, Overstimulated,
    }
    public ConversationStatus CheckEndConditions(out string[] endSentences)
    {
        ConversationStatus status = ConversationStatus.Continues;

        // MARRIAGE PROPOSAL
        if (_trust > .7f && _compatibility > .65f && _respect > .6f &&
            _attachment > .5f && _attachment < .7f && _attraction > .55f &&
            _tension < .45f && _safety > .65f && _authenticity > .6f && _progress > .65f)
        {
            // win
            status = ConversationStatus.Proposed;
        }
        // HARD FAIL
        else if (_safety < .25f && _trust < .3f)
        {
            // cut off, Conversation ends immediately
            status = ConversationStatus.CutOff;
        }
        // REJECTIONS
        else if (_attraction < .35f && _curiosity < .35f && _progress > .4f)
        {
            // no spark
            status = ConversationStatus.NoSpark;
        }
        else if (_safety < .35f && _trust < .4f)
        {
            // dont feel safe
            status = ConversationStatus.UnsafeFeeling;
        }
        else if (_respect < .3f && _progress > .25f)
        {
            // lost respect
            status = ConversationStatus.LostRespect;
        }
        else if (_attachment > .6f && _trust < .45f)
        {
            // emotional mismatch
            status = ConversationStatus.Mismatch;
        }
        else if (_attachment > .65f && _progress < .45f)
        {
            // too much too fast
            status = ConversationStatus.TooFast;
        }
        // LEAVES
        else if (_progress > .75f)
        {
            // time ran out
            status = ConversationStatus.TimeRanOut;
        }
        else if (_progress > .6f && _curiosity < .35f && _tension < .35f)
        {
            // energy depleted
            status = ConversationStatus.EnergyDrained;
        }
        else if (_safety > .6f && _trust > .55f && _progress > .7f)
        {
            // comfortable pause
            status = ConversationStatus.ComfortPause;
        }

        if (status != ConversationStatus.Continues)
        { 
            endSentences = _endDateDialogs[TargetCharacterManager.Instance.CurrentCharacterName][status];
        }
        else
        {
            endSentences = null;
        }
        return status;
    }

    public string[] EmotionalStatus { get; set; } = new string[0];
    private void UpdateEmotionalStatus()
    {
        List<string> emotions = new();

        if (_progress > 0.5f && _tension < 0.25f && _curiosity < 0.3f)
            emotions.Add("bored");

        if (_curiosity > 0.5f && _attraction > 0.4f && _safety > 0.3f && _progress < 0.5f)
            emotions.Add("enthusiastic");

        if (_safety > 0.6f && _trust > 0.5f && _tension < 0.3f)
            emotions.Add("comfortable");

        if (_curiosity > .65f && _attachment < .3f && _trust < .5f)
            emotions.Add("intrigued");

        if (_attraction > .6f && _safety < .4f && _tension > .5f)
            emotions.Add("flustered");

        if (_attachment > .6f && _curiosity < .3f && _progress > .4f)
            emotions.Add("attached");

        if (_progress > .3f && _trust < .25f && _curiosity > .3f)
            emotions.Add("suspicious");

        if (_safety < .3f && _trust < .4f && _respect > .5f)
            emotions.Add("defensive");

        if (_progress > .5f && _trust < .25f && _attachment > .5f)
            emotions.Add("hurt");

        if (_progress > .5f && _attachment < .3f && _attraction < .3f)
            emotions.Add("distant");

        if (_attraction > .5f && _trust > .4f && _safety > .3f)
            emotions.Add("interested");

        if (_attachment > .5f && _safety < .35f && _curiosity < .25f)
            emotions.Add("overwhelmed");

        if (_progress > .5f && _curiosity < .2f && _attraction < .3f)
            emotions.Add("disillusioned");

        if (_trust > .6f && _safety > .6f && _compatibility > .6f)
            emotions.Add("secure");

        if (_attraction > .6f && _tension > .6f && _trust < .4f)
            emotions.Add("unsettled");

        EmotionalStatus = emotions.ToArray();
    }

    private void ApplyDeltaSigmoid(ref float value, float delta, float weight = 1)
    {
        float x = (value - 0.5f) * 4f; // map to ~[-2,2]
        x += delta * weight;
        float y = 1f / (1f + MathF.Exp(-x));
        value = y;
    }

    private void ApplyGeneralScore(float score)
    {
        var characterName = TargetCharacterManager.Instance.CurrentCharacterName;

        ApplyDeltaSigmoid(ref _trust, score, _targetStatWeights[characterName]["trust"]);
        ApplyDeltaSigmoid(ref _safety, score, _targetStatWeights[characterName]["safety"]);
        ApplyDeltaSigmoid(ref _curiosity, score, _targetStatWeights[characterName]["curiosity"]);
        ApplyDeltaSigmoid(ref _attachment, score, _targetStatWeights[characterName]["attachment"]);
        ApplyDeltaSigmoid(ref _attraction, score, _targetStatWeights[characterName]["attraction"]);
        ApplyDeltaSigmoid(ref _respect, score, _targetStatWeights[characterName]["respect"]);
        ApplyDeltaSigmoid(ref _authenticity, score, _targetStatWeights[characterName]["authenticity"]);
        ApplyDeltaSigmoid(ref _compatibility, score, _targetStatWeights[characterName]["compatibility"]);
        ApplyDeltaSigmoid(ref _tension, -score, _targetStatWeights[characterName]["tension"]);
    }

    private void SetStatsForCharacter(string characterName)
    {
        _progress = 0f;

        switch (characterName)
        {
            case "Modern":
                _trust =         .40f;
                _safety =        .50f;
                _curiosity =     .65f;
                _attachment =    .02f;
                _attraction =    .45f;
                _respect =       .55f;
                _authenticity =  .60f;
                _tension =       .35f;
                _compatibility = .50f;
                break;

            case "Caveman":
                _trust =         .30f;
                _safety =        .35f;
                _curiosity =     .70f;
                _attachment =    .00f;
                _attraction =    .60f;
                _respect =       .40f;
                _authenticity =  .85f;
                _tension =       .55f;
                _compatibility = .45f;
                break;

            case "Viking":
                _trust =         .45f;
                _safety =        .40f;
                _curiosity =     .55f;
                _attachment =    .02f;
                _attraction =    .65f;
                _respect =       .70f;
                _authenticity =  .75f;
                _tension =       .60f;
                _compatibility = .55f;
                break;

            case "Janissary":
                _trust =         .35f;
                _safety =        .45f;
                _curiosity =     .40f;
                _attachment =    .01f;
                _attraction =    .40f;
                _respect =       .80f;
                _authenticity =  .50f;
                _tension =       .30f;
                _compatibility = .60f;
                break;

            case "Cyborg":
                _trust =         .50f;
                _safety =        .60f;
                _curiosity =     .80f;
                _attachment =    .00f;
                _attraction =    .30f;
                _respect =       .65f;
                _authenticity =  .40f;
                _tension =       .20f;
                _compatibility = .55f;
                break;
        }
    }

    #region STATIC_DICTIONARIES

    private static Dictionary<string, Dictionary<string, float>> _targetStatWeights = new()
    {
        ["Modern"] = new()
        {
            ["progress"] = 1.1f,
            ["trust"] = 1.1f,
            ["safety"] = 1.1f,
            ["curiosity"] = 1.2f,
            ["attachment"] = 0.9f,
            ["attraction"] = 1.1f,
            ["respect"] = 1.1f,
            ["authenticity"] = 1.0f,
            ["tension"] = 1.0f,
            ["compatibility"] = 1.1f
        },
        ["Caveman"] = new()
        {
            ["progress"] = 0.9f,
            ["trust"] = 0.8f,
            ["safety"] = 0.8f,
            ["curiosity"] = 1.3f,
            ["attachment"] = 0.7f,
            ["attraction"] = 1.4f,
            ["respect"] = 1.0f,
            ["authenticity"] = 1.5f,
            ["tension"] = 1.4f,
            ["compatibility"] = 1.0f
        },
        ["Viking"] = new()
        {
            ["progress"] = 1.0f,
            ["trust"] = 1.1f,
            ["safety"] = 0.9f,
            ["curiosity"] = 1.0f,
            ["attachment"] = 0.8f,
            ["attraction"] = 1.5f,
            ["respect"] = 1.4f,
            ["authenticity"] = 1.3f,
            ["tension"] = 1.5f,
            ["compatibility"] = 1.1f
        },
        ["Janissary"] = new()
        {
            ["progress"] = 1.0f,
            ["trust"] = 0.9f,
            ["safety"] = 1.1f,
            ["curiosity"] = 0.7f,
            ["attachment"] = 0.6f,
            ["attraction"] = 0.8f,
            ["respect"] = 1.6f,
            ["authenticity"] = 0.7f,
            ["tension"] = 0.8f,
            ["compatibility"] = 1.3f
        },
        ["Cyborg"] = new()
        {
            ["progress"] = 1.2f,
            ["trust"] = 1.3f,
            ["safety"] = 1.2f,
            ["curiosity"] = 1.6f,
            ["attachment"] = 0.4f,
            ["attraction"] = 0.7f,
            ["respect"] = 1.2f,
            ["authenticity"] = 0.6f,
            ["tension"] = 0.5f,
            ["compatibility"] = 1.4f
        }

    };

    private static Dictionary<string, Dictionary<string, float>> _targetTopicWeights = new()
    {
        ["Caveman"] = new Dictionary<string, float>
        {
            ["Weather"] = 0.9197f,
            ["Trees"] = 0.9486f,
            ["Flowers"] = 0.9496f,
            ["Fruits"] = 0.9221f,
            ["Clouds"] = 0.9365f,
            ["Hike"] = 0.9243f,
            ["Sky"] = 0.9264f,
            ["Birds"] = 0.929f,
            ["Moon"] = 0.891f,
            ["Ocean"] = 0.9392f,
            ["Fight"] = 0.7306f,
            ["Trauma"] = 0.6426f,
            ["Loneliness"] = 0.4446f,
            ["Violence"] = 0.533f,
            ["Threat"] = 0.4716f,
            ["Abuse"] = 0.3825f,
            ["Weapons"] = 0.4711f,
            ["Fear"] = 0.5571f,
            ["Tears"] = 0.6371f,
            ["Regrets"] = 0.5011f,
            ["Family"] = 0.913f,
            ["Friends"] = 0.9128f,
            ["Dating"] = 0.8801f,
            ["Trust"] = 0.9026f,
            ["Love"] = 0.8722f,
            ["Partner"] = 0.875f,
            ["Breakups"] = 0.777f,
            ["Gossip"] = 0.8674f,
            ["Marriage"] = 0.6536f,
            ["Conflict"] = 0.496f,
            ["Games"] = 0.2961f,
            ["Movies"] = 0.0205f,
            ["Sports"] = 0.2618f,
            ["Travel"] = 0.2678f,
            ["Music"] = 0.3223f,
            ["Books"] = 0.384f,
            ["Relax"] = 0.8215f,
            ["Sleep"] = 0.9406f,
            ["Food"] = 0.9417f,
            ["Hobbies"] = 0.8875f,
            ["Work"] = 0.7754f,
            ["Chores"] = 0.4622f,
            ["Commute"] = 0.4547f,
            ["Shopping"] = 0.3946f,
            ["Routines"] = 0.2541f,
            ["Stress"] = 0.7146f,
            ["News"] = 0.3104f,
            ["Money"] = 0.3611f,
            ["Plans"] = 0.502f,
            ["Purpose"] = 0.8288f,
            ["Internet"] = 0.1675f,
            ["Phones"] = 0.0425f,
            ["Robots"] = 0.2478f,
            ["Cyber"] = 0.3162f,
            ["Gaming"] = 0.2658f,
            ["Future"] = 0.5696f,
            ["Power"] = 0.7922f,
            ["Laws"] = 0.4233f,
            ["Global Warming"] = 0.2726f,
            ["Disasters"] = 0.644f
        },

        ["Viking"] = new Dictionary<string, float>
        {
            ["Weather"] = 0.8095f,
            ["Trees"] = 0.7449f,
            ["Flowers"] = 0.7615f,
            ["Fruits"] = 0.7203f,
            ["Clouds"] = 0.7909f,
            ["Hike"] = 0.9098f,
            ["Sky"] = 0.8806f,
            ["Birds"] = 0.7847f,
            ["Moon"] = 0.8016f,
            ["Ocean"] = 0.9052f,
            ["Fight"] = 0.9248f,
            ["Trauma"] = 0.9492f,
            ["Loneliness"] = 0.7226f,
            ["Violence"] = 0.927f,
            ["Threat"] = 0.8877f,
            ["Abuse"] = 0.8022f,
            ["Weapons"] = 0.8999f,
            ["Fear"] = 0.9352f,
            ["Tears"] = 0.8911f,
            ["Regrets"] = 0.7957f,
            ["Family"] = 0.5509f,
            ["Friends"] = 0.5908f,
            ["Dating"] = 0.5642f,
            ["Trust"] = 0.7085f,
            ["Love"] = 0.8566f,
            ["Partner"] = 0.4188f,
            ["Breakups"] = 0.4242f,
            ["Gossip"] = 0.4483f,
            ["Marriage"] = 0.2771f,
            ["Conflict"] = 0.8349f,
            ["Games"] = 0.7827f,
            ["Movies"] = 0.3467f,
            ["Sports"] = 0.5458f,
            ["Travel"] = 0.4413f,
            ["Music"] = 0.3525f,
            ["Books"] = 0.1716f,
            ["Relax"] = 0.8277f,
            ["Sleep"] = 0.8711f,
            ["Food"] = 0.7542f,
            ["Hobbies"] = 0.7425f,
            ["Work"] = 0.7997f,
            ["Chores"] = 0.5892f,
            ["Commute"] = 0.5215f,
            ["Shopping"] = 0.2362f,
            ["Routines"] = 0.4624f,
            ["Stress"] = 0.6341f,
            ["News"] = 0.4958f,
            ["Money"] = 0.428f,
            ["Plans"] = 0.8565f,
            ["Purpose"] = 0.9085f,
            ["Internet"] = 0.2549f,
            ["Phones"] = 0.1846f,
            ["Robots"] = 0.2224f,
            ["Cyber"] = 0.1111f,
            ["Gaming"] = 0.4202f,
            ["Future"] = 0.7518f,
            ["Power"] = 0.9523f,
            ["Laws"] = 0.3924f,
            ["Global Warming"] = 0.2298f,
            ["Disasters"] = 0.6617f
        },

        ["Janissary"] = new Dictionary<string, float>
        {
            ["Weather"] = 0.7658f,
            ["Trees"] = 0.7502f,
            ["Flowers"] = 0.809f,
            ["Fruits"] = 0.7264f,
            ["Clouds"] = 0.7859f,
            ["Hike"] = 0.8046f,
            ["Sky"] = 0.7618f,
            ["Birds"] = 0.8784f,
            ["Moon"] = 0.7913f,
            ["Ocean"] = 0.8209f,
            ["Fight"] = 0.5629f,
            ["Trauma"] = 0.744f,
            ["Loneliness"] = 0.4366f,
            ["Violence"] = 0.6969f,
            ["Threat"] = 0.5855f,
            ["Abuse"] = 0.3959f,
            ["Weapons"] = 0.687f,
            ["Fear"] = 0.5509f,
            ["Tears"] = 0.247f,
            ["Regrets"] = 0.6029f,
            ["Family"] = 0.861f,
            ["Friends"] = 0.9022f,
            ["Dating"] = 0.817f,
            ["Trust"] = 0.9279f,
            ["Love"] = 0.8975f,
            ["Partner"] = 0.9114f,
            ["Breakups"] = 0.8215f,
            ["Gossip"] = 0.9006f,
            ["Marriage"] = 0.8866f,
            ["Conflict"] = 0.7939f,
            ["Games"] = 0.7572f,
            ["Movies"] = 0.5116f,
            ["Sports"] = 0.5677f,
            ["Travel"] = 0.5508f,
            ["Music"] = 0.5069f,
            ["Books"] = 0.5406f,
            ["Relax"] = 0.8807f,
            ["Sleep"] = 0.9158f,
            ["Food"] = 0.9141f,
            ["Hobbies"] = 0.9198f,
            ["Work"] = 0.6247f,
            ["Chores"] = 0.8206f,
            ["Commute"] = 0.6443f,
            ["Shopping"] = 0.4981f,
            ["Routines"] = 0.6391f,
            ["Stress"] = 0.4187f,
            ["News"] = 0.4714f,
            ["Money"] = 0.9135f,
            ["Plans"] = 0.8234f,
            ["Purpose"] = 0.8432f,
            ["Internet"] = 0.2847f,
            ["Phones"] = 0.1982f,
            ["Robots"] = 0.4293f,
            ["Cyber"] = 0.2223f,
            ["Gaming"] = 0.2288f,
            ["Future"] = 0.2545f,
            ["Power"] = 0.5246f,
            ["Laws"] = 0.602f,
            ["Global Warming"] = 0.4014f,
            ["Disasters"] = 0.4595f
        },

        ["Modern"] = new Dictionary<string, float>
        {
            ["Weather"] = 0.7296f,
            ["Trees"] = 0.7993f,
            ["Flowers"] = 0.7692f,
            ["Fruits"] = 0.7412f,
            ["Clouds"] = 0.8056f,
            ["Hike"] = 0.7822f,
            ["Sky"] = 0.812f,
            ["Birds"] = 0.7442f,
            ["Moon"] = 0.7458f,
            ["Ocean"] = 0.7597f,
            ["Fight"] = 0.622f,
            ["Trauma"] = 0.5245f,
            ["Loneliness"] = 0.6412f,
            ["Violence"] = 0.583f,
            ["Threat"] = 0.587f,
            ["Abuse"] = 0.6771f,
            ["Weapons"] = 0.5814f,
            ["Fear"] = 0.6271f,
            ["Tears"] = 0.619f,
            ["Regrets"] = 0.6504f,
            ["Family"] = 0.8385f,
            ["Friends"] = 0.8014f,
            ["Dating"] = 0.8004f,
            ["Trust"] = 0.7804f,
            ["Love"] = 0.8345f,
            ["Partner"] = 0.771f,
            ["Breakups"] = 0.3746f,
            ["Gossip"] = 0.797f,
            ["Marriage"] = 0.8049f,
            ["Conflict"] = 0.6974f,
            ["Games"] = 0.8144f,
            ["Movies"] = 0.7195f,
            ["Sports"] = 0.7473f,
            ["Travel"] = 0.7426f,
            ["Music"] = 0.774f,
            ["Books"] = 0.761f,
            ["Relax"] = 0.7025f,
            ["Sleep"] = 0.657f,
            ["Food"] = 0.7977f,
            ["Hobbies"] = 0.783f,
            ["Work"] = 0.1867f,
            ["Chores"] = 0.1875f,
            ["Commute"] = 0.4462f,
            ["Shopping"] = 0.8181f,
            ["Routines"] = 0.3082f,
            ["Stress"] = 0.162f,
            ["News"] = 0.7156f,
            ["Money"] = 0.8625f,
            ["Plans"] = 0.6975f,
            ["Purpose"] = 0.8387f,
            ["Internet"] = 0.8204f,
            ["Phones"] = 0.7621f,
            ["Robots"] = 0.73f,
            ["Cyber"] = 0.7607f,
            ["Gaming"] = 0.8393f,
            ["Future"] = 0.8314f,
            ["Power"] = 0.6572f,
            ["Laws"] = 0.7781f,
            ["Global Warming"] = 0.7457f,
            ["Disasters"] = 0.6715f
        },

        ["Cyborg"] = new Dictionary<string, float>
        {
            ["Weather"] = 0.0529f,
            ["Trees"] = 0.3394f,
            ["Flowers"] = 0.0644f,
            ["Fruits"] = 0.0337f,
            ["Clouds"] = 0.0287f,
            ["Hike"] = 0.1569f,
            ["Sky"] = 0.2509f,
            ["Birds"] = 0.0088f,
            ["Moon"] = 0.3143f,
            ["Ocean"] = 0.031f,
            ["Fight"] = 0.1718f,
            ["Trauma"] = 0.8235f,
            ["Loneliness"] = 0.7435f,
            ["Violence"] = 0.4946f,
            ["Threat"] = 0.7501f,
            ["Abuse"] = 0.8453f,
            ["Weapons"] = 0.687f,
            ["Fear"] = 0.4375f,
            ["Tears"] = 0.5377f,
            ["Regrets"] = 0.4824f,
            ["Family"] = 0.5705f,
            ["Friends"] = 0.6584f,
            ["Dating"] = 0.7478f,
            ["Trust"] = 0.8102f,
            ["Love"] = 0.8364f,
            ["Partner"] = 0.8601f,
            ["Breakups"] = 0.7775f,
            ["Gossip"] = 0.7787f,
            ["Marriage"] = 0.6861f,
            ["Conflict"] = 0.6613f,
            ["Games"] = 0.8514f,
            ["Movies"] = 0.8446f,
            ["Sports"] = 0.877f,
            ["Travel"] = 0.8475f,
            ["Music"] = 0.8467f,
            ["Books"] = 0.8709f,
            ["Relax"] = 0.8361f,
            ["Sleep"] = 0.0542f,
            ["Food"] = 0.7034f,
            ["Hobbies"] = 0.7802f,
            ["Work"] = 0.828f,
            ["Chores"] = 0.8412f,
            ["Commute"] = 0.7944f,
            ["Shopping"] = 0.7433f,
            ["Routines"] = 0.7928f,
            ["Stress"] = 0.508f,
            ["News"] = 0.8104f,
            ["Money"] = 0.8161f,
            ["Plans"] = 0.8469f,
            ["Purpose"] = 0.8192f,
            ["Internet"] = 0.8724f,
            ["Phones"] = 0.8372f,
            ["Robots"] = 0.8218f,
            ["Cyber"] = 0.9036f,
            ["Gaming"] = 0.8478f,
            ["Future"] = 0.8645f,
            ["Power"] = 0.8576f,
            ["Laws"] = 0.8602f,
            ["Global Warming"] = 0.8787f,
            ["Disasters"] = 0.8513f
        }
    };

    private static Dictionary<string, Dictionary<ConversationStatus, string[]>> _endDateDialogs = new()
    {
        ["Modern"] = new()
        {
            {
                ConversationStatus.Proposed,
                new[]
                {
                    "I need to say something important.",
                    "I didn’t expect this, but it feels calm and real with you.",
                    "I’m not scared when I think about the future together.",
                    "Yes. I want this. Will you marry with me?"
                }
            },
            {
                ConversationStatus.CutOff,
                new[]
                {
                    "I need to be clear.",
                    "This crossed a line and I don’t feel safe continuing.",
                    "I’m ending this now.",
                    "Goodbye."
                }
            },
            {
                ConversationStatus.NoSpark,
                new[]
                {
                    "I want to be honest with you.",
                    "I don’t feel a romantic connection here.",
                    "I don’t want to force something that isn’t there.",
                    "I think we should stop."
                }
            },
            {
                ConversationStatus.UnsafeFeeling,
                new[]
                {
                    "I need to trust my instincts.",
                    "Something about this makes me uncomfortable.",
                    "I don’t feel okay continuing this conversation.",
                    "I’m going to leave now."
                }
            },
            {
                ConversationStatus.LostRespect,
                new[]
                {
                    "I need to address something.",
                    "What you said changed how I see you.",
                    "I’ve lost respect, and I can’t ignore that.",
                    "I’m done here."
                }
            },
            {
                ConversationStatus.Mismatch,
                new[]
                {
                    "I’ve been thinking about this.",
                    "We want different things, and it’s showing.",
                    "This feels misaligned for the long term.",
                    "I’m stepping away."
                }
            },
            {
                ConversationStatus.TooFast,
                new[]
                {
                    "I need to slow this down.",
                    "This is moving faster than I’m comfortable with.",
                    "I’m feeling pressured instead of connected.",
                    "I need space."
                }
            },
            {
                ConversationStatus.TimeRanOut,
                new[]
                {
                    "I hate to cut this short.",
                    "I really have to go now.",
                    "This was nice, though.",
                    "We’ll have to leave it here."
                }
            },
            {
                ConversationStatus.EnergyDrained,
                new[]
                {
                    "I need to be honest with myself.",
                    "I’m exhausted and can’t stay present anymore.",
                    "I don’t want to keep talking like this.",
                    "I’m going to stop here."
                }
            },
            {
                ConversationStatus.ComfortPause,
                new[]
                {
                    "I want to say this carefully.",
                    "I like where this is, but I don’t want to rush it.",
                    "This doesn’t have to end.",
                    "Let’s pause here."
                }
            },
            {
                ConversationStatus.Overstimulated,
                new[]
                {
                    "I need a moment.",
                    "This is getting too intense for me right now.",
                    "My head feels full and I can’t process more.",
                    "I need to step away."
                }
            }
        },
        ["Caveman"] = new()
        {
            {
                ConversationStatus.Proposed,
                new[]
                {
                    "I want to speak while the fire is still strong.",
                    "When we share food and silence, my body feels settled.",
                    "I can imagine seasons passing with you nearby.",
                    "I want us to stay together as one pair."
                }
            },
            {
                ConversationStatus.CutOff,
                new[]
                {
                    "This talk has turned bad.",
                    "What you did breaks the way people should treat each other.",
                    "I don’t trust standing close to you anymore.",
                    "I’m leaving now."
                }
            },
            {
                ConversationStatus.NoSpark,
                new[]
                {
                    "I’ve waited to see what would grow between us.",
                    "Nothing has taken root inside me.",
                    "Sitting together brings no pull or warmth.",
                    "We should go separate ways."
                }
            },
            {
                ConversationStatus.UnsafeFeeling,
                new[]
                {
                    "This feels like being watched from the trees.",
                    "My body stays tense instead of easing.",
                    "I don’t believe this place is right for me.",
                    "I’m going away."
                }
            },
            {
                ConversationStatus.LostRespect,
                new[]
                {
                    "Your words landed hard.",
                    "They showed carelessness, not strength.",
                    "I no longer hold you in the same regard.",
                    "This ends here."
                }
            },
            {
                ConversationStatus.Mismatch,
                new[]
                {
                    "We aim for different kinds of days.",
                    "What keeps you moving does not guide me.",
                    "Trying to walk together would only slow us both.",
                    "It’s better to part."
                }
            },
            {
                ConversationStatus.TooFast,
                new[]
                {
                    "You’re rushing ahead without checking the ground.",
                    "I haven’t had time to understand what we’re building.",
                    "This feels like chasing prey without a plan.",
                    "I need you to stop."
                }
            },
            {
                ConversationStatus.TimeRanOut,
                new[]
                {
                    "The light is fading fast.",
                    "I need to return before the cold settles in.",
                    "This exchange was worthwhile.",
                    "We end it here."
                }
            },
            {
                ConversationStatus.EnergyDrained,
                new[]
                {
                    "My body feels spent from the day.",
                    "Keeping focus now takes too much effort.",
                    "I don’t have strength left for more words.",
                    "I need rest."
                }
            },
            {
                ConversationStatus.ComfortPause,
                new[]
                {
                    "Things are balanced as they are.",
                    "There’s no reason to push further right now.",
                    "We can leave this untouched.",
                    "Another time will come."
                }
            },
            {
                ConversationStatus.Overstimulated,
                new[]
                {
                    "There’s too much happening around us.",
                    "My thoughts are colliding instead of settling.",
                    "I can’t make sense of it all right now.",
                    "I need to step away."
                }
            }
        },
        ["Viking"] = new()
        {
            {
                ConversationStatus.Proposed,
                new[]
                {
                    "Hear me now, before the fire dies.",
                    "I have crossed seas and spilled blood, and my heart does not wander lightly.",
                    "Stand with me as shieldmate and hearth-keeper, and let our fates be tied by the Norns.",
                    "If you accept, we walk forward together; if not, I will still honor this moment."
                }
            },
            {
                ConversationStatus.CutOff,
                new[]
                {
                    "I must speak before silence claims us.",
                    "My words fall against a wall, and even Odin knows when wisdom is wasted.",
                    "I will not shout into the wind any longer.",
                    "So I turn away now, while honor still stands between us."
                }
            },
            {
                ConversationStatus.NoSpark,
                new[]
                {
                    "Let me say this plainly, as a man before battle.",
                    "I feel no fire from the gods here, no pull of fate in my chest.",
                    "We share the same hall, but not the same saga.",
                    "It is better we part now than pretend the runes say otherwise."
                }
            },
            {
                ConversationStatus.UnsafeFeeling,
                new[]
                {
                    "Before we go further, hear my unease.",
                    "My instincts sharpen like a blade, and they tell me this ground is not firm.",
                    "Even the bravest warrior steps back when the signs are wrong.",
                    "I will leave now, not in fear, but in wisdom."
                }
            },
            {
                ConversationStatus.LostRespect,
                new[]
                {
                    "I need to speak, though it weighs heavy.",
                    "What I have seen has chipped away at my respect, piece by piece, like a cracked shield.",
                    "Without honor, no bond survives—not in war, not in love.",
                    "So this is where my path turns away from yours."
                }
            },
            {
                ConversationStatus.Mismatch,
                new[]
                {
                    "Hear this before we drink from different horns.",
                    "The gods did not carve us for the same road or the same ending.",
                    "You seek one saga; I am bound to another.",
                    "We should part now, before resentment poisons the journey."
                }
            },
            {
                ConversationStatus.TooFast,
                new[]
                {
                    "Wait—before we rush ahead.",
                    "Even longships break if they sail before the wood is seasoned.",
                    "What we are building needs time, or it will not survive the storms.",
                    "If we cannot slow, then we should stop here."
                }
            },
            {
                ConversationStatus.TimeRanOut,
                new[]
                {
                    "I must speak before the moment passes.",
                    "Too much time has slipped through our hands like sand on the shore.",
                    "The tide has turned, and the chance we had will not return.",
                    "So I say farewell, and let the past rest."
                }
            },
            {
                ConversationStatus.EnergyDrained,
                new[]
                {
                    "Hear me, though my strength is low.",
                    "This bond drains me more than a long winter without sun.",
                    "A warrior who cannot recover will fall, no matter his courage.",
                    "I must step away to preserve what remains of me."
                }
            },
            {
                ConversationStatus.ComfortPause,
                new[]
                {
                    "Let us speak calmly, not with raised voices.",
                    "What we have has grown quiet, like a hearth left unattended.",
                    "Perhaps we need distance to see if the embers still glow.",
                    "I will step back now, and we will see what remains."
                }
            },
            {
                ConversationStatus.Overstimulated,
                new[]
                {
                    "Stop—before my thoughts splinter.",
                    "Too many voices, too many demands, like battle cries without order.",
                    "Even a berserker must rest before he loses himself.",
                    "I will withdraw now, before this turns into ruin."
                }
            }
        },
        ["Janissary"] = new()
        {
            {
                ConversationStatus.Proposed,
                new[]
                {
                    "Listen to me now, before silence claims this moment.",
                    "I do not offer my word lightly, nor do I bind my soul without reflection.",
                    "If you would stand with me under one roof and one fate, then let it be written by Allah’s will, Inshallah.",
                    "If not, I accept His decree and step away with honor."
                }
            },
            {
                ConversationStatus.CutOff,
                new[]
                {
                    "I will speak once more, then close my mouth.",
                    "Your answers no longer reach me, as if a door has been sealed.",
                    "I will not beg at a gate that does not open.",
                    "So I turn my back and leave this matter to fate."
                }
            },
            {
                ConversationStatus.NoSpark,
                new[]
                {
                    "I must be honest, even if the truth is dry.",
                    "My heart does not stir, and no warmth answers my prayers here.",
                    "We share words, but not fire.",
                    "It is better we part now, Inshallah, than lie to ourselves."
                }
            },
            {
                ConversationStatus.UnsafeFeeling,
                new[]
                {
                    "Wait. Something here troubles my spirit.",
                    "My instincts whisper caution, as if Allah warns me to slow my step.",
                    "Not every road is meant to be walked, even with good intention.",
                    "I will retreat now and place my trust in Him."
                }
            },
            {
                ConversationStatus.LostRespect,
                new[]
                {
                    "I must speak plainly, without sweetness.",
                    "What I have seen has diminished my respect, piece by piece.",
                    "Without respect, even patience becomes sin.",
                    "This path ends here."
                }
            },
            {
                ConversationStatus.Mismatch,
                new[]
                {
                    "Hear me with calm ears.",
                    "Your nature and mine do not align under the same sky.",
                    "You move by one rhythm, I by another written long ago.",
                    "We should separate now before resentment grows."
                }
            },
            {
                ConversationStatus.TooFast,
                new[]
                {
                    "Slow yourself.",
                    "Even the strongest decisions require sabr and reflection.",
                    "What is rushed often breaks under its own weight.",
                    "If we cannot slow, then we must stop."
                }
            },
            {
                ConversationStatus.TimeRanOut,
                new[]
                {
                    "I speak as this moment slips from our hands.",
                    "Too much time has passed without clarity.",
                    "Some chances are not renewed once they fade.",
                    "I accept this ending, Inshallah, and move forward."
                }
            },
            {
                ConversationStatus.EnergyDrained,
                new[]
                {
                    "I am weary, and I will not hide it.",
                    "This drains me more than long duty without rest or prayer.",
                    "A man emptied cannot offer sincerity.",
                    "I must step away and restore myself."
                }
            },
            {
                ConversationStatus.ComfortPause,
                new[]
                {
                    "Let us put distance between these words for now.",
                    "What we have has grown still, like water left untouched.",
                    "Silence may reveal what noise has hidden.",
                    "I will wait and see what Allah reveals."
                }
            },
            {
                ConversationStatus.Overstimulated,
                new[]
                {
                    "Enough. This overwhelms me.",
                    "Too many words, too many demands, no balance.",
                    "Even discipline has limits set by the soul.",
                    "I withdraw now before disorder takes root."
                }
            }
        },
        ["Cyborg"] = new()
        {
            {
                ConversationStatus.Proposed,
                new[]
                {
                    "I need to say this before we disconnect.",
                    "I’ve run this scenario more times than I usually allow.",
                    "You fit into a future I’m willing to lock in.",
                    "If you agree, I won’t roll it back."
                }
            },
            {
                ConversationStatus.CutOff,
                new[]
                {
                    "I’ll keep this brief.",
                    "I’ve waited long enough for a response that never came.",
                    "I don’t keep channels open without purpose.",
                    "I’m closing this one."
                }
            },
            {
                ConversationStatus.NoSpark,
                new[]
                {
                    "I checked myself for bias before saying this.",
                    "Nothing between us escalated beyond baseline.",
                    "We talked, but nothing propagated.",
                    "I’m stepping away."
                }
            },
            {
                ConversationStatus.UnsafeFeeling,
                new[]
                {
                    "Hold on.",
                    "Something about this doesn’t sit within my safety margins.",
                    "I don’t ignore those warnings anymore.",
                    "I’m backing off."
                }
            },
            {
                ConversationStatus.LostRespect,
                new[]
                {
                    "I need to address a change.",
                    "The way you handled this lowered my trust in the process.",
                    "Without trust, I don’t continue.",
                    "This ends here."
                }
            },
            {
                ConversationStatus.Mismatch,
                new[]
                {
                    "I compared how we operate.",
                    "Our patterns don’t align long-term.",
                    "Forcing it would degrade both of us.",
                    "So I’m cutting it clean."
                }
            },
            {
                ConversationStatus.TooFast,
                new[]
                {
                    "Slow down.",
                    "You’re pushing this faster than I can stabilize.",
                    "I don’t commit under pressure.",
                    "If it keeps accelerating, I’m out."
                }
            },
            {
                ConversationStatus.TimeRanOut,
                new[]
                {
                    "I need to wrap this up.",
                    "We had a window, and it closed without resolution.",
                    "I don’t reopen expired timelines.",
                    "I’m moving on."
                }
            },
            {
                ConversationStatus.EnergyDrained,
                new[]
                {
                    "I’m running low.",
                    "This interaction is costing more than it returns.",
                    "I don’t burn cycles unnecessarily.",
                    "I’m stepping back."
                }
            },
            {
                ConversationStatus.ComfortPause,
                new[]
                {
                    "Let’s pause this.",
                    "We’ve hit a flat zone.",
                    "Distance might reset the signal.",
                    "I’ll check back later—if it makes sense."
                }
            },
            {
                ConversationStatus.Overstimulated,
                new[]
                {
                    "That’s enough.",
                    "Too many inputs, too little structure.",
                    "I won’t let this turn into noise.",
                    "I’m disconnecting now."
                }
            }
        }

    };

    #endregion

}

public static class Extensions
{
    public static int OccurenceOf(this string[] array, params string[] keys)
    {
        int occ = 0;

        foreach (var s in array)
        {
            foreach (var k in keys)
            {
                if (s == k)
                {
                    occ++;
                }
            }
        }

        return occ;
    }
}