using FishNet.Object;
using FishNet.Managing.Timing;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class a_lightningbolt : NetworkBehaviour, Ability
{
    [SerializeField] private GameObject lb;
    [SerializeField] private GameObject lbc;
    [SerializeField] private Transform proj_spawn;
    [SerializeField] private float proj_force;
    [SerializeField] private AudioSource start;
    [SerializeField] private float startLength;

    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource charge;
    [SerializeField] private AudioSource fire;

    [SerializeField] private ParticleSystem ownerCharge;
    [SerializeField] private ParticleSystem clientCharge;
    [SerializeField] private ParticleSystem ownerFullCharge;
    [SerializeField] private ParticleSystem clientFullCharge;

    private Movement mv;
    private TimeManager tm;
    Queue<GameObject> clientObjs = new Queue<GameObject>();

    #region cooldowns
    //LightningBolt
    [SerializeField] private float lb_chargeRate;
    private float lb_charge;
    private float lb_maxCharge;
    private bool chargeStarted;
    #endregion

    #region UI
    [SerializeField] GameObject cdRepresentation;
    [SerializeField] Image LightningBolt;
    #endregion

    public override void OnStartClient()
    {
        base.OnStartClient();
        //if (IsOwner)
            //cdRepresentation.SetActive(true);
    }

    void Start()
    {
        chargeStarted = false;
        mv = GetComponent<Movement>();
        tm = GameObject.FindWithTag("NetworkManager").GetComponent<TimeManager>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (!mv.disableAB && Input.GetButtonDown("Fire1"))
        {
            chargeStarted = true;
            lb_charge = 0f;
            mv.disableAB = true;

            startChargeSoundServer();
            start.Play();
            charge.PlayDelayed(startLength);

            ownerCharge.Play();
            ownerFullCharge.Play();
        }

        if (chargeStarted)
        {
            lb_charge += lb_chargeRate * Time.deltaTime;
        }

        if (lb_charge >= 100f)
            LightningBolt.color = new Color32(255, 249, 24, 180);

        if (lb_charge > 0 && Input.GetButtonUp("Fire1"))
        {
            start.Stop();
            charge.Stop();
            stopChargeSoundServer();

            ownerCharge.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ownerFullCharge.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            if (lb_charge >= 100f)
            {
                fire.Play();

                GameObject clientObj = Instantiate(lbc, proj_spawn.position, proj_spawn.rotation);
                clientObjs.Enqueue(clientObj);
                MoveProjectileClient proj = clientObj.GetComponent<MoveProjectileClient>();
                proj.Initialize(proj_spawn.forward * proj_force, Mathf.Min(180f, (float)tm.RoundTripTime) / 1000f);

                shootLightningBolt(base.TimeManager.GetPreciseTick(TickType.Tick), proj_spawn.position, proj_spawn.rotation);
            }
            chargeStarted = false;
            lb_charge = 0f;
            mv.disableAB = false;
            LightningBolt.color = new Color32(255, 255, 255, 180);
        }
        UpdateUI();
    }

    [ServerRpc]
    private void startChargeSoundServer()
    {
        startChargeSound();
    }
    [ObserversRpc]
    private void startChargeSound()
    {
        if (!IsOwner)
        {
            start.Play();
            animator.SetTrigger("chargeStarted");
            charge.PlayDelayed(startLength);

            clientCharge.Play();
            clientFullCharge.Play();
        }
    }

    [ServerRpc]
    private void stopChargeSoundServer()
    {
        stopChargeSound();
    }
    [ObserversRpc]
    private void stopChargeSound()
    {
        if (!IsOwner)
        {
            start.Stop();
            charge.Stop();
            animator.SetTrigger("chargeEnded");

            clientCharge.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            clientFullCharge.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    [ObserversRpc]
    private void playShootSound()
    {
        if (!IsOwner)
        {
            fire.Play();
            animator.SetTrigger("chargeFired");
        }
        else
            Destroy(clientObjs.Dequeue());
    }

    [ServerRpc]
    private void shootLightningBolt(PreciseTick pt, Vector3 startLoc, Quaternion startRot)
    {
        playShootSound();
        GameObject spawned = Instantiate(lb, startLoc, startRot);
        base.Spawn(spawned);

        Projectile proj = spawned.GetComponent<Projectile>();
        proj.Initialize(pt, startRot * Vector3.forward * proj_force, base.Owner.ClientId);
    }

    private void UpdateUI()
    {
        LightningBolt.fillAmount = lb_charge / 100f;
    }

    public void Reset()
    {
        start.Stop();
        charge.Stop();
        stopChargeSoundServer();

        ownerCharge.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ownerFullCharge.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        chargeStarted = false;
        lb_charge = 0f;
        LightningBolt.color = new Color32(255, 255, 255, 180);
    }
}
