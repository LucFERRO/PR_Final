using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour
{
    public void TeleportToClosestWall(Camera cam, Vector3 playerPosition)
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray);

        float closestDistance = Mathf.Infinity; // Distance la plus proche trouvée
        Vector3 closestPoint = Vector3.zero; // Point de destination pour la téléportation

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.CompareTag("tpWall")) // Vérifie si le mur a le tag "tpWall"
            {
                float distance = Vector3.Distance(playerPosition, hit.point);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPoint = hit.point; // Mise à jour du point le plus proche
                }
            }
        }

        if (closestDistance < Mathf.Infinity) // Si un mur a été trouvé
        {
            // Téléporte le joueur au point de contact
            // Assume que le joueur est un Rigidbody, ajustez selon votre besoin
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = closestPoint; // Téléporte le joueur
            }
        }
    }
}