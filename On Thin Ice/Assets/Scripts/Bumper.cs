using Fragsurf.Movement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bumper : MonoBehaviour
{
    public SurfCharacter player;
    public GameObject bumper;
    public float bounceCoef;

    private void OnTriggerEnter(Collider other)
    {
        Vector3 bumpedSpeed = player._moveData.velocity;
        bumpedSpeed.y = -bumpedSpeed.y * bounceCoef;
        player._moveData.velocity = bumpedSpeed;
        bumper.GetComponent<Animation>().Play("Armature_ArmatureAction");
    }
}
