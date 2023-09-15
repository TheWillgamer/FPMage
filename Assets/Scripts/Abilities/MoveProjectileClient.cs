using FishNet.Managing.Timing;
using UnityEngine;

public class MoveProjectileClient : MonoBehaviour
{
    private Vector3 velocity = Vector3.zero;
    private bool activated = false;
    [SerializeField] private float gravity;
    [SerializeField] private bool bouncing;
    private float startYPos;
    private float startTime;
    private float endTime;

    public void Initialize(Vector3 velo, float activationTime)
    {
        velocity = velo;
        Invoke("Activate", activationTime);
        startYPos = transform.position.y;
        startTime = Time.time;
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
        if (!activated)
            return;

        if (gravity != 0)
        {
            float elapsedTime = Time.time - startTime;
            transform.position = new Vector3(transform.position.x + velocity.x * Time.deltaTime, startYPos + velocity.y * elapsedTime - .5f * gravity * Mathf.Pow(elapsedTime, 2), transform.position.z + velocity.z * Time.deltaTime);
        }
        else
        {
            transform.position += velocity * Time.deltaTime;
        }
        
        if (Time.time > endTime)
            gameObject.transform.GetChild(0).gameObject.SetActive(false);
    }
}
