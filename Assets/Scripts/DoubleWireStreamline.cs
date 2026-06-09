using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Equivalente básico de MATLAB contour(X,Y,Z,niveles,'k','LineWidth',1)
/// usando Marching Squares + LineRenderer en Unity.
///
/// Úsalo así:
/// 1) Crea un GameObject vacío.
/// 2) Añade este script.
/// 3) Asigna un material a lineMaterial.
/// 4) Pulsa Play.
///
/// El script genera un campo escalar de ejemplo:
///     f(x,y) = sin(x) + cos(y)
/// y dibuja varias curvas de nivel.
/// </summary>
public class DoubleWireStreamline : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private int columns = 80;      // Número de celdas en X
    [SerializeField] private int rows = 80;         // Número de celdas en Y

    [Header("Field Example")]
    [SerializeField] private float xMin = -6.28318f;
    [SerializeField] private float xMax =  6.28318f;
    [SerializeField] private float yMin = -6.28318f;
    [SerializeField] private float yMax =  6.28318f;

    [Header("Contour Appearance")]
    [SerializeField] private Color lineColor = Color.black;
    [SerializeField] private float lineWidth = 0.03f;
    [SerializeField] private Material lineMaterial;
    [SerializeField] private bool useWorldSpace = false;
    [SerializeField] private int sortingOrder = 10;

    [Header("Rendering")]
    [SerializeField] private bool clearOnStart = true;
    [SerializeField] private bool drawOnStart = true;

    [Header("Color Mapping")]
    [SerializeField] private Gradient colorGradient = new Gradient();
    [SerializeField] private int colorLevels = 50;
    [SerializeField] private float colorWidth = 0.015f;

    // Campo escalar en nodos: tamaño [columns+1, rows+1]
    private float[,] field;

    // Contenedor de líneas
    private Transform contourRoot;

    public void UpdateStreamlineField(List<float> x_h, List<float> y_h, List<float> I, int planesCount, float fieldZ, int resolution) {

        columns = (resolution > 0) ? resolution : columns;
        rows = (resolution > 0) ? resolution : rows; 

        gameObject.SetActive(true);
        ClearContours();
        float mu = 1f;

        float[,] Ax = ComputeAx(y_h, x_h, I, mu, columns, rows);
        GetMinMax(Ax, out float Amin, out float Amax);
        int Nlineas = 15;
        float[] niveles = Linspace(Amin, Amax, Nlineas + 2);

        DrawContours(Ax, niveles, planesCount, fieldZ);
    }

    public void UpdateMapColorField(List<float> x_h, List<float> y_h, List<float> I, int planesCount, float fieldZ, int resolution) {
        columns = (resolution > 0) ? resolution : columns;
        rows = (resolution > 0) ? resolution : rows;
        gameObject.SetActive(true);
        ClearContours();
        float mu = 1f;

        float[,] Ax = ComputeAx(y_h, x_h, I, mu, columns, rows);
        GetMinMax(Ax, out float Amin, out float Amax);
        int Nlineas = colorLevels;
        float[] niveles = Linspace(Amin, Amax, Nlineas);
        bool negativeIntensity = I[0] < 0f || I[1] < 0f;
        if (negativeIntensity) {
            colorGradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(Color.blue, 0.0f),
                    new GradientColorKey(Color.cyan, 0.25f),
                    new GradientColorKey(Color.green, 0.5f),
                    new GradientColorKey(Color.yellow, 0.75f),
                    new GradientColorKey(Color.red, 1.0f),
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(1.0f, 1.0f)
                }
            );
        } else {
            colorGradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.red, 0.0f),
                new GradientColorKey(Color.yellow, 0.25f),
                new GradientColorKey(Color.green, 0.5f),
                new GradientColorKey(Color.cyan, 0.75f),
                new GradientColorKey(Color.blue, 1.0f),
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(1.0f, 1.0f)
            }
        );
        }
        

        DrawMapColorContours(Ax, niveles, planesCount, fieldZ, Amin, Amax);
    }

    private float[,] ComputeAx(List<float> y_h, List<float> x_h, List<float> I, float mu, int cols, int rows) {
        int nx = cols + 1;
        int ny = rows + 1;

        float[,] Ax = new float[nx, ny];
        for (int i = 0; i < nx; i++) {
            for (int j = 0; j < ny; j++) {
                float x = Mathf.Lerp(xMin, xMax, (float)i / cols);
                float y = Mathf.Lerp(yMin, yMax, (float)j / rows);

                float value = 0f;
                for (int k = 0; k < y_h.Count; k++) {
                    float dy = y - y_h[k];
                    float dx = x - x_h[k];
                    
                    float r = Mathf.Sqrt(dy * dy + dx * dx);
                    if (r < 0.05f) continue;
                    value += (I[k]) * Mathf.Log(r);
                }

                Ax[i, j] = (mu / (2f * Mathf.PI)) * value;
            }
        }
        return Ax;
    }

    private void GetMinMax(float[,] data, out float min, out float max) {
        min = float.MaxValue;
        max = float.MinValue;

        int nx = data.GetLength(0);
        int ny = data.GetLength(1);

        for (int i = 0; i < nx; i++) {
            for (int j = 0; j < ny; j++) {
                float v = data[i, j];
                if (float.IsNaN(v)) continue;

                if (v < min) min = v;
                if (v > max) max = v;
            }
        }
    }

    private float[] Linspace(float min, float max, int n) {
        float[] result = new float[n];
        if (n == 1) {
            result[0] = min;
            return result;
        }

        float step = (max - min) / (n - 1);
        for (int i = 0; i < n; i++) {
            result[i] = min + i * step;
        }
        return result;
    }

    /// <summary>
    /// Dibuja curvas de nivel para todos los niveles indicados.
    /// </summary>
    public void DrawContours(float[,] scalarField, float[] contourLevels, int planesCount, float fieldZ)
    {
        if (scalarField == null)
        {
            Debug.LogError("Scalar field nulo.");
            return;
        }

        if (contourLevels == null || contourLevels.Length == 0)
        {
            Debug.LogError("No hay niveles de contorno.");
            return;
        }

        if (contourRoot == null)
        {
            GameObject root = new GameObject("Contours");
            root.transform.SetParent(transform, false);
            contourRoot = root.transform;
        }

        int nx = scalarField.GetLength(0) - 1;
        int ny = scalarField.GetLength(1) - 1;

        for (int p = 0; p < planesCount; p++) {
            float z = (planesCount == 1) ? fieldZ : Mathf.Lerp(-5f, 5f, (float)p / (planesCount - 1));
            for (int li = 0; li < contourLevels.Length; li++)
            {
                float level = contourLevels[li];
                List<Segment> segments = new List<Segment>();

                // Recorremos cada celda de la rejilla
                for (int ix = 0; ix < nx; ix++)
                {
                    for (int iy = 0; iy < ny; iy++)
                    {
                        // Esquinas de la celda
                        // p0 ---- p1
                        // |        |
                        // p3 ---- p2
                        Vector3 p0 = GridToLocal(ix,     iy + 1);
                        Vector3 p1 = GridToLocal(ix + 1, iy + 1);
                        Vector3 p2 = GridToLocal(ix + 1, iy);
                        Vector3 p3 = GridToLocal(ix,     iy);

                        float v0 = scalarField[ix,     iy + 1];
                        float v1 = scalarField[ix + 1, iy + 1];
                        float v2 = scalarField[ix + 1, iy];
                        float v3 = scalarField[ix,     iy];

                        // Bitmask Marching Squares
                        // bit 0 -> p0
                        // bit 1 -> p1
                        // bit 2 -> p2
                        // bit 3 -> p3
                        int mask = 0;
                        if (v0 >= level) mask |= 1;
                        if (v1 >= level) mask |= 2;
                        if (v2 >= level) mask |= 4;
                        if (v3 >= level) mask |= 8;

                        if (mask == 0 || mask == 15)
                            continue;

                        // Intersecciones sobre aristas
                        // e0: p0-p1 (arriba)
                        // e1: p1-p2 (derecha)
                        // e2: p3-p2 (abajo)
                        // e3: p0-p3 (izquierda)
                        Vector3 e0 = Interpolate(p0, p1, v0, v1, level, z);
                        Vector3 e1 = Interpolate(p1, p2, v1, v2, level, z);
                        Vector3 e2 = Interpolate(p3, p2, v3, v2, level, z);
                        Vector3 e3 = Interpolate(p0, p3, v0, v3, level, z);

                    // Casos estándar de Marching Squares
                        switch (mask)
                        {
                            case 1:   AddSegment(segments, e3, e0); break;
                            case 2:   AddSegment(segments, e0, e1); break;
                            case 3:   AddSegment(segments, e3, e1); break;
                            case 4:   AddSegment(segments, e1, e2); break;
                            case 5:   AddSegment(segments, e3, e0); AddSegment(segments, e1, e2); break; // ambiguo
                            case 6:   AddSegment(segments, e0, e2); break;
                            case 7:   AddSegment(segments, e3, e2); break;
                            case 8:   AddSegment(segments, e2, e3); break;
                            case 9:   AddSegment(segments, e0, e2); break;
                            case 10:  AddSegment(segments, e0, e1); AddSegment(segments, e2, e3); break; // ambiguo
                            case 11:  AddSegment(segments, e1, e2); break;
                            case 12:  AddSegment(segments, e1, e3); break;
                            case 13:  AddSegment(segments, e0, e1); break;
                            case 14:  AddSegment(segments, e0, e3); break;
                        }
                    }
                }

                // En esta versión simple dibujamos un LineRenderer por segmento.
                // Funciona bien para empezar. Más adelante puedes unir segmentos
                // para obtener polilíneas largas y menos GameObjects.
                for (int i = 0; i < segments.Count; i++)
                {
                    CreateLineRenderer(segments[i].a, segments[i].b, level, i);
                }
            }
        }
    }

    private void DrawMapColorContours(float[,] scalarField, float[] contourLevels, int planesCount, float fieldZ, float minValue, float maxValue) {
        if (scalarField == null)
        {
            Debug.LogError("Scalar field nulo.");
            return;
        }

        if (contourLevels == null || contourLevels.Length == 0)
        {
            Debug.LogError("No hay niveles de contorno.");
            return;
        }

        if (contourRoot == null)
        {
            GameObject root = new GameObject("Contours");
            root.transform.SetParent(transform, false);
            contourRoot = root.transform;
        }
        
        int nx = scalarField.GetLength(0) - 1;
        int ny = scalarField.GetLength(1) - 1;
        for (int p = 0; p < planesCount; p++) {
            float z = (planesCount == 1) ? fieldZ : Mathf.Lerp(-5f, 5f, (float)p / (planesCount - 1));
            foreach (float level in contourLevels) {
            List<Segment> segments = new List<Segment>();
            for (int ix = 0; ix < nx; ix++)
                {
                    for (int iy = 0; iy < ny; iy++)
                    {
                        // Esquinas de la celda
                        // p0 ---- p1
                        // |        |
                        // p3 ---- p2
                        Vector3 p0 = GridToLocal(ix,     iy + 1);
                        Vector3 p1 = GridToLocal(ix + 1, iy + 1);
                        Vector3 p2 = GridToLocal(ix + 1, iy);
                        Vector3 p3 = GridToLocal(ix,     iy);

                        float v0 = scalarField[ix,     iy + 1];
                        float v1 = scalarField[ix + 1, iy + 1];
                        float v2 = scalarField[ix + 1, iy];
                        float v3 = scalarField[ix,     iy];

                        // Bitmask Marching Squares
                        // bit 0 -> p0
                        // bit 1 -> p1
                        // bit 2 -> p2
                        // bit 3 -> p3
                        int mask = 0;
                        if (v0 >= level) mask |= 1;
                        if (v1 >= level) mask |= 2;
                        if (v2 >= level) mask |= 4;
                        if (v3 >= level) mask |= 8;

                        if (mask == 0 || mask == 15)
                            continue;

                        // Intersecciones sobre aristas
                        // e0: p0-p1 (arriba)
                        // e1: p1-p2 (derecha)
                        // e2: p3-p2 (abajo)
                        // e3: p0-p3 (izquierda)
                        Vector3 e0 = Interpolate(p0, p1, v0, v1, level, z);
                        Vector3 e1 = Interpolate(p1, p2, v1, v2, level, z);
                        Vector3 e2 = Interpolate(p3, p2, v3, v2, level, z);
                        Vector3 e3 = Interpolate(p0, p3, v0, v3, level, z);

                    // Casos estándar de Marching Squares
                        switch (mask)
                        {
                            case 1:   AddSegment(segments, e3, e0); break;
                            case 2:   AddSegment(segments, e0, e1); break;
                            case 3:   AddSegment(segments, e3, e1); break;
                            case 4:   AddSegment(segments, e1, e2); break;
                            case 5:   AddSegment(segments, e3, e0); AddSegment(segments, e1, e2); break; // ambiguo
                            case 6:   AddSegment(segments, e0, e2); break;
                            case 7:   AddSegment(segments, e3, e2); break;
                            case 8:   AddSegment(segments, e2, e3); break;
                            case 9:   AddSegment(segments, e0, e2); break;
                            case 10:  AddSegment(segments, e0, e1); AddSegment(segments, e2, e3); break; // ambiguo
                            case 11:  AddSegment(segments, e1, e2); break;
                            case 12:  AddSegment(segments, e1, e3); break;
                            case 13:  AddSegment(segments, e0, e1); break;
                            case 14:  AddSegment(segments, e0, e3); break;
                        }
                    }
                }
                float t = Mathf.InverseLerp(minValue, maxValue, level);
                Color lineColor = colorGradient.Evaluate(t);
                foreach (Segment seg in segments) {
                    CreateColouredLineRenderer(seg.a, seg.b, level, lineColor, fieldZ);
                }
        }
        }
    }
    private void CreateColouredLineRenderer(Vector3 a, Vector3 b, float level, Color lineColor, float z) {
        Vector3 positionA = new Vector3(a.x, a.y, z);
        Vector3 positionB = new Vector3(b.x, b.y, z);
        GameObject go = new GameObject($"Contour_{level:0.###}");  
        go.transform.SetParent(contourRoot, false);

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.colorGradient = colorGradient;
        lr.useWorldSpace = useWorldSpace;
        lr.positionCount = 2;
        lr.SetPosition(0, positionA);
        lr.SetPosition(1, positionB);
        lr.startWidth = colorWidth;
        lr.endWidth = colorWidth;

        lr.startColor = lineColor;
        lr.endColor = lineColor;
        // Material de respaldo. En URP/HDRP quizá prefieras asignarlo por Inspector.
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
            lr.material = new Material(shader);
        lr.sortingOrder = sortingOrder;
    }

    /// <summary>
    /// Borra todas las curvas previamente dibujadas.
    /// </summary>
    [ContextMenu("Clear Contours")]
    public void ClearContours()
    {
        Transform existing = transform.Find("Contours");
        if (existing != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(existing.gameObject);
            else
                Destroy(existing.gameObject);
#else
            Destroy(existing.gameObject);
#endif
        }

        contourRoot = null;
    }

    /// <summary>
    /// Convierte índices de la rejilla a coordenadas locales del objeto.
    /// </summary>
    private Vector3 GridToLocal(int ix, int iy)
    {
        float x = Mathf.Lerp(xMin, xMax, (float)ix / columns);
        float y = Mathf.Lerp(yMin, yMax, (float)iy / rows);
        return new Vector3(x, y, 0f);
    }

    /// <summary>
    /// Interpolación lineal sobre una arista para hallar el cruce con el nivel.
    /// </summary>
    private Vector3 Interpolate(Vector3 pA, Vector3 pB, float vA, float vB, float iso, float z)
    {
        float dv = vB - vA;

        if (Mathf.Abs(dv) < 1e-6f)
            return 0.5f * (pA + pB);

        float t = (iso - vA) / dv;
        t = Mathf.Clamp01(t);
        Vector3 interpolation = Vector3.Lerp(pA, pB, t);
        interpolation.z = z;
        return interpolation;
    }

    private void AddSegment(List<Segment> segments, Vector3 a, Vector3 b)
    {
        if ((a - b).sqrMagnitude < 1e-10f)
            return;

        segments.Add(new Segment(a, b));
    }

    private void CreateLineRenderer(Vector3 a, Vector3 b, float level, int index)
    {
        GameObject go = new GameObject($"Contour_{level:0.###}_{index}");
        go.transform.SetParent(contourRoot, false);

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = useWorldSpace;
        lr.positionCount = 2;
        lr.SetPosition(0, a);
        lr.SetPosition(1, b);

        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.startColor = lineColor;
        lr.endColor = lineColor;
        lr.loop = false;

        // Para proyectos 2D suele venir bien
        lr.numCapVertices = 2;
        lr.numCornerVertices = 2;
        lr.sortingOrder = sortingOrder;

        if (lineMaterial != null)
        {
            lr.material = lineMaterial;
        }
        else
        {
            // Material de respaldo. En URP/HDRP quizá prefieras asignarlo por Inspector.
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                lr.material = new Material(shader);
        }
    }

    private readonly struct Segment
    {
        public readonly Vector3 a;
        public readonly Vector3 b;

        public Segment(Vector3 a, Vector3 b)
        {
            this.a = a;
            this.b = b;
        }
    }
}