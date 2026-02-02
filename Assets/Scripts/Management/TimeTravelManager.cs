using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TimeTravelManager : MonoBehaviour
{
    [SerializeField] ScriptedSpeechManager _scriptedSpeechManager;
    [SerializeField] DissolveEffect _curtainDissolveEffect;
    [SerializeField] Animator _clockAnimator;
    private AudioSource _audioSource;

    private readonly ScriptedSpeechManager.ScriptedSpeech _encouterCavemanScript = new( -1.5f,
        "HOLY HELL!",
        "Is that a man or a geological event?",
        "I need that prehistoric, dense, glorious slab of muscle to immediately crush every single one of my reservations!");

    private readonly ScriptedSpeechManager.ScriptedSpeech _encouterVikingScript = new(-1.5f,
        "BY THE GODS!",
        "That man is a raid I want to lose!",
        "Who allowed that magnificent, shining, goddamn specimen of Nordic perfection to exist outside of my fever dreams?!");

    private readonly ScriptedSpeechManager.ScriptedSpeech _encouterJanissaryScript = new(-1.5f,
        "ASTAGHFIRULLAH!",
        "That exquisite man is wearing more wealth than I'll see in a lifetime...",
        "...but his face is the true luxury item!",
        "I am collapsing from the sheer, tailored majesty!");

    private readonly ScriptedSpeechManager.ScriptedSpeech _encouterCyborgScript = new(-1.5f,
        "WHOA!",
        "That's not just a man, that's a whole damn rave!",
        "His chassis is flawless, his lights are blinding!",
        "I need to interface with that futuristic, perfect hardware right now!");

    private readonly ScriptedSpeechManager.ScriptedSpeech _encouterModernScript = new(-1.5f,
        "God! Finally the timeline i wanted to get to!",
        "And GOD! He's here too...",
        "Still him. Still perfect.",
        "I need that man gone—straight into my bed—before I break something.");

    private readonly string[] initialSentences = {
        "My bad, I zeroed the flux capacitor wrong. Where in time am I stuck now?",
        "Damn it, the chronometer is completely off! Where the hell did I land?",
        "Ugh, the setting was wrong. I completely screwed the time jump. What year is it?",
        "Crap! I botched the temporal coordinates; what godforsaken era is this?"
    };

    public void Travel()
    {
        _scriptedSpeechManager.StartScript(new ScriptedSpeechManager.ScriptedSpeech( -1.5f,
            "I FAILED! AGAIN!",
            "I am utterly worthless!",
            "At least I can go back in time to shoot my shot again?"
            ), () => StartCoroutine(CurtainDissolveEffect( () => 
            {
                TargetCharacterManager.Instance.SwitchToNextCharacter();
                TargetCharacterManager.Instance.ShowHideCharacter(false);
            }, () =>
            {
                var randomSentence = initialSentences[UnityEngine.Random.Range(0, initialSentences.Length)];
                _scriptedSpeechManager.StartScript(new ScriptedSpeechManager.ScriptedSpeech(0, randomSentence), () =>
                {
                    TargetCharacterManager.Instance.ShowHideCharacter(true);

                    var newScript = TargetCharacterManager.Instance.CurrentCharacterIndex switch
                    {
                        0 => _encouterModernScript,
                        1 => _encouterCavemanScript,
                        2 => _encouterVikingScript,
                        3 => _encouterJanissaryScript,
                        4 => _encouterCyborgScript,
                        _ => _encouterModernScript,
                    };

                    _scriptedSpeechManager.Whistle();
                    _scriptedSpeechManager.StartScript(newScript, () =>
                    {
                        GameplayStateManager.Instance.RequestStateChange(GameplayStateManager.GameplayState.PlayersTurn);
                    });
                });
            })));
        GameplayStateManager.Instance.RequestStateChange(GameplayStateManager.GameplayState.ScriptedSpeech);
    }

    public void FinishGame()
    {
        _scriptedSpeechManager.StartScript(new ScriptedSpeechManager.ScriptedSpeech(-1.5f,
            "FUCK!!!",
            "I've burned through every goddamn timeline and still couldn't pull a single one of these jacked gorgeouses!",
            "I have no more cards to play, the clock has finally run out...",
            "...and I'm right where I belong—utterly alone and worth nothing.",
            "I'm returning home..."
            ), () => StartCoroutine(CurtainDissolveEffect(() =>
            {
                EndingScene.Ending = EndingScene.EndingType.NoCards;
                SceneManager.LoadScene("EndingScene");
            }, () =>
            {
                Debug.Log("Game Over Animation End!");
            })));
        GameplayStateManager.Instance.RequestStateChange(GameplayStateManager.GameplayState.ScriptedSpeech);
    }

    private IEnumerator CurtainDissolveEffect(Action onTravel, Action onComplete)
    {
        _curtainDissolveEffect.gameObject.SetActive(true);
        _curtainDissolveEffect.DissolveAmount = 0;

        _audioSource.Play();

        var startTime = Time.time;
        var elapsedTime = 0f;
        while (elapsedTime < 1)
        {
            yield return null;
            elapsedTime = Time.time - startTime;

            _curtainDissolveEffect.DissolveAmount = 1 - elapsedTime;
        }

        _clockAnimator.Play("TimeTravelClock", -1);

        yield return new WaitForSeconds(3f);
        onTravel?.Invoke();

        startTime = Time.time;
        elapsedTime = 0f;
        while (elapsedTime < 1)
        {
            yield return null;
            elapsedTime = Time.time - startTime;

            _curtainDissolveEffect.DissolveAmount = elapsedTime;
        }

        _curtainDissolveEffect.gameObject.SetActive(true);

        onComplete?.Invoke();
    }

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }

}
