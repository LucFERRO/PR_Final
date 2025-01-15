using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideUI : MonoBehaviour
{

    public GameObject StartBouton;
    public GameObject Options;
    public GameObject Quit;
    public GameObject Logo;
    public GameObject Animation;


    void Start()
    {
        StartBouton.SetActive(false);
        Options.SetActive(false);
        Quit.SetActive(false);
        Logo.SetActive(false);

        StartCoroutine(showUI());
        StartCoroutine(desacAnim());
    }

    IEnumerator showUI()
    {
        yield return new WaitForSeconds(0.6f);
        StartBouton.SetActive(true);
        Options.SetActive(true);
        Quit.SetActive(true);
        Logo.SetActive(true);
    }

    IEnumerator desacAnim()
    {
        yield return new WaitForSeconds(1.3f);
        Animation.SetActive(false);
    }


}
