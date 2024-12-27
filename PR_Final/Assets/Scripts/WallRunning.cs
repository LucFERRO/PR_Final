using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallRunning : MonoBehaviour
{
    [Header("Inputs")]
    float horizontalInput;
    float verticalInput;

    [Header("Detection")]
    public float wallCheckDistance;
    public float minJumpHeight;
    private RaycastHit rightWallhit;
    private RaycastHit leftWallhit;
    private bool wallRight;
    private bool wallLeft;
    private bool grounded;
    private bool stuckToWall;


    [Header("Wallrunning")]
    public LayerMask whatIsGround;
    public LayerMask whatIsWall;

    [Header("References")]
    public Transform orientation;
    public Rigidbody rb;
    private PlayerActions playerMovement;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerMovement = GetComponent<PlayerActions>();
        grounded = playerMovement.grounded;
        //stuckToWall = playerMovement.stuckToWall;
    }

    void Update()
    {
        CheckForWall();
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if ((wallLeft || wallRight) && verticalInput > 0 && !grounded)
        {
            if (!playerMovement.wallRunning)
                StartWallRunning();
        }

        else
            if (playerMovement.wallRunning)
            StopWallRunning();

        //if (!playerMovement.stuckToWall)
        //{
        //    StickToWall();
        //}
    }

    private void FixedUpdate()
    {
        if (playerMovement.wallRunning)
            WallRunningMovement();
    }

    private void CheckForWall()
    {
        wallRight = Physics.Raycast(transform.position, orientation.right, out rightWallhit, wallCheckDistance, whatIsWall);
        wallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWallhit, wallCheckDistance, whatIsWall);
    }

    //private void StickToWall()
    //{

    //    if (Input.GetKeyDown(KeyCode.Space) && (wallLeft || wallRight))
    //    {
    //        Vector3 wallNormal = wallRight ? rightWallhit.normal : leftWallhit.normal;
    //        Debug.Log(wallNormal);
    //        playerMovement.stuckToWall = true;
    //        playerMovement.wallRunning = false;
    //    }
    //}

    private void StartWallRunning()
    {
        rb.useGravity = false;
        playerMovement.wallRunning = true;
    }
    private void StopWallRunning()
    {
        playerMovement.wallRunning = false;
        rb.useGravity = true;
    }
    //private void StartStickingToWall()
    //{
    //    playerMovement.stuckToWall = true;
    //    rb.useGravity = false;
    //}
    //private void StopStickingToWall()
    //{
    //    playerMovement.stuckToWall = false;
    //    rb.useGravity = true;
    //}
    private void WallRunningMovement()
    {

        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        Vector3 wallNormal = wallRight ? rightWallhit.normal : leftWallhit.normal;
        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

        //IMPORTANT sinon slide en reverse sur certains walls selon leur forward. Si vous voulez test de commenter la ligne et wallrun sur le premier black, ça va toujours slide a droite
        if ((orientation.forward - wallForward).magnitude > (orientation.forward - -wallForward).magnitude)
            wallForward = -wallForward;

        rb.AddForce(wallForward * playerMovement.movementSpeed, ForceMode.Force);
    }

    public void WallJump()
    {
        rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        Vector3 wallNormal = wallRight ? rightWallhit.normal : leftWallhit.normal;
        rb.AddForce((transform.up + wallNormal) * playerMovement.jumpForce * 2f, ForceMode.Impulse);
    }
}
