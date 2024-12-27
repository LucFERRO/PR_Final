using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioSource clinkSound; 

    public void PlayClinkSound() //méthode pour lancer le son
    {
        if (clinkSound != null)
        {
            clinkSound.Play(); 
        }
    }
}