using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
public class TruckProximityMessage : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI messageText;

    [Header("Messages")]
    public string tasksNotDoneMessage = "Complete all tasks first!";
    public string allDoneMessage      = "ALL DONE! Go to the Truck!";

    [Header("All-Done Banner")]
    [Tooltip("How long the ALL DONE banner stays on screen. 0 = forever.")]
    public float allDoneBannerDuration = 4f;

    bool  _playerInside  = false;
    bool  _allDoneBanner = false;
    float _bannerTimer   = 0f;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Start()
    {
        GameEvents.OnTaskCompleted += OnTaskCompleted;
    }

    void OnDestroy()
    {
        GameEvents.OnTaskCompleted -= OnTaskCompleted;
    }

    void OnTaskCompleted(string _)
    {
        if (_allDoneBanner) return;
        if (GameHUD.Instance == null || !GameHUD.Instance.AllTasksDone()) return;

        _allDoneBanner = true;
        _bannerTimer   = allDoneBannerDuration > 0f ? allDoneBannerDuration : float.MaxValue;
    }

    void Update()
    {
        if (_allDoneBanner && allDoneBannerDuration > 0f)
        {
            _bannerTimer -= Time.deltaTime;
            if (_bannerTimer <= 0f)
                _allDoneBanner = false;
        }
    }

    bool IsPlayer(Collider other)
    {
        if (other.CompareTag("Player")) return true;
        GameObject root = other.attachedRigidbody != null
            ? other.attachedRigidbody.gameObject
            : other.transform.root.gameObject;
        return root.CompareTag("Player");
    }

    void OnTriggerEnter(Collider other) { if (IsPlayer(other)) _playerInside = true; }
    void OnTriggerStay (Collider other) { if (IsPlayer(other)) _playerInside = true; }
    void OnTriggerExit (Collider other) { if (IsPlayer(other)) _playerInside = false; }

    void OnGUI()
    {
        bool showBanner    = _allDoneBanner;
        bool showProximity = _playerInside && !_allDoneBanner
                             && (GameHUD.Instance == null || !GameHUD.Instance.AllTasksDone());

        if (!showBanner && !showProximity) return;

        string msg      = showBanner ? allDoneMessage : tasksNotDoneMessage;
        Color  msgColor = showBanner ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.9f, 0.3f);

        float w = 420f, h = 54f;
        float x = (Screen.width  - w) / 2f;
        float y =  Screen.height - 220f;

        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(x - 2, y - 2, w + 4, h + 4), Texture2D.whiteTexture);

        GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);

        GUI.color = Color.white;
        GUIStyle style = new GUIStyle
        {
            fontSize  = 22,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = msgColor }
        };
        GUI.Label(new Rect(x, y, w, h), msg, style);
        GUI.color = Color.white;
    }
}