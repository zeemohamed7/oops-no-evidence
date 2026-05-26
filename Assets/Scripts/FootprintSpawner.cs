using UnityEngine;

public class FootprintSpawner : MonoBehaviour
{
    public GameObject footprintPrefab;
    public float distanceBetweenPrints = 2f;

    private Vector3 _lastPrintPos;
    private FootprintTracker _tracker;

    void Start()
    {
        _tracker = GetComponent<FootprintTracker>() ?? GetComponentInParent<FootprintTracker>();
        _lastPrintPos = transform.position;
    }

    void Update()
    {
        if (_tracker == null || !_tracker.HasBloodyShoes) return;

        if (Vector3.Distance(transform.position, _lastPrintPos) > distanceBetweenPrints)
            SpawnPrint();
    }

    void SpawnPrint()
    {
        _lastPrintPos = transform.position;
        if (footprintPrefab == null) return;

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 2f))
        {
            GameObject fp = Instantiate(
                footprintPrefab,
                hit.point + new Vector3(0, 0.01f, 0),
                Quaternion.LookRotation(transform.forward));
            fp.tag = "Footprint";
            foreach (Collider c in fp.GetComponentsInChildren<Collider>())
                c.enabled = false;
            FootprintTracker.ActiveFootprints.Add(fp);
        }
    }
}