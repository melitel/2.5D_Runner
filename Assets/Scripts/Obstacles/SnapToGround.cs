using UnityEngine;

/// <summary>
/// Utility class for aligning objects with the ground surface.
/// Provides a method that snaps an object's transform vertically so that its collider
/// rests on top of the detected ground.
/// </summary>
public static class GroundUtil
{
    /// <summary>
    /// Snaps the given transform onto the ground surface below it.
    /// Casts a ray downward from above the object and repositions it so that
    /// its collider sits exactly on the detected ground with a small clearance.
    /// </summary>
    /// <param name="t">Transform to snap to the ground.</param>
    /// <param name="groundMask">Layer mask specifying what counts as "ground".</param>
    /// <param name="rayHeight">How far above the object the raycast should start.
    /// This ensures the ray is cast from above even if the object starts inside geometry.</param>
    /// <param name="extraClearance">Optional vertical offset above the ground to avoid z-fighting (default: 0.02f).</param>
    /// <returns>
    /// True if ground was detected and the object was successfully repositioned;
    /// false if no ground surface was found.
    /// </returns>
    public static bool SnapToGround(Transform t, LayerMask groundMask, float rayHeight, float extraClearance = 0.02f)
    {
        var col = t.GetComponentInChildren<Collider>();
        if (!col) return false;

        // Start the ray slightly above the current position to ensure it clears geometry
        Vector3 origin = t.position + Vector3.up * rayHeight;

        // Cast ray downward to detect ground
        if (Physics.Raycast(origin, Vector3.down, out var hit, rayHeight * 2f, groundMask))
        {
            // Half the collider's height in world space
            float halfH = col.bounds.extents.y;

            // Target Y ensures the collider rests on the ground, with an extra clearance offset
            float targetY = hit.point.y + halfH + extraClearance;

            var p = t.position;
            p.y = targetY;
            t.position = p;
            return true;
        }
        return false;
    }
}
