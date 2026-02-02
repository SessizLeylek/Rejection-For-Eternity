using LLMUnity;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScreenManager : MonoBehaviour
{
    private bool _sceneLoading = false;

    public void StartGame()
    {
        if (_sceneLoading) return;

        SceneManager.LoadSceneAsync("GameplayScene");
    }

    public void QuitGame()
    {
        if (_sceneLoading) return;

        Application.Quit();
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.RightShift))
        {
            if (Input.GetKey(KeyCode.Alpha1))
            {
                EndingScene.Ending = EndingScene.EndingType.NoCards;
                SceneManager.LoadScene("EndingScene");
            }
            else if (Input.GetKey(KeyCode.Alpha2))
            {
                EndingScene.Ending = EndingScene.EndingType.Modern;
                SceneManager.LoadScene("EndingScene");
            }
            else if(Input.GetKey(KeyCode.Alpha3))
            {
                EndingScene.Ending = EndingScene.EndingType.Caveman;
                SceneManager.LoadScene("EndingScene");
            }
            else if(Input.GetKey(KeyCode.Alpha4))
            {
                EndingScene.Ending = EndingScene.EndingType.Viking;
                SceneManager.LoadScene("EndingScene");
            }
            else if (Input.GetKey(KeyCode.Alpha5))
            {
                EndingScene.Ending = EndingScene.EndingType.Janissary;
                SceneManager.LoadScene("EndingScene");
            }
            else if (Input.GetKey(KeyCode.Alpha6))
            {
                EndingScene.Ending = EndingScene.EndingType.Cyborg;
                SceneManager.LoadScene("EndingScene");
            }
        }
    }

}
