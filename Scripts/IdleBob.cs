using UnityEngine;

public class IdleBob : MonoBehaviour
{
    [Header("Bob Settings")]
    public float amplitude = 0.1f; // how high/low it moves
    public float frequency = 1f;// how fast it bobs
    public float RandomOffset;//make it different

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
        RandomOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        float offsetY = Mathf.Sin(Time.time * frequency + RandomOffset) * amplitude;
        transform.localPosition = startPos + new Vector3(0, offsetY, 0);
    }
}
