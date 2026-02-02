using LLMUnity;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SetupScene : MonoBehaviour
{
    [SerializeField] GameObject LlmObject;

    private bool _llmObjectSetup = false;

    private void Update()
    {
        if (!_llmObjectSetup && LlmObject.scene.buildIndex == -1 && Input.GetKeyDown(KeyCode.Space))
        {
            _llmObjectSetup=true;

            SceneManager.LoadSceneAsync("TitleScene");
        }
    }
}
