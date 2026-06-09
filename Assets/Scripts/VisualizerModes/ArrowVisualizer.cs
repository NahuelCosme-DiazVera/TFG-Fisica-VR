using UnityEngine;
using System.Collections.Generic;
/*public class ArrowData
{
        public GameObject arrowObject;
        public Transform arrowTransform;
        public Transform visualsTransform;
        public Renderer[] arrowRenderers;
        public MaterialPropertyBlock propertyBlock;
        public SpriteRenderer arrowSpriteRenderer;

        public ArrowData(GameObject obj, string prefab)
        {
            arrowObject = obj;
            arrowTransform = obj.transform;
            visualsTransform = arrowTransform.Find(prefab);
            arrowRenderers = obj.GetComponentsInChildren<Renderer>();
            propertyBlock = new MaterialPropertyBlock();
            arrowSpriteRenderer = obj.GetComponentInChildren<SpriteRenderer>();
        }

}*/

public class ArrowVisualizer : MonoBehaviour
{
/*    private VisualizerSettings settings;
    private List<ArrowData> arrowDataList = new List<ArrowData>();
    private Transform poolContainer;

    public void Initialize(VisualizerSettings settings)
    {
        this.settings = settings;
        poolContainer = new GameObject("ArrowPool").transform;
        poolContainer.SetParent(this.transform);
    }

    public void RefreshParameters()
    {
        int totalRequired = settings.gridpoints * settings.gridpoints;
        ManageArrowPooling(totalRequired);
    }

    private void ManageArrowPooling(int totalPointsNeeded)
    {
        while (arrowDataList.Count < totalPointsNeeded) {
            GameObject arrowObject = Instantiate(prefab, transform);
            arrowDataList.Add(new ArrowData(arrowObject, visuals));
        }

        for (int i = 0; i < arrowDataList.Count; i++) {
            if (i >= totalPointsNeeded && arrowDataList[i].arrowObject.activeSelf) {
                arrowDataList[i].arrowObject.SetActive(false);
            }
        }
    }

    public void UpdateVisuals(IMagneticField fieldProvider, VisualizerSettings settings)
    {
        if (settings.gridpoints < 2) settings.gridpoints = 2;
        int totalPoints = settings.gridpoints * settings.gridpoints * (disableSliding != null && disableSliding.isOn ? numPlanes : 1);

        ManageArrowPooling(arrowDataList, arrowDirections, totalPoints);
        ManageArrowPooling(isosurfaceArrowDataList, isosurfaceArrowDirections, 0);
        ManageArrowPooling(streamsliceArrowDataList, streamsliceArrowDirections, 0);

        float gridspacing = 3.1f / Mathf.Max(1, settings.gridpoints - 1);
        float dynamicArrowScale = Mathf.Max(minArrowScale, gridspacing * arrowScale);

        float[] x = linspace(-2.5f, 2.5f, settings.gridpoints);
        float[] y = linspace(2f, 7f, settings.gridpoints);
        float[] Bx = new float[settings.gridpoints * settings.gridpoints];
        float[] By = new float[settings.gridpoints * settings.gridpoints];

        float maxFieldMagnitude = magneticFieldProvider.GetMagneticFieldGrid(x, y, Bx, By);
        MaterialPropertyBlock tempPropertyBlock = new MaterialPropertyBlock();

        int arrowIndex = 0;
        int planesCount = disableSliding != null && disableSliding.isOn ? numPlanes : 1;
        float h = 0.95f;
        float b = 4*h - 1;
        float c = (-4)*h + 2;
        for (int p = 0; p < planesCount; p++) {
            float z = (planesCount == 1) ? fieldZcoordinate : Mathf.Lerp(-5f, 5f, (float)p / (planesCount - 1));
            for (int i = 0; i < settings.gridpoints; i++) {
                for (int j = 0; j < settings.gridpoints; j++) {
                    int arrowDataIndex = i * settings.gridpoints + j;
                    ArrowData arrowData = arrowDataList[arrowIndex];

                    float distance = CalculateArrowDistance(x[j], y[i], magneticFieldProvider.GetWireCoordinateX(), magneticFieldProvider.GetWireCoordinateY());
                    if (distance > minArrowDistance * minArrowDistance) {
                        if (!arrowData.arrowObject.activeSelf) {
                            arrowData.arrowObject.SetActive(true);
                        }

                        arrowData.arrowTransform.localPosition = new Vector3(x[j], y[i], z);
                        Vector3 arrowDirection = new Vector3(Bx[arrowDataIndex], By[arrowDataIndex], 0f);
                        arrowDirections[arrowIndex] = arrowDirection.normalized;

                        if (arrowDirection.magnitude > 1e-12f) {
                            arrowData.visualsTransform.localRotation = Quaternion.LookRotation(arrowDirection, Vector3.forward);
                        }

                        float currentMagnitude = Mathf.Sqrt(Bx[arrowDataIndex] * Bx[arrowDataIndex] + By[arrowDataIndex] * By[arrowDataIndex]);
                        
                        ArrowInfo arrowInfo = arrowData.arrowObject.GetComponentInChildren<ArrowInfo>();
                        if (arrowInfo != null) {
                            arrowInfo.magneticFieldMagnitude = currentMagnitude / 1e7f;
                            arrowInfo.arrowX = x[j];
                            arrowInfo.arrowY = y[i];

                            if (!arrowInfo.isBeingGrabbed()) {
                                arrowData.arrowTransform.localPosition = new Vector3(x[j], y[i], z);
                            }
                        }

                        float normalizedMagnitude = currentMagnitude / maxFieldMagnitude;
                        float t = b * normalizedMagnitude + c * normalizedMagnitude * normalizedMagnitude;
                        //Color interpolation based on field magnitude
                        Color arrowColor = Color.Lerp(minFieldColor, maxFieldColor, t);

                        if (arrowData.arrowSpriteRenderer != null) {
                            arrowData.arrowSpriteRenderer.color = arrowColor;
                            arrowData.visualsTransform.localScale = Vector3.one * dynamicArrowScale * spriteScale;
                        }
                        else {
                            tempPropertyBlock.Clear();
                            tempPropertyBlock.SetColor("_BaseColor", arrowColor);

                            int numRenderers = arrowData.arrowRenderers.Length;
                            for (int r = 0; r < numRenderers; r++) {
                                arrowData.arrowRenderers[r].SetPropertyBlock(tempPropertyBlock);
                            }

                            arrowData.visualsTransform.localScale = Vector3.one * dynamicArrowScale;   
                        }
                    }
                    else {
                        if (arrowData.arrowObject.activeSelf) {
                            arrowData.arrowObject.SetActive(false);
                        }
                    }
                    arrowIndex++;
                }
            }
        }
    }

    public void Activate() => gameObject.SetActive(true);
    public void Deactivate() => gameObject.SetActive(false);*/
}
