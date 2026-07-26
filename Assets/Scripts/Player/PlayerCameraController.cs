using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCameraController : MonoBehaviour
{
    [Header("Look Settings")]
    public float mouseSense = 4f;
    public Transform playerBody;

    [Header("Head Anchor Settings")]
    public Transform headBone;
    public Vector3 firstPersonEyeOffset = new Vector3(0f, 0.05f, 0.1f);

    [Header("First Person Visibility & Clipping")]
    public LayerMask playerMeshLayer;
    public bool completelyHideMeshInFP = true;
    public LayerMask environmentalLayers;
    public float firstPersonBufferRadius = 0.25f;
    public float minFirstPersonGroundHeight = 0.25f;

    private int originalCullingMask;

    [Header("Look Behind Settings")]
    public float lookBehindPanSpeed = 15f;

    [Header("Perspective")]
    public float thirdPersonDistance = 4f;
    public float perspectiveSwitchSpeed = 10f;
    public LayerMask wallClippingLayers;

    [Header("Third Person Framing")]
    public float characterTurnSpeed = 10f;
    public float thirdPersonHeightOffset = 0.45f;
    public float thirdPersonPitchOffset = 10f;
    public float minThirdPersonX = -30f;
    public float maxThirdPersonX = 60f;

    [Header("Ragdoll Camera Settings")]
    [Tooltip("Extra vertical lift applied when tracking the ragdoll so the camera doesn't hug the floor.")]
    public float ragdollHeightOffset = 1.5f;
    [Tooltip("Extra distance to back the camera away from the dead body.")]
    public float ragdollDistanceModifier = 1.5f;
    [Tooltip("Fixed camera angle looking down slightly at the body once dead.")]
    public float ragdollPitchAngle = 25f;

    private float xRotation = 0f;
    private float yRotation = 0f;
    private float currentCameraY = 0f;

    public bool IsThirdPerson => isThirdPerson;
    public float GetCleanYRotation => yRotation;

    private bool isThirdPerson = false;
    private float currentCameraDistance = 0f;
    private float currentHeightOffset = 0f;
    private float currentPitchOffset = 0f;

    private Vector3 defaultLocalPosition;
    private PlayerController playerMovement;
    private PlayerMovementModifiers movementModifiers;
    private Camera targetCameraComponent;

    private float firstPersonHeadWeight = 0f;

    private Transform currentTargetTransform;
    private bool trackingRagdoll = false;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        defaultLocalPosition = transform.localPosition;
        targetCameraComponent = GetComponent<Camera>();

        if (targetCameraComponent != null)
        {
            originalCullingMask = targetCameraComponent.cullingMask;
            targetCameraComponent.nearClipPlane = 0.01f;
        }

        if (playerBody != null)
        {
            playerMovement = playerBody.GetComponent<PlayerController>();
            movementModifiers = playerBody.GetComponent<PlayerMovementModifiers>();
            currentTargetTransform = playerBody;
        }
    }

    void Update()
    {
        if (trackingRagdoll) return;

        if (Keyboard.current[OptionsManager.TogglePerspective].wasPressedThisFrame)
        {
            isThirdPerson = !isThirdPerson;
        }

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        float clampedX = Mathf.Clamp(mouseDelta.x, -100f, 100f);
        float clampedY = Mathf.Clamp(mouseDelta.y, -100f, 100f);

        xRotation -= clampedY * mouseSense * 0.01f;
        xRotation = isThirdPerson ? Mathf.Clamp(xRotation, minThirdPersonX, maxThirdPersonX) : Mathf.Clamp(xRotation, -90f, 90f);
        yRotation += clampedX * mouseSense * 0.01f;
    }

    void LateUpdate()
    {
        if (currentTargetTransform == null) return;

        if (targetCameraComponent != null)
        {
            float targetFOV = isThirdPerson ? 60f : OptionsManager.FirstPersonFOV;
            targetCameraComponent.fieldOfView = Mathf.Lerp(targetCameraComponent.fieldOfView, targetFOV, perspectiveSwitchSpeed * Time.deltaTime);
        }

        bool strictlyWallSliding = movementModifiers != null && movementModifiers.IsWallSliding && !movementModifiers.IsWallRunning;

        bool isMoving = false;
        float moveInputX = 0f;
        float moveInputZ = 0f;

        if (playerMovement != null && !trackingRagdoll)
        {
            if (Keyboard.current[OptionsManager.MoveLeft].isPressed) moveInputX = -1f;
            if (Keyboard.current[OptionsManager.MoveRight].isPressed) moveInputX = 1f;
            if (Keyboard.current[OptionsManager.MoveBackward].isPressed) moveInputZ = -1f;
            if (Keyboard.current[OptionsManager.MoveForward].isPressed) moveInputZ = 1f;

            isMoving = (moveInputX != 0f || moveInputZ != 0f);
        }

        bool isLookingBehind = !trackingRagdoll && Keyboard.current[OptionsManager.LookBehind].isPressed;

        if (playerMovement != null && playerMovement.anim != null)
        {
            playerMovement.anim.SetBool("IsLookingBehind", isLookingBehind);
        }

        if (targetCameraComponent != null)
        {
            if (!isThirdPerson && (isLookingBehind || completelyHideMeshInFP))
            {
                targetCameraComponent.cullingMask = originalCullingMask & ~playerMeshLayer.value;
            }
            else
            {
                targetCameraComponent.cullingMask = originalCullingMask;
            }
        }

        if (!strictlyWallSliding && !trackingRagdoll)
        {
            if (!isThirdPerson)
            {
                playerBody.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            }
            else if (isMoving && !isLookingBehind)
            {
                Vector3 camForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
                Vector3 camRight = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;

                Vector3 moveDirection = (camRight * moveInputX + camForward * moveInputZ).normalized;

                // --- FIXED BACKWARD RUN SNAPPING LOOP ---
                // If moving predominantly backwards, we do not force the mesh orientation to snap violently away
                if (moveDirection.sqrMagnitude > 0.001f && moveInputZ >= -0.1f)
                {
                    Quaternion targetBodyRotation = Quaternion.LookRotation(moveDirection);
                    playerBody.rotation = Quaternion.Slerp(playerBody.rotation, targetBodyRotation, characterTurnSpeed * Time.deltaTime);
                }
            }
        }

        float targetCameraY = isLookingBehind ? 180f : 0f;
        currentCameraY = Mathf.LerpAngle(currentCameraY, targetCameraY, lookBehindPanSpeed * Time.deltaTime);

        float targetHeight = isThirdPerson ? thirdPersonHeightOffset : 0f;
        float targetPitch = isThirdPerson ? thirdPersonPitchOffset : 0f;

        if (trackingRagdoll)
        {
            targetHeight = ragdollHeightOffset;
            targetPitch = ragdollPitchAngle;
        }

        currentHeightOffset = Mathf.Lerp(currentHeightOffset, targetHeight, perspectiveSwitchSpeed * Time.deltaTime);
        currentPitchOffset = Mathf.Lerp(currentPitchOffset, targetPitch, perspectiveSwitchSpeed * Time.deltaTime);

        float absoluteYRotation = isThirdPerson ? yRotation : currentTargetTransform.eulerAngles.y;
        transform.rotation = Quaternion.Euler(trackingRagdoll ? targetPitch : (xRotation + currentPitchOffset), absoluteYRotation + currentCameraY, 0f);

        float targetWeight = isThirdPerson ? 0f : 1f;
        firstPersonHeadWeight = Mathf.Lerp(firstPersonHeadWeight, targetWeight, perspectiveSwitchSpeed * Time.deltaTime);

        // --- FIXED CAMERA PIVOT HEIGHT POST-MORTEM ---
        Vector3 capsulePivotOrigin;
        if (trackingRagdoll)
        {
            // Lift origin upward along the world Y-axis so the camera target floats safely above the ground
            capsulePivotOrigin = currentTargetTransform.position + (Vector3.up * currentHeightOffset);
        }
        else
        {
            capsulePivotOrigin = currentTargetTransform.TransformPoint(defaultLocalPosition) + (Vector3.up * currentHeightOffset);
        }

        Vector3 headEyeOrigin = capsulePivotOrigin;

        if (headBone != null && !trackingRagdoll)
        {
            headEyeOrigin = headBone.TransformPoint(firstPersonEyeOffset);

            if (!isThirdPerson)
            {
                Vector3 checkDirection = headEyeOrigin - capsulePivotOrigin;
                float checkDistance = checkDirection.magnitude;

                if (checkDistance > 0.01f)
                {
                    checkDirection.Normalize();
                    if (Physics.SphereCast(capsulePivotOrigin, 0.12f, checkDirection, out RaycastHit wallHit, checkDistance + firstPersonBufferRadius, environmentalLayers))
                    {
                        float safeDistance = Mathf.Max(0f, wallHit.distance - firstPersonBufferRadius);
                        headEyeOrigin = capsulePivotOrigin + (checkDirection * safeDistance);
                    }
                }

                Vector3 rayStart = new Vector3(headEyeOrigin.x, headEyeOrigin.y + 0.5f, headEyeOrigin.z);
                if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit groundHit, 2f, environmentalLayers))
                {
                    float absoluteMinimumY = groundHit.point.y + minFirstPersonGroundHeight;
                    if (headEyeOrigin.y < absoluteMinimumY) headEyeOrigin.y = absoluteMinimumY;
                }
            }
        }

        Vector3 finalOriginPosition = Vector3.Lerp(capsulePivotOrigin, headEyeOrigin, firstPersonHeadWeight);

        float baseDistance = isThirdPerson ? thirdPersonDistance : 0f;
        float targetDistance = trackingRagdoll ? (baseDistance + ragdollDistanceModifier) : baseDistance;

        if (isThirdPerson && Physics.Raycast(finalOriginPosition, -transform.forward, out RaycastHit hit, targetDistance, wallClippingLayers))
        {
            targetDistance = Mathf.Max(0.2f, hit.distance - 0.2f);
        }

        currentCameraDistance = Mathf.Lerp(currentCameraDistance, targetDistance, perspectiveSwitchSpeed * Time.deltaTime);
        transform.position = finalOriginPosition - (transform.forward * currentCameraDistance);
    }

    public void SwitchToRagdollTarget(Transform newBoneTarget)
    {
        currentTargetTransform = newBoneTarget;
        isThirdPerson = true;
        trackingRagdoll = true;
    }
}