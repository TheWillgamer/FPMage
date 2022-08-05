using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

public class JoinTeam : NetworkBehaviour
{
    public void JoinRed()
    {
        if (IsOwner)
            GameObject.Find("WizardDuelManager").GetComponent<WizardDuelManager>().QueueRed(base.Owner);
        gameObject.SetActive(false);
    }
    public void JoinBlue()
    {
        if (IsOwner)
            GameObject.Find("WizardDuelManager").GetComponent<WizardDuelManager>().QueueBlue(base.Owner);
        gameObject.SetActive(false);
    }
}
