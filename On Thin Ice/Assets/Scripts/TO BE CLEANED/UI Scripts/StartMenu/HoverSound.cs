using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

public class HoverSound : MonoBehaviour
{


    public string hoverSoundEvent;
    public string onClickSound;

    public void PlayHoverSound()
    {
            RuntimeManager.PlayOneShot(hoverSoundEvent);
    }

    public void OnClickSound()
    {
        RuntimeManager.PlayOneShot(onClickSound);
    }

}
