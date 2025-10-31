using UnityEngine;

[CreateAssetMenu(menuName = "BumperEffects/Enemy Scatter")]
public class EnemyScatterEffect : BumperEffect
{
    public float scatterForce = 100f;
    public float radius = 50f;

    public override void Activate(Bumper bumper, BullController bull)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(bumper.transform.position, radius);
        foreach (Collider2D hit in hits)
        {
            Enemy e = hit.GetComponent<Enemy>();
            if (e != null)
            {
                Rigidbody2D rb = e.GetComponent<Rigidbody2D>();
                if (rb != null)
                    rb.AddForce((e.transform.position - bumper.transform.position).normalized * scatterForce, ForceMode2D.Impulse);
            }
        }
    }
}
