using FishNet.Object;

public class ParentObject : NetworkBehaviour
{
    public void DespawnPrefab()
    {
        base.Despawn();
    }
}
