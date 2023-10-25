using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using TMPro;

public class GameplayManager : MonoBehaviour
{
    public event Action<NetworkObject> OnSpawned;

    public Transform[] StartingSpawns = new Transform[0];       // where they spawn
    private int _nextSpawn;
    private int playerCounter;
    [SerializeField] private NetworkObject[] playerPrefabs;     // what is spawned

    public TMP_Text playerHp;
    public TMP_Text oppoHp;
    public GameObject[] playerLives;
    public GameObject[] oppoLives;
    public TMP_Text playerName;
    public TMP_Text oppoName;

    private NetworkManager _networkManager;

    private void Start()
    {
        InitializeOnce();
    }

    private void InitializeOnce()
    {
        _networkManager = InstanceFinder.NetworkManager;
        playerCounter = 0;
        if (_networkManager == null)
        {
            Debug.LogWarning($"PlayerSpawner on {gameObject.name} cannot work as NetworkManager wasn't found on this object or within parent objects.");
            return;
        }
    }

    public void SpawnWizard(NetworkConnection conn, int type)
    {
        Vector3 position;
        Quaternion rotation;
        SetSpawn(playerPrefabs[0].transform, out position, out rotation);

        NetworkObject nob = Instantiate(playerPrefabs[type], position, rotation);
        _networkManager.ServerManager.Spawn(nob, conn);
        _networkManager.SceneManager.AddOwnerToDefaultScene(nob);

        OnSpawned?.Invoke(nob);
    }

    public void SetLives(bool owner, int lives)
    {
        if (owner)
            playerLives[lives].SetActive(false);
        else
            oppoLives[lives].SetActive(false);
    }

    private void SetSpawn(Transform prefab, out Vector3 pos, out Quaternion rot)
    {
        //No spawns specified.
        if (StartingSpawns.Length == 0)
        {
            SetSpawnUsingPrefab(prefab, out pos, out rot);
            return;
        }

        Transform result = StartingSpawns[_nextSpawn];
        if (result == null)
        {
            SetSpawnUsingPrefab(prefab, out pos, out rot);
        }
        else
        {
            pos = result.position;
            rot = result.rotation;
        }

        //Increase next spawn and reset if needed.
        _nextSpawn++;
        if (_nextSpawn >= StartingSpawns.Length)
            _nextSpawn = 0;
    }

    private void SetSpawnUsingPrefab(Transform prefab, out Vector3 pos, out Quaternion rot)
    {
        pos = prefab.position;
        rot = prefab.rotation;
    }

    // Kill player if touching the death zone
    private void OnTriggerStay(Collider other)
    {
        if (other.tag == "Player")
        {
            other.GetComponent<PlayerHealth>().Die();
        }
    }
}
