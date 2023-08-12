using FishNet.Object;
using FishNet.Managing.Timing;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
//using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public class a_fireball : NetworkBehaviour
{
    [SerializeField] private GameObject fb;
    [SerializeField] private Transform proj_spawn;
    [SerializeField] private float proj_force;
    AudioSource m_shootingSound;

    private Movement mv;

    #region cooldowns
    //Fireball
    private int fb_charges;
    [SerializeField] private float fb_cd;
    private float fb_offcd;
    #endregion

    #region UI
    [SerializeField] GameObject cdRepresentation;
    [SerializeField] Image[] Fireball;
    #endregion

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (IsOwner)
            cdRepresentation.SetActive(true);
    }

    void Start()
    {
        m_shootingSound = GetComponent<AudioSource>();
        fb_charges = 3;
        fb_offcd = Time.time;
        mv = GetComponent<Movement>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (!mv.disableAB && Input.GetButtonDown("Fire1") && fb_charges > 0)
        {
            Vector3 endPoint = proj_spawn.position + proj_spawn.forward * 100f;
            RaycastHit hit;
            if (Physics.Raycast(proj_spawn.position, proj_spawn.forward, out hit, 100f))
            {
                endPoint = hit.point;
            }

            shootFireball(endPoint);
            fb_charges--;
            if (fb_charges == 2)
            {
                fb_offcd = Time.time + fb_cd;
            }
        }

        if (fb_charges < 3 && Time.time > fb_offcd)
        {
            fb_charges += 1;
            fb_offcd = Time.time + fb_cd;
        }
        UpdateUI();
    }

    [ObserversRpc]
    private void playShootSound()
    {
        m_shootingSound.Play();
    }

    [ServerRpc]
    private void shootFireball(Vector3 endPoint)
    {
        playShootSound();
        GameObject spawned = Instantiate(fb, proj_spawn.position, proj_spawn.rotation);
        //Physics.IgnoreCollision(spawned.GetComponent<Collider>(), GetComponent<Collider>());

        //UnitySceneManager.MoveGameObjectToScene(spawned.gameObject, gameObject.scene);
        base.Spawn(spawned);

        Projectile proj = spawned.GetComponent<Projectile>();
        proj.Initialize(base.TimeManager.GetPreciseTick(TickType.Tick), (endPoint - proj_spawn.position).normalized * proj_force, base.Owner.ClientId);
    }

    private void UpdateUI()
    {
        for (int i = 0; i < 3; i++)
        {
            Fireball[i].gameObject.SetActive(fb_charges > i);
        }
    }
}
