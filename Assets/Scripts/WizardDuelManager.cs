using FirstGearGames.LobbyAndWorld.Clients;
using FirstGearGames.LobbyAndWorld.Global;
using FirstGearGames.LobbyAndWorld.Global.Canvases;
using FirstGearGames.LobbyAndWorld.Lobbies;
using FirstGearGames.LobbyAndWorld.Lobbies.JoinCreateRoomCanvases;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public class WizardDuelManager : NetworkBehaviour
{
    #region Serialized
    [Header("Spawning")]
    /// <summary>
    /// Location where players respawn. (and observers spawn)
    /// </summary>
    [Tooltip("Areas in which players may respawn.")]
    [SerializeField]
    public Transform Re_spawn;
    /// <summary>
    /// Region players may spawn.
    /// </summary>
    [Tooltip("Areas in which players may spawn.")]
    [SerializeField]
    public Transform[] Startingspawns = new Transform[0];
    /// <summary>
    /// Wizard prefab to spawn.
    /// </summary>
    [Tooltip("Prefab to spawn.")]
    [SerializeField]
    private NetworkObject r_playerPrefab = null;
    [SerializeField]
    private NetworkObject b_playerPrefab = null;
    /// <summary>
    /// Observer prefab to spawn.
    /// </summary>
    [Tooltip("Prefab to spawn.")]
    [SerializeField]
    private NetworkObject _obPrefab = null;
    #endregion

    /// <summary>
    /// RoomDetails for this game. Only available on the server.
    /// </summary>
    private RoomDetails _roomDetails = null;
    /// <summary>
    /// LobbyNetwork.
    /// </summary>
    private LobbyNetwork _lobbyNetwork = null;
    /// <summary>
    /// Becomes true once someone has won.
    /// </summary>
    private bool _winner = false;
    /// <summary>
    /// Currently spawned player objects. Only exist on the server.
    /// </summary>
    private List<NetworkObject> _spawnedPlayerObjects = new List<NetworkObject>();
    /// <summary>
    /// List of players on red team
    /// </summary>
    public Queue<NetworkConnection> r_team = new Queue<NetworkConnection>();
    /// <summary>
    /// List of players on blue team
    /// </summary>
    public Queue<NetworkConnection> b_team = new Queue<NetworkConnection>();
    /// <summary>
    /// Next spawns to use.
    /// </summary>
    private NetworkObject currentRedPlayer;
    private NetworkObject currentBluePlayer;
    private bool started = false;

    private int _nextSpawn;

    private int r_lives;        // Lives remaining for Red Team
    private int b_lives;        // Lives remaining for Blue Team

    #region UI
    public GameObject UI;
    #endregion

    #region Initialization and Deinitialization.
    private void OnDestroy()
    {
        if (_lobbyNetwork != null)
        {
            _lobbyNetwork.OnClientJoinedRoom -= LobbyNetwork_OnClientStarted;
            _lobbyNetwork.OnClientLeftRoom -= LobbyNetwork_OnClientLeftRoom;
        }
    }

    private void Update()
    {
        if (!started)
        {
            if (r_team.Count > 0 && b_team.Count > 0)
            {
                NetworkConnection temp = r_team.Dequeue();
                for (int i = 0; i < _spawnedPlayerObjects.Count; i++)
                {
                    NetworkObject entry = _spawnedPlayerObjects[i];
                    //Entry is null. Remove and iterate next.
                    if (entry == null)
                    {
                        _spawnedPlayerObjects.RemoveAt(i);
                        i--;
                        continue;
                    }

                    //If same connection to client (owner) as client instance of leaving player.
                    if (_spawnedPlayerObjects[i].Owner == temp)
                    {
                        //Destroy entry then remove from collection.
                        InstanceFinder.ServerManager.Despawn(entry.gameObject);
                        _spawnedPlayerObjects.RemoveAt(i);
                        i--;
                    }
                }
                SpawnPlayer(temp, 0);
                r_team.Enqueue(temp);
                
                temp = b_team.Dequeue();
                for (int i = 0; i < _spawnedPlayerObjects.Count; i++)
                {
                    NetworkObject entry = _spawnedPlayerObjects[i];
                    //Entry is null. Remove and iterate next.
                    if (entry == null)
                    {
                        _spawnedPlayerObjects.RemoveAt(i);
                        i--;
                        continue;
                    }

                    //If same connection to client (owner) as client instance of leaving player.
                    if (_spawnedPlayerObjects[i].Owner == temp)
                    {
                        //Destroy entry then remove from collection.
                        InstanceFinder.ServerManager.Despawn(entry.gameObject);
                        _spawnedPlayerObjects.RemoveAt(i);
                        i--;
                    }

                }
                SpawnPlayer(temp, 1);
                b_team.Enqueue(temp);
                started = true;
            }
        }
        else
        {
            CheckWin();
            UpdateUI();
        }
    }

    /// <summary>
    /// Initializes this script for use.
    /// </summary>
    public void FirstInitialize(RoomDetails roomDetails, LobbyNetwork lobbyNetwork)
    {
        _roomDetails = roomDetails;
        _lobbyNetwork = lobbyNetwork;
        _lobbyNetwork.OnClientStarted += LobbyNetwork_OnClientStarted;
        _lobbyNetwork.OnClientLeftRoom += LobbyNetwork_OnClientLeftRoom;
    }

    /// <summary>
    /// Called when a client leaves the room.
    /// </summary>
    /// <param name="arg1"></param>
    /// <param name="arg2"></param>
    private void LobbyNetwork_OnClientLeftRoom(RoomDetails arg1, NetworkObject arg2)
    {
        //Destroy all of clients objects, except their client instance.
        for (int i = 0; i < _spawnedPlayerObjects.Count; i++)
        {
            NetworkObject entry = _spawnedPlayerObjects[i];
            //Entry is null. Remove and iterate next.
            if (entry == null)
            {
                _spawnedPlayerObjects.RemoveAt(i);
                i--;
                continue;
            }

            //If same connection to client (owner) as client instance of leaving player.
            if (_spawnedPlayerObjects[i].Owner == arg2.Owner)
            {
                //Destroy entry then remove from collection.
                InstanceFinder.ServerManager.Despawn(entry.gameObject);
                _spawnedPlayerObjects.RemoveAt(i);
                i--;
            }

        }
    }

    /// <summary>
    /// Called when a client starts a game.
    /// </summary>
    /// <param name="roomDetails"></param>
    /// <param name="client"></param>
    private void LobbyNetwork_OnClientStarted(RoomDetails roomDetails, NetworkObject client)
    {
        //Not for this room.
        if (roomDetails != _roomDetails)
            return;
        //NetIdent is null or not a player.
        if (client == null || client.Owner == null)
            return;

        /* POSSIBLY USEFUL INFORMATION!!!!!
            * POSSIBLY USEFUL INFORMATION!!!!!
            * If you want to wait until all players are in the scene
            * before spaning then check if roomDetails.StartedMembers.Count
            * is the same as roomDetails.MemberIds.Count. A member is considered
            * started AFTER they have loaded all of the scenes. */
        SpawnObserver(client.Owner);
    }
    #endregion

    #region Death.
    /// <summary>
    /// Called when object exits trigger. Used to respawn players.
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        if (!base.IsServer)
            return;

        NetworkObject netIdent = other.gameObject.GetComponent<NetworkObject>();
        //If doesn't have a netIdent or no owning client exit.
        if (netIdent == null || netIdent.Owner == null)
            return;

        //If there is an owning client then destroy the object and respawn.
        NetworkConnection conn = netIdent.Owner;
        InstanceFinder.ServerManager.Despawn(netIdent.gameObject);
        SpawnObserver(conn);
        CheckWin();
    }
    #endregion

    #region Winning.
    /// <summary>
    /// Called when a player wins.
    /// </summary>
    private void CheckWin()
    {
        if(r_lives < 1)
        {
            TeamWon(b_team);
        }
        else if (b_lives < 1)
        {
            TeamWon(r_team);
        }
    }

    /// <summary>
    /// Ends the game announcing winner and sending clients back to lobby.
    /// </summary>
    /// <returns></returns>
    private IEnumerator TeamWon(Queue<NetworkConnection> team)
    {
        //Send out winner text.
        foreach (NetworkObject item in _roomDetails.StartedMembers)
        {
            if (item != null && item.Owner != null)
                TargetShowWinner(item.Owner, team.Contains(item.Owner));
        }

        //Wait a moment then kick the players out. Not required.
        yield return new WaitForSeconds(4f);
        List<NetworkObject> collectedIdents = new List<NetworkObject>();
        foreach (NetworkObject item in _roomDetails.StartedMembers)
        {
            ClientInstance cli = ClientInstance.ReturnClientInstance(item.Owner);
            if (cli != null)
                collectedIdents.Add(cli.NetworkObject);
        }
        foreach (NetworkObject item in collectedIdents)
            _lobbyNetwork.TryLeaveRoom(item);
    }

    #region addPlayersToTeamQueue
    public void QueueRed(NetworkConnection conn)
    {
        r_team.Enqueue(conn);
        if (!IsOwner)
            QueueRedObs(conn);
    }

    public void QueueBlue(NetworkConnection conn)
    {
        b_team.Enqueue(conn);
        if (!IsOwner)
            QueueBlueObs(conn);
    }

    [ObserversRpc]
    public void QueueRedObs(NetworkConnection conn)
    {
        r_team.Enqueue(conn);
    }

    [ObserversRpc]
    public void QueueBlueObs(NetworkConnection conn)
    {
        b_team.Enqueue(conn);
    }
    #endregion

    /// <summary>
    /// Displayers who won.
    /// </summary>
    /// <param name="winner"></param>
    [TargetRpc]
    private void TargetShowWinner(NetworkConnection conn, bool won)
    {
        Color c = (won) ? MessagesCanvas.LIGHT_BLUE : Color.red;
        string text = (won) ? "Victory!" :
            $"Defeat!";
        GlobalManager.CanvasesManager.MessagesCanvas.InfoMessages.ShowTimedMessage(text, c, 4f);
    }
    #endregion

    #region Spawning.
    /// <summary>
    /// Spawns a player at a random position for a connection.
    /// </summary>
    /// <param name="conn"></param>
    private void SpawnObserver(NetworkConnection conn)
    {
        Vector3 position = Re_spawn.position;
        Quaternion rotation = Re_spawn.rotation;

        //Make object and move it to proper scene.
        NetworkObject netIdent = Instantiate<NetworkObject>(_obPrefab, position, rotation);
        UnitySceneManager.MoveGameObjectToScene(netIdent.gameObject, gameObject.scene);

        _spawnedPlayerObjects.Add(netIdent);
        base.Spawn(netIdent.gameObject, conn);

        //NetworkObject netIdent = conn.identity;            
        netIdent.transform.position = position;
        RpcTeleport(netIdent, position);
    }

    // 0 for red, 1 for blue
    private void SpawnPlayer(NetworkConnection conn, int team)
    {
        Vector3 position;
        Quaternion rotation;
        SetSpawn(out position, out rotation);

        //Make object and move it to proper scene.
        NetworkObject netIdent;
        if (team == 0)
            netIdent = Instantiate<NetworkObject>(r_playerPrefab, position, rotation);
        else
            netIdent = Instantiate<NetworkObject>(b_playerPrefab, position, rotation);
        UnitySceneManager.MoveGameObjectToScene(netIdent.gameObject, gameObject.scene);

        _spawnedPlayerObjects.Add(netIdent);
        string un = ClientInstance.ReturnClientInstance(conn).PlayerSettings.GetUsername();
        if (team == 0)
        {
            currentRedPlayer = netIdent;
            if (!IsOwner)
                setPlayer(netIdent, 0);
            UI.transform.GetChild(0).GetChild(1).GetComponent<TMPro.TextMeshProUGUI>().text = un;
        }
        else
        {
            currentBluePlayer = netIdent;
            if (!IsOwner)
                setPlayer(netIdent, 1);
            UI.transform.GetChild(1).GetChild(1).GetComponent<TMPro.TextMeshProUGUI>().text = un;
        }
        base.Spawn(netIdent.gameObject, conn);

        //NetworkObject netIdent = conn.identity;            
        netIdent.transform.position = position;
        RpcTeleport(netIdent, position);
    }
    [ObserversRpc]
    private void setPlayer(NetworkObject obj, int team)
    {
        if (team == 0)
        {
            currentRedPlayer = obj;
        }
        else
        {
            currentBluePlayer = obj;
        }
    }
    /// <summary>
    /// teleports a NetworkObject to a position.
    /// </summary>
    /// <param name="ident"></param>
    /// <param name="position"></param>
    [ObserversRpc]
    private void RpcTeleport(NetworkObject ident, Vector3 position)
    {
        ident.transform.position = position;
    }
    #endregion

    /// <summary>
    /// Sets a spawn position and rotation.
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="rot"></param>
    private void SetSpawn(out Vector3 pos, out Quaternion rot)
    {
        Transform result = Startingspawns[_nextSpawn];
        pos = result.position;
        rot = result.rotation;

        //Increase next spawn and reset if needed.
        _nextSpawn++;
        if (_nextSpawn >= Startingspawns.Length)
            _nextSpawn = 0;
    }

    private void UpdateUI()
    {
        if (currentRedPlayer != null)
        {
            UI.transform.GetChild(0).GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = currentRedPlayer.transform.GetChild(0).GetComponent<PlayerHealth>().hp.ToString() + "%";
        }
        if (currentBluePlayer != null)
        {
            UI.transform.GetChild(1).GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = currentBluePlayer.transform.GetChild(0).GetComponent<PlayerHealth>().hp.ToString() + "%";
        }
    }
}