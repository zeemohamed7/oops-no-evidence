using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VisionCone))]
public class VisionCodeEditor : Editor
{
    private void OnSceneGUI()
    {
        var visionCone = (VisionCone)target;
        Handles.color = Color.white;
        Handles.DrawWireArc(visionCone.transform.position, Vector3.up, Vector3.forward, 360, visionCone.radius);

        // Viewing angle
        var viewAngle01 = DirectionFromAngle(visionCone.transform.eulerAngles.y, -visionCone.angle / 2);
        var viewAngle02 = DirectionFromAngle(visionCone.transform.eulerAngles.y, visionCone.angle / 2);

        Handles.color = Color.yellow;
        Handles.DrawLine(visionCone.transform.position,
            visionCone.transform.position + viewAngle01 * visionCone.radius);
        Handles.DrawLine(visionCone.transform.position,
            visionCone.transform.position + viewAngle02 * visionCone.radius);

        // Indication if player is seen
        if (visionCone.canSeePlayer)
        {
            Handles.color = Color.green;
            Handles.DrawLine(visionCone.transform.position, visionCone.playerRef.transform.position);
        }
    }

    private Vector3 DirectionFromAngle(float eulerY, float angleInDegrees)
    {
        angleInDegrees += eulerY;
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
}