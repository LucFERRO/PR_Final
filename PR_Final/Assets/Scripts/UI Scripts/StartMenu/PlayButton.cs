using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayButton : MonoBehaviour
{

    public GameObject fadeIn;

    public void PlayGame()
    {
        fadeIn.GetComponent<Animation>().Play("FadeIn");
        StartCoroutine(toGameScene());
    }

    IEnumerator toGameScene()
    {
        yield return new WaitForSeconds(1);
        SceneManager.LoadSceneAsync("Game");
    }

}
