using FishNet.Object;
using FishNet.Managing.Timing;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class a_lightningblink : NetworkBehaviour, Ability
{
    [SerializeField] private float maxDistance;
    [SerializeField] private float endVelocityMultiplier;

    private Movement mv;
    private Rigidbody rb;
    private PlayerHealth ph;

    [SerializeField] private AudioSource fire;
    [SerializeField] private GameObject lightningBlink;

    #region cooldowns
    [SerializeField] private float blink_cd;
    private float blink_offcd;
    private GameObject spawned;     // effect that is spawned
    #endregion

    #region UI
    [SerializeField] Image background;
    [SerializeField] Image meter;
    [SerializeField] TMP_Text countdown;
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        blink_offcd = Time.deltaTime;
        mv = GetComponent<Movement>();
        rb = GetComponent<Rigidbody>();
        ph = GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (ph.dashable && Input.GetButtonDown("Fire3") && Time.time > blink_offcd)
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
            playSound(transform.position, .4f * (hit.point.y - transform.position.y - 1f) / maxDistance);
        }
        else
            playSound(transform.position);

        rb.velocity = (endPoint - transform.position).normalized * endVelocityMultiplier;
        transform.position = endPoint;
    }

    [ObserversRpc]
    private void playSound(Vector3 pos, float length = 1f)
    {
        fire.Play();
        spawned = Instantiate(lightningBlink, pos, transform.rotation);
        Invoke("DestroyEffect", length);
    }

    private void DestroyEffect()
    {
        Destroy(spawned);
    }

    private void UpdateUI()
    {
        float remainingCD = blink_offcd - Time.time;

        if (remainingCD > 0)
        {
            background.color = new Color32(100, 100, 100, 255);
            meter.fillAmount = 1 - (blink_offcd - Time.time) / blink_cd;
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
        blink_offcd = Time.time;
    }
}
