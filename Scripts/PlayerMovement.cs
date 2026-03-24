using JetBrains.Annotations;
using System.Collections;
using System.Runtime.CompilerServices;
using Unity.Collections;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.ProBuilder.MeshOperations;
using UnityEngine.U2D;




public class PlayerMovement : MonoBehaviour
{

    [Header("Movement")]
    private float moveSpeed;
    public float walkSpeed;
    public float sprintSpeed;
    public float dashSpeed;
    public float dashSpeedChangeFactor;
    public float slideSpeed;
    public float wallrunSpeed;
    public float swingSpeed;
    public float dot;


    private float desiredMoveSpeed;
    private float lastDesiredMoveSpeed;
    private MovementState lastState;
    private bool keepMomentum;

    public float speedIncreaseMultiplier;
    public float slopeIncreaseMultiplier;


    public float groundDrag;
    public bool slidingUpSlope;
    public float slowedTimer = 5f;

    public float slowMaxCooldown = 10f;
    public float slowCurrentCooldown = 0f;


    [Header("Jumping")]
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool readyToJump = true;
    public bool canJump = true;

    [Header("Crouching")]
    public float crouchSpeed;
    public float crouchYScale;
    private float startYScale;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.C;
    public KeyCode timeSlowKey = KeyCode.T;
    public KeyCode PauseMenuKey = KeyCode.O;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    public bool isGrounded;

    [Header("SlopeHandling")]
    public float maxSlopeAngle;
    private RaycastHit slopeHit;
    private bool exitingSlope;
    public float angle;
    public float slopAngle;
    private RaycastHit hit;

    [Header("References")]
    public Climbing climbingScript;
    public Animator animator;
    public GameObject speedyLines;
    public Transform slopeAngle;
    public Image timeSlowBG;
    public GameObject PauseMenu;



    [Header("Camera Effects")]
    public PlayerCam cam;



    public Transform orientation;

    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;

    Rigidbody rb;



    public MovementState state;
    public enum MovementState
    {
        freeze,
        swinging,
        walking,
        sprinting,
        wallrunning,
        crouching,
        dashing,
        sliding,
        air
    }

    public bool swinging;
    public bool freeze;
    public bool dashing;
    public bool sliding;
    public bool wallrunning;
    public bool timeSlowed;

    public bool activeGrapple;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;


        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;


        startYScale = transform.localScale.y;

        slowCurrentCooldown = slowMaxCooldown;

        PauseMenu.SetActive(false);


    }

    // Update is called once per frame
    void Update()
    {
        //ground check
        isGrounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);

        if (isGrounded)
        {
            animator.SetBool("IsGrounded", true);
        }

        MyInput();
        SpeedControl();
        StateHandler();

        //handle drag
        if (state == MovementState.walking && !activeGrapple && !swinging || state == MovementState.sprinting && !activeGrapple && !swinging || state == MovementState.crouching && !activeGrapple && !swinging)
        {
            rb.linearDamping = groundDrag;
        }
        else
        {
            rb.linearDamping = 0;
        }


        //Speedy Lines
        if (state == MovementState.wallrunning || state == MovementState.air || state == MovementState.sliding || state == MovementState.swinging || state == MovementState.dashing)
        {
            speedyLines.SetActive(true);
        }
        else
        {
            speedyLines.SetActive(false);
        }

        if (Physics.Raycast(slopeAngle.position, Vector3.down, out hit, playerHeight * 0.5f + 0.3f))
        {
            slopAngle = Vector3.Angle(Vector3.up, hit.normal);

        }

        IsSlidingUpSlope(moveDirection);

        if (dot < 0)
        {
            slidingUpSlope = true;
        }
        else
        {
            slidingUpSlope = false;
        }


        //Time Slow
        timeSlowBG.fillAmount = slowCurrentCooldown / slowMaxCooldown;

        //slow cooldown
        if (!timeSlowed && slowCurrentCooldown != slowMaxCooldown)
        {
            slowCurrentCooldown += Time.deltaTime;

            if (slowCurrentCooldown >= slowMaxCooldown)
            {
                slowCurrentCooldown = slowMaxCooldown;
            }

        }

        //actual slow
        if (Input.GetKeyDown(timeSlowKey) && slowCurrentCooldown >= slowMaxCooldown)
        {
            Time.timeScale = 0.5f;
            timeSlowed = true;
            slowCurrentCooldown = 0;
        }

        if (timeSlowed)
        {
            slowedTimer -= Time.deltaTime;
        }

        if (slowedTimer <= 0)
        {
            Time.timeScale = 1f;
            timeSlowed = false;
            slowedTimer = 5;
        }

        if (Input.GetKeyDown(PauseMenuKey))
        {
            if (PauseMenu.activeInHierarchy)
            {
                PauseMenu.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                PauseMenu.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

    }

    private void FixedUpdate()
    {
        MovePlayer();
    }



    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        //Check for jump input
        if (Input.GetKey(jumpKey) && readyToJump && isGrounded && !slidingUpSlope)
        {
            readyToJump = false;

            Jump();

            Invoke(nameof(ResetJump), jumpCooldown);
        }

        // start crouch
        if (Input.GetKeyDown(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            Debug.Log(transform.localScale);
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
            Debug.Log("Crouching");
        }

        // stop crouching
        if (Input.GetKeyUp(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
        }

    }

    private void StateHandler()
    {
        //Mode - Wallrunning
        if (wallrunning)
        {
            state = MovementState.wallrunning;
            desiredMoveSpeed = wallrunSpeed;
        }

        //Mode -  Sliding
        else if (sliding)
        {
            state = MovementState.sliding;

            animator.SetBool("IsWalking", false);
            animator.SetBool("IsRunning", false);
            animator.SetBool("IsJumping", false);

            if (OnSlope() && rb.linearVelocity.y < 0.1f)
                desiredMoveSpeed = slideSpeed;

            else
                desiredMoveSpeed = sprintSpeed;
        }

        //Mode - Swinging
        else if (swinging)
        {
            state = MovementState.swinging;
            desiredMoveSpeed = swingSpeed;

        }

        //Mode - Dashing
        else if (dashing)
        {
            state = MovementState.dashing;
            desiredMoveSpeed = dashSpeed;
            speedChangeFactor = dashSpeedChangeFactor;
        }

        else if (freeze)
        {
            state = MovementState.freeze;
            moveSpeed = 0;
            rb.linearVelocity = Vector3.zero;
        }

        //Mode - Crouching
        else if (Input.GetKey(crouchKey))
        {
            state = MovementState.crouching;
            desiredMoveSpeed = crouchSpeed;
        }

        //Mode - Sprinting
        else if (isGrounded && Input.GetKey(sprintKey))
        {
            state = MovementState.sprinting;
            desiredMoveSpeed = sprintSpeed;

            if (rb.linearVelocity.magnitude > 7)
            {
                animator.SetBool("IsRunning", true);
                animator.SetBool("IsWalking", false);
            }
            else if (rb.linearVelocity.magnitude < 7)
            {
                animator.SetBool("IsRunning", false);
            }
        }

        //Mode - Walking
        else if (isGrounded)
        {
            if (animator.GetBool("IsJumping") == false)
            {
                state = MovementState.walking;
                desiredMoveSpeed = walkSpeed;
                if (rb.linearVelocity.magnitude > 0.5)
                {
                    animator.SetBool("IsWalking", true);
                    animator.SetBool("IsRunning", false);
                }
                else if (rb.linearVelocity.magnitude <= 0.5)
                {
                    animator.SetBool("IsWalking", false);
                    animator.SetBool("IsRunning", false);
                }

            }




        }

        //Mode - Air

        else
        {
            state = MovementState.air;

            animator.SetBool("IsGrounded", false);
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsRunning", false);

        }



        if (lastState == MovementState.dashing) keepMomentum = true;

        if (dashing)
        {
            StopAllCoroutines();
            StartCoroutine(SmoothlyLerpMoveSpeed());
        }


        //check if desired move speed has changed drastically 
        if (Mathf.Abs(desiredMoveSpeed - lastDesiredMoveSpeed) > 2f && moveSpeed != 0)
        {
            StopAllCoroutines();
            StartCoroutine(SmoothlyLerpMoveSpeed());
        }
        else
        {

            moveSpeed = desiredMoveSpeed;
        }

        lastState = state;
        lastDesiredMoveSpeed = desiredMoveSpeed;
    }

    private float speedChangeFactor;

    private IEnumerator SmoothlyLerpMoveSpeed()
    {
        //smoothly lerp movespeed to desired value
        float time = 0;
        float difference = Mathf.Abs(desiredMoveSpeed - moveSpeed);
        float startValue = moveSpeed;

        //float boostFactor = speedChangeFactor;

        while (time < difference)
        {
            moveSpeed = Mathf.Lerp(startValue, desiredMoveSpeed, time / difference);

            if (OnSlope())
            {
                float slopeAngle = Vector3.Angle(Vector3.up, slopeHit.normal);
                float slopeAngleIncrease = 1 + (slopeAngle / 90f);

                time += Time.deltaTime * speedIncreaseMultiplier * slopeIncreaseMultiplier * slopeAngleIncrease;
            }
            else if (state == MovementState.walking || state == MovementState.sprinting)
            {
                moveSpeed = desiredMoveSpeed;

            }
            else
            {
                time += Time.deltaTime * speedIncreaseMultiplier;
            }

            yield return null;
        }

        moveSpeed = desiredMoveSpeed;
    }

    private void MovePlayer()
    {
        if (swinging) return;
        if (activeGrapple) return;
        if (state == MovementState.dashing) return;
        if (climbingScript.exitingWall) return;


        //calculate movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        // on slope
        if (OnSlope())
        {
            rb.AddForce(GetSlopeMoveDirection(moveDirection) * moveSpeed * 20f, ForceMode.Force);

            if (rb.linearVelocity.y > 0)
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
        }



        //on ground
        else if (isGrounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);

        //in air
        else if (!isGrounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);

        //turn gravity off whilst on a slope
        if (!wallrunning) rb.useGravity = !OnSlope();
    }

    private void SpeedControl()
    {
        if (activeGrapple) return;

        //limiting speed on slope
        if (OnSlope() && !exitingSlope)
        {
            if (rb.linearVelocity.magnitude > moveSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * moveSpeed;
            }
        }

        //limiting speed on ground or in air
        else if (!swinging)
        {
            Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
            }
        }

    }

    private void Jump()
    {

        if (canJump)
        {
            exitingSlope = true;


            animator.SetBool("IsJumping", true);
            animator.SetBool("IsGrounded", false);
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsRunning", false);



            //reset y velocity
            //      rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);


            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

    }

    private void ResetJump()
    {
        readyToJump = true;

        exitingSlope = false;
    }

    private bool enableMovementOnNextTouch;

    public void JumpToPosition(Vector3 targetPostision, float trajectoryHeight)
    {
        activeGrapple = true;

        velocityToSet = CalculateJumpVelocity(transform.position, targetPostision, trajectoryHeight);
        Invoke(nameof(SetVelocity), 0.1f);
    }

    private Vector3 velocityToSet;



    private void SetVelocity()
    {
        enableMovementOnNextTouch = true;
        rb.linearVelocity = velocityToSet;

        cam.DoFov(95f);
    }

    public void ResetRestrictions()
    {
        activeGrapple = false;
        cam.DoFov(60f);
    }

    private void OnCollisionEnter(Collision other)
    {
        if (enableMovementOnNextTouch)
        {
            enableMovementOnNextTouch = false;
            ResetRestrictions();

            GetComponent<Graplpling>().StopGrapple();
        }

        if (other.gameObject.layer == 7 || other.gameObject.layer == 6)
        {
            animator.SetBool("IsJumping", false);
            animator.SetBool("IsGrounded", true);

        }
    }

    public bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }

    public Vector3 GetSlopeMoveDirection(Vector3 direction)
    {
        return Vector3.ProjectOnPlane(direction, slopeHit.normal).normalized;
    }

    public bool IsSlidingUpSlope(Vector3 moveDirection)
    {
        if (OnSlope()) // Ensure this only runs when on a slope
        {
            dot = Vector3.Dot(moveDirection.normalized, slopeHit.normal);
            return dot > 0f; // If dot product is positive, player is moving UP the slope
        }
        return false;
    }

    public Vector3 CalculateJumpVelocity(Vector3 startpoint, Vector3 endpoint, float trajectoryHeight)
    {
        float gravity = Physics.gravity.y;
        float displacementY = endpoint.y - startpoint.y;
        Vector3 displacementXZ = new Vector3(endpoint.x - startpoint.x, 0f, endpoint.z - startpoint.z);

        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * trajectoryHeight);
        Vector3 velocityXZ = displacementXZ / (Mathf.Sqrt(-2 * trajectoryHeight / gravity)
            + Mathf.Sqrt(2 * (displacementY - trajectoryHeight) / gravity));

        return velocityXZ + velocityY;
    }


}