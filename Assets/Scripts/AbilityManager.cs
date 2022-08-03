using FishNet.Object;
using FishNet.Managing.Logging;
using FishNet.Managing.Timing;
using UnityEngine;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public class AbilityManager : NetworkBehaviour
{
    private bool firing = false;
    [SerializeField] private GameObject fb;
    [SerializeField] private Transform proj_spawn;
    [SerializeField] private float proj_force;
    AudioSource m_shootingSound;

    void Start()
    {
        m_shootingSound = GetComponent<AudioSource>();
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
}
