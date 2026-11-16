using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public Rigidbody2D rb;
    public Animator anim;
    public ParticleSystem trail, landParticles;
    public AudioSource stepAudio;
    public AudioSource landAudio;
    [Space] 
    public Transform hands;
    public SpriteRenderer face;
    public Sprite normalFace;
    public Sprite blinkFace;
    
    [Header("Movement Settings")]
    public float maxSpeed = 12f;
    public float timeToMax = 0.2f, timeToStop = 0.15f, timeToTurn = 0.1f;
    [Space] public float airTimeToMax = 0.2f;
    public float airTimeToStop = 0.5f;

    [Header("Jump Settings")] 
    [Tooltip("Shorter jumps when the jump button is tapped.")] public bool variableJumpHeight;
    [Space] public float jumpHeight = 3;
    public float timeToPeak = 0.25f, timeInAir = 0.1f;
    
    [Space] [Min(1f)] public float gravity = 6f;
    public float coyoteTiming = 0.15f;

    private float x;
    private float peakTimer, jumpTimer, jumpGravity, currentCoyote;
    private float fallGravity = -9.81f;
    private float blinkTimer = 0f, blinkDuration = 0.1f;
    
    private bool ascending, hanging;
    private bool spawnParticles = false;
    private bool canJump, hasJumped;
    private bool blinking = false;
    private bool spaceDown = false, startHang = false;

    void FixedUpdate()
    {
        if (x != 0)
        {
            float groundAcceleration = maxSpeed / timeToMax;
            float airAcceleration = maxSpeed / airTimeToMax;
            
            if (Mathf.Sign(x) != Mathf.Sign(rb.linearVelocity.x))
            {
                groundAcceleration = maxSpeed / timeToTurn;
                airAcceleration = maxSpeed / timeToTurn;
            }
            
            if (IsGrounded()) rb.linearVelocity += new Vector2(groundAcceleration * x * Time.fixedDeltaTime, 0);
            else rb.linearVelocity += new Vector2(airAcceleration * x * Time.fixedDeltaTime, 0);

            stepAudio.pitch = Random.Range(0.95f, 1.05f);
            stepAudio.volume = Random.Range(0.4f, 0.5f) * (Mathf.Abs(rb.linearVelocity.x) / maxSpeed);
            if (!stepAudio.isPlaying && IsGrounded()) stepAudio.Play();
        }
        else
        {
            if (IsGrounded()) rb.linearVelocity = new Vector2(Mathf.MoveTowards(rb.linearVelocity.x, 0f, (maxSpeed / timeToStop) * Time.fixedDeltaTime), rb.linearVelocity.y);
            else rb.linearVelocity = new Vector2(Mathf.MoveTowards(rb.linearVelocity.x, 0f, (maxSpeed / airTimeToStop) * Time.fixedDeltaTime), rb.linearVelocity.y);
        }
        
        if (stepAudio.isPlaying && (x == 0 || !IsGrounded())) stepAudio.volume = stepAudio.volume * (Mathf.Abs(rb.linearVelocity.x) / maxSpeed);;

        if (stepAudio.isPlaying && stepAudio.volume == 0 || !IsGrounded()) stepAudio.Stop();

        rb.linearVelocity = new Vector2(
            Mathf.Clamp(rb.linearVelocity.x, -maxSpeed, maxSpeed),
            Mathf.Clamp(rb.linearVelocity.y, -22f, Mathf.Infinity)
        );

        if (rb.linearVelocity.y < 0 && x == 0) // When guns are added, make sure this doesn't run when a gun is equipped.
        {
            hands.position = new Vector3(transform.position.x, transform.position.y - rb.linearVelocity.y * Time.fixedDeltaTime, transform.position.z);
        }
        else
        {
            hands.position = new Vector3(transform.position.x, Mathf.MoveTowards(hands.position.y, transform.position.y, .1f), transform.position.z);
        }

        if (ascending)
        {
            jumpTimer += Time.fixedDeltaTime;

            rb.linearVelocity += new Vector2(
                0f,
                jumpGravity * Time.fixedDeltaTime
            );

            if (jumpTimer >= timeToPeak && spaceDown)
            {
                rb.linearVelocity = new Vector2(
                    rb.linearVelocity.x,
                    0f
                );

                ascending = false;
                hanging = true;
                peakTimer = 0f;
            }
        }
        else if (hanging && (spaceDown || startHang))
        {
            startHang = true;
            peakTimer += Time.fixedDeltaTime;

            if (peakTimer >= timeInAir)
            {
                hanging = false;
            }
        }
        else
        {
            startHang = false;
            rb.linearVelocity += new Vector2(
                0f,
                fallGravity * gravity * Time.fixedDeltaTime
            );
        }

        if (hanging)
        {
            peakTimer += Time.fixedDeltaTime;

            if (peakTimer >= timeInAir)
            {
                hanging = false;
            }
        }

        if (!IsGrounded())
        {
            spawnParticles = true;
            trail.Stop();

            if (currentCoyote <= coyoteTiming && !hasJumped)
            {
                canJump = true;
                currentCoyote += Time.fixedDeltaTime;
            }
            else
            {
                canJump = false;
            }
        }
        else if (IsGrounded() && trail.isStopped)
        {
            trail.Play();
        }

        
        if (IsGrounded() && !ascending && spawnParticles)
        {
            landAudio.Play();
            var temp = Instantiate(landParticles, new Vector3(transform.position.x, transform.position.y - 0.4f, transform.position.z), Quaternion.Euler(Vector3.zero)).GetComponent<ParticleSystem>();
            temp.Play();
            spawnParticles = false;
            hasJumped = false;
        }

        if (IsGrounded() && !ascending && currentCoyote >= coyoteTiming)
        {
            currentCoyote = 0;
        }
        
        anim.SetFloat("x", x);
        anim.SetFloat("Move", Mathf.Abs(x));
        
        blinkTimer += Time.fixedDeltaTime;

        if (!blinking && blinkTimer >= Random.Range(3f, 6f))
        {
            blinking = true;
            blinkTimer = 0f;
            
            face.sprite = blinkFace;
        }

        if (blinking && blinkTimer >= blinkDuration)
        {
            blinking = false;
            blinkTimer = 0f;

            face.sprite = normalFace;
        }
    }

    public void Move(InputAction.CallbackContext context)
    {
        x = context.ReadValue<Vector2>().x;
    }
    
    public void Jump(InputAction.CallbackContext context)
    {
        if ((IsGrounded() || canJump) && context.performed)
        {
            hasJumped = true;
            canJump = false;
            
            float height = jumpHeight * 0.981038f;

            jumpGravity = -(2f * height) / (timeToPeak * timeToPeak);

            float jumpVel = -jumpGravity * timeToPeak;

            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                jumpVel
            );

            spaceDown = true;
            ascending = true;
            hanging = false;
            jumpTimer = 0f;
            peakTimer = 0f;
        }

        if (context.canceled && ascending && variableJumpHeight)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

            spaceDown = false;
        }
    }

    // Returns true, if player character is close enough to the ground.
    public bool IsGrounded()
    {
        return Physics2D.OverlapCircle(transform.localPosition - new Vector3(0f, 0.5f, 0f), 0.2f, LayerMask.GetMask("Ground"));
    }
}
