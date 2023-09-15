using FishNet.Object;
using FishNet.Connection;
using FishNet.Managing.Logging;
using FishNet.Managing.Timing;
using UnityEngine;

public class WindExplosion : NetworkBehaviour, Projectile
{
    [SerializeField] private int damage = 15;
    [SerializeField] private float knockback_amount = 1f;
    [SerializeField] private float knockback_growth = 20f;

    private Vector3 velocity = Vector3.zero;
    private int owner;
    [SerializeField] private GameObject explosion;
    [SerializeField] private float radius;
    private float _colliderRadius;
    private bool isExploding = false;


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

        float delta = (onTick) ? (float)base.TimeManager.TickDelta : Time.deltaTime;
        //If host move every update for smooth movement. Otherwise move OnTick.
        Move(delta);
    }

    [Server(Logging = LoggingType.Off)]
    public virtual void Initialize(PreciseTick pt, Vector3 force, int conn)
    {
        velocity = force;
        SphereCollider sc = GetComponent<SphereCollider>();
        _colliderRadius = sc.radius;
        owner = conn;

        //Move ellapsed time from when grenade was 'thrown' on thrower.
        float timePassed = (float)base.TimeManager.TimePassed(pt.Tick);
        if (timePassed > 0.2f)
            timePassed = 0.2f;

        Move(timePassed);
    }

    [Server(Logging = LoggingType.Off)]
    private void Move(float deltaTime)
    {
        //Determine how far object should travel this frame.
        float travelDistance = (velocity.magnitude * deltaTime);
        //Set trace distance to be travel distance + collider radius.
        float traceDistance = travelDistance + _colliderRadius;

        //Reflect bullet if it hits a wall
        int layerMask = 1 << 6;

        RaycastHit hit;
        // Does the ray intersect any walls

        if (Physics.Raycast(transform.position, velocity, out hit, traceDistance, layerMask) && !isExploding)
        {
            // Find the line from the gun to the point that was clicked.
            Vector3 incomingVec = hit.point - transform.position;
            // Use the point's normal to calculate the reflection vector.
            Vector3 reflectVec = Vector3.Reflect(incomingVec, hit.normal);

            velocity = reflectVec.normalized * velocity.magnitude;
        }

        transform.position += (velocity * deltaTime);
    }

    [Server(Logging = LoggingType.Off)]
    public void explode()
    {
        if (base.IsServer)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius);
            foreach (var hit in hitColliders)
            {
                if (hit.transform.tag == "Player")
                {
                    // knockback direction
                    Vector3 dir = (hit.transform.position - transform.position).normalized;

                    PlayerHealth ph = hit.transform.gameObject.GetComponent<PlayerHealth>();
                    ph.Knockback(dir.normalized, knockback_amount, knockback_growth);
                    ph.TakeDamage(damage);
                }
            }

            ObserversSpawnExplodePrefab(transform.position);

            /* If server only then call destroy now. It will follow in order
             * so clients will receive it after they get the RPC. 
             * If client host destroy in the RPC. */
            if (base.IsServerOnly)
                base.Despawn();
        }
    }

    /// <summary>
    /// Tells clients to spawn the detonate prefab.
    /// </summary>
    [ObserversRpc]
    private void ObserversSpawnExplodePrefab(Vector3 pos)
    {
        SpawnDetonatePrefab(pos);
        //If also client host destroy here.
        if (base.IsServer)
            base.Despawn();
    }

    /// <summary>
    /// Spawns the detonate prefab.
    /// </summary>
    [Client(Logging = LoggingType.Off)]
    private void SpawnDetonatePrefab(Vector3 pos)
    {
        GameObject spawned = Instantiate(explosion, pos, transform.rotation);
        //UnitySceneManager.MoveGameObjectToScene(spawned.gameObject, gameObject.scene);
    }

    public virtual void Reflect(Vector3 dir, int conn)
    {
        velocity = dir * velocity.magnitude;
        owner = conn;
    }
}
