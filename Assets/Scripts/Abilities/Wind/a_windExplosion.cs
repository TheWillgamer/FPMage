using FishNet.Object;
using FishNet.Managing.Timing;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Net;

public class a_windExplosion : NetworkBehaviour
{
    [SerializeField] private GameObject we;
    [SerializeField] private GameObject wec;
    [SerializeField] private Transform proj_spawn;
    [SerializeField] private float proj_force;
    [SerializeField] private float minTimeToExplode;
    private Projectile proj;
    private bool chargeStarted;
    AudioSource m_shootingSound;
    private bool explodable;
    private bool letGo;

    private Movement mv;
    private TimeManager tm;
    private GameObject clientObj;

    #region cooldowns
    //Meteor
    [SerializeField] private float we_cd;
    private float we_offcd;
    #endregion

    #region UI
    [SerializeField] Image Explosion;
    #endregion

    public override void OnStartClient()
    {
        base.OnStartClient();
    }

    void Start()
    {
        m_shootingSound = GetComponent<AudioSource>();
        we_offcd = Time.time;
        mv = GetComponent<Movement>();
        chargeStarted = false;
        letGo = false;
        explodable = false;
        tm = GameObject.FindWithTag("NetworkManager").GetComponent<TimeManager>();
    }

    private void Update()
    {
        if (!mv.disableAB && Input.GetButtonDown("Fire2"))
        {
            if (IsOwner && Time.time > we_offcd)
            {
                m_shootingSound.Play();

                clientObj = Instantiate(wec, proj_spawn.position, proj_spawn.rotation);
                MoveProjectileClient proj = clientObj.GetComponent<MoveProjectileClient>();
                proj.Initialize(proj_spawn.forward * proj_force, Mathf.Min(180f, (float)tm.RoundTripTime) / 1000f);

                shootWind(base.TimeManager.GetPreciseTick(TickType.Tick), proj_spawn.position, proj_spawn.rotation);
                mv.disableAB = true;
                chargeStarted = true;
                explodable = false;
                Invoke("makeExplodable", minTimeToExplode);
            }
        }

        if (chargeStarted && Input.GetButtonUp("Fire2"))
        {
            letGo = true;
        }

        if (letGo && explodable)
        {
            explodeWind();
            mv.disableAB = false;
            chargeStarted = false;
            we_offcd = Time.time + we_cd;
            letGo = false;
        }
        UpdateUI();
    }

    private void makeExplodable()
    {
        explodable = true;
    }

    [ObserversRpc]
    private void playShootSound()
    {
        if (!IsOwner)
            m_shootingSound.Play();
        else
            Destroy(clientObj);
    }

    [ServerRpc]
    private void shootWind(PreciseTick pt, Vector3 startLoc, Quaternion startRot)
    {
        playShootSound();
        GameObject spawned = Instantiate(we, startLoc, startRot);
        base.Spawn(spawned);

        proj = spawned.GetComponent<Projectile>();
        proj.Initialize(pt, startRot * Vector3.forward * proj_force, base.Owner.ClientId);
    }

    [ServerRpc]
    private void explodeWind()
    {
        if (proj != null)
        {
            WindExplosion we = (WindExplosion)proj;
            we.explode();
        }
    }

    private void UpdateUI()
    {
        Explosion.fillAmount = 1 - (we_offcd - Time.time) / we_cd;
    }
}
