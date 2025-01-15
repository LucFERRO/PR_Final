using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowBounds : MonoBehaviour
{
    void Start()
    {

    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(gameObject.GetComponent<MeshCollider>().bounds.center, gameObject.GetComponent<MeshCollider>().bounds.size);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
