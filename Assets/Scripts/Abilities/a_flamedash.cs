using FishNet.Object;
using FishNet.Managing.Timing;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
//using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public class a_flamedash : NetworkBehaviour
{
    [SerializeField] private float dashForce;
    [SerializeField] private float dashDur;
    [SerializeField] private float dashDelay;
    [SerializeField] private GameObject charge;
    [SerializeField] private GameObject dashparticles;
    [SerializeField] private Transform cam;

    AudioSource m_shootingSound;
    Movement mv;
    Rigidbody rb;

    #region cooldowns
    [SerializeField] private float dash_cd;
    private float dash_offcd;
    private bool dashStarted;
    #endregion

    #region UI
    //[SerializeField] GameObject cdRepresentation;
    //[SerializeField] Image Wind;
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
        dash_offcd = Time.deltaTime;
        dashStarted = false;
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetButtonDown("Fire3") && Time.time > dash_offcd)
        {
            setDashing();
            Invoke("startDashingServer", dashDelay);
            if (!IsServer)
            {
                mv.disableMV = true;
                mv.gravity = false;
                mv.dashModifier = dashForce;
                mv.dashDuration = dashDur;
                dashStarted = false;
                StartCoroutine(SlowDown());
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

    [ServerRpc]
    private void setDashing()
    {
        startCharge();
        mv.disableMV = true;
        mv.gravity = false;
        mv.dashModifier = dashForce;
        mv.dashDuration = dashDur;
        dashStarted = false;
        StartCoroutine(SlowDown());
    }

    private void startDashing()
    {
        dashStarted = true;
        mv.m_dashing = true;
        dash_offcd = Time.time + dash_cd;
    }

    [ServerRpc]
    private void startDashingServer()
    {
        startDash();
        dashStarted = true;
        mv.m_dashing = true;
        dash_offcd = Time.time + dash_cd;
        Invoke("endDash", dashDur);
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
    }

    private void UpdateUI()
    {
        //Wind.fillAmount = 1 - (dash_offcd - Time.time) / dash_cd;
    }
}
