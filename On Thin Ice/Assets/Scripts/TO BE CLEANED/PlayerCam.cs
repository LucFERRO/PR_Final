using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCam : MonoBehaviour
{
    public float sensX;
    public float sensY;
    public float multiplier;

    public Transform orientation;
    public Transform camHolder;

    public Rigidbody rb;

    float xRotation;
    float yRotation;

    private float curAccel = 10f;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * sensX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * sensY;

        yRotation += mouseX * multiplier;

        xRotation -= mouseY * multiplier;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        camHolder.position = transform.position;
        camHolder.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        orientation.rotation = Quaternion.Euler(0, yRotation, 0);

        //rb.velocity = CalculateMovement(new Vector2(Input.GetAxis("Horizontal"),Input.GetAxis("Vertical")), rb.velocity);

        //Debug.Log("xRotation " + xRotation);
        //if (Mathf.Abs(xRotation) > 45f)
        //{
        //    Debug.Log("VIRAGE");
        //    Vector3 newVelocity = Quaternion.Euler(xRotation, yRotation, 0) * rb.velocity;
        //    Vector3 currentVelocity = rb.velocity; // get the current velocity
        //    rb.AddForce(newVelocity - currentVelocity, ForceMode.VelocityChange); // add the difference.
        //}



        //rb.velocity = Quaternion.Euler(xRotation, yRotation, 0) * rb.velocity;

        //Faire quelque chose de ça
        //Quaternion.LookRotation(velocity)
    }

    private Vector3 CalculateMovement(Vector2 input, Vector3 velocity)
    {

        //Get rotation input and make it a vector
        Vector3 camRotation = new Vector3(0f, camHolder.transform.rotation.eulerAngles.y, 0f);
        Vector3 inputVelocity = Quaternion.Euler(camRotation) * new Vector3(input.x * curAccel, 0f, input.y * curAccel);

        //Ignore vertical component of rotated input
        Vector3 alignedInputVelocity = new Vector3(inputVelocity.x, 0f, inputVelocity.z) * Time.deltaTime;

        //Get current velocity
        Vector3 currentVelocity = new Vector3(velocity.x, 0f, velocity.z);

        //How close the current speed to max velocity is (1 = not moving, 0 = at/over max speed)
        //float max = Mathf.Max(0f, 1 - (currentVelocity.magnitude / curMaxSpeed));

        //How perpendicular the input to the current velocity is (0 = 90°)
        float velocityDot = Vector3.Dot(currentVelocity, alignedInputVelocity);

        //Scale the input to the max speed
        Vector3 modifiedVelocity = alignedInputVelocity * 1.2f;

        //The more perpendicular the input is, the more the input velocity will be applied
        Vector3 correctVelocity = Vector3.Lerp(alignedInputVelocity, modifiedVelocity, velocityDot);


        //Return
        return correctVelocity;
    }
}