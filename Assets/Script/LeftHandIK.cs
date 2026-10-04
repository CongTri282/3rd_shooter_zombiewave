using UnityEngine;

[RequireComponent(typeof(Animator))]
public class LeftHandIK : MonoBehaviour
{
    public Transform leftHandGrip; // Drag your "LeftHandGrip" child object from Gun here
    [Range(0f, 1f)]
    public float ikWeight = 1f;    // 1 = glued to gun barrel, 0 = use raw Mixamo animation

    private Animator anim;

    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    // Unity calls this automatically every frame when "IK Pass" is checked on the Animator Layer
    private void OnAnimatorIK(int layerIndex)
    {
        if (anim == null || leftHandGrip == null) return;

        // Lock the Left Hand's position and rotation to the LeftHandGrip on the gun
        anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, ikWeight);
        anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, ikWeight);

        anim.SetIKPosition(AvatarIKGoal.LeftHand, leftHandGrip.position);
        anim.SetIKRotation(AvatarIKGoal.LeftHand, leftHandGrip.rotation);
    }
}