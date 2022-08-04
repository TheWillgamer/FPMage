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
    [SerializeField] Transform SpellStart;
    [SerializeField] Transform SpellEnd;
    [SerializeField] float MaxDistance;

    [Server(Logging = LoggingType.Off)]
    public void LightningHit()
    {
        ServerFire(transform.position, transform.forward);
    }

    private void ServerFire(Vector3 start, Vector3 direction)
    {
        //base.RollbackManager.Rollback(pt, RollbackManager.PhysicsType.ThreeDimensional, base.IsOwner);

        Ray ray = new Ray(start, direction);
        RaycastHit hit;
        

        //If ray hits.
        if (Physics.Raycast(ray, out hit, MaxDistance))
        {
            SpellStart.position = start;
            SpellEnd.position = hit.point;
            if (hit.transform.gameObject.tag == "Player")
            {
                //Apply damage and other server things.
                if (base.IsServer)
                {
                    PlayerHealth ph = hit.transform.gameObject.GetComponent<PlayerHealth>();
                    ph.TakeDamage(damage);
                    ph.Knockback(Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized + Vector3.up / 4, knockback_amount, knockback_growth);
                }
            }
        }
        else
        {
            SpellStart.position = start;
            SpellEnd.position = start + direction * MaxDistance;
        }
        ShowLightning(SpellStart.position, SpellEnd.position);
        //base.RollbackManager.Return();
        spell.Trigger();
    }

    [ObserversRpc]
    private void ShowLightning(Vector3 start, Vector3 end)
    {
        SpellStart.position = start;
        SpellEnd.position = end;
        spell.Trigger();
    }
}