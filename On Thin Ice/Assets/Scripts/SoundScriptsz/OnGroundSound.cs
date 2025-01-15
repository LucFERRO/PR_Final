using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using Fragsurf.Movement;
using UnityEngine;

public class OnGroundSound : MonoBehaviour
{
    public SurfCharacter surfCharacter;
    public StudioEventEmitter eventEmitter;
    private bool isPlaying;

    void Update()
    {

        if (surfCharacter.grounded && surfCharacter.currentSpeed > 0)
        {
            if (!isPlaying)
            {
                eventEmitter.Play();
                isPlaying = true;
            }
        }
        else
        {
            if (isPlaying)
            {
                
                eventEmitter.Stop();
                isPlaying = false;
            }
        }



    }


  




}
