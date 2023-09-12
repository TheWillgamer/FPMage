using FishNet.Connection;
using UnityEngine;

public interface PredictedProjectile
{
    /// <summary>
    /// Initializes projectile with force.
    /// </summary>
    /// <param name="force"></param>
    void Initialize(Vector3 force, NetworkConnection conn);

    //void Reflect(Vector3 dir, int conn);
}
