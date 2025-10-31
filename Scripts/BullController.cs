using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.WSA;

[RequireComponent(typeof(Rigidbody2D))]
public class BullController : MonoBehaviour
{

    public float damageMultiplier = 1f;
    public float health = 3;
    public float launchPower = 15f;
    public float speedDecayRate = 0.98f;   // slows per frame
    public int maxLaunches = 3;
    public int remainingLaunches;
    public float relaunchCooldown = 0.5f;
    public float baseSpeed = 15f;
    public float armorClass = 5f;
    public float currentSpeed;
    public float maxImpact;
    public GameObject deathEffect;

    private Rigidbody2D rb;
    private bool canLaunch = true;
    private bool hasLaunched = false;
    private Vector2 launchDir;

    private Dictionary<string, Coroutine> activeEffects = new();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentSpeed = baseSpeed;
        remainingLaunches = maxLaunches;
        maxImpact = baseSpeed * armorClass;
    }

    void Update()
    {
        // Launch on tap or click
        if (canLaunch && Input.GetMouseButtonDown(0))
        {
            // Aim toward mouse position (you can replace this with a UI power meter later)
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = 20f; // Distance from camera to gameplay plane
            Vector2 target = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            launchDir = (target - (Vector2)transform.position).normalized;
            rb.AddForce(launchDir * currentSpeed, ForceMode2D.Impulse);
            hasLaunched = true;
            remainingLaunches--;
            canLaunch = false;
            Invoke(nameof(EnableLaunch), relaunchCooldown);

        }
        if (hasLaunched && rb.linearVelocity.magnitude < 0.2f && canLaunch)
            hasLaunched = false;
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
            }
        }

        // Lose health on hard impacts with walls
        if (collision.collider.CompareTag("Wall") && collision.relativeVelocity.magnitude > maxImpact)
        {
            float smash = Mathf.Ceil(collision.relativeVelocity.magnitude / maxImpact);
            TakeDamage(smash);
        }
    }
    void EnableLaunch()
    {
        if (remainingLaunches>0)
        canLaunch = true;
    }

    public float GetSpeed()
    {
        return rb.linearVelocity.magnitude;
    }

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
        remainingLaunches--;
        GameManager.Instance.OnBullDestroyed();
        Destroy(gameObject);
    }


}
