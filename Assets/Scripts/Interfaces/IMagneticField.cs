using UnityEngine;

public interface IMagneticField
{
    float GetMagneticFieldGrid(float[] x, float[] y, float[] Bx, float[] By);
    Vector3 GetMagneticFieldAt(Vector3 position);
    float GetPrefactor();
    float GetWireCoordinateX();
    float GetWireCoordinateY();
    void SetMur(float newMur);
}
