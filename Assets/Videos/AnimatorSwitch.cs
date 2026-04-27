using UnityEngine;

public class AnimatorSwitcher : MonoBehaviour
{
    public Animator targetAnimator;
    public RuntimeAnimatorController newController;

    void OnEnable()
    {
        SwitchNow();
    }

    public void SwitchNow()
    {
        if (targetAnimator != null && newController != null)
            targetAnimator.runtimeAnimatorController = newController;
    }
}
