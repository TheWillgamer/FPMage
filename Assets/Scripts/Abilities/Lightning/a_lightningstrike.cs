using FishNet.Object;
using FishNet.Managing.Timing;
using DigitalRuby.ThunderAndLightning;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class a_lightningstrike : NetworkBehaviour
{
    [SerializeField] private int damage = 15;
    [SerializeField] private float range = 200f;
    [SerializeField] private float knockback_amount = 1f;
    [SerializeField] private float knockback_growth = 20f;

    [SerializeField] LightningBoltPrefabScript spell;
    [SerializeField] private Transform proj_spawn;
    [SerializeField] private float chargeTime;
    [SerializeField] GameObject SpellStart;
    [SerializeField] GameObject SpellEnd;

    //[SerializeField] private GameObject ownerMeteorGM;
    //private ParticleSystem ownerMeteor;
    //[SerializeField] private GameObject clientMeteorGM;
    //private ParticleSystem clientMeteor;
    [SerializeField] private AudioSource fire;

    private Movement mv;

    #region cooldowns
    //Meteor
    [SerializeField] private float ls_cd;
    private float ls_offcd;
    private bool chargeStarted;
    private float chargeReady;
    #endregion

    #region UI
    //[SerializeField] GameObject cdRepresentation;
    [SerializeField] Image LightningStrike;
    #endregion

    public override void OnStartClient()
    {
        base.OnStartClient();
        //if (IsOwner)
        //cdRepresentation.SetActive(true);
    }

    void Start()
    {
        //ownerMeteor = ownerMeteorGM.GetComponent<ParticleSystem>();
        //clientMeteor = clientMeteorGM.GetComponent<ParticleSystem>();
        ls_offcd = Time.time;
        chargeStarted = false;
        mv = GetComponent<Movement>();
    }

    private void Update()
    {
        if (!mv.disableAB && Input.GetButtonDown("Fire4"))
        {
            if (IsOwner && Time.time > ls_offcd)
            {
                mv.disableAB = true;
                chargeStarted = true;
                //ownerMeteorGM.SetActive(true);
                //ownerMeteor.Play();
                //startMeteorGMServer();
                chargeReady = Time.time + chargeTime;
            }
        }
        if (chargeStarted && Time.time > chargeReady)
        {
            mv.disableAB = false;
            chargeStarted = false;
            //ownerMeteorGM.SetActive(false);
            ls_offcd = Time.time + ls_cd;

            RaycastHit hit;
            if (Physics.Raycast(proj_spawn.position, proj_spawn.forward, out hit, range))
            {
                if (hit.transform.tag == "Player")
                {
                    PlayerHealth ph = hit.transform.gameObject.GetComponent<PlayerHealth>();
                    shootLightning(hit.point, proj_spawn.forward, ph);
                }
                else
                    shootLightning(hit.point, proj_spawn.forward);
            }
            else
                shootLightning(proj_spawn.position + proj_spawn.forward * range, proj_spawn.forward);
        }
        UpdateUI();
    }

    [ObserversRpc]
    private void playShootSound()
    {
        fire.Play();
    }

    //[ServerRpc]
    //private void startMeteorGMServer()
    //{
    //    startMeteorGM();
    //}

    //[ObserversRpc]
    //private void startMeteorGM()
    //{
    //    if (base.IsOwner)
    //        return;
    //    clientMeteorGM.SetActive(true);
    //    clientMeteor.Play();
    //}

    //[ObserversRpc]
    //private void endMeteorGM()
    //{
    //    if (base.IsOwner)
    //        return;
    //    clientMeteorGM.SetActive(false);
    //}

    [ServerRpc]
    private void shootLightning(Vector3 hitLoc, Vector3 dir, PlayerHealth ph = null)
    {
        //endMeteorGM();
        playShootSound();
        ShowLightning(hitLoc);

        if (ph == null)
            return;

        ph.Knockback(dir, knockback_amount, knockback_growth);
        ph.TakeDamage(damage);
    }

    [ObserversRpc]
    private void ShowLightning(Vector3 end)
    {
        SpellStart.transform.position = proj_spawn.position;
        SpellEnd.transform.position = end;
        spell.Trigger();
    }

    private void UpdateUI()
    {
        LightningStrike.fillAmount = 1 - (ls_offcd - Time.time) / ls_cd;
    }
}
