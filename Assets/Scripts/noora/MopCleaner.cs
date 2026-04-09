using UnityEngine;

public class MopCleaner : MonoBehaviour
{
    public Camera cam;
    public RenderTexture renderTexture;
    public Material drawMaterial;
    public Texture initialTexture;

    void Start()
    {
        Graphics.Blit(initialTexture, renderTexture);
    }
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Paint(hit.textureCoord);
            }
        }
    }

    void Paint(Vector2 uv)
    {
        drawMaterial.SetVector("_Coordinate", new Vector4(uv.x, uv.y, 0, 0));
        drawMaterial.SetFloat("_Size", 0.05f);

        RenderTexture temp = RenderTexture.GetTemporary(renderTexture.width, renderTexture.height);

        Graphics.Blit(renderTexture, temp);
        Graphics.Blit(temp, renderTexture, drawMaterial);

        RenderTexture.ReleaseTemporary(temp);
    }
}