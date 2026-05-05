using UnityEngine;

public class FootprintSpawner : MonoBehaviour
{
    public GameObject footprintPrefab;
    public float distanceBetweenPrints = 2f;
    private Vector3 _lastPrintPos;

    void Start() => Destroy(gameObject, 30f); // Disappears after 30 seconds
    
    void Update()
    {
        if (Vector3.Distance(transform.position, _lastPrintPos) > distanceBetweenPrints)
        {
            SpawnPrint();
        }
    }

    void SpawnPrint()
    {
        _lastPrintPos = transform.position;
        // Raycast down to find the floor height
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 2f))
        {
            Instantiate(footprintPrefab, hit.point + new Vector3(0, 0.01f, 0), Quaternion.LookRotation(transform.forward));
        }
    }
}