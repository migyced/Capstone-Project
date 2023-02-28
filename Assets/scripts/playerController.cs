using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using Extensions;
using States;
// This code is based on sample unity movement API code.

[RequireComponent(typeof(CharacterController), typeof(PlayerInput))]
public class playerController : MonoBehaviour, IFrameCheckHandler
{
    [SerializeField]
    public float playerSpeed = 2.5f;
    [SerializeField]
    private float walkSpeed = 1.5f;
    [SerializeField]
    private float walkThreshold = 0.5f;
    [SerializeField]
    private float jumpHeight = 1.0f;
    [SerializeField]
    private float gravityValue = -9.81f;
    [SerializeField]
    public float turnSmoothTime = 0.0f;
    [SerializeField]
    public float rollCooldown = 3.0f;
    private float rollCooldownTimer = 0.0f;
    [SerializeField]
    private FrameParser jumpClip;
    [SerializeField]
    private FrameChecker jumpFrameChecker;

    float turnSmoothVelocity;

    public CharacterController controller;
    private PlayerInput playerInput;
    private RollManager rollManager;
    private BroomAttackManager attackManager;
    private GameObject model;
    private GameObject metarig;
    private GameObject hip;
    private Vector3 playerVelocity;
    private float lastY;
    private float lastRootY;
    private bool groundedPlayer;
    private bool inJumpsquat = false;

    public IEnumerator coroutine;

    private Transform cam;

    public InputAction moveAction;
    public InputAction walkAction;
    public InputAction jumpAction;
    public InputAction rollAction;
    public InputAction attackAction;

    public States.PlayerStates state;

    //Animation stuff
    Animator animator;

    // jump framedata management
    public void onActiveFrameStart() 
    {
        playerVelocity.y += Mathf.Sqrt(jumpHeight * -3.0f * gravityValue);
    }
    public void onActiveFrameEnd() 
    {
        inJumpsquat = false;
    }
    public void onAttackCancelFrameStart() { }
    public void onAttackCancelFrameEnd() { }
    public void onAllCancelFrameStart() { }
    public void onAllCancelFrameEnd() { }
    public void onLastFrameStart(){}
    public void onLastFrameEnd(){}


    private void Awake()
    {
        controller  = gameObject.GetComponent<CharacterController>();
        playerInput = gameObject.GetComponent<PlayerInput>();
        animator    = gameObject.GetComponentInChildren<Animator>();
        model       = transform.Find("maid68").gameObject;
        metarig     = transform.Find("maid68/metarig").gameObject;
        hip         = transform.Find("maid68/metarig/hip").gameObject;
        rollManager = gameObject.GetComponent<RollManager>();
        attackManager = gameObject.GetComponent<BroomAttackManager>();
        cam = Camera.main.transform;
        lastRootY = hip.transform.localPosition.y;
        // add actions from playerControls here
        moveAction   = playerInput.actions["Run"];
        jumpAction   = playerInput.actions["Jump"];
        attackAction = playerInput.actions["Attack"];
        walkAction   = playerInput.actions["Walk"];
        rollAction   = playerInput.actions["Roll"];

        jumpClip.initialize();
        jumpFrameChecker.initialize(this, jumpClip);
        SetState(States.PlayerStates.Idle);
        // coroutine = attackManager.handleAttacks();
    }

    void Update()
    {
        //Debug.Log(state);
        lastY = controller.transform.position.y;
        jumpFrameChecker.checkFrames();
        groundedPlayer = controller.isGrounded;

        // handle edge case where player lands without ever gaining downward velocity
        if (groundedPlayer && !inJumpsquat) { animator.SetBool("Jumping", false); }
        // stop falling animation on land
        if (groundedPlayer && playerVelocity.y < 0){
            playerVelocity.y = -2.0f;
            animator.SetBool("Falling", false);
        }

        // store direction input 
        Vector2 input = moveAction.ReadValue<Vector2>();

        rollCooldownTimer = Mathf.Max(0f, rollCooldownTimer - Time.deltaTime);
        if (state == States.PlayerStates.Rolling) {
            rollManager.updateMe();
        }

        if (state == States.PlayerStates.Attacking) {
            attackManager.updateMe();
        }
        if (state != States.PlayerStates.Attacking && state !=States.PlayerStates.Rolling) {
            SetState(States.PlayerStates.Idle);
            model.transform.localPosition = Vector3.zero;
            
            if (input.x != 0 || input.y != 0) { // if there is movement input
                Debug.Log("moving");
                bool walking = false;
                Vector3 move = new Vector3(input.x, 0, input.y);
                if (move.magnitude < walkThreshold || walkAction.triggered) { walking = true; }

                // store calculated model rotation
                move = RotatePlayer(input);

                if (walking) {
                    controller.Move(move * Time.deltaTime * walkSpeed);
                    animator.SetBool("Walking", true);
                    animator.SetBool("Running", false);
                }
                else {
                    controller.Move(move * Time.deltaTime * playerSpeed);
                    animator.SetBool("Running", true);
                    animator.SetBool("Walking", false);
                }
            }
            else
            {
                animator.SetBool("Running", false);
                animator.SetBool("Walking", false);
            }

            // Changes the height position of the player
            if (jumpAction.triggered && groundedPlayer && state != States.PlayerStates.Jumping) {
                Jump();
            }

            // animate falling player
            if (controller.transform.position.y <= lastY && !groundedPlayer && !inJumpsquat)
            {
                //Debug.Log(transform.position.y);
                //Debug.Log(lastY);
                animator.SetBool("Jumping", false);
                animator.SetBool("Falling", true);
            }

            if (attackAction.triggered && !inJumpsquat)
            {
                Debug.Log("attacking");
                // log current root bone position
                lastRootY = hip.transform.localPosition.y;
                //set state to attacking 
                SetState(States.PlayerStates.Attacking);
                // launch animations and attacks
                attackManager.handleAttacks();
            }

            if (rollAction.triggered && rollCooldownTimer == 0 && groundedPlayer)
            {
                SetState(States.PlayerStates.Rolling);
                rollCooldownTimer = rollCooldown;
                rollManager.Roll();
            }
        }

        //TO DO: check if the player is in a valid attack state
        
        // add gravity
        ApplyGravity();
    }

    private void ApplyGravity(){
        playerVelocity.y += gravityValue * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }

    public void SetState(States.PlayerStates newState){
        state = newState;
    }

    public void Jump() {
        SetState(States.PlayerStates.Jumping);
        animator.SetBool("Jumping", true);
        inJumpsquat = true;
        jumpFrameChecker.initCheck();
        animator.Play("jump", 0);
    }

    public Vector3 RotatePlayer(Vector2 input){
        // calculate model rotation
        float targetAngle = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg + cam.eulerAngles.y; // from front facing position to direction pressed + camera angle.
        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
        transform.rotation = Quaternion.Euler(0f, angle, 0f); // apply rotation

        // move according to calculated target angle
        return Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
    }

    // Simulates root motion when called during an animation.
    public void MoveRoot() {
        if (lastRootY != hip.transform.localPosition.y) {
            float diff = lastRootY - hip.transform.localPosition.y;
            Debug.Log(diff);
            controller.Move(transform.forward * diff * metarig.transform.localScale.y * transform.localScale.z);
            model.transform.localPosition = model.transform.localPosition + (Vector3.forward * -diff * metarig.transform.localScale.y);
        }
        lastRootY = hip.transform.localPosition.y;
    }
}