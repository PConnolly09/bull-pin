using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.WSA;

[RequireComponent(typeof(Rigidbody2D))]
public class BullController : MonoBehaviour
{
    [Header("Launch Settings")]
    public float maxLaunchForce = 200f;
    public float minLaunchForce = 50f;
    public float chargeTime = 5f; // seconds to reach full power
    public AnimationCurve launchArc; // e.g., controls upward curve multiplier

    [Header("References")]
    public Rigidbody2D rb;
    public GameManager gameManager;

    [SerializeField] private LineRenderer line;

    private float chargeStartTime;
    private bool isCharging;
    private bool isLaunched;
    private bool hasStopped;
    private Vector2 dragStartPos;
    private Vector2 dragEndPos;
    private bool isDragging;

    public float damageMultiplier = 1f;
    public float health = 1000;
    public float speedDecayRate = 0.98f;   // slows per frame
    public float armorClass = 5f;
    public float currentSpeed;
    public float maxImpact;
    public GameObject deathEffect;

    private Dictionary<string, Coroutine> activeEffects = new();

    void Start()
    {
        gameManager = GameManager.Instance;
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        isLaunched = false;
        hasStopped = false;
        maxImpact = armorClass * maxLaunchForce;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
    }

    void Update()
    {
        if (!isLaunched)
        {
            HandleLaunchInput();
        }
        else
        {
            CheckForStop();
        }

        if (isDragging)
        {
            Vector2 currentPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            line.enabled = true;
            line.SetPosition(0, dragStartPos);
            line.SetPosition(1, currentPos);
        }
        else
        {
            line.enabled = false;
        }
    }

    private void HandleLaunchInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isCharging = true;
            chargeStartTime = Time.time;
            dragStartPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            isDragging = true;
        }

        if (isCharging && Input.GetMouseButtonUp(0))
        {
            isCharging = false;
            isDragging = false;
            dragEndPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 direction = (dragStartPos - dragEndPos).normalized;
            LaunchBull(direction);
        }
    }

    private void LaunchBull(Vector2 direction)
    {
        float chargeDuration = Mathf.Clamp(Time.time - chargeStartTime, 0, chargeTime);
        float t = chargeDuration / chargeTime;
        float force = Mathf.Lerp(minLaunchForce, maxLaunchForce, t);

        // Launch direction: up + left with slight randomness
        direction.y += launchArc.Evaluate(t) * 0.5f;

        rb.bodyType = RigidbodyType2D.Dynamic;
        StartCoroutine(IgnoreWallsTemporarily());
        rb.linearVelocity = direction * force;
        isLaunched = true;
    }

    private IEnumerator IgnoreWallsTemporarily()
    {
        int bullLayer = gameObject.layer;
        int wallLayer = LayerMask.NameToLayer("Placement Plane");

        Physics2D.IgnoreLayerCollision(bullLayer, wallLayer, true);
        yield return new WaitForSeconds(0.2f); // just long enough to clear spawn
        Physics2D.IgnoreLayerCollision(bullLayer, wallLayer, false);
    }

    private void CheckForStop()
    {
        if (!hasStopped && rb.linearVelocity.magnitude < 0.1f)
        {
            hasStopped = true;
            Invoke(nameof(DespawnBull), 1f);
        }
    }

        private void DespawnBull()
    {
        gameManager.OnBullDespawned(this);
        Destroy(gameObject);
    }
    void FixedUpdate()
    {
        // Apply gradual speed decay for smoother control
        if (isLaunched)
            rb.linearVelocity *= speedDecayRate;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Damage enemies on collision
        if (collision.collider.CompareTag("Enemy"))
        {
            var enemy = collision.collider.GetComponent<Enemy>();
            if (enemy != null)
            {
                float impactForce = collision.relativeVelocity.magnitude;
                enemy.TakeDamage(impactForce * damageMultiplier);

                if (enemy.level >= 3)
                {
                    // Big enemy — strong reflection
                    Vector2 reflectDir = Vector2.Reflect(rb.linearVelocity.normalized, collision.contacts[0].normal);
                    rb.linearVelocity =  0.8f * rb.linearVelocity.magnitude * reflectDir;
                }
                else
                {
                    // Weak enemy — pass through
                    Physics2D.IgnoreCollision(collision.collider, GetComponent<Collider2D>());
                }
            }
        }

        // Lose health on hard impacts with walls
        if (collision.collider.CompareTag("Wall") && collision.relativeVelocity.magnitude > maxImpact)
        {
            float smash = Mathf.Ceil(collision.relativeVelocity.magnitude / maxImpact);
            TakeDamage(smash);
        }
    }
    //void EnableLaunch()
    //{
    //    if (remainingLaunches>0)
    //    canLaunch = true;
    //    else
    //        NotifyOutOfLaunches();
    //}

    void NotifyOutOfLaunches()
    {
        // Inform GameManager if no launches remain
        GameManager.Instance.OnBullDespawned(this);
    }

    public float GetSpeed() => rb.linearVelocity.magnitude;

    public void AddImpulse(Vector2 direction, float force)
    {
        rb.AddForce(direction.normalized * force, ForceMode2D.Impulse);
    }

    public void ModifySpeed(float multiplier, float duration = 0)
    {
        StartCoroutine(TemporarySpeedChange(multiplier, duration));
    }

    private IEnumerator TemporarySpeedChange(float multiplier, float duration)
    {
        float original = currentSpeed;
        currentSpeed *= multiplier;
        yield return new WaitForSeconds(duration);
        currentSpeed = original;
    }

    public void TeleportTo(Vector2 position)
    {
        rb.position = position;
        rb.linearVelocity = Vector2.zero;
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        if (health <= 0) Die();
    }

    private void Die()
    {
        if (deathEffect != null)
        {
            var effect = Instantiate(deathEffect, transform.position, Quaternion.identity);
            var ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
                Destroy(effect, ps.main.duration + ps.main.startLifetime.constantMax);
            else
                Destroy(effect, 2f);
        }


        GameManager.Instance.OnBullDespawned(this);
        Destroy(gameObject);
    }

    private void HandleGameStateChanged(GameManager.GameState state)
    {
        // Optional reactions (e.g. freeze bull on pause or game over)
        if (state == GameManager.GameState.GameOver || state == GameManager.GameState.Victory)
            rb.linearVelocity = Vector2.zero;
    }


}
