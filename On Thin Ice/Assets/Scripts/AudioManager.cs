using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioSource clinkSound; 

    public void PlayClinkSound() //m�thode pour lancer le son
    {
        if (clinkSound != null)
        {
            clinkSound.Play(); 
        }
    }
}