using System.Collections;
using System.Collections.Generic;
using HeathenEngineering.SteamworksIntegration;
using HeathenEngineering.SteamworksIntegration.UI;
using UnityEngine;
using UnityEngine.UI;
using Steamworks;
using System;

public class LobbyUIController : MonoBehaviour
{
    public LobbyManager lobbyManager;
    public GameObject menuScreen;
    public GameObject sessionPanel;
    public GameObject[] border;
    public Image character;
    public GameObject mapSelector;
    public Image map;
    public SetUserAvatar[] memberSlotAvatars;
    public SetUserName[] memberSlotNames;
    public GameObject readyButton;
    public GameObject startButton;

    // cash of the members of the lobby other than my self
    private List<LobbyMemberData> partyMembersOtherThanMe = new List<LobbyMemberData>();


    [SerializeField] private int mapCount;      // number of maps in the game
    [SerializeField] private int charCount;     // number of characters in the game

    /// <summary>
    /// Occures when we have entered a lobby .. that is when we have joined a lobby we didn't create
    /// </summary>
    public void OnJoinedALobby()
    {
        var member = lobbyManager.Lobby.Me;
        member["char"] = "0";

        UpdateUI();

        //Check if the server has already been set for this lobby
        if (lobbyManager.HasServer)
            //If so update the user about that
            OnGameCreated(lobbyManager.GameServer);
    }
    /// <summary>
    /// Occurs when we create a lobby ... we know this only happens when we are the lobby "owner"
    /// </summary>
    /// <param name="lobby"></param>
    public void OnLobbyCreated(LobbyData lobby)
    {
        var member = lobby.Me;
        member["char"] = "0";

        UpdateUI();
    }
    public void HandleOnMemberLeft(UserLobbyLeaveData lobbyLeaveData)
    {
        Debug.Log($"A user named {lobbyLeaveData.user.Nickname} left the lobby");
        UpdateUI();
    }
    /// <summary>
    /// Occurs when the player clicks the "Ready" or "Wait" button
    /// </summary>
    public void SetReady()
    {
        lobbyManager.IsPlayerReady = !lobbyManager.IsPlayerReady;
    }
    /// <summary>
    /// Occurs when the player clicks the button to change characters
    /// </summary>
    public void ChangeCharacter(bool right)
    {
        var member = lobbyManager.Lobby.Me;
        int currentChar = Convert.ToInt32(member["char"]);
        currentChar = right ? currentChar + 1 : currentChar - 1;
        if (currentChar >= charCount)
            currentChar = 0;
        else if (currentChar < 0)
            currentChar = charCount - 1;
        member["char"] = currentChar.ToString();
    }
    /// <summary>
    /// Occurs when the player clicks the button to change maps
    /// </summary>
    public void ChangeMap(bool right)
    {
        var lobby = lobbyManager.Lobby;
        int currentMap = Convert.ToInt32(lobby["map"]);
        currentMap = right ? currentMap + 1 : currentMap - 1;
        if (currentMap >= mapCount)
            currentMap = 0;
        else if (currentMap < 0)
            currentMap = mapCount - 1;
        lobby["map"] = currentMap.ToString();
    }
    /// <summary>
    /// This occurs when the owner clicks "Start Session"
    /// </summary>
    public void StartGame()
    {
        if (!lobbyManager.Lobby.AllPlayersReady)
        {
            Debug.Log($"You chose to start the session before all players where marked ready");
        }

        Debug.Log($"If this was a real game the owner would need to start up the network session.\n" +
            $"For a typical P2P game this would mean loading the network scene, calling StartHost or similar on your networking HLAPI and setting up the environment to make it ready for others to join.\n" +
            $"For a Client/Server game this would mean allocating a server, connecting to it and configuring it to make it ready for others to join.\n" +
            $"Once the network session is set up and ready for others to connect ... the owner of the lobby should call SetGameServer as demonstrated in our source code.");

        //This will notify all other members on the lobby that the network session is ready to connect to
        lobbyManager.Lobby.SetGameServer();
    }
    /// <summary>
    /// Occurs when the server info has been set on this lobby
    /// </summary>
    /// <param name="server"></param>
    public void OnGameCreated(LobbyGameServer server)
    {
        //If we are the owner ... we already know we set this and can skip this
        if (lobbyManager.Lobby.IsOwner)
            return;

        Debug.Log($"The owner of the session lobby has notified us that the server is ready to connect to and that the address of the server is\n\n" +
            $"CSteamID:{server.id}\n" +
            $"IP:{server.IpAddress}\n" +
            $"Port:{server.port}\n\n" +
            $"Yes its normal for the IP and Port to be 0 ... this is a P2P session and is assuming your using a Steam based transport which uses the CSteamID as the address");
    }
    /// <summary>
    /// We call this anytime there has been some change to the membership or data of the lobby
    /// </summary>
    public void UpdateUI()
    {
        //If we have no lobby clear all data and return
        if (!lobbyManager.HasLobby)
        {
            foreach (var b in border)
                b.SetActive(false);

            menuScreen.SetActive(true);
            sessionPanel.SetActive(false);

            return;
        }

        //If we got here then we have a lobby so lets update our view of it
        //Get the tracked lobby
        var lobby = lobbyManager.Lobby;
        menuScreen.SetActive(false);
        sessionPanel.SetActive(true);

        // Adds graphic for the map
        switch (lobby["map"])
        {
            case "0":
                map.color = Color.magenta;
                break;
            case "1":
                map.color = Color.cyan;
                break;
            case "2":
                map.color = Color.gray;
                break;
            case "3":
                map.color = Color.yellow;
                break;
            default:
                print("ERROR! map not found");
                break;
        }

        switch (lobby.Me["char"])
        {
            case "0":
                character.color = Color.red;
                break;
            case "1":
                character.color = Color.green;
                break;
            case "2":
                character.color = Color.blue;
                break;
            default:
                print("ERROR! character not found");
                break;
        }

        // Adds graphic for your chosen character

        if (lobby.IsOwner)
        {
            readyButton.SetActive(false);
            startButton.SetActive(true);
        }
        else
        {
            readyButton.SetActive(true);
            startButton.SetActive(false);
        }

        //Set my owner pip ... 
        if (lobby.IsOwner)
            border[0].GetComponent<Image>().color = new Color32(255, 215, 0, 100);

        //Set my ready pip
        else if (lobby.IsReady)
            border[0].GetComponent<Image>().color = Color.green;
        else
            border[0].GetComponent<Image>().color = Color.red;

        //Get the members in the lobby
        var members = lobby.Members;

        //Clear our member cash
        partyMembersOtherThanMe.Clear();

        //Rebuild the cash
        foreach (var member in members)
        {
            //If this is not me, then add it to the cash
            if (member.user != UserData.Me)
                partyMembersOtherThanMe.Add(member);
        }

        //Clear all pips
        border[1].SetActive(false);

        //Set slot 1 information
        //If we have a member in slot 1 e.g. we have more than 0 members
        if (partyMembersOtherThanMe.Count > 0)
        {
            border[1].SetActive(true);

            //Set the ownership pip ... 
            if (partyMembersOtherThanMe[0].IsOwner)
                border[1].GetComponent<Image>().color = new Color32(255, 215, 0, 100);

            //Set the read pip ...
            else if (partyMembersOtherThanMe[0].IsReady)
                border[1].GetComponent<Image>().color = Color.green;
            else
                border[1].GetComponent<Image>().color = Color.red;

            //Set the avatar
            memberSlotAvatars[0].UserData = partyMembersOtherThanMe[0].user;
            memberSlotNames[0].UserData = partyMembersOtherThanMe[0].user;
        }
    }

    // Joins 
    public void JoinLobby(string lobbyInput)
    {
        SteamMatchmaking.JoinLobby(new CSteamID(Convert.ToUInt64(lobbyInput)));
    }
}
