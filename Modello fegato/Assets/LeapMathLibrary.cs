using UnityEngine;

public abstract class LeapMathLibrary 
{
    public static float Gaussian(float distanceFromHitPoint, float sigma1)
    {
        float factor = 1 / (Mathf.Sqrt(2 * Mathf.PI * sigma1 * sigma1));
        float exponent = -((distanceFromHitPoint * distanceFromHitPoint) / (2f * sigma1 * sigma1));
        return factor * Mathf.Exp(exponent);
    }
}
