using FishNet.Managing.Timing;
using UnityEngine;
using FishNet.Connection;

public interface Projectile
{
    /// <summary>
    /// Initializes projectile with force.
    /// </summary>
    /// <param name="force"></param>
    void Initialize(PreciseTick pt, Vector3 force);
}
