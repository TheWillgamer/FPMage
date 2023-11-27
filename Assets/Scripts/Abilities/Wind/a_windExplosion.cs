using FishNet.Object;
using FishNet.Managing.Timing;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Net;
using TMPro;

public class a_windExplosion : NetworkBehaviour
{
    [SerializeField] private GameObject we;
    [SerializeField] private GameObject wec;
    [SerializeField] private Transform proj_spawn;
    [SerializeField] private float proj_force;
    [SerializeField] private float minTimeToExplode;
    private Projectile proj;
    private bool chargeStarted;
    private bool explodable;
    private bool letGo;

    private Movement mv;
    private TimeManager tm;
    private GameObject clientObj;

    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource fire;

    #region cooldowns
    //Meteor
    [SerializeField] private float we_cd;
    private float we_offcd;
    #endregion

    #region UI
    [SerializeField] Image background;
    [SerializeField] Image meter;
    [SerializeField] TMP_Text countdown;
    #endregion

    public override void OnStartClient()
    {
        base.OnStartClient();
    }

    void Start()
    {
        we_offcd = Time.time;
        mv = GetComponent<Movement>();
        chargeStarted = false;
        letGo = false;
        explodable = false;
        tm = GameObject.FindWithTag("NetworkManager").GetComponent<TimeManager>();
    }

    private void Update()
    {
        if (!mv.disableAB && Input.GetButtonDown("Fire2"))
        {
            if (IsOwner && Time.time > we_offcd)
            {
                fire.Play();

                clientObj = Instantiate(wec, proj_spawn.position, proj_spawn.rotation);
                MoveProjectileClient proj = clientObj.GetComponent<MoveProjectileClient>();
                proj.Initialize(proj_spawn.forward * proj_force, Mathf.Min(180f, (float)tm.RoundTripTime) / 1000f);

                shootWind(base.TimeManager.GetPreciseTick(TickType.Tick), proj_spawn.position, proj_spawn.rotation);
                mv.disableAB = true;
                chargeStarted = true;
                explodable = false;
                Invoke("makeExplodable", minTimeToExplode);
            }
        }

        if (chargeStarted && Input.GetButtonUp("Fire2"))
        {
            letGo = true;
        }

        if (letGo && explodable)
        {
            explodeWind();
            mv.disableAB = false;
            chargeStarted = false;
            we_offcd = Time.time + we_cd;
            letGo = false;
        }
        UpdateUI();
    }

    private void makeExplodable()
    {
        explodable = true;
    }

    [ObserversRpc]
    private void playShootSound()
    {
        animator.SetBool("windBall", true);
        animator.SetTrigger("windBallStart");
        if (!IsOwner)
            fire.Play();
        else
            Destroy(clientObj);
    }

    [ObserversRpc]
    private void stopAnimation()
    {
        animator.SetBool("windBall", false);
    }

    [ServerRpc]
    private void shootWind(PreciseTick pt, Vector3 startLoc, Quaternion startRot)
    {
        playShootSound();
        GameObject spawned = Instantiate(we, startLoc, startRot);
        base.Spawn(spawned);

        proj = spawned.GetComponent<Projectile>();
        proj.Initialize(pt, startRot * Vector3.forward * proj_force, base.Owner.ClientId);
    }

    [ServerRpc]
    private void explodeWind()
    {
        if (proj != null)
        {
            WindExplosion we = (WindExplosion)proj;
            we.explode();
        }
        stopAnimation();
    }

    private void UpdateUI()
    {
        float remainingCD = we_offcd - Time.time;

        if (chargeStarted)
        {
            background.color = new Color32(255, 190, 0, 255);
        }
        else if (remainingCD > 0)
        {
            background.color = new Color32(100, 100, 100, 255);
            meter.fillAmount = 1 - remainingCD / we_cd;
            countdown.text = ((int)(remainingCD) + 1).ToString();
        }
        else
        {
            background.color = new Color32(255, 255, 255, 255);
            meter.fillAmount = 0;
            countdown.text = "";
        }
    }
}
