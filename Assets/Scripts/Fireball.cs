using FishNet.Object;
using FishNet.Managing.Logging;
using FishNet.Managing.Timing;
using UnityEngine;

public class Fireball : NetworkBehaviour, Projectile
{
    [SerializeField] private float knockback_amount = 1f;
    [SerializeField] private float knockback_growth = 20f;
    private Vector3 velocity = Vector3.zero;
    private bool active = false;
    private float lastDistance = Mathf.Infinity;


    public override void OnStartServer()
    {
        base.OnStartServer();
        base.TimeManager.OnTick += TimeManager_OnTick;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        base.TimeManager.OnTick -= TimeManager_OnTick;
    }

    protected virtual void Update()
    {
        PerformUpdate(false);
    }

    private void TimeManager_OnTick()
    {
        PerformUpdate(true);
    }

    [Server(Logging = LoggingType.Off)]
    private void PerformUpdate(bool onTick)
    {
        /* If a tick but also host then do not
         * update. The update will occur outside of
         * OnTick, using the update loop. */
        if (onTick && base.IsHost)
            return;
        /* If not called from OnTick and is server
         * only then exit. OnTick will handle movements. */
        else if (!onTick && base.IsServerOnly)
            return;

        //Explode bullet if it goes through the wall
        int layerMask = 1 << 6;
        RaycastHit hit;
        // Does the ray intersect any walls

        //if (GameObject.Find("PhysSim").GetComponent<PhysSim>()._physicsScene.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, Mathf.Infinity, layerMask))
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, Mathf.Infinity, layerMask))
        {
            if(hit.distance>lastDistance)
            {
                explode();
            }
            lastDistance = hit.distance;
        }
        else if(lastDistance < 100000f)
        {
            explode();
        }

        float delta = (onTick) ? (float)base.TimeManager.TickDelta : Time.deltaTime;
        //If host move every update for smooth movement. Otherwise move OnTick.
        Move(delta);
    }

    [Server(Logging = LoggingType.Off)]
    public virtual void Initialize(PreciseTick pt, Vector3 force)
    {
        velocity = force;
        Debug.Log(velocity);
        Invoke("MakeActive", 0.1f);
    }

    [Server(Logging = LoggingType.Off)]
    private void Move(float deltaTime)
    {
        transform.position += (velocity * Time.deltaTime);
    }

    [Server(Logging = LoggingType.Off)]
    private void OnCollisionEnter(Collision collision)
    {
        // Prevents shooting yourself
        if (!active)
            return;

        if (collision.gameObject.tag == "Player")
        {
            PlayerHealth ph = collision.gameObject.GetComponent<PlayerHealth>();
            ph.TakeDamage(15);
            ph.Knockback(Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized + Vector3.up/4, knockback_amount, knockback_growth);
        }
        explode();
    }

    [Server(Logging = LoggingType.Off)]
    private void OnCollisionExit(Collision collision)
    {
        MakeActive();
    }

    [Server(Logging = LoggingType.Off)]
    private void explode()
    {
        Destroy(gameObject);
    }

    [Server(Logging = LoggingType.Off)]
    private void MakeActive()
    {
        active = true;
    }
}
