using LLMUnity;
using System.Collections;
using UnityEngine;

public class TitleScreenAnimations : MonoBehaviour
{
    [SerializeField] Sprite[] _backgroundSprites;
    [SerializeField] SpriteRenderer _backgroundRenderer;
    [SerializeField] SpriteRenderer _secondaryBackgroundRenderer;
    [SerializeField] Camera _camera;
    [SerializeField] Transform _titleTransform;
    [SerializeField] Transform _buttonsTransform;

    private WaitForSeconds _sleep = new WaitForSeconds(6f);

    void Start()
    {
        StartCoroutine(ZoomOut());
        StartCoroutine(ChangeBackground());

        var llm = GetComponent<LLM>();
    }

    IEnumerator ZoomOut()
    {
        float startTime = Time.time;
        float elapsedTime = 0;
        while (elapsedTime < 2)
        {
            elapsedTime = Time.time - startTime;
            yield return null;

            float t = 1 - (1 - elapsedTime * .5f) * (1 - elapsedTime * .5f);
            _camera.transform.position = Vector3.Lerp(new Vector3(8, -3, -10), new Vector3(0, 0, -10), t);
            _camera.orthographicSize = Mathf.Lerp(2, 8, t);

            if (t > 0.5f)
            {
                float t2 = 2 * (t - 0.5f);
                _titleTransform.position = Vector3.Lerp(new Vector3(0, 12, -5), new Vector3(0, 4, -5), t2);
                _buttonsTransform.position = Vector3.up * Mathf.Lerp(-8, 0, t2);
            }
        }
    }

    IEnumerator ChangeBackground()
    {
        _secondaryBackgroundRenderer.sprite = _backgroundRenderer.sprite;
        _secondaryBackgroundRenderer.color = Color.white;
        _backgroundRenderer.sprite = _backgroundSprites[Random.Range(0, _backgroundSprites.Length)];

        float startTime = Time.time;
        float elapsedTime = 0;
        while (elapsedTime < 1)
        {
            elapsedTime = Time.time - startTime;
            yield return null;

            _secondaryBackgroundRenderer.color = new Color(1, 1, 1, 1 - elapsedTime);
        }

        yield return _sleep;
        StartCoroutine(ChangeBackground());
    }
}
