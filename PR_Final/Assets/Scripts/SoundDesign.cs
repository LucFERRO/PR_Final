using System.Collections;
using System.Collections.Generic;
using Fragsurf.Movement;
using UnityEngine;
using FMODUnity;

public class SoundDesign : MonoBehaviour
{
    public SurfCharacter surfCharacter;
    public StudioEventEmitter eventEmitter;
    private bool isPlaying;

    void Update()
    {
        if (surfCharacter.wallRunning)
        {
            if (!isPlaying)
            {
                eventEmitter.Play();
                isPlaying = true;
                Debug.Log("SON ON");
            }
        }
        else
        {
            if (isPlaying)
            {
                eventEmitter.Stop();
                isPlaying = false;
                Debug.Log("SON OFF");
            }
        }
    }
}