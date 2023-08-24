using FishNet.Object;
using FishNet.Managing.Timing;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
//using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public class a_flamedash : NetworkBehaviour, Dash
{
    [SerializeField] private float dashForce;
    [SerializeField] private float dashDur;
    [SerializeField] private float dashDelay;
    [SerializeField] private GameObject charge;
    [SerializeField] private GameObject dashparticles;
    [SerializeField] private Transform cam;
    [SerializeField] private GameObject hitbox;

    AudioSource m_shootingSound;
    Movement mv;
    Rigidbody rb;

    #region cooldowns
    [SerializeField] private float dash_cd;
    private float dash_offcd;
    private bool dashStarted;       // only resets cd when true
    private Coroutine slower;       // makes player slow down
    #endregion

    #region UI
    //[SerializeField] GameObject cdRepresentation;
    [SerializeField] Image Wind;
    #endregion

    public override void OnStartClient()
    {
        base.OnStartClient();
        //if (IsOwner)
            //cdRepresentation.SetActive(true);
    }

    void Start()
    {
        //m_shootingSound = GetComponent<AudioSource>();
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

    [ObserversRpc]
    private void playDashSound()
    {
        //m_shootingSound.Play();
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
        //mv.m_dashing = true;
        dash_offcd = Time.time + dash_cd;
    }

    [ServerRpc]
    private void startDashingServer()
    {
        if (dashStarted)
            return;
        startDash();
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
    }

    [ObserversRpc]
    private void startDash()
    {
        charge.SetActive(false);
        dashparticles.SetActive(true);
        dashparticles.transform.rotation = Quaternion.LookRotation(cam.forward);
    }

    [ObserversRpc]
    private void endDash()
    {
        dashparticles.SetActive(false);
        charge.SetActive(false);
        if (IsOwner)
        {
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
        Wind.fillAmount = 1 - (dash_offcd - Time.time) / dash_cd;
    }

    public virtual void CancelDash()
    {
        if (slower != null)
            StopCoroutine(slower);
        if (IsOwner && !dashStarted)
            dash_offcd = Time.time + dash_cd;

        dashStarted = true;
        CancelInvoke();
        CancelDashClient();
        endDash();
        mv.EndDash();
        hitbox.SetActive(false);
    }

    [ObserversRpc]
    private void CancelDashClient()
    {
        CancelInvoke();
        if (!IsOwner) return;

        if (!dashStarted)
            dash_offcd = Time.time + dash_cd;

        dashStarted = true;
        if (slower != null)
            StopCoroutine(slower);
    }
}
