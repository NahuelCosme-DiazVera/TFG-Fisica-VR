using UnityEngine;
[System.Serializable]
public class VisualizerSettings
{
    [Range(10, 50)]
    public int gridpoints = 20;
    [Range(-5f, 5f)]
    public float fieldZcoordinate = 0f;

    public GameObject arrowPrefab;
    [Range(0.125f, 1f)]
    public float arrowScale = 0.5f;
    [Range(0.05f, 0.4f)]
    public float spriteScale = 0.2f;
    public Color minFieldColor = Color.blue;
    public Color maxFieldColor = Color.red;
}
