using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputCollector : MonoBehaviour
{
    private PlayerControls controls;
    private PlayerInputData currentInput;
    private uint tickCounter;

    // Held states
    private Vector2 moveInput;
    private Vector2 pointerScreenPos;
    private bool jumpHeld;
    private bool attack1Held;
    private bool attack2Held;

    // Pressed-this-tick flags
    private bool jumpPressedThisTick;
    private bool attack1PressedThisTick;
    private bool attack2PressedThisTick;
    private bool reloadPressedThisTick;
    private bool dashPressedThisTick;

    [SerializeField] private Camera cam; // Assign in Inspector (defaults to Camera.main)

    void Awake()
    {
        controls = new PlayerControls();
        if (cam == null)
            cam = Camera.main;

        // Movement
        controls.Gameplay.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Gameplay.Move.canceled += ctx => moveInput = Vector2.zero;

        // Look (screen position of pointer)
        controls.Gameplay.Look.performed += ctx => pointerScreenPos = ctx.ReadValue<Vector2>();
        controls.Gameplay.Look.canceled += ctx => pointerScreenPos = Vector2.zero;

        // Jump
        controls.Gameplay.Jump.started += _ => jumpPressedThisTick = true;
        controls.Gameplay.Jump.performed += _ => jumpHeld = true;
        controls.Gameplay.Jump.canceled += _ => jumpHeld = false;

        // Attack 1
        controls.Gameplay.Attack1.started += _ => attack1PressedThisTick = true;
        controls.Gameplay.Attack1.performed += _ => attack1Held = true;
        controls.Gameplay.Attack1.canceled += _ => attack1Held = false;

        // Attack 2
        controls.Gameplay.Attack2.started += _ => attack2PressedThisTick = true;
        controls.Gameplay.Attack2.performed += _ => attack2Held = true;
        controls.Gameplay.Attack2.canceled += _ => attack2Held = false;

        // Reload
        controls.Gameplay.Reload.started += _ => reloadPressedThisTick = true;

        // Dash
        controls.Gameplay.Dash.started += _ => dashPressedThisTick = true;
    }
    public void FixedUpdate()
    {
        GatherInput();
    }

    public void GatherInput()
    {
        // Convert pointer screen position to world
        float zDistance = Mathf.Abs(cam.transform.position.z - transform.position.z);
        Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(pointerScreenPos.x, pointerScreenPos.y, zDistance));

        // Vector from player → cursor
        Vector2 toCursor = (Vector2)(worldPos - transform.position);

        // Unit direction vector
        Vector2 lookDir = toCursor.sqrMagnitude > 0.000001f ? toCursor.normalized : Vector2.right;

        // Angle in degrees (0° = right, 90° = up)
        float lookAngleDeg = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;

        // Fill the struct
        currentInput = new PlayerInputData
        {
            horizontalInput = moveInput.x,
            jumpPressed = jumpPressedThisTick,
            jumpHeld = jumpHeld,
            attack1Pressed = attack1PressedThisTick,
            attack1Held = attack1Held,
            attack2Pressed = attack2PressedThisTick,
            attack2Held = attack2Held,
            reloadPressed = reloadPressedThisTick,
            lookDirection = lookDir,
            lookAngleDeg = lookAngleDeg,
            dashPressed = dashPressedThisTick
        };


    }

    public PlayerInputData RetrieveInput()
    {
        // Reset one-tick flags
        jumpPressedThisTick = false;
        attack1PressedThisTick = false;
        attack2PressedThisTick = false;
        reloadPressedThisTick = false;
        dashPressedThisTick = false;

        return currentInput;
    }

    public PlayerInputData CurrentInput => currentInput;

    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();
}
