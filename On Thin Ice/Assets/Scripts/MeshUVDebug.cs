using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshUVDebug : MonoBehaviour
{
    // Start is called before the first frame update
    public Mesh mesh;
    public Vector3[] vertices;
    public int vertexCount;
    void Start()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        vertices = mesh.vertices;
        vertexCount = vertices.Length;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
