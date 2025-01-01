using Fragsurf.Movement;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
public class CameraQuaternions : MonoBehaviour
{
    Vector2 rotation;
    float yRotationLimit = 90;
    public float tiltAngle;
    public Transform bodyTransform;
    public SurfCharacter character;
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    void Update()
    {
        rotation.x += Input.GetAxisRaw("Mouse X");
        rotation.y += Input.GetAxisRaw("Mouse Y");
        rotation.y = Mathf.Clamp(rotation.y, -yRotationLimit, yRotationLimit);
        var xQuat = Quaternion.AngleAxis(rotation.x, Vector3.up);
        var yQuat = Quaternion.AngleAxis(rotation.y, Vector3.left);
        transform.localRotation = xQuat * yQuat;

        float wallrunTilt = character._moveData.wallRunning ? tiltAngle : 0;
        bodyTransform.rotation = Quaternion.Euler(0, rotation.x, 0);
    }

    //float WallrunTilt(bool rightOrLeft)
    //{

    //}
}