using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerActions : MonoBehaviour
{
    float horizontalInput;
    float verticalInput;
    Vector3 moveDirection;

    [Header("References")]
    public Transform orientation;
    public Rigidbody rb;
    public Camera cam;

    [Header("Movement")]
    public float movementSpeed = 10f;
    public float sprintMultiplier = 1.5f;
    public float jumpSpeedMultiplier = 1.1f;
    public float maxSpeed = 10f;
    public float groundDrag;
    public float jumpForce;
    public float jumpCooldown;
    bool readyToJump;
    public bool wallRunning;
    private WallRunning wallActions;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    public LayerMask whatIsWall;
    public bool grounded;

    public float rotationSpeed = 5f;
    private Quaternion targetRotation;

    public float gravity = 9.81f;
    public float smoothStopMultiplier = 5f;

    private float initialMovementSpeed;

    [Header("Teleportation Indicator")]
    public GameObject teleportationIndicatorPrefab;
    public GameObject currentIndicator;
    public MeshRenderer indicatorMesh;
    public float maxTeleportDistance = 50f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        cam = Camera.main;
        rb.freezeRotation = true;
        readyToJump = true;
        wallActions = GetComponent<WallRunning>();

        initialMovementSpeed = movementSpeed;
    }

    private void Update()
    {
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround | whatIsWall);

        horizontalInput = Input.GetAxisRaw("Horizontal") * movementSpeed;
        verticalInput = Input.GetAxisRaw("Vertical") * movementSpeed;

        //if (Input.GetKey(KeyCode.LeftShift))
        //{
        //    horizontalInput *= sprintMultiplier;
        //    verticalInput *= sprintMultiplier;
        //}

        float playerSpeed = rb.velocity.magnitude;

        maxTeleportDistance = Mathf.Max(playerSpeed * 1.1f, 15f);

        MovePlayer();

        if (!wallRunning && !grounded)
        {
            FixVelocity();
        }

        Debug.Log(rb.velocity);

        if (rb.velocity.magnitude > maxSpeed)
        {
            Vector3 clampedVelocity = rb.velocity.normalized * maxSpeed;
            rb.velocity = new Vector3(clampedVelocity.x, rb.velocity.y, clampedVelocity.z);
        }

        if (Input.GetKey(KeyCode.Space) && readyToJump && (grounded || wallRunning))
        {
            readyToJump = false;
            if (grounded) Jump();
            if (wallRunning) wallActions.WallJump();
            Invoke(nameof(ResetJump), jumpCooldown);


            //if (resetSpeedCoroutine != null)
            //{
            //    StopCoroutine(resetSpeedCoroutine);
            //}
            //resetSpeedCoroutine = StartCoroutine(ResetSpeedAfterDelay(2f));
        }

        TpPreview();

    }

    private void FixedUpdate()
    {

        if (!grounded)
        {
            rb.AddForce(-Vector3.up * gravity, ForceMode.Acceleration);
        }

    }

    private void FixVelocity()
    {
        float accelWish = Vector3.Dot(rb.velocity, orientation.forward);

        Vector3 wishVel = rb.velocity;
        wishVel = orientation.forward * accelWish;
        rb.velocity = new Vector3(wishVel.x, rb.velocity.y, wishVel.z);
    }

    private void MovePlayer()
    {
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
        rb.AddForce(moveDirection.normalized * movementSpeed, ForceMode.Force);

    }

    private void Jump()
    {
        rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);

        Vector3 jumpVector = verticalInput > 0 ? transform.up + orientation.forward : transform.up;
        //Vector3 jumpVector = transform.up;

        rb.AddForce(jumpVector.normalized * jumpForce, ForceMode.Impulse);
        maxSpeed *= jumpSpeedMultiplier;

    }

    private void WallJump()
    {
        rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        rb.AddForce((transform.up + transform.right) * jumpForce * 2f, ForceMode.Impulse);

        movementSpeed *= jumpSpeedMultiplier;
    }

    private void ToggleTpPreview()
    {
        indicatorMesh.enabled = !indicatorMesh.enabled;
    }

    private void TpPreview()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;


        if (Physics.Raycast(ray, out hit, maxTeleportDistance))
        {
            currentIndicator.transform.position = hit.point + hit.normal * 0.5f;
        }
        else
        {
            Vector3 newPos = ray.direction.normalized * maxTeleportDistance;
            currentIndicator.transform.position = transform.position + newPos;
        }

        if (Input.GetMouseButtonDown(0))
            ToggleTpPreview();

        if (Input.GetMouseButtonDown(1) && indicatorMesh.enabled)
            TpFunction();

        rb.drag = (grounded && horizontalInput == 0 && verticalInput == 0) ? groundDrag : 0;

        if (horizontalInput == 0 && verticalInput == 0)
        {
            float currentSpeed = rb.velocity.magnitude;
            float stopRate = currentSpeed * smoothStopMultiplier;
            rb.velocity = Vector3.Lerp(rb.velocity, new Vector3(0, rb.velocity.y, 0), Time.deltaTime * stopRate);
        }
    }

    private void TpFunction()
    {
        Vector3 preTpVelocity = rb.velocity;
        preTpVelocity.x += 1;
        transform.position = currentIndicator.transform.position;
        rb.velocity = preTpVelocity;

        //Fix inertie au tp?
        rb.AddForce(orientation.transform.forward * rb.velocity.magnitude * 2f, ForceMode.Impulse);
        ToggleTpPreview();
    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("wallJump"))
        {
            Vector3 wallNormal = collision.contacts[0].normal;

            float dotProduct = Vector3.Dot(transform.right, wallNormal);
            float rotationAngle = dotProduct > 0 ? 90f : -90f;

            targetRotation = Quaternion.Euler(0f, rotationAngle, 0f) * transform.rotation;

            FindObjectOfType<FollowPlayer>().RotateCameraBasedOnCollision(wallNormal);
        }

        if (collision.gameObject.CompareTag("tpBack"))
        {
            //Reset position if falls
            transform.position = new Vector3(237, 311, -880);
        }
    }
}