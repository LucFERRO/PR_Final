using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TpPreviewRotate : MonoBehaviour
{
    public float rotateSpeed;
    public Vector3 prevPos;
    public Vector3 deltaPos;
    private Rigidbody rb;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.ResetInertiaTensor();
    }

    // Update is called once per frame
    void Update()
    {
        //transform.eulerAngles = Vector3.Lerp(transform.eulerAngles, transform.eulerAngles + deltaPos, Time.deltaTime);
        transform.position = transform.localPosition - prevPos;
        deltaPos = transform.localPosition - prevPos;
        prevPos = transform.localPosition;
    }
}
