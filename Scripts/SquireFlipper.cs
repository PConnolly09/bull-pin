using UnityEngine;

public class SquireFlipper : MonoBehaviour
{
    public KeyCode activateKey = KeyCode.Space;
    public float flipForce = 20f;
    public float cooldown = 0.3f;

    private float lastFlipTime;

    void Update()
    {
        if (Input.GetKeyDown(activateKey) && Time.time - lastFlipTime > cooldown)
        {
            Flip();
        }
    }

    void Flip()
    {
        // Apply impulse to any bull within range
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 2f);
        foreach (var hit in hits)
        {
            BullController bull = hit.GetComponent<BullController>();
            if (bull)
            {
                Vector2 dir = (bull.transform.position - transform.position).normalized;
                Rigidbody2D rb = bull.GetComponent<Rigidbody2D>();
                rb.AddForce(dir * flipForce, ForceMode2D.Impulse);
            }
        }

        // Optional: small animation or rotation effect here
        lastFlipTime = Time.time;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 2f);
    }
}
