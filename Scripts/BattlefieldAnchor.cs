using UnityEngine;

public class BattlefieldAnchor : MonoBehaviour
{
    public static BattlefieldAnchor Instance;
    void Awake() => Instance = this;
}
