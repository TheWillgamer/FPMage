using FishNet.Connection;
using FishNet.Managing.Timing;
using UnityEngine;

public interface Projectile
{
    /// <summary>
    /// Initializes projectile with force.
    /// </summary>
    /// <param name="force"></param>
    void Initialize(PreciseTick pt, Vector3 force);
}
