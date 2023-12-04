using FishNet.Object;
using FishNet.Managing.Timing;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class a_windBlast : NetworkBehaviour, Ability
{
    [SerializeField] private float dashSpeed;
    [SerializeField] private float directionModifier;
    [SerializeField] private int damage = 15;
    [SerializeField] private float knockback_amount = 1f;
    [SerializeField] private float knockback_growth = 20f;
    [SerializeField] private float blastDelay = 0.2f;

    [SerializeField] private Transform proj_spawn;
    [SerializeField] private Transform cam;

    // shows the blast on screen
    [SerializeField] private GameObject ownerBlastGM;
    private ParticleSystem ownerBlast;
    [SerializeField] private GameObject clientBlastGM;
    private ParticleSystem clientBlast;

    private Movement mv;
    private Rigidbody rb;

    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource blast;

    #region cooldowns
    [SerializeField] private float blast_cd;
    private float blast_offcd;
    #endregion

    #region UI
    [SerializeField] Image background;
    [SerializeField] Image meter;
    [SerializeField] TMP_Text countdown;
    private bool chargeStarted;
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        blast_offcd = Time.deltaTime;
        ownerBlast = ownerBlastGM.GetComponent<ParticleSystem>();
        clientBlast = clientBlastGM.GetComponent<ParticleSystem>();
        mv = GetComponent<Movement>();
        rb = GetComponent<Rigidbody>();
        chargeStarted = false;
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (!mv.disableAB && Input.GetButtonDown("Fire4") && Time.time > blast_offcd)
        {
            Invoke("DoDamage", blastDelay);
            ShowBlastAnimServer();
            mv.disableAB = true;
            chargeStarted = true;
        }
        UpdateUI();
    }

    private void ShowBlastAnimServer()
    {
        ShowBlastAnim();
    }

    [ObserversRpc]
    private void ShowBlastAnim()
    {
        animator.SetTrigger("windBlast");
    }

    private void DoDamage()
    {
        mv.disableAB = false;
        mv.dashing = true;
        DoDamageServer(transform.position, proj_spawn.rotation, base.Owner.ClientId);

    }

    [ServerRpc]
    private void DoDamageServer(Vector3 pos, Quaternion rot, int owner)
    {
        showBlast();

        mv.dashing = true;
        StartCoroutine(turnOffDashing());

        // Movement portion
        Vector3 blastA = Vector3.Project(cam.forward, rb.velocity);      // Parallel direction
        Vector3 blastE = cam.forward - blastA;
        float modifier = 1f + rb.velocity.magnitude / directionModifier;

        if (rb.velocity.magnitude > 1f)
        {
            if (Vector3.Dot(rb.velocity, blastA) > 0)
                rb.AddForce(-blastE * dashSpeed - blastA * dashSpeed * modifier, ForceMode.Impulse);       // blast in direction of movement
            else
                rb.AddForce(-blastE * dashSpeed - blastA * dashSpeed / modifier, ForceMode.Impulse);
        }
        else
            rb.AddForce(-cam.forward * dashSpeed, ForceMode.Impulse);       // player is still


        // Actually doing damage
        RaycastHit[] hitColliders = Physics.SphereCastAll(pos, 2f, rot * Vector3.forward, 8f);
        foreach (var hit in hitColliders)
        {
            
            if (hit.transform.tag == "Player" && hit.transform.parent.GetComponent<NetworkObject>().Owner.ClientId != owner)
            {
                PlayerHealth ph = hit.transform.gameObject.GetComponent<PlayerHealth>();
                ph.Knockback(rot * Vector3.forward, knockback_amount, knockback_growth);
                ph.TakeDamage(damage);
            }
        }
    }

    private IEnumerator turnOffDashing()
    {
        yield return null;
        mv.dashing = false;
        turnOffDashingClient();
    }

    [ObserversRpc]
    private void turnOffDashingClient()
    {
        if (IsOwner)
            mv.dashing = false;
    }

    [ObserversRpc]
    private void showBlast()
    {
        blast.Play();
        if (base.IsOwner && chargeStarted)
        {
            ownerBlast.Play();
            chargeStarted = false;
            blast_offcd = Time.time + blast_cd;
        }
        else
        {
            clientBlastGM.transform.position = proj_spawn.position;
            clientBlastGM.transform.rotation = proj_spawn.rotation;
            clientBlast.Play();
        }
    }

    private void UpdateUI()
    {
        float remainingCD = blast_offcd - Time.time;

        if (chargeStarted)
        {
            background.color = new Color32(255, 190, 0, 255);
        }
        else if (remainingCD > 0)
        {
            background.color = new Color32(100, 100, 100, 255);
            meter.fillAmount = 1 - remainingCD / blast_cd;
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
        blast_offcd = Time.time;
        chargeStarted = false;
    }
}
