using UnityEngine;

public enum ToolType { Mop, BodyBag, Blacklight }

public class ToolManager : MonoBehaviour
{
    public ToolType currentTool;

    void Start()
    {
        currentTool = ToolType.Mop;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            currentTool = ToolType.Mop;

        if (Input.GetKeyDown(KeyCode.Alpha2))
            currentTool = ToolType.BodyBag;

        if (Input.GetKeyDown(KeyCode.Alpha3))
            currentTool = ToolType.Blacklight;

        //Debug.Log("Current Tool: " + currentTool);
    }
}