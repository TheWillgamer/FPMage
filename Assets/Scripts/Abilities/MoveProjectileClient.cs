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

    private void Activate()
    {
        activated = true;
        gameObject.transform.GetChild(0).gameObject.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        if (activated)
            transform.position += velocity * Time.deltaTime;
    }
}
