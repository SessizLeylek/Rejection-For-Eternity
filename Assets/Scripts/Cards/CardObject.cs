using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public class CardObject : MonoBehaviour, IClickable
{
    public bool IsSelected => _isSelected;
    public Card ThisCard => _thisCard;

    [SerializeField] Transform _cardContent;
    [SerializeField] SpriteRenderer _cardBackground;
    [SerializeField] SpriteRenderer _cardImage;
    [SerializeField] TextMeshPro _cardText;

    private Card _thisCard;
    private const float HOVER_SLIDE_SPEED = 3.0f;
    private const float MAX_CONTENT_HEIGHT = 0.5f;
    private bool _isHovered = false;
    private bool _isSelected = false;
    private Coroutine _clickEffectRoutine;

    public void Initialize(Card card, Color cardColor, Sprite cardImage = null)
    {
        _thisCard = card;
        _cardBackground.color = cardColor;
        _cardText.SetText(card.Name);

        if (_cardImage != null)
        {
            _cardImage.sprite = cardImage;
        }
    }

    public void Dissolve()
    {
        GetComponent<BoxCollider2D>().enabled = false;
        StartCoroutine(DissolveEffect());
    }

    public void MoveCardsTowards(Vector3 newPosition, float duration, bool updateOrientation = false, Action moveEndCallback = null)
    {
        StartCoroutine(MoveCardsRoutine(newPosition, duration, updateOrientation, moveEndCallback));
    }

    private void Start()
    {
    }

    private void UpdateContentHeight(float desiredHeight, float desiredDepth = 0f)
    {
        var initialHeight = _cardContent.localPosition.y;
        var slideAmount = Mathf.Sign(desiredHeight - _cardContent.localPosition.y);
        var newHeightUnclamped = _cardContent.localPosition.y + Time.deltaTime * HOVER_SLIDE_SPEED * slideAmount;
        var newHeight = Mathf.Clamp(newHeightUnclamped, Mathf.Min(initialHeight, desiredHeight), Mathf.Max(initialHeight, desiredHeight));
        _cardContent.localPosition = new Vector3(0, newHeight, desiredDepth);
    }

    private void Update()
    {
        if (GameplayStateManager.Instance.State != GameplayStateManager.GameplayState.PlayersTurn) return;

        if (_isSelected)
        {
            UpdateContentHeight(MAX_CONTENT_HEIGHT * 1.8f, -1);
        }
        else if (_isHovered)
        {
            UpdateContentHeight(MAX_CONTENT_HEIGHT, -2);
        }
        else
        {
            UpdateContentHeight(0);
        }
    }

    public GameObject GetGameObject()
    {
        return gameObject;
    }

    public void OnPointerEnter()
    {
        _isHovered = true;
    }

    public void OnPointerExit()
    {
        _isHovered = false;
    }

    public void OnPointerDown()
    {
        if (GameplayStateManager.Instance.State != GameplayStateManager.GameplayState.PlayersTurn) return;

        _isSelected = !_isSelected;

        if (_clickEffectRoutine != null)
        {
            StopCoroutine(_clickEffectRoutine);
        }
        _clickEffectRoutine = StartCoroutine(ApplyClickEffect(0.1f, 1.1f));

        HandManager.Instance.PlayCardSelectSound();
    }

    private IEnumerator ApplyClickEffect(float duration, float peakSize, float initialSize = 1f)
    {
        var startTime = Time.time;
        var elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            transform.localScale = Vector3.one * Mathf.Lerp(initialSize, peakSize, Mathf.Sin(elapsedTime / duration * Mathf.PI));

            yield return null;
            elapsedTime = Time.time - startTime;
        }
        transform.localScale = Vector3.one * initialSize;
    }

    private IEnumerator DissolveEffect()
    {

        var startTime = Time.time;
        var elapsedTime = 0f;

        var dissolvers = GetComponentsInChildren<DissolveEffect>();

        while (elapsedTime < 1)
        {
            yield return null;
            elapsedTime = Time.time - startTime;

            foreach (var dissolveEffect in dissolvers)
            {
                dissolveEffect.DissolveAmount = elapsedTime;
            }

            if (elapsedTime > 0.5f)
            {
                _cardText.gameObject.SetActive(false);
            }
        }

        Destroy(gameObject);
    }

    private IEnumerator MoveCardsRoutine(Vector3 newPosition, float duration, bool updateOrientation, Action moveEndCallback)
    {
        var initialPosition = transform.position;
        var initialRotation = transform.rotation;
        var newRotation = transform.rotation;
        var deltaPosition = newPosition - initialPosition;
        if (updateOrientation)
        {
            newRotation = Quaternion.LookRotation(Vector3.forward, deltaPosition.normalized);
        }

        var startTime = Time.time;
        var elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            yield return null;
            elapsedTime = Time.time - startTime;

            var t = EaseFunctions.EaseOut(elapsedTime / duration);
            var nextPosition = Vector3.Lerp(initialPosition, newPosition, t);
            var nextRotation = Quaternion.Lerp(initialRotation, newRotation, t);
            transform.position = nextPosition;
            transform.rotation = nextRotation;
        }

        moveEndCallback?.Invoke();
    }
}
