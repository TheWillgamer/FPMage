using HeathenEngineering.SteamworksIntegration;
using HeathenEngineering.SteamworksIntegration.UI;
using UnityEngine;
using TMPro;

public class LobbyEntry : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI name;
    [SerializeField] private TMPro.TextMeshProUGUI numPlayers;
    private LobbyData lobbyData;
    private LobbyDetails lobbyDetails;

    private void Start()
    {
        lobbyDetails = GameObject.FindWithTag("LobbyDetails").GetComponent<LobbyDetails>();
    }

    public void SetLobby(LobbyData data)
    {
        lobbyData = data;
        name.text = data.Name;
        numPlayers.text = data.MemberCount.ToString() + '/' + data.MaxMembers.ToString();
    }

    public void ShowDetails()
    {
        lobbyDetails.SetLobby(lobbyData);
        lobbyDetails.lobbyName.text = name.text;
        lobbyDetails.playerCount.text = "Players: " + numPlayers.text;
    }
}
