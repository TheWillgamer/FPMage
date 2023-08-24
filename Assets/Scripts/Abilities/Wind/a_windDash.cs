using FishNet.Object;
using FishNet.Managing.Timing;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class a_windDash : NetworkBehaviour, Dash
{
    [SerializeField] private float dashTime;
    [SerializeField] private float dashDistance;
    [SerializeField] private float endDashSpeed;

    AudioSource m_shootingSound;
    Movement mv;
    Rigidbody rb;

    #region cooldowns
    [SerializeField] private float dash_cd;
    private float dash_offcd;
    private int dashCharges;
    private Coroutine dashing;       // controls dash
    #endregion

    #region UI
    [SerializeField] Image Wind;
    #endregion

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

            //float horizontal = Input.GetAxisRaw("Horizontal");
            //float vertical = Input.GetAxisRaw("Vertical");

            //if (horizontal == 0 && vertical == 0)
            //    vertical = 1;

            //Vector3 endPos = transform.position + (transform.forward * vertical + transform.right * horizontal).normalized * dashDistance;
            //startDashingServer(endPos);

            if (!IsServer)
            {
                mv.gravity = false;
            }
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
    private void startDashingServer(Vector3 endPos)
    {
        rb.velocity = Vector3.zero;
        mv.gravity = false;
        dashing = StartCoroutine(DashTo(endPos));

        startDash();
    }

    private IEnumerator DashTo(Vector3 endPos)
    {
        float startingTime = Time.time;
        Vector3 startingPos = transform.position;
        while (Time.time - startingTime <= dashTime)
        {
            transform.position = Vector3.Lerp(startingPos, endPos, (Time.time - startingTime) / dashTime);
            yield return null;
        }
        mv.EndDash();
        Vector3 endVelocity = (endPos - startingPos).normalized * endDashSpeed;
        endDash(endVelocity);
        rb.velocity = endVelocity;
    }

    // For any dash effects
    [ObserversRpc]
    private void endDash(Vector3 endVel)
    {
        if (base.IsOwner)
        {
            mv.EndDash();
            if (endVel != Vector3.zero)
                rb.velocity = endVel;
        }
    }

    // For any dash effects
    [ObserversRpc]
    private void startDash()
    {
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
        if (dashing != null)
            StopCoroutine(dashing);

        endDash(Vector3.zero);
        mv.EndDash();
    }
}