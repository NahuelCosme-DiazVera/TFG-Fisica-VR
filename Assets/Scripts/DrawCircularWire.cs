using UnityEngine;

public class DrawCircularWire : MonoBehaviour
{
    
    [Range(10, 64)]
    private int numSegments = 32;
    private float radius = 2.5f;
    LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = numSegments + 1;
        lineRenderer.useWorldSpace = false;
        float delta = (float)(2.0f * Mathf.PI) / numSegments;
        float theta = 0f;

        for (int i = 0; i < lineRenderer.positionCount; i++)
        {
            float y = radius * Mathf.Cos(theta);
            float z = radius * Mathf.Sin(theta);
            Vector3 position = new Vector3(0f, y, z);
            lineRenderer.SetPosition(i, position);
            theta += delta;
        }
    }

    
    void Update()
    {
        
    }
}
