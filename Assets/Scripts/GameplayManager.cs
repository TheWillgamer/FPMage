using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using FishNet;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using TMPro;
using System.Collections;

public class GameplayManager : MonoBehaviour
{
    public event Action<NetworkObject> OnSpawned;

    // Connects the connection with the player number index and type of wizard that was spawned
    Dictionary<NetworkConnection, (int, int)> playerList = new Dictionary<NetworkConnection, (int, int)>();

    // Connects the connection with the position in the health ui
    Dictionary<NetworkConnection, int> uipos = new Dictionary<NetworkConnection, int>();

    public Transform[] StartingSpawns = new Transform[0];       // where they spawn
    public Transform finalCamLoc;
    public Transform winningPlayerLoc;
    private int _nextSpawn;
    private int playerCounter;
    [SerializeField] private NetworkObject[] playerPrefabs;     // what is spawned
    [SerializeField] private GameObject[] characterModels;      // for the end game screen
    private Movement[] pm = new Movement[4];                    // playerMovement
    private bool gameEnded;                                     // to prevent 2 winners

    // UI stuff
    public Image[] casterMaster;
    public TMP_Text[] hpPercentage;
    public GameObject[] lifeCounter;
    public TMP_Text[] displayName;

    public TMP_Text playerHp;
    public TMP_Text oppoHp;
    public GameObject[] playerLives;
    public GameObject[] oppoLives;
    public TMP_Text playerName;
    public TMP_Text oppoName;

    public GameObject gameSet;
    public Image blackScreen;

    private NetworkManager _networkManager;
    public int numPlayers = 1;

    private void Start()
    {
        InitializeOnce();
    }

    private void InitializeOnce()
    {
        _networkManager = InstanceFinder.NetworkManager;
        playerCounter = 0;
        gameEnded = false;
        if (_networkManager == null)
        {
            Debug.LogWarning($"PlayerSpawner on {gameObject.name} cannot work as NetworkManager wasn't found on this object or within parent objects.");
            return;
        }
    }

    // Starts the game
    private void StartGame()
    {
        for (int i = 0; i < playerCounter; i++)
        {
            pm[i].CountdownStart();
        }
    }

    // Ends the game
    public void EndGame(bool owner)
    {
        if (gameEnded)
            return;

        gameEnded = true;
        gameSet.SetActive(true);
        StartCoroutine(FadeOut());
        Instantiate(characterModels[0], winningPlayerLoc.position, winningPlayerLoc.rotation);
    }

    IEnumerator FadeOut()
    {
        Color c = blackScreen.color;
        yield return new WaitForSeconds(2);

        for (float alpha = 0f; alpha < 1f; alpha += 0.01f)
        {
            c.a = alpha;
            blackScreen.color = c;
            yield return null;
        }
        SwitchToFinalCam();
    }

    // End Game Sequence to show the winners
    private void SwitchToFinalCam()
    {
        gameSet.SetActive(false);

        Transform cam = GameObject.FindWithTag("MainCamera").transform;
        cam.parent = null;
        cam.position = finalCamLoc.position;
        cam.rotation = finalCamLoc.rotation;

        Camera c = cam.GetComponent<Camera>();
        c.clearFlags = CameraClearFlags.Skybox;
        c.cullingMask = -1;

        StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
        Color c = blackScreen.color;
        yield return new WaitForSeconds(2);

        for (float alpha = 1f; alpha > 0f; alpha -= 0.01f)
        {
            c.a = alpha;
            blackScreen.color = c;
            yield return null;
        }
    }

    public void SpawnWizard(NetworkConnection conn, int type)
    {
        Vector3 position;
        Quaternion rotation;
        SetSpawn(playerPrefabs[0].transform, out position, out rotation);

        NetworkObject nob = Instantiate(playerPrefabs[type], position, rotation);
        pm[playerCounter] = nob.transform.GetChild(0).GetComponent<Movement>();
        _networkManager.ServerManager.Spawn(nob, conn);
        _networkManager.SceneManager.AddOwnerToDefaultScene(nob);

        OnSpawned?.Invoke(nob);

        playerCounter++;
        if (playerCounter >= 1)
            Invoke("StartGame", .1f);
    }

    public void SpawnWizards()
    {
        foreach (KeyValuePair<NetworkConnection, (int, int)> player in playerList)
        {
            Vector3 position;
            Quaternion rotation;
            SetSpawn(playerPrefabs[0].transform, out position, out rotation);

            NetworkObject nob = Instantiate(playerPrefabs[player.Value.Item2], position, rotation);
            pm[player.Value.Item1] = nob.transform.GetChild(0).GetComponent<Movement>();
            _networkManager.ServerManager.Spawn(nob, player.Key);
            _networkManager.SceneManager.AddOwnerToDefaultScene(nob);

            OnSpawned?.Invoke(nob);
        }

        Invoke("StartGame", .5f);
    }

    public void SetWizard(NetworkConnection conn, int type)
    {
        playerList[conn] = (playerCounter, type);
        Debug.Log(conn);

        playerCounter++;
        if (playerCounter == numPlayers)
            SpawnWizards();
    }

    public void SetLives(bool owner, int lives)
    {
        if (owner)
            playerLives[lives].SetActive(false);
        else
            oppoLives[lives].SetActive(false);
    }

    public void SetName(NetworkConnection owner, string name)
    {
//        if (owner)
//            playername.text = name;
//        else
//            opponame.text = name;
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
