using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; 

public class LevelSelectManager : MonoBehaviour
{
    [Header("Level Setup")]
    public RectTransform[] waypoints; 
    public TruckLevelSelection truck;   
    public GameObject[] buildingModels;
    
    private int currentIndex = 0;

    void Start()
    {
        currentIndex = 0; 
        int unlockedLevel = PlayerPrefs.GetInt("ReachedLevel", 1); 
    
        // Loop through to handle visuals
        for (int i = 0; i < waypoints.Length; i++)
        {
            bool isLocked = (i + 1) > unlockedLevel;

            // UI Gray-out
            var canvasGroup = waypoints[i].GetComponent<CanvasGroup>();
            if (canvasGroup != null) canvasGroup.alpha = isLocked ? 0.5f : 1.0f;

            // Building Gray-out
            if (buildingModels != null && i < buildingModels.Length && buildingModels[i] != null)
            {
                Renderer[] parts = buildingModels[i].GetComponentsInChildren<Renderer>();
                foreach (Renderer p in parts)
                {
                    p.material.color = isLocked ? new Color(0.2f, 0.2f, 0.2f) : Color.white;
                }
            }
        }
        UpdateSelection();
    }

    void Update()
    {
        int unlockedLevel = PlayerPrefs.GetInt("ReachedLevel", 1);

        // --- NAVIGATION (Keyboard & Controller) ---
        bool moveRight = false;
        bool moveLeft = false;

        // Check Keyboard
        if (Keyboard.current != null)
        {
            if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame)
                moveRight = true;
            if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame)
                moveLeft = true;
        }

        // Check Gamepad (D-Pad or Left Stick click)
        if (Gamepad.current != null)
        {
            if (Gamepad.current.dpad.right.wasPressedThisFrame || Gamepad.current.leftStick.right.wasPressedThisFrame)
                moveRight = true;
            if (Gamepad.current.dpad.left.wasPressedThisFrame || Gamepad.current.leftStick.left.wasPressedThisFrame)
                moveLeft = true;
        }

        // Execute Move
        if (moveRight && currentIndex < waypoints.Length - 1 && (currentIndex + 2) <= unlockedLevel)
        {
            currentIndex++;
            UpdateSelection();
        }
        else if (moveLeft && currentIndex > 0)
        {
            currentIndex--;
            UpdateSelection();
        }

        // --- SUBMIT ---
        bool pressedSubmit = false;
        if (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
            pressedSubmit = true;
        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) // 'A' on Xbox / 'Cross' on PS
            pressedSubmit = true;

        if (pressedSubmit) TryStartLevel();

        // --- CHEATS FOR TESTING--- DELETE LATER 
        if (Keyboard.current != null)
        {
            if (Keyboard.current.uKey.wasPressedThisFrame) CheatUnlock(4);
            if (Keyboard.current.rKey.wasPressedThisFrame) CheatUnlock(1);
        }
    }

    void CheatUnlock(int level)
    {
        PlayerPrefs.SetInt("ReachedLevel", level);
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void UpdateSelection()
    {
        if (truck != null) truck.SetTarget(waypoints[currentIndex]);
    }

    void TryStartLevel()
    {
        int unlockedLevel = PlayerPrefs.GetInt("ReachedLevel", 1);
        if (currentIndex + 1 <= unlockedLevel)
        {
            SceneManager.LoadScene("Level" + (currentIndex + 1));
        }
    }
}