using System;
using UnityEngine;

namespace DTT.Utils.Extensions
{
    /// <summary>
    /// Provides extension methods for Vector3 values.
    /// </summary>
    public static class Vector3Extensions
    {
        /// <summary>
        /// Flattens a vector by setting its axis components to 0.
        /// </summary>
        /// <param name="vector">The vector to flatten.</param>
        /// <param name="axis">The axis to flatten (Uses enum flags).</param>
        /// <returns>The flattened vector.</returns>
        public static Vector3 Flatten(this Vector3 vector, Vector3Axis axis)
        {
            Array values = Enum.GetValues(typeof(Vector3Axis));
            
            for (int i = 0; i < values.Length; i++)
            {
                if(axis.HasFlag((Enum)values.GetValue(i)))
                    vector[i] = 0;
            }

            return vector;
        }
    }
}