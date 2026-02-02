using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndingScene : MonoBehaviour
{
    public enum EndingType
    {
        NoCards,
        Modern, Caveman, Viking, Janissary, Cyborg,
    }
    public static EndingType Ending;

    [SerializeField] Sprite[] _endingSprites;
    [SerializeField] SpriteRenderer _endingImageRenderer;
    [SerializeField] SpriteRenderer _flashImageRenderer;
    [SerializeField] TextMeshPro _endingLabel;
    [SerializeField] Camera _camera;
    [SerializeField] AudioSource _musicSource;
    [SerializeField] AudioClip[] _musicClips;
    private bool _isPlaying = false;
    private float _startTime;
    private float _clipLength;

    private readonly string[] _labels = { "FOREVER ALONE", "JUST MARRIED!", "TWO HEARTS, ONE HEARTH!", "BOUND BY THE OATH!", "MASHALLAH!", "LINKED FOR ETERNITY!" };

    void Start()
    {
        _endingImageRenderer.sprite = _endingSprites[(int)Ending];
        _endingLabel.SetText(_labels[(int)Ending]);
        _musicSource.clip = _musicClips[(int)Ending];
        _musicSource.Play();

        _startTime = Time.time;
        _clipLength = _musicClips[(int)Ending].length;

        StartCoroutine(EndingZoomOut());
    }

    void Update()
    {
        if ((Input.GetMouseButtonDown(0) && !_isPlaying) || (Time.time - _startTime > (_clipLength + 2f)))
        {
            // back to main menu
            SceneManager.LoadScene("TitleScene");
        }

        if (Input.GetKeyDown(KeyCode.Backspace) && Input.GetKey(KeyCode.RightShift))
        {
            Ending++;
            Ending = (EndingType)((int)Ending % 6);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private IEnumerator EndingZoomOut()
    {
        _camera.transform.eulerAngles = new Vector3(0, 0, -10);
        _camera.orthographicSize = 5.4f;

        _isPlaying = true;

        const float maxDuration = 10f;
        float elapsedTime = 0;
        while (elapsedTime < maxDuration)
        {
            elapsedTime = Time.time - _startTime;
            yield return null;

            if (elapsedTime < 0.5f)
            {
                _flashImageRenderer.color = Color.white * (0.5f - elapsedTime) * 2;
            }

            float t = EaseCubicOut(elapsedTime / maxDuration);

            _camera.transform.eulerAngles = new Vector3(0, 0, Mathf.Lerp(-10, 0, t));
            _camera.orthographicSize = Mathf.Lerp(5.4f, 7.2f, t);

            _endingLabel.transform.position = Vector3.up * Mathf.Lerp(-9.6f, -5.6f, t);
        }

        _isPlaying = false;
    }

    private float EaseCubicOut(float t)
    {
        float c = (1 - t);
        return 1 - c * c * c;
    }
}
