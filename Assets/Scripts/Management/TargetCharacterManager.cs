using UnityEngine;

public class TargetCharacterManager : MonoBehaviour
{
    public static TargetCharacterManager Instance { get; private set; }
    public int CurrentCharacterIndex => _currentCharacter;
    public string CurrentCharacterName => new string[] { "Modern", "Caveman", "Viking", "Janissary", "Cyborg" }[_currentCharacter];

    [SerializeField] GameObject _particlesObject;
    [SerializeField] SpriteRenderer _characterSpriteRenderer;
    [SerializeField] Sprite[] _characterSprites;
    private ParticleSystem _heartParticles;
    private int _currentCharacter = 0;
    private float _initialHeartEmission;

    public void SwitchToNextCharacter()
    {
        _currentCharacter = (_currentCharacter + 1) % _characterSprites.Length;
        _characterSpriteRenderer.sprite = _characterSprites[_currentCharacter];

        GameplayStateManager.Instance.TargetEmotionalStatus.ResetStatus();
    }

    public void ShowHideCharacter(bool show)
    {
        _characterSpriteRenderer.enabled = show;
        _particlesObject.SetActive(show);
        StartHeartsEmitting();
    }

    public void StopHeartsEmitting()
    {
        var em = _heartParticles.emission;
        em.rateOverTimeMultiplier = 0;
    }
    public void StartHeartsEmitting()
    {
        var em = _heartParticles.emission;
        em.rateOverTimeMultiplier = _initialHeartEmission;
    }

    private void Awake()
    {
        Instance = this;
        GameplayStateManager.Instance.TargetEmotionalStatus.ResetStatus();

        _heartParticles = _particlesObject.GetComponent<ParticleSystem>();
        _initialHeartEmission = _heartParticles.emission.rateOverTimeMultiplier;
    }


}
