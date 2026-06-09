using UnityEngine;

public class Draw_circle : MonoBehaviour
{

    public LineRenderer LineRenderer;
    public int segments = 10;
    public float radius = 5f;
    public float speed = 1f;
    public Material lineMaterial;
    public GameObject arrowPrefab;
    public float arrowScale = 0.5f;

    private GameObject[] arrows;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        arrows = new GameObject[4];
        for (int i = 0; i < 4; i++)
        {
            arrows[i] = Instantiate(arrowPrefab, transform);
        }

    }

    // Update is called once per frame
    void Update()
    {
        float angle = 2f * Mathf.PI / segments;
        LineRenderer.positionCount = segments;

        for (int i = 0; i < segments; i++)
        {
            float x = Mathf.Cos(angle * i) * radius;
            float y = Mathf.Sin(angle * i) * radius;
            Vector3 position = new Vector3(x, y, 0f);
            LineRenderer.SetPosition(i, position);
        }

        // Arrows positioning
        for (int i = 0; i < 4; i++)
        {
            float arrowAngle = (Time.time * speed) + i * (2f * Mathf.PI / 4); //Arrows movement in the circle
            float x = Mathf.Cos(arrowAngle) * radius;
            float y = Mathf.Sin(arrowAngle) * radius;
            Vector3 arrowPosition = new Vector3(x, y, 0f);
            arrows[i].transform.localPosition = arrowPosition;

            Vector3 direction = new Vector3(0f, 0f, Mathf.Rad2Deg * arrowAngle); //Arrow rotation around the circle
            arrows[i].transform.eulerAngles = direction;
            arrows[i].transform.localScale = new Vector3(arrowScale, arrowScale, arrowScale);
            arrows[i].GetComponentsInChildren<Renderer>()[0].material = lineMaterial;
        }
    }
}
