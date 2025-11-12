using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float health;
    public float speed;
    public int level = 1;
    public GameObject deathEffect;

    void Start()
    {
        SetHealth();
        SetSpeed();
    }
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
        if (deathEffect != null)
        {
            var effect = Instantiate(deathEffect, transform.position, Quaternion.identity);
            var ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
                Destroy(effect, ps.main.duration + ps.main.startLifetime.constantMax);
            else
                Destroy(effect, 2f);
        }


        Destroy(gameObject);
        GameManager.Instance.EnemyDied();

    }

    public void SetHealth()
    {
        health = 10f * level;
    }

    public float GetHealth()
    {
        return health;
    }

    public void SetSpeed()
    {
        speed = 2f * level;
    }
    public float GetSpeed() {
        return speed;
    }

    public void SetLevel(int newLevel)
    {
        level = newLevel;
    }
    public int GetLevel() {
        return level;
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
