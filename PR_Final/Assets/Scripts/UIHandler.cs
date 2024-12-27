using UnityEngine;
using TMPro;

public class UIHandler : MonoBehaviour
{
    public TextMeshProUGUI distanceText; 
    public AudioManager audioManager;    

    void Start()
    {
        if (audioManager == null)
        {
            audioManager = FindObjectOfType<AudioManager>();
            Debug.Log("ASSIGNE L'AUDIOMANAGER GROS");
        }
    }

    public void ShowDistanceMessage(float distance)
    {
        distanceText.text = $"{distance} m�tres parcourus !";
        distanceText.enabled = true;

 
        if (audioManager != null)
        {
            audioManager.PlayClinkSound(); 
        }

        Invoke("HideDistanceMessage", 2f); //pour appeler la fonction qui masque apr�s 2s
    }

    private void HideDistanceMessage()
    {
        distanceText.enabled = false; 
    }
}