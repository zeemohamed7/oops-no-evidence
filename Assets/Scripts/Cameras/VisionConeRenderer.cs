using UnityEngine;

[RequireComponent(typeof(VisionCone))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class VisionConeRenderer : MonoBehaviour
{
    [Header("Visual Settings")]
    public Material coneMaterial; 
    [Range(3, 60)] public int totalSegments = 30; 

    [Header("Environment Capture")]
    [Tooltip("Set this to Default/Static so the red light stops when it hits walls or floors")]
    public LayerMask environmentObstructionMask;

    private VisionCone _visionData;
    private MeshFilter _meshFilter;
    private Mesh _coneMesh;

    void Start()
    {
        _visionData = GetComponent<VisionCone>();
        _meshFilter = GetComponent<MeshFilter>();
        
        _coneMesh = new Mesh();
        _coneMesh.name = "Vision Cone Mesh";
        _meshFilter.mesh = _coneMesh;

        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (coneMaterial != null) mr.material = coneMaterial;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    void LateUpdate()
    {
        if (!_visionData.enabled)
        {
            _coneMesh.Clear();
            return;
        }

        GenerateConeMesh();
    }

    void GenerateConeMesh()
    {
        float currentAngleWidth = _visionData.angle <= 0 ? 90f : _visionData.angle;
        float currentRadiusDistance = _visionData.radius <= 0 ? 15f : _visionData.radius;

        int numVertices = totalSegments + 2;
        Vector3[] vertices = new Vector3[numVertices];
        int[] triangles = new int[totalSegments * 3];

        // Origin at local zero
        vertices[0] = Vector3.zero;

        float currentAngle = -currentAngleWidth / 2f;
        float angleIncrement = currentAngleWidth / totalSegments;

        // Grab the camera's exact true spatial vectors
        Vector3 forwardDir = transform.forward;
        Vector3 upDir = transform.up;

        for (int i = 0; i <= totalSegments; i++)
        {
            // Use AngleAxis around local Up—this perfectly matches your working cyan lines!
            Vector3 rayDirection = Quaternion.AngleAxis(currentAngle, upDir) * forwardDir;
            rayDirection.Normalize();

            Vector3 targetWorldPos;

            // Cast ray along the exact same path as the gizmo lines
            if (environmentObstructionMask != 0 && Physics.Raycast(transform.position, rayDirection, out RaycastHit hit, currentRadiusDistance, environmentObstructionMask))
            {
                targetWorldPos = hit.point;
            }
            else
            {
                targetWorldPos = transform.position + (rayDirection * currentRadiusDistance);
            }

            // Convert back to local space so the MeshFilter can map it
            vertices[i + 1] = transform.InverseTransformPoint(targetWorldPos);
            currentAngle += angleIncrement;
        }

        // Build the triangle arrays
        for (int i = 0; i < totalSegments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        _coneMesh.Clear();
        _coneMesh.vertices = vertices;
        _coneMesh.triangles = triangles;
        _coneMesh.RecalculateNormals();
    }
}