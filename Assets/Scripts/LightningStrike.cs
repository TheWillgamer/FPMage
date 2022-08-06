//
// Procedural Lightning for Unity
// (c) 2015 Digital Ruby, LLC
// Source code may be used for personal or commercial projects.
// Source code may NOT be redistributed or sold.
// 

using UnityEngine;
using DigitalRuby.ThunderAndLightning;
using FishNet.Object;
using FishNet.Managing.Logging;
using FishNet.Managing.Timing;
using FishNet.Component.ColliderRollback;
using FishNet.Connection;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

/// <summary>
/// Lightning bolt beam spell script, like a phasor or laser
/// </summary>
public class LightningStrike : NetworkBehaviour
{
    [SerializeField] private int damage = 40;
    [SerializeField] private float knockback_amount = 20f;
    [SerializeField] private float knockback_growth = 60f;
    [SerializeField] LightningBoltPrefabScript spell;
    [SerializeField] GameObject SpellStart;
    [SerializeField] GameObject SpellEnd;
    [SerializeField] float MaxDistance;
    public Transform projSpawn = null;

    public override void OnStartServer()
    {
        base.OnStartServer();
        LightningCharge(true);
    }

    protected virtual void Start()
    {
        LightningCharge(false);
    }

    [Server(Logging = LoggingType.Off)]
    private void LightningCharge(bool server)
    {
        if (server && base.IsHost)
            return;
        else if (!server && base.IsServerOnly)
            return;

        Invoke("LightningShoot", 1f);
    }

    [Server(Logging = LoggingType.Off)]
    private void LightningShoot()
    {
        RaycastHit hit;

        SpellStart.transform.position = projSpawn.position;
        //If ray hits.
        if (GameObject.Find("PhysSim").GetComponent<PhysSim>()._physicsScene.Raycast(projSpawn.position, projSpawn.forward, out hit, MaxDistance))
        {
            SpellEnd.transform.position = hit.point;
            if (hit.transform.gameObject.tag == "Player")
            {
                //Apply damage and other server things.
                if (base.IsServer)
                {
                    PlayerHealth ph = hit.transform.gameObject.GetComponent<PlayerHealth>();
                    ph.TakeDamage(damage);
                    ph.Knockback(transform.forward, knockback_amount, knockback_growth);
                }
            }
        }
        else
        {
            SpellEnd.transform.position = projSpawn.position + projSpawn.forward * MaxDistance;
        }
        if (base.IsServer)
            ShowLightning(SpellEnd.transform.position);
    }

    [ObserversRpc]
    private void ShowLightning(Vector3 end)
    {
        SpellEnd.transform.position = end;
        spell.Trigger();
    }
}