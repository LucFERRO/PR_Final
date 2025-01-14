using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

public class snowBallHit : MonoBehaviour
{

    public string snowballEvent;

    private void Start()
    {
        StartCoroutine(delay());
        RuntimeManager.PlayOneShot(snowballEvent);

    }

    IEnumerator delay()
    {
        yield return new WaitForSeconds(0.5f);
        //RuntimeManager.PlayOneShot(snowballEvent);
    }

}
