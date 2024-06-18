using HeathenEngineering.SteamworksIntegration;
using HeathenEngineering.SteamworksIntegration.UI;
using UnityEngine;
using UnityEngine.UI;

public class LobbyDetails : MonoBehaviour
{
    public TMPro.TextMeshProUGUI lobbyName;
    public Image mapImage;
    public TMPro.TextMeshProUGUI playerCount;
    public TMPro.TextMeshProUGUI ping;

    private LobbyData ld;
    [SerializeField] private Button joinButton;
    [SerializeField] private LobbyManager lm;

    public void SetLobby(LobbyData data)
    {
        joinButton.interactable = true;
        ld = data;
    }

    public void JoinLobby()
    {
        lm.Join(ld);
    }
}
