using UnityEngine;

[CreateAssetMenu(menuName = "BumperEffects/Bounce Boost")]
public class BounceBoostEffect : BumperEffect
{
    public float boostForce = 1000f;

    public override void Activate(Bumper bumper, BullController bull)
    {
        Vector2 reflectDir = (bull.transform.position - bumper.transform.position).normalized;
        bull.AddImpulse(reflectDir, boostForce);
    }
}
