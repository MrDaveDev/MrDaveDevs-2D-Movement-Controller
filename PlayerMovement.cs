/*
 * Copyright (c) 2026 MrDaveDev
 * All rights reserved.
 */

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

    [Header("Movement Settings")]
    public float maxSpeed = 12f;
    public float timeToMax = 0.2f, timeToStop = 0.15f, timeToTurn = 0.1f;
    [Space] public float airTimeToMax = 0.2f;
    public float airTimeToStop = 0.5f;

    [Header("Jump Settings")]
    [Tooltip("Shorter jumps when the jump button is tapped.")] public bool variableJumpHeight;
    [Space] public float jumpHeight = 3f;
    public float timeToPeak = 0.25f, timeInAir = 0.1f;

    [Space] [Min(1f)] public float gravity = 6f;
    public float coyoteTiming = 0.15f;

    private float x;
    private float peakTimer, jumpTimer, jumpGravity, currentCoyote;
    private float fallGravity = -9.81f;

    private bool ascending, hanging;
    private bool spawnParticles = false;
    private bool canJump, hasJumped;
    private bool spaceDown = false;

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        // Gravity is handled by this script.
        rb.gravityScale = 0f;
    }

    void FixedUpdate()
    {
        bool grounded = IsGrounded();

        if (x != 0)
        {
            float groundAcceleration = maxSpeed / Mathf.Max(timeToMax, 0.001f);
            float airAcceleration = maxSpeed / Mathf.Max(airTimeToMax, 0.001f);

            if (Mathf.Abs(rb.linearVelocity.x) > 0.01f && Mathf.Sign(x) != Mathf.Sign(rb.linearVelocity.x))
            {
                groundAcceleration = maxSpeed / Mathf.Max(timeToTurn, 0.001f);
                airAcceleration = maxSpeed / Mathf.Max(timeToTurn, 0.001f);
            }

            if (grounded) rb.linearVelocity += new Vector2(groundAcceleration * x * Time.fixedDeltaTime, 0f);
            else rb.linearVelocity += new Vector2(airAcceleration * x * Time.fixedDeltaTime, 0f);

            if (grounded && stepAudio != null)
            {
                stepAudio.pitch = Random.Range(0.95f, 1.05f);
                stepAudio.volume = Random.Range(0.4f, 0.5f) * (Mathf.Abs(rb.linearVelocity.x) / Mathf.Max(maxSpeed, 0.001f));

                if (!stepAudio.isPlaying) stepAudio.Play();
            }
        }
        else
        {
            if (grounded) rb.linearVelocity = new Vector2(Mathf.MoveTowards(rb.linearVelocity.x, 0f, (maxSpeed / Mathf.Max(timeToStop, 0.001f)) * Time.fixedDeltaTime), rb.linearVelocity.y);
            else rb.linearVelocity = new Vector2(Mathf.MoveTowards(rb.linearVelocity.x, 0f, (maxSpeed / Mathf.Max(airTimeToStop, 0.001f)) * Time.fixedDeltaTime), rb.linearVelocity.y);
        }

        if (stepAudio != null && stepAudio.isPlaying && (x == 0 || !grounded))
        {
            stepAudio.Stop();
        }

        rb.linearVelocity = new Vector2(
            Mathf.Clamp(rb.linearVelocity.x, -maxSpeed, maxSpeed),
            Mathf.Clamp(rb.linearVelocity.y, -22f, Mathf.Infinity)
        );

        if (ascending)
        {
            jumpTimer += Time.fixedDeltaTime;

            rb.linearVelocity += new Vector2(
                0f,
                jumpGravity * Time.fixedDeltaTime
            );

            if (jumpTimer >= timeToPeak)
            {
                rb.linearVelocity = new Vector2(
                    rb.linearVelocity.x,
                    0f
                );

                ascending = false;
                hanging = spaceDown && timeInAir > 0f;
                peakTimer = 0f;
            }
        }
        else if (hanging)
        {
            peakTimer += Time.fixedDeltaTime;

            if (peakTimer >= timeInAir)
            {
                hanging = false;
            }
        }
        else
        {
            rb.linearVelocity += new Vector2(
                0f,
                fallGravity * gravity * Time.fixedDeltaTime
            );
        }

        if (!grounded)
        {
            spawnParticles = true;

            if (trail != null && trail.isPlaying) trail.Stop();

            if (!hasJumped && currentCoyote < coyoteTiming)
            {
                canJump = true;
                currentCoyote += Time.fixedDeltaTime;
            }
            else
            {
                canJump = false;
            }
        }
        else
        {
            currentCoyote = 0f;

            if (!ascending && !hanging)
            {
                canJump = true;
                hasJumped = false;
            }

            if (trail != null && trail.isStopped) trail.Play();
        }

        if (grounded && !ascending && spawnParticles)
        {
            if (landAudio != null) landAudio.Play();

            if (landParticles != null)
            {
                var temp = Instantiate(landParticles, new Vector3(transform.position.x, transform.position.y - 0.4f, transform.position.z), Quaternion.Euler(Vector3.zero));
                temp.Play();
            }

            spawnParticles = false;
            hasJumped = false;
        }

        if (anim != null)
        {
            anim.SetFloat("x", x);
            anim.SetFloat("Move", Mathf.Abs(x));
        }
    }

    public void Move(InputAction.CallbackContext context)
    {
        x = context.ReadValue<Vector2>().x;
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if ((IsGrounded() || canJump) && context.performed && !hasJumped)
        {
            hasJumped = true;
            canJump = false;

            float height = jumpHeight;
            jumpGravity = -(2f * height) / (Mathf.Max(timeToPeak, 0.001f) * Mathf.Max(timeToPeak, 0.001f));
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

        if (context.canceled)
        {
            spaceDown = false;

            if (ascending && variableJumpHeight)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

                ascending = false;
                hanging = false;
            }
        }
    }

    // Returns true if the player character is close enough to the ground.
    public bool IsGrounded()
    {
        return Physics2D.OverlapCircle(
            transform.position - new Vector3(0f, 0.5f, 0f),
            0.2f,
            LayerMask.GetMask("Ground")
        );
    }
}
