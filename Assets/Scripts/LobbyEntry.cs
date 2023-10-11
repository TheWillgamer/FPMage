using HeathenEngineering.SteamworksIntegration;
using HeathenEngineering.SteamworksIntegration.UI;
using UnityEngine;
using TMPro;

public class LobbyEntry : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI name;
    [SerializeField] private TMPro.TextMeshProUGUI ping;
    [SerializeField] private TMPro.TextMeshProUGUI numPlayers;

    public void SetLobby(LobbyData data,LobbyManager lm)
    {
        name.text = data.Name;
    }
}
