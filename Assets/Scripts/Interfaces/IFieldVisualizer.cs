using UnityEngine;

public interface IFieldVisualizer
{
    void Activate();
    void Deactivate();
    void UpdateVisuals(IMagneticField fieldProvider, VisualizerSettings settings);
    void RefreshParameters();
}
