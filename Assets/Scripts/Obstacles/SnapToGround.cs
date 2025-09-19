using UnityEngine;

public static class GroundUtil
{
    public static bool SnapToGround(Transform t, LayerMask groundMask, float rayHeight, float extraClearance = 0.02f)
    {
        var col = t.GetComponentInChildren<Collider>();
        if (!col) return false;

        // стартуємо трохи вище поточної позиції
        Vector3 origin = t.position + Vector3.up * rayHeight;
        if (Physics.Raycast(origin, Vector3.down, out var hit, rayHeight * 2f, groundMask))
        {
            float halfH = col.bounds.extents.y;      // половина висоти колайдера в СВІТОВИХ координатах
            float targetY = hit.point.y + halfH + extraClearance;
            var p = t.position; p.y = targetY;
            t.position = p;
            return true;
        }
        return false;
    }
}
