using UnityEngine;

[CreateAssetMenu(fileName = "NewBumperEffects", menuName = "BumperEffects/BaseEffect")]
public abstract class BumperEffect : ScriptableObject
{
    [Header("General Info")]
    public string effectName;
    public string description;

    [Header("Visuals & Audio")]
    public Color bumperColor = Color.white;

    // Called when the Bull collides with the bumper
    public abstract void Activate(Bumper bumper, BullController bull);
}
