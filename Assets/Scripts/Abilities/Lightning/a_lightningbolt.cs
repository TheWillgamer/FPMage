using FishNet.Object;
using FishNet.Managing.Timing;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class a_lightningbolt : NetworkBehaviour
{
    [SerializeField] private GameObject lb;
    [SerializeField] private GameObject lbc;
    [SerializeField] private Transform proj_spawn;
    [SerializeField] private float proj_force;
    AudioSource m_shootingSound;

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
        if (IsOwner)
            cdRepresentation.SetActive(true);
    }

    void Start()
    {
        m_shootingSound = GetComponent<AudioSource>();
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
        }

        if (chargeStarted)
        {
            lb_charge += lb_chargeRate * Time.deltaTime;
        }

        if (lb_charge >= 100f)
            LightningBolt.color = new Color32(255, 210, 80, 180);

        if (Input.GetButtonUp("Fire1"))
        {
            if (lb_charge >= 100f)
            {
                m_shootingSound.Play();

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

    [ObserversRpc]
    private void playShootSound()
    {
        if (!IsOwner)
            m_shootingSound.Play();
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
}
