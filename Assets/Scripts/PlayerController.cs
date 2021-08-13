using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Defaults;

public class PlayerController : MonoBehaviour
{
    private CharacterController characterController;
    private DefaultInput defaultInput;
    private Vector2 rawMovement;
    private Vector2 rawLook;

    private Vector3 cameraRotation;
    private Vector3 playerRotation;
    
    [Header("References")]
    public Transform playerCam;
    public Transform playerTransform;
    public Transform foot;
    
    [Header("Settings")]
    public PlayerSettings playerSettings;
    public LayerMask playerMask;
    public float lookClampYMin = -90;
    public float lookClampYMax = 90;

    [Header("Gravity")]
    public float gravity = 0.1f;
    public float minGravity = -0.1f;
    private float playerGravity;

    public Vector3 jumpForce;
    private Vector3 jumpForceVelo;

    [Header("Stance")]
    public PlayerStances stance;
    public float stanceSmoothing;
    public PlayerStance standHeight;
    public PlayerStance crouchHeight;
    public float stanceErrorMargin = 0.05f;

    private float cameraHeight;
    private float cameraHeightVelo;

    private float jumpCooldown = 0.25f;
    private bool readyToJump = true;
    private bool jumping = false;

    private bool isSprinting = false;

    private Vector3 moveSpeed;
    private Vector3 moveSpeedVelo;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Awake()
    {
        // sensitivity
        playerSettings.lookXSensitivity = 25f;
        playerSettings.lookYSensitivity = 25f;

        // smoothing
        playerSettings.movementSmoothing = 0.15f;
        playerSettings.fallSmoothing = 0.4f;

        // speed modifiers
        playerSettings.speedModifier = 1f;
        playerSettings.crouchSpeedModifier = 0.6f;
        playerSettings.fallSpeedModifier = 0.4f;

        // walk speed
        playerSettings.walkForwardSpeed = 10f;
        playerSettings.walkBackwardSpeed = 5f;
        playerSettings.walkStrafeSpeed = 7f;

        // sprint speed
        playerSettings.sprintForwardSpeed = 15f;
        playerSettings.sprintStrafeSpeed = 12f;

        // jump
        playerSettings.jumpHeight = 8f;
        playerSettings.jumpFalloff = 1f;

        // camera heights
        standHeight.CameraHeight = 0.57f;
        crouchHeight.CameraHeight = 0.57f;

        defaultInput = new DefaultInput();
        
        defaultInput.Character.Movement.performed += e => rawMovement = e.ReadValue<Vector2>();
        defaultInput.Character.Look.performed += e => rawLook = e.ReadValue<Vector2>();

        defaultInput.Character.Jump.performed += e => Jump();
        defaultInput.Character.Jump.performed += e => jumping = true;
        defaultInput.Character.Crouch.performed += e => Crouch();
        defaultInput.Character.Sprint.performed += e => ToggleSprint();
        defaultInput.Character.SprintReleased.performed += e => StopSprint();

        defaultInput.Enable();
        
        cameraRotation = playerCam.localRotation.eulerAngles;
        playerRotation = transform.localRotation.eulerAngles;

        characterController = GetComponent<CharacterController>();

        cameraHeight = playerCam.localPosition.y;
    }
    
    private void FixedUpdate()
    {
        Movement();
    }

    private void Update()
    {
        Look();
        PreJump();
        Stance();
    }
    
    private void Look()
    {
        playerRotation.y += playerSettings.lookXSensitivity * (playerSettings.lookXInverted ? -rawLook.x : rawLook.x) * Time.deltaTime;
        transform.localRotation = Quaternion.Euler(playerRotation);

        cameraRotation.x += playerSettings.lookYSensitivity * (playerSettings.lookYInverted ? rawLook.y : -rawLook.y) * Time.deltaTime;
        cameraRotation.x = Mathf.Clamp(cameraRotation.x, lookClampYMin, lookClampYMax);
        
        playerCam.localRotation = Quaternion.Euler(cameraRotation);
    }
    
    private void Movement()
    {
        //if (readyToJump && jumping) Jump();

        if (rawMovement.y <= 0.2f)
            isSprinting = false;

        var forwardSpeed = playerSettings.walkForwardSpeed;
        var strafeSpeed = playerSettings.walkStrafeSpeed;

        if (isSprinting)
        {
            forwardSpeed = playerSettings.sprintForwardSpeed;
            strafeSpeed = playerSettings.sprintStrafeSpeed;
        }

        if (!characterController.isGrounded)
            playerSettings.speedModifier = playerSettings.fallSpeedModifier;
        else if (stance == PlayerStances.Crouch)
            playerSettings.speedModifier = playerSettings.crouchSpeedModifier;
        else
            playerSettings.speedModifier = 1f;

        forwardSpeed *= playerSettings.speedModifier;
        strafeSpeed *= playerSettings.speedModifier;

        moveSpeed = Vector3.SmoothDamp(moveSpeed, new Vector3(strafeSpeed * rawMovement.x * Time.deltaTime, 0, forwardSpeed * rawMovement.y * Time.deltaTime), ref moveSpeedVelo, characterController.isGrounded ? playerSettings.movementSmoothing : playerSettings.fallSmoothing);
        var movementSpeed = transform.TransformDirection(moveSpeed);

        if (playerGravity > minGravity)
            playerGravity -= gravity * Time.deltaTime;

        if (playerGravity < -0.1f && characterController.isGrounded)
        {
            playerGravity = -0.1f;
            jumpForce = Vector3.zero;
        }

        movementSpeed.y += playerGravity;
        movementSpeed += jumpForce * Time.deltaTime;
        characterController.Move(movementSpeed);
    }

    private void Stance()
    {
        var currentStance = standHeight;

        if (stance == PlayerStances.Crouch)
            currentStance = crouchHeight;

        cameraHeight = Mathf.SmoothDamp(playerCam.localPosition.y, currentStance.CameraHeight, ref cameraHeightVelo, stanceSmoothing);
        playerCam.localPosition = new Vector3(playerCam.localPosition.x, cameraHeight, playerCam.localPosition.z);
    }

    private void PreJump()
    {
        if (characterController.isGrounded) return;
        jumpForce = Vector3.SmoothDamp(jumpForce, Vector3.zero, ref jumpForceVelo, playerSettings.jumpFalloff);
    }

    private void Jump()
    {
        if (!characterController.isGrounded)
            return;

        if (stance == PlayerStances.Crouch)
        {
            if (StanceCheck(standHeight.collider.height))
                return;

            stance = PlayerStances.Stand;
            playerTransform.localScale = new Vector3(1f, 1.5f, 1f);
            return;
        }

        jumping = true;
        readyToJump = false;
        jumpForce = Vector3.up * playerSettings.jumpHeight;
        playerGravity = 0f;

        Invoke(nameof(ResetJump), jumpCooldown);
    }

    private void ResetJump()
    {
        readyToJump = true;
        jumping = false;
    }

    private void Crouch()
    {
        if (stance == PlayerStances.Crouch)
        {
            if (StanceCheck(standHeight.collider.height))
                return;

            stance = PlayerStances.Stand;
            playerTransform.localScale = new Vector3(1f, 1.5f, 1f);
            return;
        }

        if (StanceCheck(crouchHeight.collider.height))
            return;

        stance = PlayerStances.Crouch;
        playerTransform.localScale = new Vector3(1f, 1f, 1f);
    }

    private bool StanceCheck(float checkHeight)
    {
        var start = new Vector3(foot.position.x, foot.position.y + characterController.radius + stanceErrorMargin, foot.position.z);
        var end = new Vector3(foot.position.x, foot.position.y - characterController.radius - stanceErrorMargin + checkHeight, foot.position.z);

        return Physics.CheckCapsule(start, end, characterController.radius, playerMask);
    }

    private void ToggleSprint()
    {
        if (rawMovement.y <= 0.2f)
        {
            isSprinting = false;
            return;
        }
        isSprinting = !isSprinting;
    }

    private void StopSprint()
    {
        if (playerSettings.sprintHold)
            isSprinting = false;
    }
}
