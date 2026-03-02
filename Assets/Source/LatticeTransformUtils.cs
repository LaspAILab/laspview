using System;
using UnityEngine;

public static class LatticeTransformUtils
{
    private static readonly Vector3[] Corners = new[]
    {
        new Vector3(0, 0, 0),
        new Vector3(1, 0, 0),
        new Vector3(0, 1, 0),
        new Vector3(0, 0, 1),
        new Vector3(1, 1, 0),
        new Vector3(1, 0, 1),
        new Vector3(0, 1, 1),
        new Vector3(1, 1, 1)
    };

    /// <summary>
    /// Transforms the lattice of the crystal structure using the given transform matrix.
    /// </summary>
    /// <param name="structure">The crystal structure to transform.</param>
    /// <param name="transform">The transform matrix to apply to the lattice, used as a 3x3 matrix, any values on the 4th column or row are ignored.</param>
    /// <returns></returns>
    public static CrystalStructure TransformLattice(this CrystalStructure structure, Matrix4x4 transform)
    {
        CrystalStructure transformedStructure = new();

        var baseVectors = structure.GetBaseVectors();
        baseVectors[0] = transform.MultiplyVector(baseVectors[0]);
        baseVectors[1] = transform.MultiplyVector(baseVectors[1]);
        baseVectors[2] = transform.MultiplyVector(baseVectors[2]);

        transformedStructure.SetLattice(baseVectors);

        float maxA = float.MinValue;
        float maxB = float.MinValue;
        float maxC = float.MinValue;
        float minA = float.MaxValue;
        float minB = float.MaxValue;
        float minC = float.MaxValue;

        foreach (var corner in Corners)
        {
            // Matrix used as 3x3 here, so use `MultiplyVector` which ignores the translation part. 
            Vector3 transformedCorner = transform.MultiplyVector(corner);
            maxA = Mathf.Max(maxA, transformedCorner.x);
            maxB = Mathf.Max(maxB, transformedCorner.y);
            maxC = Mathf.Max(maxC, transformedCorner.z);
            minA = Mathf.Min(minA, transformedCorner.x);
            minB = Mathf.Min(minB, transformedCorner.y);
            minC = Mathf.Min(minC, transformedCorner.z);
        }

        int startA = Mathf.FloorToInt(minA);
        int startB = Mathf.FloorToInt(minB);
        int startC = Mathf.FloorToInt(minC);
        int endA = Mathf.CeilToInt(maxA);
        int endB = Mathf.CeilToInt(maxB);
        int endC = Mathf.CeilToInt(maxC);

        var inverseTransform = transform.inverse;

        foreach (var item in structure.Atoms)
        {
            var fractionalCoords = structure.ClipFractionalCoords(structure.CartesianToFractional(item.Coordinates));
            for (int i = startA; i < endA; i++)
            {
                for (int j = startB; j < endB; j++)
                {
                    for (int k = startC; k < endC; k++)
                    {
                        var transformedCoords = inverseTransform.MultiplyVector(fractionalCoords + new Vector3(i, j, k));

                        if (transformedCoords.x is >= 0 and <= 1 && transformedCoords.y is >= 0 and <= 1 &&
                            transformedCoords.z is >= 0 and <= 1)
                        {
                            transformedStructure.AddAtom(item.Symbol,
                                transformedStructure.FractionalToCartesian(transformedCoords), item.Charge, item.Flags);
                        }
                    }
                }
            }
        }

        return transformedStructure;
    }
}