using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class UiButton : MonoBehaviour, IClickable
{
    public bool Interactable = true;

    [SerializeField] Transform _buttonContent;
    [SerializeField] UnityEvent _onClickEvent;

    private bool _isHovered = false;
    private bool _clickEffectActive = false;
    private Coroutine _clickEffectRoutine;

    public void OnClick() 
    {
        _onClickEvent.Invoke();
    }

    void Update()
    {
        if (!Interactable)
        {
            if (_clickEffectRoutine != null)
            {
                StopCoroutine( _clickEffectRoutine );
                _clickEffectRoutine = null;
                _clickEffectActive = false;
            }

            var newSize = _buttonContent.localScale.x - Time.deltaTime;
            _buttonContent.localScale = Vector3.one * Mathf.Clamp(newSize, 1, 2);

            return;
        }

        // update button size
        if (!_clickEffectActive)
        {
            const float maxHoverSize = 1.1f;

            var newSize = _buttonContent.localScale.x;
            newSize += Time.deltaTime * (_isHovered ? 1 : -1);

            _buttonContent.localScale = Vector3.one * Mathf.Clamp(newSize, 1, maxHoverSize);
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
        if (!Interactable) return;

        if (_clickEffectRoutine != null)
        {
            StopCoroutine( _clickEffectRoutine );
        }
        _clickEffectRoutine = StartCoroutine(ApplyClickEffect());

        OnClick();
    }

    private IEnumerator ApplyClickEffect()
    {
        _clickEffectActive = true;

        var initialScale = _buttonContent.localScale;
        var peakScale = new Vector3(0.93f, 1.3f, 1f);
        var finalScale = Vector3.one * 1.1f;

        const float duration = 0.1f;

        var startTime = Time.time;
        var elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            yield return null;
            elapsedTime = Time.time - startTime;

            _buttonContent.localScale = Vector3.Lerp(initialScale, peakScale, EaseFunctions.EaseOut(elapsedTime/duration));
        }

        startTime = Time.time;
        elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            yield return null;
            elapsedTime = Time.time - startTime;

            _buttonContent.localScale = Vector3.Lerp(peakScale, finalScale, EaseFunctions.EaseIn(elapsedTime / duration));
        }

        _clickEffectActive = false;
    }

}
