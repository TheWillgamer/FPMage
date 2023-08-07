using FishNet.Object;
using FishNet.Managing.Timing;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
//using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public class a_meteor : NetworkBehaviour
{
    [SerializeField] private GameObject mt;
    [SerializeField] private Transform proj_spawn;
    [SerializeField] private float proj_force;
    [SerializeField] private float chargeTime;
    [SerializeField] private ParticleSystem ownerMeteor;
    [SerializeField] private GameObject ownerMeteorGM;
    AudioSource m_shootingSound;

    #region cooldowns
    //Meteor
    [SerializeField] private float mt_cd;
    private float mt_offcd;
    private bool chargeStarted;
    private float chargeReady;
    #endregion

    #region UI
    [SerializeField] GameObject cdRepresentation;
    [SerializeField] Image Meteor;
    #endregion

    public override void OnStartClient()
    {
        base.OnStartClient();
        //if (IsOwner)
            //cdRepresentation.SetActive(true);
    }

    void Start()
    {
        m_shootingSound = GetComponent<AudioSource>();
        mt_offcd = Time.deltaTime;
        chargeStarted = false;
    }

    private void Update()
    {
        if (Input.GetButtonDown("Fire2"))
        {
            if (IsOwner && Time.time > mt_offcd)
            {
                chargeStarted = true;
                ownerMeteorGM.SetActive(true);
                ownerMeteor.Play();
                chargeReady = Time.time + chargeTime;
            }
        }
        if (chargeStarted && Time.time > chargeReady)
        {
            shootMeteor();
            chargeStarted = false;
            ownerMeteorGM.SetActive(false);
            mt_offcd = Time.time + mt_cd;
        }
        UpdateUI();
    }

    [ObserversRpc]
    private void playShootSound()
    {
        m_shootingSound.Play();
    }

    [ServerRpc]
    private void shootMeteor()
    {
        playShootSound();
        GameObject spawned = Instantiate(mt, proj_spawn.position, proj_spawn.rotation);
        //Physics.IgnoreCollision(spawned.GetComponent<Collider>(), GetComponent<Collider>());

        //UnitySceneManager.MoveGameObjectToScene(spawned.gameObject, gameObject.scene);
        base.Spawn(spawned);

        Projectile proj = spawned.GetComponent<Projectile>();
        proj.Initialize(base.TimeManager.GetPreciseTick(TickType.Tick), proj_spawn.forward * proj_force, base.Owner.ClientId);
    }

    private void UpdateUI()
    {
    }
}
