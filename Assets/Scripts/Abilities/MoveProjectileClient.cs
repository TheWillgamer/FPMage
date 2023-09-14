using FishNet.Managing.Timing;
using UnityEngine;

public class MoveProjectileClient : MonoBehaviour
{
    private Vector3 velocity = Vector3.zero;
    private bool activated = false;
    [SerializeField] private bool bouncing;
    private float endTime;

    public void Initialize(Vector3 velo, float activationTime)
    {
        velocity = velo;
        Invoke("Activate", activationTime);
        endTime = Time.time + activationTime * 2;
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
        {
            transform.position += velocity * Time.deltaTime;
            if (Time.time > endTime)
                gameObject.transform.GetChild(0).gameObject.SetActive(false);
        }
    }
}
