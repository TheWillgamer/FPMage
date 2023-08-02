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
        if (Input.GetButtonDown("Fire3"))
        {
            if (IsOwner && Time.time > dash_offcd)
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
                dash_offcd = Time.time + dash_cd;
            }
        }
        if (IsOwner)
        {
            UpdateUI();
        }
    }

    [ObserversRpc]
    private void playDashSound()
    {
        //m_shootingSound.Play();
    }

    [ServerRpc]
    private void setDashing()
    {
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
    }

    [ServerRpc]
    private void startDashingServer()
    {
        dashStarted = true;
        mv.m_dashing = true;
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

    private void UpdateUI()
    {
        //Wind.fillAmount = 1 - (dash_offcd - Time.time) / dash_cd;
    }
}
