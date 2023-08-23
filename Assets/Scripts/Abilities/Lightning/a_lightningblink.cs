using FishNet.Object;
using FishNet.Managing.Timing;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class a_lightningblink : NetworkBehaviour
{
    [SerializeField] private float maxDistance;
    [SerializeField] private float endVelocityMultiplier;

    [SerializeField] Image Blink;

    private Movement mv;
    private Rigidbody rb;

    #region cooldowns
    [SerializeField] private float blink_cd;
    private float blink_offcd;
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        blink_offcd = Time.deltaTime;
        mv = GetComponent<Movement>();
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetButtonUp("Fire3") && Time.time > blink_offcd)
        {
            Teleport();
            blink_offcd = Time.time + blink_cd;
        }
        UpdateUI();
    }

    [ServerRpc]
    private void Teleport()
    {
        // only detects walls
        int layerMask = 1 << 6;

        Vector3 endPoint = transform.position + Vector3.up * maxDistance;
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.up, out hit, maxDistance, layerMask))
        {
            endPoint = hit.point + hit.normal;
        }
        
        rb.velocity = (endPoint - transform.position).normalized * endVelocityMultiplier;
        transform.position = endPoint;
    }

    private void UpdateUI()
    {
        Blink.fillAmount = 1 - (blink_offcd - Time.time) / blink_cd;
    }
}