
using System.Runtime.CompilerServices;
using UnityEngine;

public static class SpatialUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetBondAngle(Vector3 pos1, Vector3 pos2, Vector3 pos3)
    {
        return Vector3.Angle(pos1 - pos2, pos3 - pos2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetDihedral(Vector3 pos1, Vector3 pos2, Vector3 pos3, Vector3 pos4)
    {
        Vector3 vec = pos3 - pos2;

        Vector3 normal1 = Vector3.Cross(pos1 - pos2, vec);
        Vector3 normal2 = Vector3.Cross(-vec, pos4 - pos3);

        return Vector3.Angle(normal1, normal2);
    }

    // Keep the first two positions fixed, move the third position in the same plane to match the given angle (0-360).
    // Returns the new third position.
    public static Vector3 SetBondAngle(Vector3 pos1, Vector3 pos2, Vector3 pos3, float angle)
    {
        Vector3 dir1 = pos1 - pos2;
        Vector3 dir3 = pos3 - pos2;

        float currentAngle = Vector3.Angle(dir1, dir3);
        float delta = angle - currentAngle;

        Vector3 axis = Vector3.Cross(dir1, dir3);
        if (axis == Vector3.zero)
            return pos3;

        Vector3 newDir3 = Quaternion.AngleAxis(delta, axis.normalized) * dir3;
        return pos2 + newDir3;
    }

    // Keep the first three positions fixed, move the fourth position to match the given dihedral, the 2-3-4 angle should be kept unchanged.
    // Returns the new fourth position.
    public static Vector3 SetDihedral(Vector3 pos1, Vector3 pos2, Vector3 pos3, Vector3 pos4, float dihedral)
    {
        Vector3 axis = pos3 - pos2;
        if (axis == Vector3.zero)
            return pos4;

        Vector3 normal1 = Vector3.Cross(pos1 - pos2, axis);
        Vector3 normal2 = Vector3.Cross(-axis, pos4 - pos3);

        float currentDihedral = Vector3.SignedAngle(normal1, normal2, axis);
        float delta = dihedral - currentDihedral;

        Vector3 newDir4 = Quaternion.AngleAxis(delta, axis.normalized) * (pos4 - pos3);
        return pos3 + newDir4;
    }

    // Keep the first position fixed, move the second position to match the given bond length.
    // Returns the new second position.
    public static Vector3 SetBondLength(Vector3 pos1, Vector3 pos2, float length)
    {
        Vector3 dir = pos2 - pos1;
        if (dir == Vector3.zero)
            return pos2;

        return pos1 + dir.normalized * length;
    }
}