using FishNet.Object;
using FishNet.Managing.Timing;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class a_flamedash : NetworkBehaviour, Dash, Ability
{
    [SerializeField] private float dashForce;
    [SerializeField] private float dashDur;
    [SerializeField] private float dashDelay;
    [SerializeField] private GameObject charge;
    [SerializeField] private GameObject dashparticles;
    [SerializeField] private GameObject dashparticlesOwner;
    [SerializeField] private Transform cam;
    [SerializeField] private GameObject hitbox;

    [SerializeField] private AudioSource charg;
    [SerializeField] private AudioSource fire;
    Movement mv;
    Rigidbody rb;

    #region cooldowns
    [SerializeField] private float dash_cd;
    private float dash_offcd;
    private bool dashStarted;       // only resets cd when true
    private Coroutine slower;       // makes player slow down
    private bool dashPressed;
    #endregion

    #region UI
    //[SerializeField] GameObject cdRepresentation;
    [SerializeField] Image background;
    [SerializeField] Image meter;
    [SerializeField] TMP_Text countdown;
    #endregion

    public override void OnStartClient()
    {
        base.OnStartClient();
        //if (IsOwner)
            //cdRepresentation.SetActive(true);
    }

    void Start()
    {
        mv = GetComponent<Movement>();
        rb = GetComponent<Rigidbody>();
        dash_offcd = Time.time;
        dashStarted = true;
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (!mv.disableAB && Input.GetButtonDown("Fire3") && Time.time > dash_offcd)
        {
            setDashing();
            dashPressed = true;
            Invoke("startDashingServer", dashDelay);

            if (!IsServer)
            {
                mv.disableAB = true;
                mv.disableMV = true;
                mv.gravity = false;
                mv.dashing = true;
                dashStarted = false;
                slower = StartCoroutine(SlowDown());
                Invoke("startDashing", dashDelay);
            }
        }
        UpdateUI();
    }

    // Disables movement and slows down player to set up for a dash
    [ServerRpc]
    private void setDashing()
    {
        startCharge();
        mv.disableAB = true;
        mv.disableMV = true;
        mv.gravity = false;
        mv.dashing = true;
        dashStarted = false;
        slower = StartCoroutine(SlowDown());
    }

    private void startDashing()
    {
        dashStarted = true;
        dashPressed = false;
        dash_offcd = Time.time + dash_cd;
    }

    [ServerRpc]
    private void startDashingServer()
    {
        if (dashStarted)
            return;
        startDash();
        dashPressed = false;
        hitbox.transform.rotation = Quaternion.LookRotation(cam.forward);
        hitbox.SetActive(true);
        dashStarted = true;
        //mv.m_dashing = true;
        rb.AddForce(cam.forward * dashForce, ForceMode.Impulse);
        dash_offcd = Time.time + dash_cd;
        Invoke("endDash", dashDur);
        Invoke("endDashServer", dashDur);
    }

    private IEnumerator SlowDown()
    {
        Vector3 startingVelocity = rb.velocity;
        float startingTime = Time.time;
        while (dashDelay - Time.time + startingTime > 0f && !dashStarted)
        {
            rb.velocity = (dashDelay - Time.time + startingTime) / dashDelay * startingVelocity;
            yield return null;
        }
    }

    [ObserversRpc]
    private void startCharge()
    {
        charge.SetActive(true);
        charg.Play();
    }

    [ObserversRpc]
    private void startDash()
    {
        charge.SetActive(false);
        charg.Stop();
        fire.Play();
        if (IsOwner)
        {
            dashparticlesOwner.SetActive(true);
            dashparticlesOwner.transform.rotation = Quaternion.LookRotation(cam.forward);
        }
        else
        {
            dashparticles.SetActive(true);
            dashparticles.transform.rotation = Quaternion.LookRotation(cam.forward);
        }
    }

    [ObserversRpc]
    private void endDash()
    {
        dashparticles.SetActive(false);
        charge.SetActive(false);
        if (IsOwner)
        {
            dashparticlesOwner.SetActive(false);
            mv.EndDash();
        }
            
    }

    private void endDashServer()
    {
        mv.EndDash();
        hitbox.SetActive(false);
    }

    private void UpdateUI()
    {
        float remainingCD = dash_offcd - Time.time;

        if (dashPressed)
        {
            background.color = new Color32(255, 190, 0, 255);
        }
        else if (remainingCD > 0)
        {
            background.color = new Color32(100, 100, 100, 255);
            meter.fillAmount = 1 - (dash_offcd - Time.time) / dash_cd;
            countdown.text = ((int)(remainingCD) + 1).ToString();
        }
        else
        {
            background.color = new Color32(255, 255, 255, 255);
            meter.fillAmount = 0;
            countdown.text = "";
        }
    }

    public virtual void CancelDash()
    {
        if (slower != null)
            StopCoroutine(slower);
        if (IsOwner && !dashStarted)
            dash_offcd = Time.time + dash_cd;

        dashStarted = true;
        dashPressed = false;
        CancelInvoke();
        CancelDashClient();
        endDash();
        mv.EndDash();
        hitbox.SetActive(false);
    }

    [ObserversRpc]
    private void CancelDashClient()
    {
        charg.Stop();
        CancelInvoke();
        if (!IsOwner) return;

        if (!dashStarted)
            dash_offcd = Time.time + dash_cd;

        dashStarted = true;
        dashPressed = false;
        if (slower != null)
            StopCoroutine(slower);
    }

    public void Reset()
    {
        dash_offcd = Time.time;
    }
}
