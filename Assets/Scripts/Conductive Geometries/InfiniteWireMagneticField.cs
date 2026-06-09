using UnityEngine;

public class InfiniteWireMagneticField : MonoBehaviour, IMagneticField
{

    [Range(-2.5f, 2.5f)]
    public float x0 = 0f;
    [Range(2f, 7f)]
    public float y0 = 4.5f;
    public float core = 1e-6f;
    [Range(-20f, 20f)]
    public float intensity = 0f;
    private float mur = 1f;

    public float GetMagneticFieldGrid(float[] x, float[] y, float[] Bx, float[] By) {
        int totalPoints = x.Length;
        float mu0 = 4f * Mathf.PI * 1e-7f * mur;
        float prefactor = (mu0 * intensity / (2f * Mathf.PI)) * 1e7f;
        float core_squared = core * core;
        float maxFieldMagnitude = 0f;

        for (int i = 0; i < totalPoints; i++) {
            for (int j = 0; j < totalPoints; j++) {
                int index = i * totalPoints + j;
                float dx = x[j] - x0;
                float dy = y[i] - y0;
                float r_squared = dx * dx + dy * dy + core_squared;

                Bx[index] = prefactor * (-dy) / r_squared;
                By[index] = prefactor * (dx) / r_squared;

                float magnitude = Bx[index] * Bx[index] + By[index] * By[index];
                if (magnitude > maxFieldMagnitude) {
                    maxFieldMagnitude = magnitude;
                }
            }
        }
        maxFieldMagnitude = Mathf.Sqrt(maxFieldMagnitude);
        maxFieldMagnitude = Mathf.Max(maxFieldMagnitude, 1e-8f); //Avoid division by zero
        return maxFieldMagnitude;
    }

    public Vector3 GetMagneticFieldAt(Vector3 position) {
        float mu0 = 4f * Mathf.PI * 1e-7f;
        float prefactor = mu0 * intensity / (2f * Mathf.PI);
        float core_squared = core * core;

        float dx = position.x - x0;
        float dy = position.y - y0;
        float r_squared = dx * dx + dy * dy + core_squared;

        float Bx = prefactor * (-dy) / r_squared;
        float By = prefactor * (dx) / r_squared;

        return new Vector3(Bx, By, 0f) * 1e7f;
    }

    public float GetPrefactor() {
        float mu0 = 4f * Mathf.PI * 1e-7f;
        return (mu0 * intensity / (2f * Mathf.PI)) * 1e7f;
    }

    public float GetWireCoordinateX() {
        return x0;
    }

    public float GetWireCoordinateY() {
        return y0;
    }

    public void SetWireCoordinateX(float newX) {
        x0 = newX;
    }

    public void SetWireCoordinateY(float newY) {
        y0 = newY;
    } 

    public void SetMur(float newMur) {
        mur = newMur;
    }

    public void SetWireIntensity(float newIntensity) {
        intensity = newIntensity;
    }

    public float GetWireIntensity() {
        return intensity;
    }
}
