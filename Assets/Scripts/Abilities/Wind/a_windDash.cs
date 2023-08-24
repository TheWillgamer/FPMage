using FishNet.Object;
using FishNet.Managing.Timing;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class a_windDash : NetworkBehaviour, Dash
{
    [SerializeField] private float dashForce;
    [SerializeField] private float dashDur;

    AudioSource m_shootingSound;
    Movement mv;
    Rigidbody rb;

    #region cooldowns
    [SerializeField] private float dash_cd;
    private float dash_offcd;
    private int dashCharges;
    private Coroutine slower;       // makes player slow down
    #endregion

    #region UI
    [SerializeField] Image Wind;
    #endregion

    public override void OnStartClient()
    {
        base.OnStartClient();
    }

    void Start()
    {
        //m_shootingSound = GetComponent<AudioSource>();
        mv = GetComponent<Movement>();
        rb = GetComponent<Rigidbody>();
        dash_offcd = Time.time;
        dashCharges = 2;
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (!mv.disableAB && Input.GetButtonDown("Fire3") && dashCharges > 0)
        {
            dashCharges--;
            if (dashCharges == 1)
            {
                dash_offcd = Time.time + dash_cd;
            }
            startDashingServer();
            mv.dashModifier = dashForce;
            mv.dashDuration = dashDur;
            mv.h_dashing = true;
        }

        if (dashCharges < 2 && Time.time > dash_offcd)
        {
            dashCharges += 1;
            dash_offcd = Time.time + dash_cd;
        }
        UpdateUI();
    }

    [ObserversRpc]
    private void playDashSound()
    {
        //m_shootingSound.Play();
    }

    [ServerRpc]
    private void startDashingServer()
    {
        mv.dashModifier = dashForce;
        mv.dashDuration = dashDur;

        startDash();

        //rb.velocity = Vector3.zero;
        //if (horizontal == 0 && vertical == 0)
        //    rb.AddForce(transform.forward * dashForce, ForceMode.Impulse);
        //else
        //    rb.AddForce((transform.forward * vertical + transform.right * horizontal).normalized * dashForce, ForceMode.Impulse);

        //Invoke("endDash", dashDur);
        //Invoke("endDashServer", dashDur);
    }

    // For any dash effects
    [ObserversRpc]
    private void startDash()
    {
    }

    [ObserversRpc]
    private void endDash()
    {
        //dashparticles.SetActive(false);

        if (IsOwner)
        {
            mv.EndDash();
        }

    }

    private void endDashServer()
    {
        mv.EndDash();
    }

    private void UpdateUI()
    {
        if (dashCharges == 2)
            Wind.color = new Color32(255, 210, 80, 255);
        else if (dashCharges == 1)
        {
            Wind.fillAmount = 1;
            Wind.color = new Color32(255, 255, 255, 255);
        }
        else
        {
            Wind.fillAmount = 1 - (dash_offcd - Time.time) / dash_cd;
            Wind.color = new Color32(255, 255, 255, 255);
        }
            
    }

    public virtual void CancelDash()
    {
        //CancelInvoke();
        //CancelDashClient();
        endDash();
        mv.EndDash();
    }

    [ObserversRpc]
    private void CancelDashClient()
    {
        CancelInvoke();
    }
}