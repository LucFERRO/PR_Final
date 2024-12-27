using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Climbing : MonoBehaviour
{
    [Header("Inputs")]
    float horizontalInput;
    float verticalInput;

    [Header("Detection")]
    public float detectionLength;
    public float sphereCastRadius;
    public float maxWallLookAngle;
    private float wallLookAngle;
    private RaycastHit frontWallHit;
    private bool wallFront;

    [Header("Climbing")]
    public LayerMask whatIsWall;
    public float climbingSpeed;
    private bool climbing;

    [Header("References")]
    public Transform orientation;
    public Rigidbody rb;
    private PlayerActions playerMovement;

    void Update()
    {
        WallCheck();
        if(wallFront && Input.GetAxisRaw("Vertical")==1 && wallLookAngle < maxWallLookAngle)
            StartClimbing();
        else
            StopClimbing();

        if (climbing)
            ClimbingMovement();
    }
    private void StartClimbing()
    {
        climbing = true;
    }
    private void StopClimbing()
    {
        climbing = false;
        rb.useGravity = true;
    }

    private void ClimbingMovement()
    {
        rb.velocity = new Vector3(rb.velocity.x, climbingSpeed, rb.velocity.z); 
    }

    private void WallCheck()
    {
        wallFront = Physics.SphereCast(transform.position, sphereCastRadius, orientation.forward, out frontWallHit, detectionLength, whatIsWall);
        wallLookAngle = Vector3.Angle(orientation.forward, -frontWallHit.normal);
    }
}
