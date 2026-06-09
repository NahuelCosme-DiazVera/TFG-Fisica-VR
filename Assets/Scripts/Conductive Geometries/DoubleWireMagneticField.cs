using UnityEngine;
using System.Collections.Generic;

public class DoubleWireMagneticField : MonoBehaviour, IMagneticField
{
    private List<float> x_h = new List<float> { -0.8f, 0.8f };
    private List<float> y_h = new List<float> { 4.5f, 4.5f };
    public float core = 1e-6f;
    private List<float> I = new List<float> { 0f, 0f };
    private float mur = 1f;

    public float GetMagneticFieldGrid(float[] x, float[] y, float[] Bx, float[] By) {
        int totalPoints = x.Length;
        float mu0 = 4f * Mathf.PI * 1e-7f * mur;
        float prefactor = mu0 / (2f * Mathf.PI) * 1e7f;
        float core_squared = core * core;
        float maxFieldMagnitude = 0f;
        for (int i = 0; i < totalPoints; i++) {
            for (int j = 0; j < totalPoints; j++) {
                int index = i * totalPoints + j;
                float bx = 0f;
                float by = 0f;
                for (int k = 0; k < x_h.Count; k++) {
                    float dx = x[j] - x_h[k];
                    float dy = y[i] - y_h[k];
                    float r_squared = dx * dx + dy * dy + core_squared;
                    bx += prefactor * (I[k] * (-dy) / r_squared);
                    by += prefactor * (I[k] * dx / r_squared);
                }

                Bx[index] = bx;
                By[index] = by;

                float magnitude = Bx[index] * Bx[index] + By[index] * By[index];
                if (magnitude > maxFieldMagnitude) {
                    maxFieldMagnitude = magnitude;
                }
            }
        }
        maxFieldMagnitude = Mathf.Sqrt(maxFieldMagnitude);
        maxFieldMagnitude = Mathf.Max(maxFieldMagnitude, 1e-8f);
        return maxFieldMagnitude;
    }

    public Vector3 GetMagneticFieldAt(Vector3 position) {
        float mu0 = 4f * Mathf.PI * 1e-7f * mur;
        float prefactor = mu0 / (2f * Mathf.PI);
        float core_squared = core * core;

        float bx = 0f;
        float by = 0f;
        for (int k = 0; k < x_h.Count; k++) {
            float dx = position.x - x_h[k];
            float dy = position.y - y_h[k];
            float r_squared = dx * dx + dy * dy + core_squared;
            bx += prefactor * (I[k] * (-dy) / r_squared);
            by += prefactor * (I[k] * dx / r_squared);
        }

        return new Vector3(bx, by, 0f) * 1e7f;
    }

    public float GetPrefactor() {
        float mu0 = 4f * Mathf.PI * 1e-7f;
        return mu0 * (I[0] + I[1] / 2) / (2f * Mathf.PI) * 1e7f;
    }

    public float GetWireCoordinateX() {
        return x_h[0] + x_h[1] / 2f;
    }
    public float GetWireCoordinateY() {
        return y_h[0] + y_h[1] / 2f;
    }

    public float GetWireSeparation() {
        return Mathf.Abs(x_h[1] - x_h[0]);
    }

    public void SetMur(float newMur) {
        mur = newMur;
    }

    public void AddWire(float x, float y, float intensity) {
        x_h.Add(x);
        y_h.Add(y);
        I.Add(intensity);
    }

    public void RemoveWire(int index) {
        if (index >= 0 && index < x_h.Count) {
            x_h.RemoveAt(index);
            y_h.RemoveAt(index);
            I.RemoveAt(index);
        }
    }

    public int GetWireCount() {
        return x_h.Count;
    }

    public float GetWireIntensity(int index) {
        if (index >= 0 && index < I.Count) {
            return I[index];
        }
        return 0f;
    }

    public void SetWireIntensity(int index, float intensity) {
        if (index >= 0 && index < I.Count) {
            I[index] = intensity;
        }
    }

    public void SetXCoordinate(int index, float x) {
        if (index >= 0 && index < x_h.Count) {
            x_h[index] = x;
        }
    }

    public void SetYCoordinate(int index, float y) {
        if (index >= 0 && index < y_h.Count) {
            y_h[index] = y;
        }
    }

    public List<float> GetWireXCoordinates() {
        return x_h;
    }

    public List<float> GetWireYCoordinates() {
        return y_h;
    }

    public List<float> GetIntensities() {
        return I;
    }

    public float GetTotalIntensity() {
        float totalIntensity = 0f;
        foreach (float intensity in I) {
            totalIntensity += Mathf.Abs(intensity);
        }
        return totalIntensity;
    }
}
