using FishNet.Object;
using FishNet.Managing.Timing;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class a_lightningbolt : NetworkBehaviour
{
    [SerializeField] private GameObject lb;
    [SerializeField] private Transform proj_spawn;
    [SerializeField] private float proj_force;
    AudioSource m_shootingSound;

    private Movement mv;

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
                Vector3 endPoint = proj_spawn.position + proj_spawn.forward * 100f;
                RaycastHit hit;
                if (Physics.Raycast(proj_spawn.position, proj_spawn.forward, out hit, 100f))
                {
                    endPoint = hit.point;
                }
                shootLightningBolt(endPoint);
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
        m_shootingSound.Play();
    }

    [ServerRpc]
    private void shootLightningBolt(Vector3 endPoint)
    {
        playShootSound();
        GameObject spawned = Instantiate(lb, proj_spawn.position, proj_spawn.rotation);
        base.Spawn(spawned);

        Projectile proj = spawned.GetComponent<Projectile>();
        proj.Initialize(base.TimeManager.GetPreciseTick(TickType.Tick), (endPoint - proj_spawn.position).normalized * proj_force, base.Owner.ClientId);
    }

    private void UpdateUI()
    {
        LightningBolt.fillAmount = lb_charge / 100f;
    }
}
