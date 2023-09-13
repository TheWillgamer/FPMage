using FishNet.Managing.Timing;
using UnityEngine;

public class MoveProjectileClient : MonoBehaviour
{
    private Vector3 velocity = Vector3.zero;
    private bool activated = false;
    [SerializeField] private bool bouncing;

    public void Initialize(Vector3 velo, float activationTime)
    {
        velocity = velo;
        Invoke("Activate", activationTime);
    }

    public void Activate()
    {
        activated = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (activated)
            transform.position += velocity * Time.deltaTime;
        else
            transform.position += velocity * Time.deltaTime / 2f;
    }
}
