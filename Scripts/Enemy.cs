using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float health = 10f;
    public GameObject deathEffect;

    public void TakeDamage(float amount)
    {
        health -= amount;
        if (health <= 0)
        {
            Die();
        }
        else
        {
            // Flash red briefly (visual feedback)
            StartCoroutine(FlashColor());
        }
    }

    void Die()
    {
        if (deathEffect)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        Destroy(gameObject);
        GameManager.Instance.EnemyDied();

    }

    System.Collections.IEnumerator FlashColor()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr)
        {
            sr.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            sr.color = Color.white;
        }
    }
}
