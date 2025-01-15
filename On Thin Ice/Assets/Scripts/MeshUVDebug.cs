using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshUVDebug : MonoBehaviour
{
    // Start is called before the first frame update
    public Mesh mesh;
    public Vector3[] vertices;
    public int vertexCount;
    public Vector2[] uv;
    void Start()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        vertices = mesh.vertices;
        uv = mesh.uv;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
