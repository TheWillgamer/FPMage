using FishNet.Object;
using FishNet.Managing.Timing;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class a_windDash : NetworkBehaviour, Dash
{
    [SerializeField] private float dashTime;
    [SerializeField] private float dashDistance;
    [SerializeField] private float endDashSpeed;

    [SerializeField] private AudioSource dash;
    Movement mv;
    Rigidbody rb;

    private GameObject clientObj;
    [SerializeField] private GameObject dashVisual;
    [SerializeField] private Transform camLoc;

    #region cooldowns
    [SerializeField] private float dash_cd;
    private float dash_offcd;
    private int dashCharges;
    private Coroutine dashing;       // controls dash
    #endregion

    #region UI
    [SerializeField] Image background;
    [SerializeField] Image meter;
    [SerializeField] Image meter2;
    [SerializeField] TMP_Text countdown;
    #endregion

    void Start()
    {
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

            dash.Play();
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            if (horizontal == 0 && vertical == 0)
                vertical = 1;

            Vector3 endPos = transform.position + (transform.forward * vertical + transform.right * horizontal).normalized * dashDistance;
            startDashingServer(endPos);

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
        Invoke("endDashTrail", .04f);
        if (base.IsOwner)
        {
            mv.EndDash();
            if (endVel != Vector3.zero)
                rb.velocity = endVel;
        }
    }

    private void endDashTrail()
    {
        clientObj.transform.parent = null;
    }

    // For any dash effects
    [ObserversRpc]
    private void startDash()
    {
        if (base.IsOwner)
        {
            clientObj = Instantiate(dashVisual, camLoc.transform.position, camLoc.transform.rotation);
            clientObj.transform.parent = camLoc.transform;
        }
        else
        {
            clientObj = Instantiate(dashVisual, transform.position, transform.rotation);
            clientObj.transform.parent = transform;
            dash.Play();
        }
    }

    private void UpdateUI()
    {
        float remainingCD = dash_offcd - Time.time;

        if (dashCharges == 1)
        {
            background.color = new Color32(255, 255, 255, 255);
            meter.fillAmount = 0;
            meter2.fillAmount = 1 - remainingCD / dash_cd;
            countdown.text = "";
        }
        else if (dashCharges == 2)
        {
            background.color = new Color32(255, 255, 255, 255);
            meter.fillAmount = 0;
            meter2.fillAmount = 1;
            countdown.text = "";
        }
        else if (remainingCD > 0)
        {
            background.color = new Color32(100, 100, 100, 255);
            meter.fillAmount = 1 - remainingCD / dash_cd;
            meter2.fillAmount = 0;
            countdown.text = ((int)(remainingCD) + 1).ToString();
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