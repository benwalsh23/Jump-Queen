using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float offsetValue;

    [Header("Jump")]
    [SerializeField] private float verticalJumpForce;
    [SerializeField] private float jumpTimeMax;
    [SerializeField] private float jumpTimeMin;
    [SerializeField] private float horizontalJumpForce;

    [Header("Wall")]
    [SerializeField] private LayerMask whatIsGround;

    [Header("Fall")]
    [SerializeField] private float bigFallHeight;

    [HideInInspector] public bool isFacingRight;

    private Rigidbody2D rb;
    private Collider2D col;
    private Animator anim;
    private float moveInput;

    private float jumpTimeCounter;

    private RaycastHit2D groundHit;

    private Vector3 lastPosition;

    private bool canJump = false;

    private bool onIce = false;

    private bool onSlant = false;

    public GameObject Audio;
    private AudioSource jump;
    private AudioSource landing;
    private AudioSource hardLanding;
    

    /// <summary>
    /// Setting our components for rigidbody, animations, and collider
    /// </summary>
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();

        isFacingRight = true;


        // Assign the AudioSource components from child GameObjects
        if (Audio != null)
        {
            jump = Audio.transform.Find("jump").GetComponent<AudioSource>();
            landing = Audio.transform.Find("landing").GetComponent<AudioSource>();
            hardLanding = Audio.transform.Find("hardLanding").GetComponent<AudioSource>();
        }
        else
        {
            Debug.LogError("The Audio GameObject is missing.");
        }

        // Check if any of the AudioSource components are null
        if (jump == null || landing == null || hardLanding == null)
        {
            Debug.LogError("One or more AudioSource components are missing.");
        }
    }

    /// <summary>
    /// this just checks every frame for an input for move or jump
    /// </summary>
    private void Update()
    {
        if (IsGrounded())
        {
            Jump();
            Move();    
        }
    }

    #region Movement Functions
    /// <summary>
    /// this is the logic for movement in the game, we first check that the player is not crouching and is on the ground
    /// then set the player animation for iswalking on if the move input is there and peform a move check to make sure the player
    /// is facing the right direction, after that we can set the players x velocity to the movespeed in the direction they are going.
    /// </summary>
    private void Move()
    {
        moveInput = UserInput.instance.moveInput.x;

        if (moveInput > 0 || moveInput < 0 && !onSlant)
        {
            anim.SetBool("bigFall", false);
            anim.SetBool("isWalking", true);
            TurnCheck();
        }
        else
        {
            anim.SetBool("isWalking", false);
        }

        // Check if player is crouched before moving them and that they are not moving vertically at a high speed like jumping
        if (!anim.GetBool("isCrouching") && rb.velocity.y < 0.1 && rb.velocity.y > -0.1)
        {

            // movement on normal ground
            if (anim.GetBool("isWalking") && !onIce && !onSlant)
            {
                rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);
            }
            // for movement on ice
            else 
            {
                // if on flat ice
                if (rb.velocity.x < moveSpeed && rb.velocity.x > -moveSpeed && !onSlant)
                {
                    rb.AddForce(new Vector2(moveInput * moveSpeed * .8f, 0), ForceMode2D.Force);
                }
                // if on the slanted ice. this is here so that walking onto a slant works
                else
                {
                    if (rb.velocity.y == 0 && rb.velocity.x == 0)
                    {
                        transform.position += new Vector3(moveInput * .05f, 0f, 0f);
                    }
                }
            }
        }
    }



    /// <summary>
    /// This is the logic for the jump, first there will be a check if the player is grounded and if they are, then the player may begin their jump
    /// the jump is split into three parts
    /// 1. the frame the jump button was pressed
    ///     - the jumpTimeCounter will be set to 0
    ///     - the crouch animation will begin
    ///     - all players will be set to 0
    /// 2. while the jump button is being pressed down
    ///     - every frame the counter will be incremented
    /// 3. when the jump button is released
    ///     - if the jump counter is less than the set minimum, the player will jump the minimum
    ///     - if the jump counter is less than max, then the player will jump based on how long they held the jump button
    ///     - if the jump counter is more than max, the player will jump the max height
    ///     - the player will jump either straight up, to the left or to the right depending on their direction input
    ///     - then the crouching animation will be cancelled and the jumping animation will begin, as well as resetting the counter again
    /// </summary>
    private void Jump()
    {
        if (UserInput.instance.controls.Jumping.Jump.WasPressedThisFrame() && !onSlant)
        {
            jumpTimeCounter = 0f;
            anim.SetBool("bigFall", false);
            anim.SetBool("isCrouching", true);
            canJump = true;
            
        }

        if (UserInput.instance.controls.Jumping.Jump.IsPressed() && canJump)
        {
            if (jumpTimeCounter <= jumpTimeMax)
            {
                jumpTimeCounter += Time.deltaTime;
            }
            else
            {
                moveInput = UserInput.instance.moveInput.x;

                // execute jump
                rb.velocity = new Vector2(moveInput * horizontalJumpForce, verticalJumpForce * jumpTimeMax * 2);

                // set appropriate animation states
                anim.SetBool("isJumping", true);
                anim.SetBool("isCrouching", false);
                // reset jump counter
                jumpTimeCounter = 0f;
                jump.Play();
                canJump = false;
            }
        }

        if (UserInput.instance.controls.Jumping.Jump.WasReleasedThisFrame() && canJump)
        {
                
            // set counter to min if its below
            if (jumpTimeCounter < jumpTimeMin)
            {
                jumpTimeCounter = jumpTimeMin;
            }
            // get the player's input direction using the moveInput
            moveInput = UserInput.instance.moveInput.x;

            // execute jump
            rb.velocity = new Vector2(moveInput * horizontalJumpForce, verticalJumpForce * jumpTimeCounter * 2);

            jump.Play();

            // set appropriate animation states
            anim.SetBool("isJumping", true);
            anim.SetBool("isCrouching", false);
            // reset jump counter
            jumpTimeCounter = 0f;
            canJump = false;
        }

    }

    #endregion

    #region Ground Check
    /// <summary>
    /// This is where we perform ground checks to see if the player is currently on the ground using boxcasting.
    /// </summary>
    /// <returns>
    /// true if player is on the ground as well as sets the jumping and falling to false on the ground.
    /// false if player is in the air as well as if the players y velocity is less than zero set falling true otherwise jumping to true
    /// </returns>
    private bool IsGrounded()
    {
        // perform boxcast to check for any collision
        groundHit = Physics2D.BoxCast(col.bounds.center, col.bounds.size, 0f, Vector2.down, 0.1f, whatIsGround);

        // check if the player is touching any collider
        if (groundHit.collider != null)
        {

            // access the collider's GameObject directly
            GameObject groundObject = groundHit.collider.gameObject;

            Debug.Log("Collided with GameObject: \"" + groundObject.name + "\"");
            // set the type of ground for determining movement style
            if (groundObject.name != "Terrain")
            {
                onIce = true;
                if (groundObject.name == "Slanted Icy Terrain")
                {
                    onSlant = true;
                }
                else
                {
                    onSlant = false;
                }
            }
            else
            {
                onIce = false;
                onSlant = false;
                
            }

            if (!onSlant)
            {
                CheckBigFall();
            }
            
            // if the normal is vertical, treat it as ground
            anim.SetBool("isJumping", false);
            anim.SetBool("isFalling", false);
            return true;
        }
        else
        {
            // if no collider is hit, and velocity is negative treat it as falling
            if (rb.velocity.y <= 0 && !anim.GetBool("isFalling"))
            {
                anim.SetBool("isFalling", true);
                //set position when fall begins to determine big fall
                lastPosition = rb.position;
            }
            // otherwise treat it as jumping
            else
            {
                anim.SetBool("isJumping", true);
            }
            return false;
        }
    }

    #endregion

    #region Turn Checks
    /// <summary>
    /// This checks if the player needs to turn
    /// </summary>
    private void TurnCheck()
    {
        if (UserInput.instance.moveInput.x > 0 && !isFacingRight)
        {
            Turn();
        }

        else if (UserInput.instance.moveInput.x < 0 && isFacingRight)
        {
            Turn();
        }
    }
    /// <summary>
    /// this turns the player if the player needs to turn, it also transforms the player by an offset value because the character is not centered
    /// in the sprite.
    /// </summary>
    private void Turn()
    {
        if (isFacingRight)
        {
            transform.position += new Vector3(-offsetValue, 0f, 0f);
            Vector3 rotator = new Vector3(transform.rotation.x, 180f, transform.rotation.z);
            transform.rotation = Quaternion.Euler(rotator);
            isFacingRight = !isFacingRight;
        }
        else
        {
            transform.position += new Vector3(offsetValue, 0f, 0f);
            Vector3 rotator = new Vector3(transform.rotation.x, 0f, transform.rotation.z);
            transform.rotation = Quaternion.Euler(rotator);
            isFacingRight = !isFacingRight;
        }
    }

    #endregion

    #region Big Fall Checks
    /// <summary>
    /// checks if the player falls a distance greater than bigFallHeight and activates bigFall animation if true
    /// </summary>
    private void CheckBigFall()
    {
        float fallDistance = lastPosition.y - rb.position.y;
        if (fallDistance > bigFallHeight)
        {
            anim.SetBool("bigFall", true);
            hardLanding.Play();
        }
        if (1 < fallDistance && fallDistance < bigFallHeight)
        {
            landing.Play();
        }

        lastPosition = transform.position;
    }

    #endregion
}
