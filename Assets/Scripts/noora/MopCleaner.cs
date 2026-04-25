using UnityEngine;
using UnityEngine.InputSystem;

public class MopCleaner : MonoBehaviour
{
    public Camera cam;
    public RenderTexture renderTexture;
    public Material drawMaterial;
    public Texture initialTexture;

    [Header("Mop Dirt System")]
    public float mopDirt = 0f;
    public float maxDirt = 100f;
    public float dirtIncreaseRate = 30f;
    public float cleanThreshold = 50f;

    private InputSystem_Actions input;
    private bool isPainting;

    void Awake()
    {
        input = new InputSystem_Actions();
    }

    void OnEnable()
    {
        input.Player.Enable();
        input.Player.Attack.performed += _ => isPainting = true;
        input.Player.Attack.canceled += _ => isPainting = false;
    }

    void OnDisable()
    {
        input.Player.Disable();
    }

    void Start()
    {
        RenderTexture.active = renderTexture;
        GL.Clear(true, true, Color.black);

        Graphics.Blit(initialTexture, renderTexture);
        RenderTexture.active = null;
    }

    void Update()
    {
        if (!isPainting) return;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Paint(hit.textureCoord);
        }
    }

    void Paint(Vector2 uv)
    {
        drawMaterial.SetVector("_Coordinate", new Vector4(uv.x, uv.y, 0, 0));
        drawMaterial.SetFloat("_Size", 0.1f);

        float strength;

        if (mopDirt < cleanThreshold)
        {
            strength = -0.0005f;
            mopDirt += dirtIncreaseRate * Time.deltaTime;
        }
        else
        {
            strength = 0.01f;
        }

        drawMaterial.SetFloat("_Strength", strength);
        mopDirt = Mathf.Clamp(mopDirt, 0f, maxDirt);

        RenderTexture temp = RenderTexture.GetTemporary(renderTexture.width, renderTexture.height);

        // COPY current texture
        Graphics.Blit(renderTexture, temp);

        // 🔥 THIS IS THE FIX:
        drawMaterial.SetTexture("_BaseMap", temp);
        // APPLY shader using previous texture
        Graphics.Blit(temp, renderTexture, drawMaterial);

        RenderTexture.ReleaseTemporary(temp);
    }

    public void ResetMop()
    {
        mopDirt = 0f;
    }
}