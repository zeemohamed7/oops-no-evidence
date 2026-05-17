using UnityEngine;

public class PlayerHiding : MonoBehaviour
{
    private PlayerBreathingSound breathingSound;

    public bool IsHiding { get; private set; }

    private void Awake()
    {
        breathingSound = GetComponent<PlayerBreathingSound>();
    }

    public void EnterHide()
    {
        IsHiding = true;
        breathingSound.StartBreathing();
    }

    public void ExitHide()
    {
        IsHiding = false;
        breathingSound.StopBreathing();
    }
}
