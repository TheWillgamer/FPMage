using FishNet.Object;
using FishNet.Managing.Timing;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class a_windSlash : NetworkBehaviour
{
    [SerializeField] private GameObject ws;
    [SerializeField] private GameObject wsr;
    [SerializeField] private Transform proj_spawn;
    [SerializeField] private float proj_force;
    AudioSource m_shootingSound;
    private bool wdir;          // Direction of wind slash effect

    private Movement mv;

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
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (!mv.disableAB && !coolingDown && Input.GetButton("Fire1") && Time.time > ws_offcd)
        {
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
        m_shootingSound.Play();
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
