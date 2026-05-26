using UnityEngine;

/// <summary>
/// Draws the red vision cone mesh on the floor.
/// Attach to an empty child GameObject inside Camera_Parent.
/// Reads detection state from SecurityCamera on the parent.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SecurityCameraVisionCone : MonoBehaviour
{
    [Header("Mesh Quality")]
    [Range(8, 128)] public int rayCount = 48;

    [Header("Projection")]
    public LayerMask obstructionMask;
    public LayerMask floorMask;
    public float meshYOffset = 0.02f;
    public Vector3 tipOffset = Vector3.zero;

    [Header("Appearance")]
    public Material coneMaterial;
    public Color normalColor  = new Color(1f, 0f, 0f, 0.4f);
    public Color spottedColor = new Color(1f, 0f, 0f, 1f);
    public float flashSpeed   = 8f;

    private Mesh             _mesh;
    private MeshRenderer     _mr;
    private SecurityCamera   _camera;
    private float            _flashTimer;

    private void Start()
    {
        var mf   = GetComponent<MeshFilter>();
        _mr      = GetComponent<MeshRenderer>();
        _mr.material = new Material(coneMaterial); // instance so we can change color
        _mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _mr.receiveShadows    = false;

        _mesh    = new Mesh { name = "SecurityCameraConeMesh" };
        mf.mesh  = _mesh;

        _camera  = transform.parent.GetComponent<SecurityCamera>();

        if (_camera == null)
            Debug.LogError("SecurityCameraVisionCone: No SecurityCamera found on parent!");
    }

    private void LateUpdate()
    {
        if (_camera == null) return;
        UpdateColor();
        DrawCone(_camera.radius, _camera.angle);
    }

    private void UpdateColor()
    {
        if (_camera.canSeePlayer)
        {
            // Flash by oscillating alpha
            _flashTimer += Time.deltaTime * flashSpeed;
            float alpha = Mathf.Abs(Mathf.Sin(_flashTimer));
            _mr.material.color = new Color(spottedColor.r, spottedColor.g, spottedColor.b, alpha);
        }
        else
        {
            _flashTimer = 0f;
            _mr.material.color = normalColor;
        }
    }

    private void DrawCone(float radius, float angle)
    {
        Transform parent = transform.parent;
        Vector3   origin = parent.position;

        Vector3 flat = new Vector3(parent.forward.x, 0f, parent.forward.z).normalized;
        if (flat.sqrMagnitude < 0.0001f) flat = Vector3.forward;

        int       vertCount = rayCount + 2;
        Vector3[] verts     = new Vector3[vertCount];
        int[]     tris      = new int[rayCount * 3];

        // Tip at camera position
        verts[0] = origin + tipOffset;

        float halfAngle = angle * 0.5f;
        float step      = angle / rayCount;

        for (int i = 0; i <= rayCount; i++)
        {
            float   a   = -halfAngle + step * i;
            Vector3 dir = Quaternion.Euler(0f, a, 0f) * flat;

            float dist = radius;
            if (Physics.Raycast(origin, dir, out RaycastHit wallHit, radius, obstructionMask))
                dist = wallHit.distance;

            Vector3 arcWorld = origin + dir * dist;

            if (Physics.Raycast(arcWorld + Vector3.up * 10f, Vector3.down, out RaycastHit floorHit, 20f, floorMask))
                verts[i + 1] = floorHit.point + Vector3.up * meshYOffset;
            else
                verts[i + 1] = new Vector3(arcWorld.x, 0f, arcWorld.z);
        }

        for (int i = 0; i < rayCount; i++)
        {
            tris[i * 3]     = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = i + 2;
        }

        for (int i = 0; i < vertCount; i++)
            verts[i] = transform.InverseTransformPoint(verts[i]);

        _mesh.Clear();
        _mesh.vertices  = verts;
        _mesh.triangles = tris;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
    }
}