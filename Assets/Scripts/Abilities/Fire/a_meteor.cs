using FishNet.Object;
using FishNet.Managing.Timing;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
//using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public class a_meteor : NetworkBehaviour
{
    [SerializeField] private GameObject mt;
    [SerializeField] private GameObject mtc;
    [SerializeField] private Transform proj_spawn;
    [SerializeField] private float proj_force;
    [SerializeField] private float upAmt;
    [SerializeField] private float chargeTime;
    [SerializeField] private GameObject ownerMeteorGM;
    private ParticleSystem ownerMeteor;
    [SerializeField] private GameObject clientMeteorGM;
    private ParticleSystem clientMeteor;
    AudioSource m_shootingSound;

    private Movement mv;
    private TimeManager tm;
    private GameObject clientObj;

    #region cooldowns
    //Meteor
    [SerializeField] private float mt_cd;
    private float mt_offcd;
    private bool chargeStarted;
    private float chargeReady;
    #endregion

    #region UI
    //[SerializeField] GameObject cdRepresentation;
    [SerializeField] Image Meteor;
    #endregion

    public override void OnStartClient()
    {
        base.OnStartClient();
        //if (IsOwner)
            //cdRepresentation.SetActive(true);
    }

    void Start()
    {
        m_shootingSound = GetComponent<AudioSource>();
        ownerMeteor = ownerMeteorGM.GetComponent<ParticleSystem>();
        clientMeteor = clientMeteorGM.GetComponent<ParticleSystem>();
        mt_offcd = Time.time;
        chargeStarted = false;
        mv = GetComponent<Movement>();
        tm = GameObject.FindWithTag("NetworkManager").GetComponent<TimeManager>();
    }

    private void Update()
    {
        if (!mv.disableAB && Input.GetButtonDown("Fire4"))
        {
            if (IsOwner && Time.time > mt_offcd)
            {
                mv.disableAB = true;
                chargeStarted = true;
                ownerMeteorGM.SetActive(true);
                ownerMeteor.Play();
                startMeteorGMServer();
                chargeReady = Time.time + chargeTime;
            }
        }
        if (chargeStarted && Time.time > chargeReady)
        {
            mv.disableAB = false;
            m_shootingSound.Play();

            clientObj = Instantiate(mtc, proj_spawn.position, proj_spawn.rotation);
            MoveProjectileClient proj = clientObj.GetComponent<MoveProjectileClient>();
            proj.Initialize(proj_spawn.forward * proj_force + Vector3.up * upAmt, Mathf.Min(180f, (float)tm.RoundTripTime) / 1000f);

            shootMeteor(base.TimeManager.GetPreciseTick(TickType.Tick), proj_spawn.position, proj_spawn.rotation);
            chargeStarted = false;
            ownerMeteorGM.SetActive(false);
            mt_offcd = Time.time + mt_cd;
        }
        UpdateUI();
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
    private void startMeteorGMServer()
    {
        startMeteorGM();
    }

    [ObserversRpc]
    private void startMeteorGM()
    {
        if (base.IsOwner)
            return;
        clientMeteorGM.SetActive(true);
        clientMeteor.Play();
    }

    [ObserversRpc]
    private void endMeteorGM()
    {
        if (base.IsOwner)
            return;
        clientMeteorGM.SetActive(false);
    }

    [ServerRpc]
    private void shootMeteor(PreciseTick pt, Vector3 startLoc, Quaternion startRot)
    {
        endMeteorGM();
        playShootSound();
        GameObject spawned = Instantiate(mt, startLoc, startRot);

        base.Spawn(spawned);

        Projectile proj = spawned.GetComponent<Projectile>();
        proj.Initialize(pt, startRot * Vector3.forward * proj_force + Vector3.up * upAmt, base.Owner.ClientId);
    }

    private void UpdateUI()
    {
        Meteor.fillAmount = 1 - (mt_offcd - Time.time) / mt_cd;
    }
}
