using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Connection;

public class JoinTeam : NetworkBehaviour
{
    WizardDuelManager wdm;
    public override void OnStartClient()
    {
        base.OnStartClient();
        wdm = GameObject.Find("WizardDuelManager").GetComponent<WizardDuelManager>();
        if (IsOwner)
        {
            transform.GetChild(1).gameObject.SetActive(true);
        }
    }

    public void JoinRed()
    {
        if (IsOwner)
        {
            if (!IsServer)
                wdm.QueueRed(base.Owner);
            joinTeam(base.Owner, 0);
            transform.GetChild(1).gameObject.SetActive(false);
            GetComponent<SpectatorCamera>().movable = true;
        }
    }

    public void JoinBlue()
    {
        if (IsOwner)
        {
            if (!IsServer)
                wdm.QueueBlue(base.Owner);
            joinTeam(base.Owner, 1);
            transform.GetChild(1).gameObject.SetActive(false);
            GetComponent<SpectatorCamera>().movable = true;
        }
    }

    [ServerRpc]
    private void joinTeam(NetworkConnection conn, int team)
    {
        if (team == 0)
            wdm.QueueRed(conn);
        else
            wdm.QueueBlue(conn);
    }
}
