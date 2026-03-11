using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshDeformer : MonoBehaviour
{
    [Header("Parametri deformazione")]
    public float springForce = 50000f;
    public float damping = 15f;
    public float rigidity = 20f; 
    public float sigma = 8f;

    private Mesh deformingMesh;
    private Vector3[] originalVertices, displacedVertices;
    private Vector3[] vertexVelocities;
    private float uniformScale = 1f;

    void Start()
    {
        deformingMesh = GetComponent<MeshFilter>().mesh;
        originalVertices = deformingMesh.vertices;
        displacedVertices = new Vector3[originalVertices.Length];

        for (int i = 0; i < originalVertices.Length; i++)
        {
            displacedVertices[i] = originalVertices[i];
        }
        
        vertexVelocities = new Vector3[originalVertices.Length];
    }

    void Update()
    {
        uniformScale = transform.localScale.x;

        for (int i = 0; i < displacedVertices.Length; i++)
        {
            UpdateVertex(i);
        }

        deformingMesh.vertices = displacedVertices;
        deformingMesh.RecalculateNormals();
    }

    private void UpdateVertex(int i)
    {
        Vector3 velocity = vertexVelocities[i];
        Vector3 displacement = displacedVertices[i] - originalVertices[i];
        displacement *= uniformScale;
        velocity -= displacement * springForce * Time.deltaTime;
        velocity *= 1f - damping * Time.deltaTime;
        vertexVelocities[i] = velocity;
        displacedVertices[i] += velocity * Time.deltaTime;
    }
 
    public void AddDeformingForce(Vector3 point, Vector3 force)
    {
        point = transform.InverseTransformPoint(point);
        force = transform.InverseTransformVector(force);

        for (int i = 0; i < displacedVertices.Length; i++)
        {
            AddForceToVertex(i, point, force);
        }
    }

    void AddForceToVertex(int i, Vector3 point, Vector3 force)
    {
        Vector3 displacement = displacedVertices[i] - originalVertices[i];
        float distance = Vector3.Distance(displacedVertices[i], point);
        float gaussianDecay = LeapMathLibrary.Gaussian(distance, sigma);
        float attenuatedForce = (force.magnitude * gaussianDecay) / (displacement.magnitude * rigidity + 1f);
        float velocity = attenuatedForce * Time.deltaTime;
        vertexVelocities[i] += (force.normalized * velocity);
    }
}
