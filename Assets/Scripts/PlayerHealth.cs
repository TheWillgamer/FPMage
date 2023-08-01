using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealth : NetworkBehaviour
{
    [SyncVar] public int hp;  //keeps track of player health
    public TMP_Text hpPercentage;
    public Image dmgScreen;
    private Rigidbody rb;
    private Movement mv;

    //Audio
    public AudioSource hit;
    public AudioSource dead;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        mv = GetComponent<Movement>();
    }

    // Start is called before the first frame update
    void Start()
    {
        hp = 0;
    }

    // Reduces hp based on the parameter amount
    public void TakeDamage(int amt)
    {
        int oldHp = hp;
        hp += amt;

        if (base.IsServer)
        {
            ObserversTakeDamage(amt, oldHp);
            if (base.IsOwner)
                UpdateUI();
        }
    }

    // Knocks back the player in a given direction: kb_growth determines how much percentage determines the knockback amount
    public void Knockback(Vector3 direction, float base_kb, float kb_growth)
    {
        //mv.disableCM = true;
        rb.AddForce(direction * (((hp / kb_growth) + 1) * base_kb), ForceMode.Impulse);
        Invoke("EnableCM", .2f);
    }

    [ObserversRpc]
    private void ObserversTakeDamage(int value, int priorHealth)
    {
        //Prevents endless loop
        if (base.IsServer)
            return;

        /* Set current health to prior health so that
         * in case client somehow magically got out of sync
         * this will fix it before trying to remove health. */
        hp = priorHealth;

        TakeDamage(value);
        if (base.IsOwner)
        {
            UpdateUI();
            StartCoroutine(Fade());
        }
    }

    IEnumerator Fade()
    {
        Color c = dmgScreen.color;
        for (float alpha = .2f; alpha >= 0; alpha -= 0.001f)
        {
            c.a = alpha;
            dmgScreen.color = c;
            yield return null;
        }
    }

    private void UpdateUI()
    {
        hpPercentage.text = hp.ToString() + "%";
    }

    private void EnableCM()
    {
        //mv.disableCM = false;
    }
}
