using FishNet.Object;
using FishNet.Managing.Timing;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class a_windSlash : NetworkBehaviour
{
    [SerializeField] private GameObject ws;
    [SerializeField] private GameObject wsr;
    [SerializeField] private GameObject wsc;
    [SerializeField] private GameObject wsrc;
    [SerializeField] private Transform proj_spawn;
    [SerializeField] private float proj_force;
    AudioSource m_shootingSound;
    private bool wdir;          // Direction of wind slash effect
    private bool wdirc;          // Direction of wind slash effect for client

    private Movement mv;
    private TimeManager tm;
    Queue<GameObject> clientObjs = new Queue<GameObject>();

    #region cooldowns
    //WindSlash
    private float ws_charge;
    [SerializeField] private float ws_chargeRate;
    [SerializeField] private float ws_dechargeRate;
    [SerializeField] private float ws_cd;
    private float ws_offcd;
    private bool readyToFire;
    private bool coolingDown;
    #endregion

    #region UI
    [SerializeField] GameObject cdRepresentation;
    [SerializeField] Image WindSlash;
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
        ws_charge = 0f;
        ws_offcd = Time.time;
        mv = GetComponent<Movement>();
        tm = GameObject.FindWithTag("NetworkManager").GetComponent<TimeManager>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (!mv.disableAB && !coolingDown && Input.GetButton("Fire1") && Time.time > ws_offcd)
        {
            m_shootingSound.Play();

            GameObject clientObj;
            if (wdir)
                clientObj = Instantiate(wsc, proj_spawn.position, proj_spawn.rotation);
            else
                clientObj = Instantiate(wsrc, proj_spawn.position, proj_spawn.rotation);
            wdir = !wdir;
            clientObjs.Enqueue(clientObj);
            MoveProjectileClient proj = clientObj.GetComponent<MoveProjectileClient>();
            proj.Initialize(proj_spawn.forward * proj_force, Mathf.Min(180f, (float)tm.RoundTripTime) / 1000f);

            shootWind(base.TimeManager.GetPreciseTick(TickType.Tick), proj_spawn.position, proj_spawn.rotation);
            ws_offcd = Time.time + ws_cd;

            ws_charge += ws_chargeRate;

            if (ws_charge > 100f)
            {
                ws_charge = 100f;
                WindSlash.color = new Color32(255, 210, 80, 180);
                coolingDown = true;
            }
        }

        ws_charge -= ws_dechargeRate * Time.deltaTime;
        if (ws_charge < 0f)
        {
            ws_charge = 0f;
            WindSlash.color = new Color32(255, 255, 255, 180);
            coolingDown = false;
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
    private void shootWind(PreciseTick pt, Vector3 startLoc, Quaternion startRot)
    {
        playShootSound();
        GameObject spawned;
        if (wdir)
            spawned = Instantiate(ws, startLoc, startRot);
        else
            spawned = Instantiate(wsr, startLoc, startRot);
        wdir = !wdir;

        base.Spawn(spawned);

        Projectile proj = spawned.GetComponent<Projectile>();
        proj.Initialize(pt, startRot * Vector3.forward * proj_force, base.Owner.ClientId);
    }

    private void UpdateUI()
    {
        WindSlash.fillAmount = ws_charge / 100f;
    }
}
