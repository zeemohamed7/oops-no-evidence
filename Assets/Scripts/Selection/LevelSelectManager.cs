using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelectManager : MonoBehaviour
{
    [Header("Level Setup")]
    public RectTransform[] waypoints; 
    public TruckLevelSelection truck;    
    
    [Header("Navigation Settings")]
    private int currentIndex = 0; // Tracks level you're choosing
    private float inputThreshold = 0.5f; // To prevent "hyper-fast" scrolling
    private bool canMove = true;

    void Start()
    {
       
        int unlockedLevel = PlayerPrefs.GetInt("ReachedLevel", 1); // Highest level unlocked FOR VISUAL
        
        // Visually "Lock" the cards that aren't available yet
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (i + 1 > unlockedLevel)
            {
                // Lower the opacity or change the color of locked objects
                var canvasGroup = waypoints[i].GetComponent<CanvasGroup>();
                if (canvasGroup != null) canvasGroup.alpha = 0.5f;
            }
        }

        // Tell the truck to move to the starting position.
        UpdateSelection();
    }

    void Update()
    {
        HandleInput(); // Constantly check if the player is pushing a button.

        // If player presses "Submit" (A on Xbox, Cross on PS, or Enter)
        if (Input.GetButtonDown("Submit")) 
        {
            TryStartLevel();
        }
    }

    void HandleInput()
    {
        
        float moveX = Input.GetAxisRaw("Horizontal"); // Gets -1 or 1 to know which way to go
        if (canMove) // Only move if the "gate" is open
        {
            if (moveX > inputThreshold && currentIndex < waypoints.Length - 1) // Move tgo the right
            {
                currentIndex++;
                UpdateSelection();
                StartCoroutine(InputCooldown()); // Close for a second to prevent hyper scrolling
            }
            else if (moveX < -inputThreshold && currentIndex > 0) // Move to the left
            {
                currentIndex--;
                UpdateSelection();
                StartCoroutine(InputCooldown());
            }
        }
    }

    void UpdateSelection()
    {
        Debug.Log("Manager: Telling truck to move to waypoint " + currentIndex);
    
        if (truck != null) {
            truck.SetTarget(waypoints[currentIndex]);
        } else {
            Debug.LogError("Manager: THE TRUCK SLOT IS EMPTY IN THE INSPECTOR!");
        }
    }

    void TryStartLevel()
    {
        int unlockedLevel = PlayerPrefs.GetInt("ReachedLevel", 1);  // Highest level unlocked FOR LOGIC
    
        // Check if the card we are currently on is less than or equal to our progress.
        if (currentIndex + 1 <= unlockedLevel)
        {
            // Load the level! (Make sure your scenes are named "Level1", "Level2", etc.)
            SceneManager.LoadScene("Level" + (currentIndex + 1));
        }
    }

    System.Collections.IEnumerator InputCooldown()
    {
        canMove = false;
        yield return new WaitForSeconds(0.2f); // Stops the truck from flying across 5 levels in one tap
        canMove = true;
    }
}