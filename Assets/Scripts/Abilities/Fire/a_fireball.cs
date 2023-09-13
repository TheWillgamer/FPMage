using FishNet.Object;
using FishNet.Managing.Timing;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
//using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public class a_fireball : NetworkBehaviour
{
    [SerializeField] private GameObject fb;
    [SerializeField] private GameObject fbc;
    [SerializeField] private Transform proj_spawn;
    [SerializeField] private float proj_force;
    AudioSource m_shootingSound;

    private Movement mv;
    private TimeManager tm;
    Queue<GameObject> clientObjs = new Queue<GameObject>();

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
        tm = GameObject.FindWithTag("NetworkManager").GetComponent<TimeManager>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (!mv.disableAB && Input.GetButtonDown("Fire1") && fb_charges > 0)
        {
            m_shootingSound.Play();

            GameObject clientObj = Instantiate(fbc, proj_spawn.position, proj_spawn.rotation);
            clientObjs.Enqueue(clientObj);
            MoveProjectileClient proj = clientObj.GetComponent<MoveProjectileClient>();
            proj.Initialize(proj_spawn.forward * proj_force, Mathf.Min(180f, (float)tm.RoundTripTime) / 1000f);

            shootFireball(base.TimeManager.GetPreciseTick(TickType.Tick), proj_spawn.position, proj_spawn.rotation);
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
        if (!IsOwner)
            m_shootingSound.Play();
        //else
            //Destroy(clientObjs.Dequeue());
    }

    [ServerRpc]
    private void shootFireball(PreciseTick pt, Vector3 startLoc, Quaternion startRot)
    {
        playShootSound();

        GameObject spawned = Instantiate(fb, startLoc, startRot);
        base.Spawn(spawned);

        Projectile proj = spawned.GetComponent<Projectile>();
        proj.Initialize(pt, startRot * Vector3.forward * proj_force, base.Owner.ClientId);
    }

    private void UpdateUI()
    {
        for (int i = 0; i < 3; i++)
        {
            Fireball[i].gameObject.SetActive(fb_charges > i);
        }
    }
}
