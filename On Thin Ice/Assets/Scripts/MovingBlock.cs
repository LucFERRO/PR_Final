using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MovingBlock : MonoBehaviour
{
    public float speed = 10f;
    public Vector3 moveDirection;
    private Vector3 startingPos;
    private Vector3 movementVector;
    private Vector3 prevPos;
    [HideInInspector]
    public Vector3 deltaPos;
    private float xFluctuation;
    private float yFluctuation;
    private float zFluctuation;
    private bool xFluctuationBool;
    private bool yFluctuationBool;
    private bool zFluctuationBool;

    private void Start()
    {
        startingPos = transform.position;
        prevPos = transform.position;
        xFluctuationBool = moveDirection.x != 0;
        yFluctuationBool = moveDirection.y != 0;
        zFluctuationBool = moveDirection.z != 0;
    }

    void Update()
    {
        //POURQUOI NE MARCHE PAS ??
        //xFluctuation = movementVector.x != 0 ? Mathf.PingPong(Time.time * speed, moveDirection.x) : 0;
        //yFluctuation = movementVector.y != 0 ? Mathf.PingPong(Time.time * speed, moveDirection.y) : 0;
        //zFluctuation = movementVector.z != 0 ? Mathf.PingPong(Time.time * speed, moveDirection.z) : 0;
        //movementVector = new Vector3(xFluctuation, yFluctuation, zFluctuation);


        movementVector = new Vector3(Mathf.Sign(moveDirection.x) * Mathf.PingPong(Time.time * speed, Mathf.Abs(moveDirection.x)), Mathf.Sign(moveDirection.y) * Mathf.PingPong(Time.time * speed, Mathf.Abs(moveDirection.y)), Mathf.Sign(moveDirection.z) * Mathf.PingPong(Time.time * speed, Mathf.Abs(moveDirection.z)));

        if (!xFluctuationBool)
            movementVector.x = 0;
        if (!yFluctuationBool)
            movementVector.y = 0;
        if (!zFluctuationBool)
            movementVector.z = 0;

        transform.position = startingPos + movementVector;

        deltaPos = transform.position - prevPos;
        prevPos = transform.position;
    }
}