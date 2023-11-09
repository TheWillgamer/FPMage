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
    [SerializeField] private ParticleSystem ownerMeteor;
    [SerializeField] private ParticleSystem clientMeteor;

    private Movement mv;
    private TimeManager tm;
    private GameObject clientObj;
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource charge;
    [SerializeField] private AudioSource fire;

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
                ownerMeteor.Play();
                charge.Play();
                //animator.SetTrigger("Meteor");
                startMeteorGMServer();
                chargeReady = Time.time + chargeTime;
            }
        }
        if (chargeStarted && Time.time > chargeReady)
        {
            mv.disableAB = false;
            charge.Stop();
            fire.Play();

            clientObj = Instantiate(mtc, proj_spawn.position, proj_spawn.rotation);
            MoveProjectileClient proj = clientObj.GetComponent<MoveProjectileClient>();
            proj.Initialize(proj_spawn.forward * proj_force + Vector3.up * upAmt, Mathf.Min(180f, (float)tm.RoundTripTime) / 1000f);

            shootMeteor(base.TimeManager.GetPreciseTick(TickType.Tick), proj_spawn.position, proj_spawn.rotation);
            chargeStarted = false;
            ownerMeteor.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            mt_offcd = Time.time + mt_cd;
        }
        UpdateUI();
    }

    [ObserversRpc]
    private void playShootSound()
    {
        if (!IsOwner)
        {
            charge.Stop();
            fire.Play();
        }
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
        clientMeteor.Play();
        charge.Play();
        animator.SetTrigger("Meteor");
    }

    [ObserversRpc]
    private void endMeteorGM()
    {
        if (base.IsOwner)
            return;
        clientMeteor.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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
