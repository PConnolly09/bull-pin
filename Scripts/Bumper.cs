using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Bumper : MonoBehaviour
{
    public float bounceForce = 10f;
    public float minimumVelocity = 5f;
    public float flashDuration = 0.1f;
    public float scalePulse = 1.3f;

    public ParticleSystem hitEffect;
    public AudioClip bounceSound;

    public BumperEffect currentEffect;

    private AudioSource audioSource;
    private SpriteRenderer sr;
    private Color originalColor;
    private Vector3 originalScale;
    private bool isFlashing;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;
        originalScale = transform.localScale;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        //Rigidbody2D rb = collision.rigidbody;
        //if (rb == null) return;

        //Vector2 incomingVelocity = rb.linearVelocity;
        //Vector2 normal = collision.contacts[0].normal;
        //Vector2 reflected = Vector2.Reflect(incomingVelocity, normal).normalized;

        //rb.AddForce(reflected * bounceForce, ForceMode2D.Impulse);

        //if (rb.linearVelocity.magnitude < minimumVelocity)
        //    rb.linearVelocity = rb.linearVelocity.normalized * minimumVelocity;

        //if (hitEffect != null)
        //    Instantiate(hitEffect, collision.contacts[0].point, Quaternion.identity);

        //if (bounceSound != null)
        //    audioSource.PlayOneShot(bounceSound);

        BullController bull = collision.gameObject.GetComponent<BullController>();  
        if (bull != null)
        {
            currentEffect?.Activate(this, bull);
        }

        StartCoroutine(BounceFlash());
    }

    System.Collections.IEnumerator BounceFlash()
    {
        if (isFlashing) yield break;
        isFlashing = true;

        // Quick flash & scale pulse
        sr.color = Color.white;
        transform.localScale = originalScale * scalePulse;

        yield return new WaitForSeconds(flashDuration);

        // Revert back
        sr.color = originalColor;
        transform.localScale = originalScale;

        isFlashing = false;
    }
}

// THIS IS THE SUGGESTED CHANGE TO BUMPER CLASS TO ALLOW ALL EFFECTS TO COME AS SEPARATE ASSETS
//[RequireComponent(typeof(Collider2D))]
//public class Bumper : MonoBehaviour
//{
//    public BumperEffect currentEffect;

//    private void OnCollisionEnter2D(Collision2D collision)
//    {
//        BullController bull = collision.gameObject.GetComponent<BullController>();
//        if (bull != null)
//        {
//            currentEffect?.Activate(bull);
//        }
//    }
//}
