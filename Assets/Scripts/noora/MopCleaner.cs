using UnityEngine;
using UnityEngine.InputSystem;

public class MopCleaner : MonoBehaviour
{
    public Camera cam;
    public RenderTexture renderTexture;
    public Material drawMaterial;
    public Texture initialTexture;

    private InputSystem_Actions input;
    private bool isPainting;

    void Awake()
    {
        input = new InputSystem_Actions();
    }

    void OnEnable()
    {
        input.Player.Enable();

        input.Player.Attack.performed += OnPaintStart;
        input.Player.Attack.canceled += OnPaintStop;
    }

    void OnDisable()
    {
        input.Player.Attack.performed -= OnPaintStart;
        input.Player.Attack.canceled -= OnPaintStop;

        input.Player.Disable();
    }

    void Start()
    {
        Graphics.Blit(initialTexture, renderTexture);
    }

    void Update()
    {
        if (isPainting)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();

            Ray ray = cam.ScreenPointToRay(mousePos);
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

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log("Interact pressed!");
        }
    }

    void OnPaintStart(InputAction.CallbackContext context)
    {
        isPainting = true;
    }

    void OnPaintStop(InputAction.CallbackContext context)
    {
        isPainting = false;
    }
}