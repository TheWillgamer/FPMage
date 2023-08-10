using FishNet.Object;
using FishNet.Managing.Timing;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class a_windSlash : NetworkBehaviour
{
    [SerializeField] private GameObject ws;
    [SerializeField] private Transform proj_spawn;
    [SerializeField] private float proj_force;
    AudioSource m_shootingSound;

    private Movement mv;

    #region cooldowns
    //WindSlash
    private float ws_charge;
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
        ws_offcd = Time.deltaTime;
        mv = GetComponent<Movement>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (!mv.disableAB && Input.GetButton("Fire1") && readyToFire)
        {
            Vector3 endPoint = proj_spawn.position + proj_spawn.forward * 30f;
            RaycastHit hit;
            if (Physics.Raycast(proj_spawn.position, proj_spawn.forward, out hit, 30f))
            {
                endPoint = hit.point;
            }

            shootWind(endPoint);
        }
        UpdateUI();
    }

    [ObserversRpc]
    private void playShootSound()
    {
        m_shootingSound.Play();
    }

    [ServerRpc]
    private void shootWind(Vector3 endPoint)
    {
        playShootSound();
        GameObject spawned = Instantiate(ws, proj_spawn.position, proj_spawn.rotation);

        base.Spawn(spawned);

        Projectile proj = spawned.GetComponent<Projectile>();
        proj.Initialize(base.TimeManager.GetPreciseTick(TickType.Tick), (endPoint - proj_spawn.position).normalized * proj_force, base.Owner.ClientId);
    }

    private void UpdateUI()
    {
    }
}
