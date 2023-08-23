using FishNet.Object;
using FishNet.Managing.Timing;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class a_lightningcrash : NetworkBehaviour
{
    [SerializeField] private int damage = 15;
    [SerializeField] private float knockback_amount = 1f;
    [SerializeField] private float knockback_growth = 20f;

    [SerializeField] private GameObject explosion;
    [SerializeField] private GameObject indicator;
    [SerializeField] private Transform cam;
    [SerializeField] private float radius;
    [SerializeField] private float timeTillStrike;
    [SerializeField] private float timeToSpam;
    [SerializeField] Image Crash;

    private Movement mv;
    private Vector3 crashLoc1;
    private Vector3 crashLoc2;
    private Vector3 crashLoc3;
    private int charges;

    #region cooldowns
    [SerializeField] private float crash_cd;
    private float crash_offcd;
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        crash_offcd = Time.deltaTime;
        mv = GetComponent<Movement>();
        charges = 0;
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (!mv.disableAB && Input.GetButtonUp("Fire2") && charges < 3 && Time.time > crash_offcd)
        {
            // only detects walls
            int layerMask = 1 << 6;

            RaycastHit hit;
            if (Physics.Raycast(cam.position, cam.forward, out hit, 1000f, layerMask))
            {
                CancelInvoke("StartCD");
                charges++;
                DamageSetup(hit.point, charges);
                Invoke("StartCD", timeToSpam);
            }
        }
        UpdateUI();
    }

    private void StartCD()
    {
        crash_offcd = Time.time + crash_cd;
        charges = 0;
    }

    [ServerRpc]
    private void DamageSetup(Vector3 pos, int crashNum)
    {
        showIndicator(pos);

        switch (crashNum)
        {
            case 1:
                crashLoc1 = pos;
                Invoke("DoDamage1", timeTillStrike);
                break;
            case 2:
                crashLoc2 = pos;
                Invoke("DoDamage2", timeTillStrike);
                break;
            case 3:
                crashLoc3 = pos;
                Invoke("DoDamage3", timeTillStrike);
                break;
            default:
                print("Not valid crash number.");
                break;
        }
    }

    private void DoDamage1()
    {
        showCrash(crashLoc1);

        Collider[] hitColliders = Physics.OverlapSphere(crashLoc1, radius);
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
        showCrash(crashLoc2);

        Collider[] hitColliders = Physics.OverlapSphere(crashLoc2, radius);
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
    private void DoDamage3()
    {
        showCrash(crashLoc3);

        Collider[] hitColliders = Physics.OverlapSphere(crashLoc3, radius);
        foreach (var hit in hitColliders)
        {
            if (hit.transform.tag == "Player")
            {
                // knockback direction
                Vector3 dir = (hit.transform.position - crashLoc3).normalized;

                PlayerHealth ph = hit.transform.gameObject.GetComponent<PlayerHealth>();
                ph.Knockback(dir.normalized, knockback_amount, knockback_growth);
                ph.TakeDamage(damage);
            }
        }
    }

    [ObserversRpc]
    private void showIndicator(Vector3 pos)
    {
        GameObject spawned = Instantiate(indicator, pos, transform.rotation);
    }

    [ObserversRpc]
    private void showCrash(Vector3 pos)
    {
        GameObject spawned = Instantiate(explosion, pos, transform.rotation);
    }

    private void UpdateUI()
    {
        Crash.fillAmount = 1 - (crash_offcd - Time.time) / crash_cd;
    }
}