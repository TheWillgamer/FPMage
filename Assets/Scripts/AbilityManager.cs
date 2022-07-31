using FishNet.Object;
using FishNet.Managing.Logging;
using FishNet.Managing.Timing;
using UnityEngine;

public class AbilityManager : NetworkBehaviour
{
    private bool firing = false;
    [SerializeField] private GameObject fb;
    [SerializeField] private Transform proj_spawn;
    [SerializeField] private float proj_force;

    private void Update()
    {
        if (base.IsOwner)
        {
            if (Input.GetButtonDown("Fire1"))
            {
                shootFireball();
            }
        }
    }

    [ServerRpc]
    private void shootFireball()
    {
        GameObject spawned = Instantiate(fb, proj_spawn.position, proj_spawn.rotation);
        base.Spawn(spawned);
        
        Projectile throwable = spawned.transform.GetChild(0).GetComponent<Projectile>();
        throwable.Initialize(new PreciseTick(), proj_spawn.forward * proj_force);
    }
}
