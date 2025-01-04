using UnityEngine;
using TMPro;
using Fragsurf.Movement;
using UnityEngine.UI;

public class Stats : MonoBehaviour
{
    public SurfCharacter surfCharacter; 
    //public TextMeshProUGUI percentageText; 
    public TextMeshProUGUI currentSpeedText;
    public Image bar;
    // Image contour;


    void Update()
    {
        int vitesse = Mathf.RoundToInt(surfCharacter.currentSpeed);
        int percentage = Mathf.Clamp(surfCharacter.percentage, 0, 100);

   


        //percentageText.text = $"Wallride : {percentage} %";
        currentSpeedText.text = $"Speed: {vitesse}";
        bar.fillAmount = surfCharacter.percentage / 100f;
        //contour.fillAmount = surfCharacter.percentage / 100f;

        //Color color = contour.color;
        //color.a = percentage / 100f; 
        //contour.color = color;

        if (surfCharacter.percentage == 0)
        {
            bar.fillAmount = 0;
            //color.a = 0;
            //contour.color = color;
        }

    }

}