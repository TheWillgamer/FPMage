//
// Procedural Lightning for Unity
// (c) 2015 Digital Ruby, LLC
// Source code may be used for personal or commercial projects.
// Source code may NOT be redistributed or sold.
// 

using UnityEngine;
using DigitalRuby.ThunderAndLightning;
using FishNet.Object;

/// <summary>
/// Lightning bolt beam spell script, like a phasor or laser
/// </summary>
public class LightningStrike : NetworkBehaviour
{
    [SerializeField] LightningBoltPrefabScript spell;
    [SerializeField] Transform SpellStart;
    [SerializeField] Transform SpellEnd;
    [SerializeField] float MaxDistance;

    /// <summary>
    /// Callback for collision events
    /// </summary>
    [HideInInspector]
    public System.Action<RaycastHit> CollisionCallback;

    private void CheckCollision()
    {
        RaycastHit hit;
        Vector3 Direction = SpellEnd.position - SpellStart.position;

        // send out a ray to see what gets hit
        if (Physics.Raycast(SpellStart.position, Direction, out hit, MaxDistance))
        {
            // we hit something, set the end object position
            SpellEnd.position = hit.point;
        }
        else
        {
            // extend beam to max length
            SpellEnd.position = SpellStart.position + (Direction * MaxDistance);
        }
        spell.Trigger();
    }

    /// <summary>
    /// Update
    /// </summary>
    private void LateUpdate()
    {
        CheckCollision();
    }
}