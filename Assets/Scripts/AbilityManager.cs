using FishNet.Object;
using FishNet.Managing.Logging;
using FishNet.Managing.Timing;
using UnityEngine;
using System.Collections;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public class AbilityManager : NetworkBehaviour
{
    [SerializeField] private GameObject fb;
    [SerializeField] private GameObject ls;
    [SerializeField] private Transform proj_spawn;
    [SerializeField] private float proj_force;
    [SerializeField] private float lightningChargingSpeedDecreaseRate;
    [SerializeField] private float dashForce;
    [SerializeField] private float dashDur;

    AudioSource m_shootingSound;
    Movement mv;

    void Start()
    {
        m_shootingSound = GetComponent<AudioSource>();
        mv = GetComponent<Movement>();
    }

    private void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            if (IsOwner)
            {
                shootFireball();
            }
            if (IsServer)
            {
                playShootSound();
            }
        }
        if (Input.GetButtonDown("Fire2"))
        {
            if (IsOwner)
            {
                fireLightning();
            }
            if (IsServer)
            {
                //playShootSound();
            }
        }
        if (Input.GetButtonDown("Fire3"))
        {
            if (IsOwner)
            {
                setDashing();
                if (!IsServer)
                {
                    mv.h_dashing = true;
                    mv.dashModifier = dashForce;
                    mv.dashDuration = dashDur;
                }
            }
        }
    }

    [ObserversRpc]
    private void playShootSound(){
        m_shootingSound.Play();
    }

    [ServerRpc]
    private void shootFireball()
    {
        playShootSound();
        GameObject spawned = Instantiate(fb, proj_spawn.position, proj_spawn.rotation);
        Physics.IgnoreCollision(spawned.GetComponent<Collider>(), GetComponent<Collider>());

        UnitySceneManager.MoveGameObjectToScene(spawned.gameObject, gameObject.scene);
        base.Spawn(spawned);
        
        Projectile proj = spawned.GetComponent<Projectile>();
        proj.Initialize(base.TimeManager.GetPreciseTick(TickType.Tick), proj_spawn.forward * proj_force);
    }

    [ServerRpc]
    private void fireLightning()
    {
        GameObject spawned = Instantiate(ls, proj_spawn.position, proj_spawn.rotation);

        UnitySceneManager.MoveGameObjectToScene(spawned.gameObject, gameObject.scene);
        base.Spawn(spawned);

        spawned.GetComponent<LightningStrike>().projSpawn = proj_spawn;
    }

    [ServerRpc]
    private void setDashing()
    {
        Movement mv = GetComponent<Movement>();
        mv.h_dashing = true;
        mv.dashModifier = dashForce;
        mv.dashDuration = dashDur;
    }
}
