using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    private int hp;  //keeps track of player health
    public int maxHealth = 100;
    public Transform hpMeter;
    public Image dmgScreen;
    private Rigidbody rb;

    //Audio
    public AudioSource hit;
    public AudioSource dead;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Start is called before the first frame update
    void Start()
    {
        hp = maxHealth;
    }

    // Reduces hp based on the parameter amount
    public void TakeDamage(int amt)
    {
        hp -= amt;
        Debug.Log(hp);
        //UpdateUI();
    }

    private void UpdateUI()
    {
        hpMeter.GetComponent<Image>().fillAmount = (float)hp / maxHealth;
    }

    public void Knockback(float amount, Vector3 direction)
    {
        float startTime = Time.time;

        rb.AddForce(direction * amount + transform.up * amount / 4, ForceMode.Impulse);
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
}
