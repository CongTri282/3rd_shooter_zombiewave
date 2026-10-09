using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    private InputSystem_Actions inputActions;
    private Vector2 moveInput;
    private Rigidbody playerRb;
    private Camera mainCam;
    private Animator playerAnim;
    private float boundaryZ = 35f;
    private float boundaryX = 35f;

    [Header("Camera Setup")]
    public Transform focalPoint;
    public float cameraSensitivity = 10f;
    public Vector3 normalCameraOffset = new Vector3(0.45f, 0.1f, -2.8f);
    public Vector3 aimCameraOffset = new Vector3(0.65f, 0.05f, -1.5f); // Closer & further right over shoulder

    [Header("Camera Wall Collision")]
    public bool enableCameraCollision = true;
    public float cameraCollisionRadius = 0.2f;
    public LayerMask cameraCollisionLayers = ~0;

    private float shoulderSide = 1f; // 1 = Right Shoulder, -1 = Left Shoulder
    private float horizontalRotation = 0f;
    private float verticalRotation = 0f;

    [Header("Player Health & HUD UI")]
    public float maxHealth = 100f;
    public float currentHealth;
    public Slider playerHealthBar;
    public Image powerUpFillImage;
    public GameObject gameOverPanel;
    private bool isDead = false;
    private Coroutine activePowerUpCoroutine;

    [Header("Movement & Physics")]
    public float walkSpeed = 5f;
    public float runSpeed = 9f;
    public float aimWalkSpeed = 3.5f;
    public float jumpForce = 6f;
    public bool isGrounded = true;
    private bool jumpRequested = false;

    [Header("Aiming Setup")]
    public float normalFOV = 60f;
    public float aimFOV = 40f;
    public float aimTransitionSpeed = 10f;
    private float currentUpperBodyWeight = 0f;
    private bool isAiming = false;
    private bool isSprinting = false;

    [Header("Multi-Bone Spine Aiming & Anti-Clip Setup")]
    [Range(0.2f, 1f)] public float spinePitchMultiplier = 0.65f; // Prevents extreme 60-degree spine folding
    public float maxSpineDownAngle = 35f;                        // Clamps how far the back can bend downward
    public Vector3 spineTwistOffset = new Vector3(0f, 12f, 0f);  // Rotates chest slightly right so stock clears the head
    public Vector3 headClearanceOffset = new Vector3(-10f, -15f, 5f); // Lifts & turns head slightly away from the scope

    [Header("Modular Weapon System & Dual-Hand IK")]
    public Transform weaponPivot;
    public Weapon[] weapons;
    public int currentWeaponIndex = 0;
    public float minAimDistance = 6f;
    public float maxGunPivotAngle = 12f;

    [Tooltip("Check this in Play Mode while positioning the Gun or Grip sockets manually in Scene View!")]
    public bool debugPauseWeaponPose = false;

    private Weapon activeWeapon;
    private Vector3 currentRecoilPos;
    private Vector3 currentRecoilRot;
    private float currentSpineAimWeight = 0f;

    // Cached Humanoid Bones
    private Transform spineBone;      // mixamorig:Spine
    private Transform chestBone;      // mixamorig:Spine1
    private Transform upperChestBone; // mixamorig:Spine2
    private Transform neckBone;       // mixamorig:Neck
    private Transform headBone; // mixamorig:Head

    private Transform rightShoulder, rightUpperArm, rightLowerArm, rightHand;
    private Transform leftShoulder, leftUpperArm, leftLowerArm, leftHand;

    [Header("Dynamic Crosshair Setup")]
    public RectTransform crosshairRect;
    public Color normalCrosshairColor = Color.white;
    public Color enemyCrosshairColor = Color.red;
    public float baseCrosshairSize = 20f;     // Resting size when standing still
    public float moveCrosshairExpand = 15f;   // Extra spread while walking
    public float shootCrosshairImpulse = 25f; // Instant pop when firing a bullet
    private float currentCrosshairSize = 20f;
    private UnityEngine.UI.Image[] crosshairImages;

    [Header("Shooting Setup (Fallback & PowerUp)")]
    public float fireClipLength = 0.267f;
    private float fireRateMultiplier = 1f; // Used by PowerUpRoutine to boost fire rate across any equipped weapon
    private float nextFireTime = 0f;

    void Awake()
    {
        inputActions = new InputSystem_Actions();
        playerRb = GetComponent<Rigidbody>();
        mainCam = Camera.main;
        playerAnim = GetComponentInChildren<Animator>();

        if (playerAnim != null && playerAnim.isHuman)
        {
            spineBone = playerAnim.GetBoneTransform(HumanBodyBones.Spine);
            chestBone = playerAnim.GetBoneTransform(HumanBodyBones.Chest);
            upperChestBone = playerAnim.GetBoneTransform(HumanBodyBones.UpperChest);
            neckBone = playerAnim.GetBoneTransform(HumanBodyBones.Neck);
            headBone = playerAnim.GetBoneTransform(HumanBodyBones.Head);

            rightShoulder = playerAnim.GetBoneTransform(HumanBodyBones.RightShoulder);
            rightUpperArm = playerAnim.GetBoneTransform(HumanBodyBones.RightUpperArm);
            rightLowerArm = playerAnim.GetBoneTransform(HumanBodyBones.RightLowerArm);
            rightHand = playerAnim.GetBoneTransform(HumanBodyBones.RightHand);

            leftShoulder = playerAnim.GetBoneTransform(HumanBodyBones.LeftShoulder);
            leftUpperArm = playerAnim.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            leftLowerArm = playerAnim.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            leftHand = playerAnim.GetBoneTransform(HumanBodyBones.LeftHand);
        }

        // Freeze ALL physics rotation (X, Y, and Z) so collisions never tip or spin the player
        playerRb.constraints = RigidbodyConstraints.FreezeRotation;
        horizontalRotation = transform.eulerAngles.y;

        if (mainCam != null)
        {
            normalFOV = mainCam.fieldOfView;
        }

        if (crosshairRect != null)
        {
            crosshairImages = crosshairRect.GetComponentsInChildren<UnityEngine.UI.Image>();
            currentCrosshairSize = baseCrosshairSize;
        }
    }

    void Start()
    {
        // Lock the cursor to the center of the screen and hide the mouse pointer
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        EquipWeapon(currentWeaponIndex);

        if (crosshairRect != null)
        {
            crosshairRect.gameObject.SetActive(false);
        }

        currentHealth = maxHealth;
        if (playerHealthBar != null)
        {
            playerHealthBar.maxValue = maxHealth;
            playerHealthBar.value = currentHealth;
        }
        if (powerUpFillImage != null)
        {
            powerUpFillImage.gameObject.SetActive(false);
        }
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    void OnEnable()
    {
        inputActions.Enable();
    }

    void OnDisable()
    {
        inputActions.Disable();
    }

    public void EquipWeapon(int index)
    {
        if (weapons == null || weapons.Length == 0 || index < 0 || index >= weapons.Length) return;

        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null)
            {
                weapons[i].gameObject.SetActive(i == index);
            }
        }

        currentWeaponIndex = index;
        activeWeapon = weapons[index];
    }

    void Update()
    {
        if (isDead) return;
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();

        // Read aim and sprint inputs
        isAiming = inputActions.Player.Aim.IsPressed();
        isSprinting = inputActions.Player.Sprint.IsPressed() && !isAiming && moveInput.sqrMagnitude > 0.1f;

        // Jump input: Only allow jumping if the player is grounded
        if (inputActions.Player.Jump.WasPressedThisFrame() && isGrounded)
        {
            jumpRequested = true;
            if (playerAnim != null) playerAnim.SetTrigger("Jump");
        }

        // Quick weapon switching using number keys 1, 2, 3...
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) EquipWeapon(0);
            if (Keyboard.current.digit2Key.wasPressedThisFrame && weapons.Length > 1) EquipWeapon(1);
            if (Keyboard.current.digit3Key.wasPressedThisFrame && weapons.Length > 2) EquipWeapon(2);
        }

        // Only allow shooting when holding right click (isAiming)
        bool isHoldingFire = inputActions.Player.Shoot.IsPressed();
        if (isAiming && isHoldingFire && activeWeapon != null && Time.time >= nextFireTime)
        {
            float currentFireRate = activeWeapon.fireRate * fireRateMultiplier;
            nextFireTime = Time.time + currentFireRate;
            ShootBullet(currentFireRate);
        }

        PlayerLook();
        UpdateAnimationsAndModelRotation();
        UpdateCrosshair();
    }

    void LateUpdate()
    {
        UpdateCameraPosition();

        if (isDead || playerAnim == null || mainCam == null) return;

        float targetWeight = isAiming ? 1f : 0f;
        currentSpineAimWeight = Mathf.Lerp(currentSpineAimWeight, targetWeight, Time.deltaTime * aimTransitionSpeed);

        // 1. Multi-Bone Spine Pitch & Head Clearance
        if (currentSpineAimWeight > 0.001f)
        {
            float clampedPitch = Mathf.Clamp(verticalRotation * spinePitchMultiplier, -30f, maxSpineDownAngle);
            float weightedPitch = clampedPitch * currentSpineAimWeight;

            // Use the Player's horizontal right axis (transform.right) so pitch never rolls the spine sideways
            Vector3 pitchAxis = transform.right;

            if (spineBone != null)
            {
                spineBone.rotation = Quaternion.AngleAxis(weightedPitch * 0.30f, pitchAxis) * spineBone.rotation;
            }
            if (chestBone != null)
            {
                chestBone.rotation = Quaternion.AngleAxis(weightedPitch * 0.35f, pitchAxis) * chestBone.rotation;
            }
            if (upperChestBone != null)
            {
                Quaternion twistFix = Quaternion.Euler(spineTwistOffset * currentSpineAimWeight);
                upperChestBone.rotation = Quaternion.AngleAxis(weightedPitch * 0.35f, pitchAxis) * upperChestBone.rotation * twistFix;
            }
            // Stabilize Neck & Head so walking animations never tilt the helmet into the rifle stock!
            // 4. Stabilize Head against walking wobble & pitch it cleanly up/down with the camera
            if (headBone != null)
            {
                // A. Keep the head's animated yaw/offset, but strip out the walking side-tilt (roll)
                // by projecting the head's current forward/up vectors onto the player's upright plane
                Vector3 cleanForward = Vector3.ProjectOnPlane(headBone.forward, Vector3.up).normalized;
                if (cleanForward.sqrMagnitude < 0.001f) cleanForward = transform.forward;

                Quaternion levelHeadRot = Quaternion.LookRotation(cleanForward, Vector3.up);

                // B. Pitch the head up/down around the player's horizontal right axis (transform.right)
                // Using 0.85f so the head looks down/up 1:1 with where you aim!
                float headPitch = Mathf.Clamp(verticalRotation * 0.85f, -40f, 55f);
                Quaternion pitchedHeadRot = Quaternion.AngleAxis(headPitch, transform.right) * levelHeadRot;

                // C. Apply your Inspector Head Clearance Offset (turns head slightly left/up away from the scope)
                Quaternion finalHeadRot = pitchedHeadRot * Quaternion.Euler(headClearanceOffset);

                // Smoothly lock the head when aiming
                headBone.rotation = Quaternion.Slerp(headBone.rotation, finalHeadRot, currentSpineAimWeight);
            }
        }

        // 2. Position & Aim the Active Weapon, then Lock Both Hands via Dual-IK
        // 2. Position & Aim the Active Weapon, then Lock Both Hands via Dual-IK
        if (activeWeapon != null && weaponPivot != null)
        {
            // TỰ ĐỘNG CÂN BẰNG WEAPON PIVOT:
            // Khi Idle (weight = 0): Đi theo góc xoay tự nhiên của xương ngực (upperChestBone).
            // Khi Aiming (weight = 1): Khóa trục +Z của WeaponPivot thẳng theo hướng nhìn của Camera (focalPoint)
            // để Aim Local Euler (0, 0, 0) luôn chỉa thẳng về phía trước!
            if (upperChestBone != null && focalPoint != null)
            {
                Quaternion chestRot = upperChestBone.rotation;
                Quaternion forwardAimRot = Quaternion.Euler(verticalRotation, transform.eulerAngles.y, 0f);
                weaponPivot.rotation = Quaternion.Slerp(chestRot, forwardAimRot, currentSpineAimWeight);
            }

            // Smoothly recover procedural recoil toward zero
            currentRecoilPos = Vector3.Lerp(currentRecoilPos, Vector3.zero, Time.deltaTime * activeWeapon.recoilReturnSpeed);
            currentRecoilRot = Vector3.Lerp(currentRecoilRot, Vector3.zero, Time.deltaTime * activeWeapon.recoilReturnSpeed);

            if (!debugPauseWeaponPose)
            {
                Vector3 basePos = Vector3.Lerp(activeWeapon.idleLocalPos, activeWeapon.aimLocalPos, currentSpineAimWeight);
                Quaternion baseRot = Quaternion.Slerp(
                    Quaternion.Euler(activeWeapon.idleLocalEuler),
                    Quaternion.Euler(activeWeapon.aimLocalEuler),
                    currentSpineAimWeight
                );

                activeWeapon.transform.localPosition = basePos + currentRecoilPos;
                activeWeapon.transform.localRotation = baseRot * Quaternion.Euler(currentRecoilRot);

                // Point the barrel (+Z of AssaultRifle root) directly at the 3D crosshair hit point when aiming
                if (currentSpineAimWeight > 0.01f)
                {
                    Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                    Vector3 targetPoint = (Physics.Raycast(ray, out RaycastHit hit, 100f, cameraCollisionLayers))
                        ? ray.GetPoint(Mathf.Max(hit.distance, minAimDistance))
                        : ray.GetPoint(100f);

                    Quaternion baseWorldRot = activeWeapon.transform.rotation;
                    Quaternion desiredLookRot = Quaternion.LookRotation(
                        targetPoint - activeWeapon.transform.position,
                        weaponPivot.up
                    );

                    Quaternion clampedRot = Quaternion.RotateTowards(baseWorldRot, desiredLookRot, maxGunPivotAngle);
                    activeWeapon.transform.rotation = Quaternion.Slerp(baseWorldRot, clampedRot, currentSpineAimWeight) * Quaternion.Euler(currentRecoilRot);
                }
            }

            // 3. Solve Two-Bone IK for BOTH Arms so both hands stay glued to the gun
            SolveLimbIK(rightShoulder, rightUpperArm, rightLowerArm, rightHand, activeWeapon.rightHandGrip, true);
            SolveLimbIK(leftShoulder, leftUpperArm, leftLowerArm, leftHand, activeWeapon.leftHandGrip, false);
        }
    }

    private void FixedUpdate()
    {
        if (isDead) return;
        MovePlayer();
        PlayerJump();
    }

    void SolveLimbIK(Transform clavicle, Transform upperArm, Transform lowerArm, Transform hand, Transform gripTarget, bool isRightArm)
    {
        if (gripTarget == null || upperArm == null || lowerArm == null || hand == null) return;

        Vector3 targetPos = gripTarget.position;

        // 1. Subtle Clavicle Reach Assist
        if (clavicle != null)
        {
            Vector3 toHand = (hand.position - clavicle.position).normalized;
            Vector3 toGrip = (targetPos - clavicle.position).normalized;
            Quaternion nudge = Quaternion.FromToRotation(toHand, toGrip);
            clavicle.rotation = Quaternion.Slerp(Quaternion.identity, nudge, 0.2f) * clavicle.rotation;
        }

        Vector3 rootPos = upperArm.position;
        float upperLen = Vector3.Distance(upperArm.position, lowerArm.position);
        float lowerLen = Vector3.Distance(lowerArm.position, hand.position);
        float maxReach = (upperLen + lowerLen) - 0.002f;

        Vector3 toTarget = targetPos - rootPos;
        float targetDist = Mathf.Clamp(toTarget.magnitude, 0.05f, maxReach);
        Vector3 targetDir = toTarget.normalized;
        Vector3 reachableTargetPos = rootPos + targetDir * targetDist;

        // 2. Pole Vector: Keeps right elbow down-right and left elbow down-left
        Transform charTransform = (playerAnim != null) ? playerAnim.transform : transform;
        float sideSign = isRightArm ? 1f : -1f;
        Vector3 poleDir = (-charTransform.up + charTransform.right * (0.45f * sideSign)).normalized;

        Vector3 bendNormal = Vector3.Cross(targetDir, poleDir).normalized;
        if (bendNormal.sqrMagnitude < 0.001f) bendNormal = transform.forward;

        // 3. Law of Cosines for exact shoulder angle
        float cosShoulder = Mathf.Clamp(
            ((upperLen * upperLen) + (targetDist * targetDist) - (lowerLen * lowerLen)) / (2f * upperLen * targetDist),
            -1f, 1f
        );
        float shoulderAngle = Mathf.Acos(cosShoulder) * Mathf.Rad2Deg;

        // 4. Rotate Upper Arm & Forearm into position
        Vector3 upperArmDir = Quaternion.AngleAxis(shoulderAngle, bendNormal) * targetDir;
        upperArm.rotation = Quaternion.FromToRotation((lowerArm.position - upperArm.position).normalized, upperArmDir) * upperArm.rotation;
        lowerArm.rotation = Quaternion.FromToRotation((hand.position - lowerArm.position).normalized, (reachableTargetPos - lowerArm.position).normalized) * lowerArm.rotation;

        // 5. Lock Wrist & Fingers to the Grip Socket
        hand.rotation = gripTarget.rotation;
    }

    void UpdateCameraPosition()
    {
        if (focalPoint == null || mainCam == null) return;

        // Press SwitchShoulder action ('V') to swap between Right and Left shoulder
        if (inputActions.Player.SwitchShoulder.WasPressedThisFrame())
        {
            shoulderSide *= -1f;
        }

        // 1. Pick target local offset based on aiming state
        Vector3 targetCamOffset = isAiming ? aimCameraOffset : normalCameraOffset;
        targetCamOffset.x *= shoulderSide;

        // 2. Prevent camera from clipping through walls behind the player
        if (enableCameraCollision)
        {
            Vector3 desiredWorldPos = focalPoint.TransformPoint(targetCamOffset);
            Vector3 castDir = desiredWorldPos - focalPoint.position;
            float maxDist = castDir.magnitude;

            if (Physics.SphereCast(focalPoint.position, cameraCollisionRadius, castDir.normalized, out RaycastHit hit, maxDist, cameraCollisionLayers))
            {
                targetCamOffset = targetCamOffset.normalized * Mathf.Max(hit.distance - 0.1f, 0.3f);
            }
        }

        // 3. Smoothly slide Main Camera to the target offset
        mainCam.transform.localPosition = Vector3.Lerp(
            mainCam.transform.localPosition,
            targetCamOffset,
            Time.deltaTime * aimTransitionSpeed
        );
    }

    void UpdateCrosshair()
    {
        if (crosshairRect == null) return;

        // 1. Show only while holding Right-Click
        if (crosshairRect.gameObject.activeSelf != isAiming)
        {
            crosshairRect.gameObject.SetActive(isAiming);
        }

        if (!isAiming) return;

        // 2. Raycast from screen center to check if pointing at an "Enemy"
        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        bool isTargetingEnemy = Physics.Raycast(ray, out RaycastHit hit, 100f, cameraCollisionLayers) && hit.collider.CompareTag("Enemy");
        Color targetColor = isTargetingEnemy ? enemyCrosshairColor : normalCrosshairColor;

        if (crosshairImages != null)
        {
            for (int i = 0; i < crosshairImages.Length; i++)
            {
                crosshairImages[i].color = Color.Lerp(crosshairImages[i].color, targetColor, Time.deltaTime * 15f);
            }
        }

        // 3. Smoothly recover crosshair spread toward resting/walking size
        float moveSpread = Mathf.Clamp01(moveInput.magnitude) * moveCrosshairExpand;
        float targetSize = baseCrosshairSize + moveSpread;

        currentCrosshairSize = Mathf.Lerp(currentCrosshairSize, targetSize, Time.deltaTime * 10f);
        crosshairRect.sizeDelta = new Vector2(currentCrosshairSize, currentCrosshairSize);
    }

    void UpdateAnimationsAndModelRotation()
    {
        // Zoom camera FOV smoothly when holding right mouse button (aiming)
        if (mainCam != null)
        {
            float targetFOV = isAiming ? aimFOV : normalFOV;
            mainCam.fieldOfView = Mathf.Lerp(mainCam.fieldOfView, targetFOV, aimTransitionSpeed * Time.deltaTime);
        }

        if (playerAnim == null) return;

        // Calculate 1D Speed parameter: 0 = Idle, 0.5 = Walk, 1 = Run
        float inputMagnitude = Mathf.Clamp01(moveInput.magnitude);
        float targetAnimSpeed = 0f;
        if (inputMagnitude > 0.01f)
        {
            targetAnimSpeed = isSprinting ? 1f : 0.5f;
        }

        // Send parameters to Animator
        playerAnim.SetFloat("Speed", targetAnimSpeed, 0.1f, Time.deltaTime);
        playerAnim.SetFloat("MoveX", moveInput.x, 0.1f, Time.deltaTime);
        playerAnim.SetFloat("MoveY", moveInput.y, 0.1f, Time.deltaTime);
        playerAnim.SetBool("IsAiming", isAiming);
        playerAnim.SetBool("IsGrounded", isGrounded);

        // Rotate Mixamo child model: Lock forward when aiming, face WASD when not aiming
        if (isAiming)
        {
            playerAnim.transform.localRotation = Quaternion.Slerp(
                playerAnim.transform.localRotation,
                Quaternion.identity,
                Time.deltaTime * 15f
            );
        }
        else if (moveInput.sqrMagnitude > 0.01f)
        {
            float targetAngle = Mathf.Atan2(moveInput.x, moveInput.y) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);

            playerAnim.transform.localRotation = Quaternion.Slerp(
                playerAnim.transform.localRotation,
                targetRotation,
                Time.deltaTime * 12f
            );
        }

        // Keep UpperBody Layer (index 1) active while Right-Click is held
        float targetWeight = isAiming ? 1f : 0f;
        currentUpperBodyWeight = Mathf.Lerp(currentUpperBodyWeight, targetWeight, Time.deltaTime * aimTransitionSpeed);

        if (playerAnim.layerCount > 1)
        {
            playerAnim.SetLayerWeight(1, currentUpperBodyWeight);
        }
    }

    void MovePlayer()
    {
        float currentSpeed = walkSpeed;
        if (isAiming)
        {
            currentSpeed = aimWalkSpeed;
        }
        else if (isSprinting)
        {
            currentSpeed = runSpeed;
        }

        Vector3 moveDirection = transform.forward * moveInput.y + transform.right * moveInput.x;
        if (moveDirection.sqrMagnitude > 1f) moveDirection.Normalize();

        Vector3 targetPosition = playerRb.position + moveDirection * currentSpeed * Time.fixedDeltaTime;
        targetPosition.x = Mathf.Clamp(targetPosition.x, -boundaryX, boundaryX);
        targetPosition.z = Mathf.Clamp(targetPosition.z, -boundaryZ, boundaryZ);

        playerRb.MovePosition(targetPosition);
    }

    void PlayerJump()
    {
        if (jumpRequested)
        {
            playerRb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
            jumpRequested = false;
        }
    }

    void PlayerLook()
    {
        Vector2 lookInput = inputActions.Player.Look.ReadValue<Vector2>();

        float mouseX = lookInput.x * cameraSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * cameraSensitivity * Time.deltaTime;

        // 1. Accumulate horizontal rotation and hard-lock X and Z to 0f so the player never tilts
        horizontalRotation += mouseX;
        transform.rotation = Quaternion.Euler(0f, horizontalRotation, 0f);

        // 2. Rotate the FocalPoint vertically to orbit the camera
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -30f, 60f);
        if (focalPoint != null)
        {
            focalPoint.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }
    }

    void ShootBullet(float currentFireRate)
    {
        if (activeWeapon == null || activeWeapon.firePoint == null || activeWeapon.bulletPrefab == null) return;

        // Cast a ray from the exact center of the screen (where your crosshair is)
        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPoint = Physics.Raycast(ray, out RaycastHit hit, 100f, cameraCollisionLayers)
                ? hit.point
                : ray.GetPoint(100f);

        Vector3 shootDirection = (targetPoint - activeWeapon.firePoint.position).normalized;

        // PREVENT EXTREME TWISTING: Check the angle between the barrel's natural forward and the needed trajectory
        float twistAngle = Vector3.Angle(activeWeapon.firePoint.forward, shootDirection);

        if (twistAngle > 25f) // If it twists more than 25 degrees, the enemy is awkwardly close
        {
            shootDirection = activeWeapon.firePoint.forward; // Just shoot straight forward from the gun
        }

        Instantiate(activeWeapon.bulletPrefab, activeWeapon.firePoint.position, Quaternion.LookRotation(shootDirection));

        if (activeWeapon != null)
        {
            Weapon currentWeapon = activeWeapon.GetComponent<Weapon>();
            if (currentWeapon != null)
            {
                currentWeapon.PlayFireEffects();
            }
        }

        // Apply snappy procedural recoil to the active weapon
        currentRecoilPos += activeWeapon.recoilKickBack;
        currentRecoilRot += activeWeapon.recoilRotation;

        if (playerAnim != null)
        {
            float animSpeed = fireClipLength / Mathf.Max(currentFireRate, 0.01f);
            playerAnim.SetFloat("FireSpeedMultiplier", animSpeed);
            playerAnim.CrossFadeInFixedTime("Firing Rifle", 0.04f, 1, 0f);
        }

        currentCrosshairSize = Mathf.Min(currentCrosshairSize + shootCrosshairImpulse, baseCrosshairSize + 60f);
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth = Mathf.Clamp(currentHealth - damage, 0f, maxHealth);
        if (playerHealthBar != null)
        {
            playerHealthBar.value = currentHealth;
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        // 1. Stop physics sliding and disable Aiming states
        isAiming = false;
        isSprinting = false;
        moveInput = Vector2.zero;

        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.isKinematic = true; // Prevents enemies from pushing the player's corpse around
        }

        // Disable collider so enemies stop attacking/bumping into the dead body
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // 2. Turn off UpperBody Aiming Layer (Layer 1) and trigger the Death animation on Base Layer
        if (playerAnim != null)
        {
            if (playerAnim.layerCount > 1)
            {
                playerAnim.SetLayerWeight(1, 0f);
            }
            playerAnim.SetFloat("Speed", 0f);
            playerAnim.SetBool("IsAiming", false);
            playerAnim.SetTrigger("Die");
        }

        // 3. Optional: Let the rifle drop to the ground when the player dies!
        if (activeWeapon != null)
        {
            activeWeapon.transform.SetParent(null);
            Rigidbody gunRb = activeWeapon.gameObject.AddComponent<Rigidbody>();
            BoxCollider gunCol = activeWeapon.gameObject.AddComponent<BoxCollider>();
            gunCol.size = new Vector3(0.15f, 0.25f, 0.8f);
            gunRb.AddForce(transform.forward * 1.5f + Vector3.up * 1f, ForceMode.Impulse);
        }

        // 4. Hide Crosshair & Show Game Over UI
        if (crosshairRect != null) crosshairRect.gameObject.SetActive(false);
        if (gameOverPanel != null) GameManager.Instance.TriggerGameOver();

        // Unlock mouse cursor for UI buttons
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ActivatePowerUp(float duration)
    {
        if (activePowerUpCoroutine != null)
        {
            StopCoroutine(activePowerUpCoroutine);
        }
        activePowerUpCoroutine = StartCoroutine(PowerUpRoutine(duration));
    }

    public IEnumerator PowerUpRoutine(float duration)
    {
        fireRateMultiplier = 0.5f; // Doubles firing speed across any active weapon

        if (powerUpFillImage != null)
        {
            powerUpFillImage.gameObject.SetActive(true);
            powerUpFillImage.fillAmount = 1f;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (powerUpFillImage != null)
            {
                // Smoothly drain the PowerUp bar from 100% down to 0%
                powerUpFillImage.fillAmount = 1f - (elapsed / duration);
            }
            yield return null;
        }

        fireRateMultiplier = 1f;
        if (powerUpFillImage != null)
        {
            powerUpFillImage.gameObject.SetActive(false);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }
}