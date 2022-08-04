using FishNet.Object;
using FishNet.Managing.Logging;
using FishNet.Managing.Timing;
using UnityEngine;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public class Fireball : NetworkBehaviour, Projectile
{
    [SerializeField] private int damage = 15;
    [SerializeField] private float knockback_amount = 1f;
    [SerializeField] private float knockback_growth = 20f;
    private Vector3 velocity = Vector3.zero;
    [SerializeField] private GameObject explosion;
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
    public virtual void Initialize(PreciseTick pt, Vector3 force)
    {
        velocity = force;
        SphereCollider sc = GetComponent<SphereCollider>();
        _colliderRadius = sc.radius;

        //Move ellapsed time from when grenade was 'thrown' on thrower.
        float timePassed = (float)base.TimeManager.TimePassed(pt.Tick);
        if (timePassed > 0.1f)
            timePassed = 0.1f;

        //Debug.Log(Owner.ClientId);

        Debug.Log(timePassed);
        Move(timePassed);
    }

    [Server(Logging = LoggingType.Off)]
    private void Move(float deltaTime)
    {
        //Determine how far object should travel this frame.
        float travelDistance = (velocity.magnitude * Time.deltaTime);
        //Set trace distance to be travel distance + collider radius.
        float traceDistance = travelDistance + _colliderRadius;

        //Explode bullet if it goes through the wall
        int layerMask = 1 << 6;

        RaycastHit hit;
        // Does the ray intersect any walls

        //if (GameObject.Find("PhysSim").GetComponent<PhysSim>()._physicsScene.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, Mathf.Infinity, layerMask))
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, traceDistance, layerMask) && !isExploding)
        {
            explode();
            isExploding = true;
        }

        transform.position += (velocity * Time.deltaTime);
    }

    //[Server(Logging = LoggingType.Off)]
    //private void OnCollisionEnter(Collision collision)
    //{
    //    if (collision.gameObject.tag == "Player")
    //    {
    //        PlayerHealth ph = collision.gameObject.GetComponent<PlayerHealth>();
    //        ph.TakeDamage(damage);
    //        ph.Knockback(Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized + Vector3.up/4, knockback_amount, knockback_growth);
    //    }
    //    if(!isExploding)
    //    {
    //        explode();
    //        isExploding = true;
    //    }
    //}

    [Server(Logging = LoggingType.Off)]
    private void explode()
    {
        if (base.IsServer)
        {
            ObserversSpawnExplodePrefab();

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
    private void ObserversSpawnExplodePrefab()
    {
        SpawnDetonatePrefab();
        //If also client host destroy here.
        if (base.IsServer)
            base.Despawn();
    }

    /// <summary>
    /// Spawns the detonate prefab.
    /// </summary>
    [Client(Logging = LoggingType.Off)]
    private void SpawnDetonatePrefab()
    {
        GameObject spawned = Instantiate(explosion, transform.position, transform.rotation);
        UnitySceneManager.MoveGameObjectToScene(spawned.gameObject, gameObject.scene);
    }
}
