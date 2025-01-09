using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public float rotationSpeed;
    float horizontalInput;
    float verticalInput;

    [Header("References")]
    public Transform orientation;
    public Transform player;
    public Transform playerObj;
    public Rigidbody rb;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        Vector3 viewDirection = player.position - new Vector3(transform.position.x, player.position.y, transform.position.z);
        orientation.forward = viewDirection.normalized;
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");
        Vector3 inputDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        if (inputDirection != Vector3.zero) playerObj.forward = Vector3.Slerp(playerObj.forward, inputDirection.normalized, rotationSpeed * Time.deltaTime);
    }
}
