using FishNet.Object;
using FishNet.Managing.Timing;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Net;

public class a_windExplosion : NetworkBehaviour
{
    [SerializeField] private GameObject we;
    [SerializeField] private Transform proj_spawn;
    [SerializeField] private float proj_force;
    [SerializeField] private float minTimeToExplode;
    private Projectile proj;
    private bool chargeStarted;
    AudioSource m_shootingSound;
    private bool explodable;
    private bool letGo;

    private Movement mv;

    #region cooldowns
    //Meteor
    [SerializeField] private float we_cd;
    private float we_offcd;
    #endregion

    #region UI
    [SerializeField] Image Explosion;
    #endregion

    public override void OnStartClient()
    {
        base.OnStartClient();
    }

    void Start()
    {
        m_shootingSound = GetComponent<AudioSource>();
        we_offcd = Time.time;
        mv = GetComponent<Movement>();
        chargeStarted = false;
        letGo = false;
        explodable = false;
    }

    private void Update()
    {
        if (!mv.disableAB && Input.GetButtonDown("Fire2"))
        {
            if (IsOwner && Time.time > we_offcd)
            {
                Vector3 endPoint = proj_spawn.position + proj_spawn.forward * 100f;
                RaycastHit hit;
                if (Physics.Raycast(proj_spawn.position, proj_spawn.forward, out hit, 100f))
                {
                    endPoint = hit.point;
                }
                shootWind(endPoint);
                mv.disableAB = true;
                chargeStarted = true;
                explodable = false;
                Invoke("makeExplodable", minTimeToExplode);
            }
        }

        if (chargeStarted && Input.GetButtonUp("Fire2"))
        {
            letGo = true;
        }

        if (letGo && explodable)
        {
            explodeWind();
            mv.disableAB = false;
            chargeStarted = false;
            we_offcd = Time.time + we_cd;
            letGo = false;
        }
        UpdateUI();
    }

    private void makeExplodable()
    {
        explodable = true;
    }

    [ObserversRpc]
    private void playShootSound()
    {
        m_shootingSound.Play();
    }

    [ServerRpc]
    private void shootWind(Vector3 endPoint)
    {
        playShootSound();
        GameObject spawned = Instantiate(we, proj_spawn.position, transform.rotation);
        //Physics.IgnoreCollision(spawned.GetComponent<Collider>(), GetComponent<Collider>());

        //UnitySceneManager.MoveGameObjectToScene(spawned.gameObject, gameObject.scene);
        base.Spawn(spawned);

        proj = spawned.GetComponent<Projectile>();
        proj.Initialize(base.TimeManager.GetPreciseTick(TickType.Tick), (endPoint - proj_spawn.position).normalized * proj_force, base.Owner.ClientId);
    }

    [ServerRpc]
    private void explodeWind()
    {
        if (proj != null)
        {
            WindExplosion we = (WindExplosion)proj;
            we.explode();
        }
    }

    private void UpdateUI()
    {
        Explosion.fillAmount = 1 - (we_offcd - Time.time) / we_cd;
    }
}
