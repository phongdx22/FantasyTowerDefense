using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonsFunctions : MonoBehaviour
{
    public float timeToWait = 5f;
    public bool isLoading = false;

    public void QuitGame()
    {
        Application.Quit();
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    private void Start()
    {
        if (isLoading)
        {
            StartCoroutine(LoadMain());
        }
    }

    private IEnumerator LoadMain()
    {
        yield return new WaitForSeconds(timeToWait);
        SceneManager.LoadSceneAsync("MainMenu");
    }
}
