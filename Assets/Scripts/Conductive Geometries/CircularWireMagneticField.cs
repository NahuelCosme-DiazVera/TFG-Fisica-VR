using UnityEngine;

public class CircularWireMagneticField : MonoBehaviour, IMagneticField
{
    [Range(-2.5f, 2.5f)]
    public float x0 = 0f;
    [Range(2f, 7f)]
    public float y0 = 4.5f;
    public float core = 1e-6f;
    [Range(-20f, 20f)]
    public float intensity = 5f;
    [Range(0.04f, 2.5f)]
    public float a = 2.5f;
    private float mur = 1f;

    public float GetMagneticFieldGrid(float[] x, float[] y, float[] Bx, float[] By)
    {
        int totalPoints = x.Length;
        float mu0 = 4f * Mathf.PI * 1e-7f * mur;
        float prefactor = -(mu0 * intensity * a*a / 2f) * 1e7f;
        float core_squared = core * core;
        float maxFieldMagnitude = 0f;

        for (int i = 0; i < totalPoints; i++)
        {
            if (x[i] != x0)
            {
                float dx = x[i] - x0;
                float divider = a * a + dx * dx + core_squared;
                Bx[i] = prefactor * (1 / Mathf.Pow(divider, 3f/2f));
            }
            else
            {
                Bx[i] = -(mu0 * intensity / 2 * a);
            }
            By[i] = 0f;

            float magnitude = Bx[i] * Bx[i];
            if (magnitude > maxFieldMagnitude)
            {
                maxFieldMagnitude = magnitude;
            }
        }
        maxFieldMagnitude = Mathf.Sqrt(maxFieldMagnitude);
        maxFieldMagnitude = Mathf.Max(maxFieldMagnitude, 1e-8f);
        return maxFieldMagnitude;
    }

    public Vector3 GetMagneticFieldAt(Vector3 position)
    {
        float mu0 = 4f * Mathf.PI * 1e-7f;
        float prefactor = -(mu0 * intensity * a*a / 2f);
        float core_squared = core * core;

        if (position.x == x0)
        {
            return new Vector3(-(mu0 * intensity / 2 * a), 0f, 0f) * 1e7f;
        }
        float dx = position.x - x0;
        float divider = a * a + dx * dx + core_squared;

        float Bx = prefactor * (1 / Mathf.Pow(divider, 3f/2f));
        return new Vector3(Bx, 0f, 0f) * 1e7f;
    }

    public float GetPrefactor() {
        float mu0 = 4f * Mathf.PI * 1e-7f;
        return -(mu0 * intensity * a*a / 2f) * 1e7f;
    }

    public float GetWireCoordinateX() {
        return x0;
    }
    public float GetWireCoordinateY() {
        return y0;
    }

    public float GetWireRadius() {
        return a;
    }

    public void SetMur(float newMur) {
        mur = newMur;
    }
}
