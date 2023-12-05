using FishNet.Object;
using FishNet.Managing.Timing;
using DigitalRuby.ThunderAndLightning;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class a_lightningstrike : NetworkBehaviour, Ability
{
    [SerializeField] private int damage = 15;
    [SerializeField] private float range = 200f;
    [SerializeField] private float knockback_amount = 1f;
    [SerializeField] private float knockback_growth = 20f;

    [SerializeField] LightningBoltPrefabScript spell;
    [SerializeField] private Transform ownerSpawn;
    [SerializeField] private Transform clientSpawn;
    [SerializeField] private Transform cam;
    [SerializeField] private float chargeTime;
    [SerializeField] GameObject SpellStart;
    [SerializeField] GameObject SpellEnd;

    [SerializeField] private ParticleSystem ownerCharge;
    [SerializeField] private ParticleSystem clientCharge;
    [SerializeField] private GameObject hitImpact;
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource charge;
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
    [SerializeField] Image background;
    [SerializeField] Image meter;
    [SerializeField] TMP_Text countdown;
    #endregion

    void Start()
    {
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
                ownerCharge.Play();
                startChargeGMServer();
                charge.Play();

                chargeReady = Time.time + chargeTime;
            }
        }
        if (chargeStarted && Time.time > chargeReady)
        {
            mv.disableAB = false;
            chargeStarted = false;
            ls_offcd = Time.time + ls_cd;

            ShootRaycast(true);
        }
        UpdateUI();
    }

    private void ShootRaycast(bool firstTime)
    {
        Vector3 pos = firstTime ? cam.position : cam.position + cam.forward * 2f;
        
        RaycastHit hit;
        if (Physics.Raycast(pos, cam.forward, out hit, range))
        {
            if (hit.transform.tag == "Player")
            {
                if (hit.transform.parent.GetComponent<NetworkObject>().Owner != base.Owner)
                {
                    PlayerHealth ph = hit.transform.gameObject.GetComponent<PlayerHealth>();
                    shootLightning(hit.point, cam.forward, ph);
                }
                else if (firstTime)
                    ShootRaycast(false);
                else
                    shootLightning(cam.position + cam.forward * range, cam.forward);
            }
            else
                shootLightning(hit.point, cam.forward);
        }
        else
            shootLightning(cam.position + cam.forward * range, cam.forward);
    }

    [ObserversRpc]
    private void playShootSound()
    {
        charge.Stop();
        fire.Play();
    }

    [ServerRpc]
    private void startChargeGMServer()
    {
        startChargeGM();
    }

    [ObserversRpc]
    private void startChargeGM()
    {
        if (base.IsOwner)
            return;
        clientCharge.Play();
        animator.SetTrigger("hitscan");
        charge.Play();
    }

    [ServerRpc]
    private void shootLightning(Vector3 hitLoc, Vector3 dir, PlayerHealth ph = null)
    {
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
        if (base.IsOwner)
            SpellStart.transform.position = ownerSpawn.position;
        else
            SpellStart.transform.position = clientSpawn.position;
        SpellEnd.transform.position = end;
        Instantiate(hitImpact, end, cam.rotation);
        spell.Trigger();
    }

    private void UpdateUI()
    {
        float remainingCD = ls_offcd - Time.time;

        if (chargeStarted)
        {
            background.color = new Color32(255, 190, 0, 255);
        }
        else if (remainingCD > 0)
        {
            background.color = new Color32(100, 100, 100, 255);
            meter.fillAmount = 1 - remainingCD / ls_cd;
            countdown.text = ((int)(remainingCD) + 1).ToString();
        }
        else
        {
            background.color = new Color32(255, 255, 255, 255);
            meter.fillAmount = 0;
            countdown.text = "";
        }
    }

    public void Reset()
    {
        charge.Stop();
        clientCharge.Stop();
        ownerCharge.Stop();

        ls_offcd = Time.time;
        chargeStarted = false;
    }
}
