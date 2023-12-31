using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class advPlayerMove : Entity
{
    [Header("Assignables")]
    //Assingables
    public Transform playerCam;
    public Transform orientation;

    [Header("Other")]
    //Other
    private Rigidbody rb;
    private GunController gunController;

    [Header("Rotation&Look")]
    //Rotation and look
    private float xRotation;
    private float sensitivity = 50f;
    private float sensMultiplier = 1f;

    [Header("Movement")]
    //Movement
    public float moveSpeed = 4500;
    public float maxSpeed = 20;
    public bool grounded;
    public LayerMask whatIsGround;

    public float counterMovement = 0.175f;
    public float maxSlopeAngle = 35f;
    private float threshold = 0.01f;

    [Header("Croush Slide")]
    //Crouch & Slide
    public float slideForce = 800;
    public float slideCounterMovement = 0.2f;
    private Vector3 crouchScale = new Vector3(1, 0.5f, 1);
    private Vector3 playerScale;

    [Header("Jumping")]
    //Jumping
    public float jumpForce = 550f;
    private bool readyToJump = true;
    private float jumpCooldown = 0.25f;

    [Header("Dash")]
    //Dash
    public float dashSpeed;
    public float dashForce = 550;
    public float dashDuration = 2f;
    public float arialDashSpeed = 2f;
    public float airalDashForce = 100;
    public float dashCooldown = 0.25f;
    public float dashCounterMovement = 1.2f;
    private bool readyToDash = true;

    [Header("Input")]
    //Input
    float x, y;
    bool jumping, sprinting, crouching, dashing, shooting;

    public static event System.Action OnDeathStatic;
    public ParticleSystem deathFX;
    int damage = 1;

    [Header("Sliding")]
    //Sliding
    private Vector3 normalVector = Vector3.up;
    private Vector3 wallNormalVector;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        gunController = GetComponent<GunController>();
    }

    protected override void Start()
    {
        base.Start();
        health = startingHP;
        playerScale = transform.localScale;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
    }


    private void FixedUpdate()
    {
        Movement();
    }

    private void Update()
    {
        MyInput();
        Look();
    }

    public Vector2 walkInput; // stores input from key
    public Vector2 lookInput; // stores input from mouse

    public void MyInput()
    {
        x = walkInput.x;
        y = walkInput.y;

        //Movement
        x = Input.GetAxisRaw("Horizontal");
        y = Input.GetAxisRaw("Vertical");
        jumping = Input.GetButton("Jump");
        crouching = Input.GetKey(KeyCode.LeftControl);
        dashing = Input.GetKey(KeyCode.LeftShift);

        //Crouching
        if (Input.GetKeyDown(KeyCode.LeftControl))
            StartCrouch();
        if (Input.GetKeyUp(KeyCode.LeftControl))
            StopCrouch();
        
        //Guns
        if (Input.GetMouseButton(0))
            gunController.OnTriggerHold();
        if (Input.GetMouseButtonUp(0))
            gunController.OnTriggerRelease();
        if (Input.GetKeyDown(KeyCode.R))
            gunController.Reload();

        //Shot
        if (Input.GetKeyUp(KeyCode.Q))
            TakeDamage(damage);
    }

    private void StartCrouch()
    {
        transform.localScale = crouchScale;
        transform.position = new Vector3(transform.position.x, transform.position.y - 0.5f, transform.position.z);
        if (rb.velocity.magnitude > 0.5f)
        {
            if (grounded)
            {
                rb.AddForce(orientation.transform.forward * slideForce);
            }
        }
    }

    private void StopCrouch()
    {
        transform.localScale = playerScale;
        transform.position = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);
    }

    private void Movement()
    {
        //Extra gravity
        rb.AddForce(Vector3.down * Time.deltaTime * 10);

        //Find actual velocity relative to where player is looking
        Vector2 mag = FindVelRelativeToLook();
        float xMag = mag.x, yMag = mag.y;
        
        //Counteract sliding and sloppy movement
        CounterMovement(x, y, mag);

        //If holding jump && ready to jump, then jump
        if (readyToJump && jumping) Jump();
        //If holding dash && ready to dash, then jump
        if (readyToDash && dashing) Dash();


        //Set max speed
        float maxSpeed = this.maxSpeed;

        //If sliding down a ramp, add force down so player stays grounded and also builds speed
        if (crouching && grounded && readyToJump)
        {
            rb.AddForce(Vector3.down * Time.deltaTime * 3000);
            return;
        }

        //If speed is larger than maxspeed, cancel out the input so you don't go over max speed
        if (x > 0 && xMag > maxSpeed) x = 0;
        if (x < 0 && xMag < -maxSpeed) x = 0;
        if (y > 0 && yMag > maxSpeed) y = 0;
        if (y < 0 && yMag < -maxSpeed) y = 0;

        //Some multipliers
        float multiplier = 1f, multiplierV = 1f;

        // Movement in air
        if (!grounded)
        {
            multiplier = 0.5f;
            multiplierV = 0.5f;
        }

        // Movement while sliding
        if (grounded && crouching) multiplierV = 0f;

        //Apply forces to move player
        rb.AddForce(orientation.transform.forward * y * moveSpeed * Time.deltaTime * multiplier * multiplierV);
        rb.AddForce(orientation.transform.right * x * moveSpeed * Time.deltaTime * multiplier);
    }

    private void Jump()
    {
        if (grounded && readyToJump)
        {
            readyToJump = false;

            //Add jump forces
            rb.AddForce(Vector2.up * jumpForce * 1.5f);
            rb.AddForce(normalVector * jumpForce * 0.5f);

            //If jumping while falling, reset y velocity.
            Vector3 vel = rb.velocity;
            if (rb.velocity.y < 0.5f)
                rb.velocity = new Vector3(vel.x, 0, vel.z);
            else if (rb.velocity.y > 0)
                rb.velocity = new Vector3(vel.x, vel.y / 2, vel.z);

            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    private void Dash()
    {
        if (readyToDash && !crouching)
        {
            readyToDash = false;

            Vector3 forceToApply = orientation.forward * dashForce;

            //rb.AddForce(forceToApply, ForceMode.Impulse);
            //rb.AddForce(orientation.forward * dashForce * 1.5f);
            if (dashing && grounded)
            {
                dashSpeed = 20f * 100f;         
                if (x != 0)
                {
                    if (x > 0)
                    {
                        rb.AddForce(orientation.right * dashForce, ForceMode.Impulse);
                    }
                    else
                    {
                        rb.AddForce(-orientation.right * dashForce, ForceMode.Impulse);
                    }
                }

                if (y != 0)
                {
                    if (y > 0)
                    {
                        rb.AddForce(orientation.forward * dashForce, ForceMode.Impulse);
                    }
                    else
                    {
                        rb.AddForce(-orientation.forward * dashForce, ForceMode.Impulse);
                    }
                }
            }

/*            if (dashing && !grounded)
            {
                dashSpeed = 10f;
                if (x != 0)
                {
                    if (x > 0)
                    {
                        rb.AddForce(orientation.right * dashForce, ForceMode.Impulse);
                    }
                    else
                    {
                        rb.AddForce(-orientation.right * dashForce, ForceMode.Impulse);
                    }
                }

                if (y != 0)
                {
                    if (y > 0)
                    {
                        rb.AddForce(orientation.forward * dashForce, ForceMode.Impulse);
                    }
                    else
                    {
                        rb.AddForce(-orientation.forward * dashForce, ForceMode.Impulse);
                    }
                }
            }*/

            Invoke(nameof(ResetDash), dashDuration);
        }
    }

    private void ResetDash()
    {
        readyToDash = true;
    }


    private float desiredX;

    private void Look()
    {
        float mouseX;
        float mouseY;
        mouseX = lookInput.x;
        mouseY = lookInput.y;

        mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.fixedDeltaTime * sensMultiplier;
        mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.fixedDeltaTime * sensMultiplier;

        //Find current look rotation
        Vector3 rot = playerCam.transform.localRotation.eulerAngles;
        desiredX = rot.y + mouseX;

        //Rotate, and also make sure we dont over- or under-rotate.
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        //Perform the rotations
        playerCam.transform.localRotation = Quaternion.Euler(xRotation, desiredX, 0);
        orientation.transform.localRotation = Quaternion.Euler(0, desiredX, 0);
    }

    //BackUp Look in case something goes wrong. Base Ver.
   /* private void LookAdv()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.fixedDeltaTime * sensMultiplier;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.fixedDeltaTime * sensMultiplier;

        //Find current look rotation
        Vector3 rot = playerCam.transform.localRotation.eulerAngles;
        desiredX = rot.y + mouseX;

        //Rotate, and also make sure we dont over- or under-rotate.
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        //Perform the rotations
        playerCam.transform.localRotation = Quaternion.Euler(xRotation, desiredX, 0);
        orientation.transform.localRotation = Quaternion.Euler(0, desiredX, 0);
    }*/

    private void CounterMovement(float x, float y, Vector2 mag)
    {
        if (!grounded || jumping) return;

        //Slow down sliding
        if (crouching)
        {
            rb.AddForce(moveSpeed * Time.deltaTime * -rb.velocity.normalized * slideCounterMovement);
            return;
        }

        /*if (!grounded && dashing)
        {
            rb.AddForce(-dashSpeed * Time.deltaTime * -rb.velocity.normalized * slideCounterMovement);
            return;
        }*/

        //Counter movement
        if (Mathf.Abs(mag.x) > threshold && Mathf.Abs(x) < 0.05f || (mag.x < -threshold && x > 0) || (mag.x > threshold && x < 0))
        {
            rb.AddForce(moveSpeed * orientation.transform.right * Time.deltaTime * -mag.x * counterMovement);
        }
        if (Mathf.Abs(mag.y) > threshold && Mathf.Abs(y) < 0.05f || (mag.y < -threshold && y > 0) || (mag.y > threshold && y < 0))
        {
            rb.AddForce(moveSpeed * orientation.transform.forward * Time.deltaTime * -mag.y * counterMovement);
        }

        //Limit diagonal running. This will also cause a full stop if sliding fast and un-crouching, so not optimal.
        if (Mathf.Sqrt((Mathf.Pow(rb.velocity.x, 2) + Mathf.Pow(rb.velocity.z, 2))) > maxSpeed)
        {
            float fallspeed = rb.velocity.y;
            Vector3 n = rb.velocity.normalized * maxSpeed;
            rb.velocity = new Vector3(n.x, fallspeed, n.z);
        }
    }

    /// <summary>
    /// Find the velocity relative to where the player is looking
    /// Useful for vectors calculations regarding movement and limiting movement
    /// </summary>
    /// <returns></returns>
    public Vector2 FindVelRelativeToLook()
    {
        float lookAngle = orientation.transform.eulerAngles.y;
        float moveAngle = Mathf.Atan2(rb.velocity.x, rb.velocity.z) * Mathf.Rad2Deg;

        float u = Mathf.DeltaAngle(lookAngle, moveAngle);
        float v = 90 - u;

        float magnitue = rb.velocity.magnitude;
        float yMag = magnitue * Mathf.Cos(u * Mathf.Deg2Rad);
        float xMag = magnitue * Mathf.Cos(v * Mathf.Deg2Rad);

        return new Vector2(xMag, yMag);
    }

    private bool IsFloor(Vector3 v)
    {
        float angle = Vector3.Angle(Vector3.up, v);
        return angle < maxSlopeAngle;
    }

    private bool cancellingGrounded;

    /// <summary>
    /// Handle ground detection
    /// </summary>
    private void OnCollisionStay(Collision other)
    {
        //Make sure we are only checking for walkable layers
        int layer = other.gameObject.layer;
        if (whatIsGround != (whatIsGround | (1 << layer))) return;

        //Iterate through every collision in a physics update
        for (int i = 0; i < other.contactCount; i++)
        {
            Vector3 normal = other.contacts[i].normal;
            //FLOOR
            if (IsFloor(normal))
            {
                grounded = true;
                cancellingGrounded = false;
                normalVector = normal;
                CancelInvoke(nameof(StopGrounded));
            }
        }

        //Invoke ground/wall cancel, since we can't check normals with CollisionExit
        float delay = 3f;
        if (!cancellingGrounded)
        {
            cancellingGrounded = true;
            Invoke(nameof(StopGrounded), Time.deltaTime * delay);
        }
    }

    private void StopGrounded()
    {
        grounded = false;
    }

    public override void Die()
    {

        //AudioManager.instance.PlaySound("Player Death", transform.position);
        base.Die();
        GameObject.Destroy(gunController);

    }
}
