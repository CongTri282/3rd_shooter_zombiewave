using UnityEngine;

public class Weapon : MonoBehaviour
{
    [Header("Weapon Sockets")]
    public Transform rightHandGrip;
    public Transform leftHandGrip;
    public Transform firePoint;

    [Header("Chest Mount Offsets (Relative to WeaponPivot)")]
    // Relaxed / Running stance (held diagonally across the chest)
    public Vector3 idleLocalPos = new Vector3(0.08f, -0.12f, 0.25f);
    public Vector3 idleLocalEuler = new Vector3(20f, -35f, -15f);

    // Aiming stance (tucked into right shoulder pocket, pointing straight forward)
    public Vector3 aimLocalPos = new Vector3(0.18f, 0.04f, 0.32f);
    public Vector3 aimLocalEuler = new Vector3(0f, 0f, 0f);

    [Header("Combat & Procedural Recoil")]
    public GameObject bulletPrefab;
    public float fireRate = 0.15f;
    public Vector3 recoilKickBack = new Vector3(0f, 0.005f, -0.04f); // Kicks straight back (-Z)
    public Vector3 recoilRotation = new Vector3(-2.5f, 0f, 0f);      // Slight muzzle climb (-X)
    public float recoilReturnSpeed = 12f;

    // Helper buttons to save positions directly from the Scene view!
    [ContextMenu("Save Current Transform as IDLE Pose")]
    private void SaveIdlePose()
    {
        idleLocalPos = transform.localPosition;
        idleLocalEuler = transform.localEulerAngles;
        Debug.Log($"{name}: Saved IDLE Pose!");
    }

    [ContextMenu("Save Current Transform as AIMING Pose")]
    private void SaveAimPose()
    {
        aimLocalPos = transform.localPosition;
        aimLocalEuler = transform.localEulerAngles;
        Debug.Log($"{name}: Saved AIMING Pose!");
    }
}