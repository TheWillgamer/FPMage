using FishNet.Object;
using FishNet.Managing.Timing;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class a_lightningcrash : NetworkBehaviour
{
    [SerializeField] private int damage = 15;
    [SerializeField] private float knockback_amount = 1f;
    [SerializeField] private float knockback_growth = 20f;

    [SerializeField] private Animator animator;

    [SerializeField] private GameObject explosion;
    [SerializeField] private GameObject indicator;
    private Transform indicatorPos;
    [SerializeField] private Transform cam;
    [SerializeField] private float radius;
    [SerializeField] private float height;
    [SerializeField] private float timeTillStrike;

    private Movement mv;
    private Vector3 crashLoc1;
    private Vector3 crashLoc2;
    private bool alternatingCrash;              // So that crashLoc alternates
    private int charges;

    #region cooldowns
    [SerializeField] private float crash_cd;
    private float crash_offcd;
    #endregion

    #region UI
    [SerializeField] Image background;
    [SerializeField] Image meter;
    [SerializeField] Image meter2;
    [SerializeField] TMP_Text countdown;
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        crash_offcd = Time.deltaTime;
        mv = GetComponent<Movement>();
        charges = 2;
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Indicator

        // only detects walls
        int layerMask = 1 << 6;
        RaycastHit hit;
        if (!mv.disableAB && Input.GetButton("Fire2") && charges > 0 && Physics.Raycast(cam.position, cam.forward, out hit, 1000f, layerMask))
        {
            if (indicatorPos == null)
                indicatorPos = Instantiate(indicator, hit.point, transform.rotation).transform;
            else
                indicatorPos.position = hit.point;
        }
        else if (indicatorPos != null)
            Destroy(indicatorPos.gameObject);

        // Actual
        if (!mv.disableAB && Input.GetButtonUp("Fire2") && charges > 0)
        {
            if (Physics.Raycast(cam.position, cam.forward, out hit, 1000f, layerMask))
            {
                charges--;
                if (charges == 1)
                    crash_offcd = Time.time + crash_cd;
                DamageSetup(hit.point);
            }
        }

        if (charges < 2 && Time.time > crash_offcd)
        {
            charges += 1;
            crash_offcd = Time.time + crash_cd;
        }

        UpdateUI();
    }

    [ServerRpc]
    private void DamageSetup(Vector3 pos)
    {
        showCrash(pos);

        if (alternatingCrash)
        {
            crashLoc1 = pos;
            Invoke("DoDamage1", timeTillStrike);
        }
        else
        {
            crashLoc2 = pos;
            Invoke("DoDamage2", timeTillStrike);
        }

        alternatingCrash = !alternatingCrash;
    }

    private void DoDamage1()
    {
        Collider[] hitColliders = Physics.OverlapCapsule(crashLoc1, crashLoc1 + new Vector3(0f, height, 0f), radius);
        foreach (var hit in hitColliders)
        {
            if (hit.transform.tag == "Player")
            {
                // knockback direction
                Vector3 dir = (hit.transform.position - crashLoc1).normalized;

                PlayerHealth ph = hit.transform.gameObject.GetComponent<PlayerHealth>();
                ph.Knockback(dir.normalized, knockback_amount, knockback_growth);
                ph.TakeDamage(damage);
            }
        }
    }
    private void DoDamage2()
    {
        Collider[] hitColliders = Physics.OverlapCapsule(crashLoc2, crashLoc2 + new Vector3(0f, height, 0f), radius);
        foreach (var hit in hitColliders)
        {
            if (hit.transform.tag == "Player")
            {
                // knockback direction
                Vector3 dir = (hit.transform.position - crashLoc2).normalized;

                PlayerHealth ph = hit.transform.gameObject.GetComponent<PlayerHealth>();
                ph.Knockback(dir.normalized, knockback_amount, knockback_growth);
                ph.TakeDamage(damage);
            }
        }
    }

    private void showIndicator(Vector3 pos)
    {
        //GameObject spawned = Instantiate(indicator, pos, transform.rotation);
    }

    [ObserversRpc]
    private void showCrash(Vector3 pos)
    {
        GameObject spawned = Instantiate(explosion, pos, transform.rotation);
        animator.SetTrigger("crash");
    }

    private void UpdateUI()
    {
        float remainingCD = crash_offcd - Time.time;

        if (charges == 1)
        {
            background.color = new Color32(255, 255, 255, 255);
            meter.fillAmount = 0;
            meter2.fillAmount = 1 - remainingCD / crash_cd;
            countdown.text = "";
        }
        else if (charges == 2)
        {
            background.color = new Color32(255, 255, 255, 255);
            meter.fillAmount = 0;
            meter2.fillAmount = 1;
            countdown.text = "";
        }
        else if (remainingCD > 0)
        {
            background.color = new Color32(100, 100, 100, 255);
            meter.fillAmount = 1 - remainingCD / crash_cd;
            meter2.fillAmount = 0;
            countdown.text = ((int)(remainingCD) + 1).ToString();
        }
    }
}