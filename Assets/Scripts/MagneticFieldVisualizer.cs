using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using TMPro;

using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.InputSystem;
public class ArrowData
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

}

public class TrailData
{
    public GameObject trailObject;
    public Transform trailTransform;
    public Renderer trailRenderer;
    public LineRenderer trailLineRenderer;
    public MaterialPropertyBlock propertyBlock;
    public List<Vector3> positions;
    public float lifeTime;

    public TrailData(GameObject obj)
    {
        trailObject = obj;
        trailTransform = obj.transform;
        trailRenderer = obj.GetComponent<Renderer>();
        trailLineRenderer = obj.GetComponent<LineRenderer>();
        propertyBlock = new MaterialPropertyBlock();
        positions = new List<Vector3>();
        lifeTime = Random.value * 5f;
    }
}

public class StreamlineData
{
    public GameObject streamlineObject;
    public Transform streamlineTransform;
    public Renderer streamlineRenderer;
    public LineRenderer streamlineLineRenderer;
    public MaterialPropertyBlock propertyBlock;

    public StreamlineData(GameObject obj)
    {
        streamlineObject = obj;
        streamlineTransform = obj.transform;
        streamlineRenderer = obj.GetComponent<Renderer>();
        streamlineLineRenderer = obj.GetComponent<LineRenderer>();
        propertyBlock = new MaterialPropertyBlock();
    }
}

public class MagneticFieldVisualizer : MonoBehaviour
{
    [Range(20, 35)]
    public int gridpoints = 20;
    [Range(-5f, 5f)]
    public float fieldZcoordinate = 0f;
    public Toggle disableSliding;
    public Toggle switchIntensitySign;

    public Transform wireTransform;
    public GameObject arrowPrefab;
    public GameObject streamsliceArrowPrefab;
    [Range(0.125f, 1f)]
    public float arrowScale = 0.5f;
    [Range(0.05f, 0.4f)]
    public float spriteScale = 0.2f;
    
    public float minArrowScale = 0.2f;
    public float minArrowDistance = 0.2f;
    public Color minFieldColor = Color.blue;
    public Color maxFieldColor = Color.red;
    private enum FieldDisplayMode { Arrows, Trails, Streamlines, Streamslices, Colours };
    private FieldDisplayMode fieldDisplayMode = FieldDisplayMode.Arrows;

    private int numPlanes = 5;
    public LineRenderer borderRenderer;
    private List<GameObject> slides = new List<GameObject>();

    public GameObject trailPrefab;
    private int maxTrailLength = 15;
    public Material trailMaterial;

    private bool parametersChanged = true;

    private List<ArrowData> arrowDataList = new List<ArrowData>();
    private List<Vector3> arrowDirections = new List<Vector3>();

    private List<TrailData> trailDataList = new List<TrailData>();
    private MaterialPropertyBlock propertyBlock;
    private Vector3[] temporalPositions;
    private Material planeMaterial;

    private Material isosurfaceMaterial;

    private enum GeometryType { InfiniteWire, DoubleWire, Ring };
    public GameObject infiniteWireObject;
    public GameObject doubleWireObject;
    public GameObject ringObject;
    private IMagneticField magneticFieldProvider;
    [SerializeField] private MonoBehaviour sourceObject;

    private List<StreamlineData> streamlineList = new List<StreamlineData>();
    public int streamlineSteps = 100;
    public float streamlineSize = 0.05f;
    private List<ArrowData> streamsliceArrowDataList = new List<ArrowData>();
    private List<Vector3> streamsliceArrowDirections = new List<Vector3>();

    public int sliceSteps = 10;
    public float sliceSize = 0.1f;
    public float minr0 = 0.2f;
    public float maxr0 = 2.5f;
    public int r0Count = 24;
    public int theta0Count = 32;
    public float streamsliceWidth = 0.02f;
    public float streamlineWidth = 0.015f;

    public int contourSteps = 80;
    public float contourStepSize = 0.05f;
    public float contourWidth = 0.05f;
    private Vector3[] contourPoints;
    
    private float lastChangeTime;
    private float changeCooldown = 0.5f;
    private bool isChanging = false;

    private Color minContourColor = Color.blue;
    private Color maxContourColor = Color.red;

    public DoubleWireStreamline dwStreamlineScript;
    public WireManagement wireManagementScript;
    private int lowResolutionDimension = 40;
    private int highResolutionDimension = 300;
    private int lowMapcolorResolution = 20;
    private int highMapcolorResolution = 40;

    private GameObject currentStreamlineInfo;
    private int selectedWireIndex = -1;
    private GameObject selectedWireObject;
    
    public NearFarInteractor rightNearFarInteractor;
    public NearFarInteractor leftNearFarInteractor;
    public InputActionProperty actionTrigger;
    public InputActionProperty wireManagementAction;
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable selectedWireGrabInteractable;

    private Vector3 cutPoint;
    void Awake() {
        magneticFieldProvider = sourceObject as IMagneticField;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        propertyBlock = new MaterialPropertyBlock();
        int maxArrows = 35 * 35 * numPlanes;

        temporalPositions = new Vector3[maxTrailLength + 1];
        planeMaterial = new Material(Shader.Find("Unlit/Color"));
        planeMaterial.color = Color.black;

        isosurfaceMaterial = trailMaterial;
        contourPoints = new Vector3[contourSteps];
        cutPoint = new Vector3(0f, 4.5f, fieldZcoordinate);
    }

    void Update()
    {
        if (parametersChanged) {
            lastChangeTime = Time.time;
            parametersChanged = false;
            isChanging = true;
            UpdateFieldDisplay(true);
        }
        if (isChanging && Time.time - lastChangeTime > changeCooldown) {
            UpdateFieldDisplay(false);
            isChanging = false;
        }
        bool isTriggerPressed = actionTrigger.action.WasPressedThisFrame();
        switch (fieldDisplayMode)
        {
            case FieldDisplayMode.Arrows:
                UpdateArrowRotations(arrowDataList, arrowDirections);
                break;
            case FieldDisplayMode.Streamslices:
                UpdateArrowRotations(streamsliceArrowDataList, streamsliceArrowDirections);
                break;
            case FieldDisplayMode.Trails:
                float intensity = 0f;
                if (magneticFieldProvider is InfiniteWireMagneticField wField) {
                    intensity = wField.GetWireIntensity();
                }
                else if (magneticFieldProvider is DoubleWireMagneticField doubleWField) {
                    intensity = doubleWField.GetTotalIntensity();
                }
                if (intensity != 0f) {
                    UpdateTrailField();
                    if (isTriggerPressed) {
                        SpawnParticleWithController();
                    }
                }
                break;
            case FieldDisplayMode.Streamlines:
                if (currentStreamlineInfo != null) {
                    UpdateStreamlineInfo();
                }
                else if (isTriggerPressed)
                {
                    HandleStreamlineInfo();
                }
                break;
        }
        bool isWireManagementPressed = wireManagementAction.action.WasPressedThisFrame();
        if (isWireManagementPressed && magneticFieldProvider is DoubleWireMagneticField dwField) {
            Ray ray = new Ray(leftNearFarInteractor.attachTransform.position, leftNearFarInteractor.attachTransform.forward);
            if (Physics.Raycast(ray, out RaycastHit hitInfo)) {
                Vector3 point = hitInfo.point;
                if (wireManagementScript != null) {
                    if (hitInfo.collider.CompareTag("Wire")) {
                        int index = hitInfo.collider.transform.GetSiblingIndex();
                        wireManagementScript.RemoveWire(index, dwField, doubleWireObject);

                        if (doubleWireObject.transform.childCount > 0) {
                            GameObject firstWire = doubleWireObject.transform.GetChild(0).gameObject;
                            SetDoubleWirePosition(0, firstWire.transform.position.x, firstWire.transform.position.y, firstWire, true);
                        }
                    }
                    else {
                        wireManagementScript.AddWire(point, 5f, dwField, doubleWireObject);
                    }
                }
                SetParametersChanged();
            }
        }

        bool leftHandTriggerPressed = leftNearFarInteractor.isSelectActive && leftNearFarInteractor.interactablesSelected.Contains(selectedWireGrabInteractable);
        if (leftHandTriggerPressed && magneticFieldProvider is InfiniteWireMagneticField) {
            selectedWireGrabInteractable.trackPosition = false;
            Vector3 anchor = cutPoint;
            Vector3 pointer = leftNearFarInteractor.attachTransform.position;
            Vector3 direction = (pointer - anchor).normalized;
            UpdateWireOrientation(direction, anchor); 
        }
        else {
            selectedWireGrabInteractable.trackPosition = true;
        }
    }

    private void UpdateWireOrientation(Vector3 dir, Vector3 anchor) {
        float halfLength = 5f;
        Vector3 newStart = anchor - dir * halfLength;
        Vector3 newEnd = anchor + dir * halfLength;
        wireTransform.forward = dir;
        LineRenderer wireRenderer = wireTransform.GetComponentInChildren<LineRenderer>();
        
        wireRenderer.SetPosition(0, newStart);
        wireRenderer.SetPosition(1, newEnd);
        SetParametersChanged();
    }

    public void UpdateFieldDisplay(bool lowResolution)
    {
        float intensity = 0f;
        if (magneticFieldProvider is InfiniteWireMagneticField wField) {
            intensity = wField.GetWireIntensity();
        }
        else if (magneticFieldProvider is DoubleWireMagneticField dwField) {
            intensity = dwField.GetTotalIntensity();
        }
        switch (fieldDisplayMode) {
            case FieldDisplayMode.Arrows:
                SetTrailsActive(false);
                SetStreamlinesActive(false);
                arrowScale = 1f;
                if (intensity != 0f) {
                    SetArrowsActive(true, arrowDataList);
                    UpdateArrowField();
                }
                else {
                    SetArrowsActive(false, arrowDataList);
                }
                break;
            case FieldDisplayMode.Trails:
                SetTrailsActive(true);
                SetArrowsActive(false, arrowDataList);
                SetStreamlinesActive(false);
                if (intensity != 0f) {
                    UpdateTrailField();
                }
                break;
            case FieldDisplayMode.Streamlines:
                SetArrowsActive(false, arrowDataList);
                SetTrailsActive(false);
                if (magneticFieldProvider is InfiniteWireMagneticField) {
                    streamlineSteps = 150;
                    streamlineSize = 0.012f;
                    minr0 = 0.25f;
                    maxr0 = 2.5f;
                    r0Count = 8;
                    theta0Count = 12;
                    streamlineWidth = 0.015f;

                    if (dwStreamlineScript != null) dwStreamlineScript.gameObject.SetActive(false);
                    if (intensity != 0f) {
                        UpdateStreamlineField();
                    }
                    else {
                        SetStreamlinesActive(false);
                    }
                }
                else if (magneticFieldProvider is DoubleWireMagneticField dwField) {
                    SetStreamlinesActive(false);
                    ManageArrowPooling(streamsliceArrowDataList, streamsliceArrowDirections, 0);

                    List<float> intensities = dwField.GetIntensities();
                    int planesCount = disableSliding != null && disableSliding.isOn ? numPlanes : 1;

                    int resolution = lowResolution ? lowResolutionDimension : highResolutionDimension;
                    if (dwStreamlineScript != null && intensity != 0f) {
                        dwStreamlineScript.UpdateStreamlineField(dwField.GetWireXCoordinates(), dwField.GetWireYCoordinates(), intensities, planesCount, fieldZcoordinate, resolution);
                    }
                    else {
                        dwStreamlineScript.gameObject.SetActive(false);
                    }
                }
                break;
            case FieldDisplayMode.Streamslices:
                SetArrowsActive(false, arrowDataList);
                SetTrailsActive(false);
                SetStreamlinesActive(false);
                arrowScale = 0.5f;
                minr0 = 0.5f; 
                r0Count = 10;
                theta0Count = 8;
                if (intensity != 0f) {
                    UpdateStreamsliceField();   
                }
                else {
                    SetArrowsActive(false, streamsliceArrowDataList);
                }
                break;
            case FieldDisplayMode.Colours:
                SetArrowsActive(false, arrowDataList);
                SetTrailsActive(false);
                SetStreamlinesActive(false);
                if (magneticFieldProvider is InfiniteWireMagneticField) {
                    minr0 = 0.5f;
                    r0Count = 10;
                    theta0Count = 10;
                    if (dwStreamlineScript != null) dwStreamlineScript.gameObject.SetActive(false);
                    UpdateContourField();
                }
                else if (magneticFieldProvider is DoubleWireMagneticField dwField) {
                    ManageArrowPooling(streamsliceArrowDataList, streamsliceArrowDirections, 0);
                    int planesCount = disableSliding != null && disableSliding.isOn ? numPlanes : 1;
                    
                    List<float> intensities = dwField.GetIntensities();
                    int resolution = lowResolution ? lowMapcolorResolution : highMapcolorResolution + 10 * (dwField.GetWireCount() - 2);
                    dwStreamlineScript.UpdateMapColorField(dwField.GetWireXCoordinates(), dwField.GetWireYCoordinates(), intensities, planesCount, fieldZcoordinate, resolution);
                }
                break;
        }
        UpdatePlanes();
    }

    void UpdateArrowRotations(List<ArrowData> dataList, List<Vector3> directions) {
        Camera mainCamera = Camera.main;
        Vector3 cameraPosition = mainCamera.transform.position;
        if (mainCamera != null) {
            for (int i = 0; i < dataList.Count; i++) {
                ArrowData arrowData = dataList[i];
                if (arrowData.arrowObject.activeSelf && directions[i] != Vector3.zero) {
                    Vector3 direction = directions[i];
                    if (direction.sqrMagnitude > 1e-12f) {
                        if (arrowData.arrowSpriteRenderer != null) {
                            arrowData.visualsTransform.LookAt(cameraPosition);
                            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                            float dot = Vector3.Dot(mainCamera.transform.forward, transform.forward);
                            if (dot > 0f) {
                                angle = -angle + 180f;
                            }
                            arrowData.visualsTransform.Rotate(0, 0, angle - 90f, Space.Self);
                        }
                        else {
                            Quaternion fieldRotation = Quaternion.LookRotation(direction, Vector3.forward);
                            Vector3 toCamera = cameraPosition - arrowData.visualsTransform.position;
                            Vector3 cameraProjection = Vector3.ProjectOnPlane(toCamera, direction);
                            if (cameraProjection.sqrMagnitude > 0.001f) {
                                float angle = Vector3.SignedAngle(fieldRotation * Vector3.down, cameraProjection, direction);
                                arrowData.visualsTransform.localRotation = fieldRotation * Quaternion.AngleAxis(angle, Vector3.forward);
                            }
                            else {
                                arrowData.visualsTransform.localRotation = fieldRotation;
                            }
                        }
                    }
                }
            }
        }
    }

    private void UpdateArrowField() {
        if (gridpoints < 2) gridpoints = 2;
        bool noSliding = disableSliding != null && disableSliding.isOn;
        int arrowDensity = noSliding ? 20 : gridpoints;
        int totalPoints = arrowDensity * arrowDensity * (noSliding ? numPlanes : 1);

        ManageArrowPooling(arrowDataList, arrowDirections, totalPoints);
        ManageArrowPooling(streamsliceArrowDataList, streamsliceArrowDirections, 0);

        float gridspacing = 3.1f / Mathf.Max(1, arrowDensity - 1);
        float dynamicArrowScale = Mathf.Max(minArrowScale, gridspacing * arrowScale);

        float halfLength = 5f;
        Vector3 wireStart = cutPoint - wireTransform.forward * halfLength;
        Vector3 wireEnd = cutPoint + wireTransform.forward * halfLength;
        Vector3 wireVec = (wireEnd - wireStart).normalized;

        float alfaHipotenuse = Mathf.Sqrt((wireStart.y - cutPoint.y) * (wireStart.y - cutPoint.y) + (wireStart.z - cutPoint.z) * (wireStart.z - cutPoint.z));
        float alfaCos = (-(wireStart.z - cutPoint.z) / alfaHipotenuse);
        float betaHipotenuse = Mathf.Sqrt((wireStart.x - cutPoint.x) * (wireStart.x - cutPoint.x) + (wireStart.z - cutPoint.z) * (wireStart.z - cutPoint.z));
        float betaCos = (-(wireStart.z - cutPoint.z) / betaHipotenuse);
        
        float[] x = linspace(-2.5f, 2.5f, arrowDensity);
        float[] y = linspace(2f, 7f, arrowDensity);
        float[] xTimesCosBeta = x.Select(xi => xi * betaCos).ToArray(); //(betaCos > 0.0001f) ?  : x;
        float[] yTimesCosAlfa = linspace((cutPoint.y - (cutPoint.y - 2f) * alfaCos), (cutPoint.y + (7f - cutPoint.y) * alfaCos), arrowDensity);
        float[] Bx = new float[arrowDensity * arrowDensity];
        float[] By = new float[arrowDensity * arrowDensity];
        float maxFieldMagnitude = 0f;
        if (magneticFieldProvider is InfiniteWireMagneticField wireField) {
                float wireX = wireField.GetWireCoordinateX();
                float wireY = wireField.GetWireCoordinateY();
                wireField.SetWireCoordinateX(cutPoint.x);
                wireField.SetWireCoordinateY(cutPoint.y);
                maxFieldMagnitude = wireField.GetMagneticFieldGrid(xTimesCosBeta, yTimesCosAlfa, Bx, By);
                wireField.SetWireCoordinateX(wireX);
                wireField.SetWireCoordinateY(wireY);
        }
        else if (magneticFieldProvider is DoubleWireMagneticField) {
            maxFieldMagnitude = magneticFieldProvider.GetMagneticFieldGrid(x, y, Bx, By);
        }
        float fixedMagnitude = 150f;
        MaterialPropertyBlock tempPropertyBlock = new MaterialPropertyBlock();

        int arrowIndex = 0;
        int planesCount = noSliding ? numPlanes : 1;
        float h = 0.95f;
        float b = 4*h - 1;
        float c = (-4)*h + 2;

        float currentMagnitude = 0f;
        Vector3 arrowDirection = Vector3.zero;
        for (int p = 0; p < planesCount; p++) {
            float z = (planesCount == 1) ? fieldZcoordinate : Mathf.Lerp(-5f, 5f, (float)p / (planesCount - 1));
            for (int i = 0; i < arrowDensity; i++) {
                for (int j = 0; j < arrowDensity; j++) {
                    int arrowDataIndex = i * arrowDensity + j;
                    ArrowData arrowData = arrowDataList[arrowIndex];

                    float distance = CalculateArrowDistance(x[j], y[i], magneticFieldProvider.GetWireCoordinateX(), magneticFieldProvider.GetWireCoordinateY());
                    if (distance > minArrowDistance * minArrowDistance) {
                        if (!arrowData.arrowObject.activeSelf) {
                            arrowData.arrowObject.SetActive(true);
                        }
                        Vector3 position = new Vector3(x[j], y[i], z);
                        arrowData.arrowTransform.localPosition = position;

                        if (magneticFieldProvider is InfiniteWireMagneticField wField) {
                            Vector3 vectorR = position - wireStart;
                            Vector3 vectorProjection = Vector3.Project(vectorR, wireVec);
                            Vector3 shortDist = vectorR - vectorProjection;
                            arrowDirection = Vector3.Cross(wireVec, shortDist).normalized;
                            if (betaCos > 0.0001f && alfaCos > 0.0001f) {
                                arrowDirection = wField.intensity >= 0 ? -arrowDirection : arrowDirection;
                            }
                            else if (wField.intensity < 0f) {
                                arrowDirection = -arrowDirection;
                            }
                        }
                        else if (magneticFieldProvider is DoubleWireMagneticField dwField) {
                            arrowDirection = new Vector3(Bx[arrowDataIndex], By[arrowDataIndex], 0f);
                        }
                        if (arrowDirection.magnitude > 1e-12f) {
                            arrowData.visualsTransform.localRotation = Quaternion.LookRotation(arrowDirection, wireVec);
                        }
                        arrowDirections[arrowIndex] = arrowDirection;

                        currentMagnitude = Mathf.Sqrt(Bx[arrowDataIndex] * Bx[arrowDataIndex] + By[arrowDataIndex] * By[arrowDataIndex]);

                        VRArrowInfo arrowInfo = arrowData.arrowObject.GetComponentInChildren<VRArrowInfo>();
                        if (arrowInfo != null) {
                            arrowInfo.magneticFieldMagnitude = currentMagnitude / 1e7f;
                            arrowInfo.Bx = Bx[arrowDataIndex] / 1e7f;
                            arrowInfo.By = By[arrowDataIndex] / 1e7f;
                            arrowInfo.arrowX = x[j];
                            arrowInfo.arrowY = y[i];

                            if (!arrowInfo.isBeingGrabbed()) {
                                arrowData.arrowTransform.localPosition = new Vector3(x[j], y[i], z);
                            }
                        }
                        float normalizedMagnitude = currentMagnitude / fixedMagnitude;
                        float t = b * normalizedMagnitude + c * normalizedMagnitude * normalizedMagnitude;
                        //Color interpolation based on field magnitude
                        Color arrowColor = Color.Lerp(minFieldColor, maxFieldColor, Mathf.Clamp01(t));

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

    private void UpdateTrailField() {
        bool noSliding = disableSliding != null && disableSliding.isOn;
        int planesCount = noSliding ? numPlanes : 1;
        int numParticlesPerPlane = (gridpoints * gridpoints) / 4;
        int numParticles = numParticlesPerPlane * planesCount;
        float step = 1.25f;

        ManageTrailPooling(numParticles);
        ManageArrowPooling(streamsliceArrowDataList, streamsliceArrowDirections, 0);

        for (int i = 0; i < trailDataList.Count; i++) {
            TrailData trailData = trailDataList[i];

            if (i >= numParticles) {
                if (trailData.trailObject.activeSelf) {
                    trailData.trailObject.SetActive(false);
                }
                continue;
            }

            if (!trailData.trailObject.activeSelf) {
                trailData.trailObject.SetActive(true);
            }

            int p = i / numParticlesPerPlane;
            float z = (planesCount == 1) ? fieldZcoordinate : Mathf.Lerp(-5f, 5f, (float)p / (planesCount - 1));

            trailData.lifeTime -= Time.deltaTime;
            if (trailData.lifeTime <= 0f || trailData.positions.Count == 0) {
                RespawnParticle(trailData, z);
                continue;
            }
            Vector3 lastPosition = trailData.positions[trailData.positions.Count - 1];

            Vector3 fieldDirection = magneticFieldProvider.GetMagneticFieldAt(lastPosition);
            Vector3 nextPosition = lastPosition + (fieldDirection.normalized * step * Time.deltaTime);
            nextPosition.z = z;

            trailData.positions.Add(nextPosition);
            if (trailData.positions.Count > maxTrailLength) {
                trailData.positions.RemoveAt(0);
            }
            trailData.trailTransform.localPosition = nextPosition;

            int positionCount = trailData.positions.Count;
            for (int j = 0; j < positionCount; j++) {
                temporalPositions[j] = trailData.positions[j];
            }
            trailData.trailLineRenderer.positionCount = positionCount;
            trailData.trailLineRenderer.SetPositions(temporalPositions);

            trailData.trailLineRenderer.startWidth = 0.05f;
            trailData.trailLineRenderer.endWidth = 0.00f;
            
            Color trailColor = Color.Lerp(minFieldColor, maxFieldColor, Mathf.Clamp01(fieldDirection.magnitude));
            trailData.trailRenderer.GetPropertyBlock(trailData.propertyBlock);
            trailData.propertyBlock.SetColor("_BaseColor", trailColor);
            trailData.trailRenderer.SetPropertyBlock(trailData.propertyBlock);

            trailData.trailLineRenderer.startColor = trailColor;
            trailData.trailLineRenderer.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
        }
        
    }

    private void SpawnParticleWithController() {
        Vector3 interactorPosition = rightNearFarInteractor.attachTransform.position;
        bool noSliding = disableSliding != null && disableSliding.isOn;
        int planesCount = noSliding ? numPlanes : 1;
        int numParticlesPerPlane = (gridpoints * gridpoints) / 4;
        int numParticles = numParticlesPerPlane * planesCount;

        TrailData trailData = null;
        float minLifeTime = float.MaxValue;
        for (int i = 0; i < numParticles && i < trailDataList.Count; i++) {
            TrailData td = trailDataList[i];
            if (!td.trailObject.activeSelf) {
                trailData = td;
                break;
            }
            else if (td.lifeTime < minLifeTime) {
                minLifeTime = td.lifeTime;
                trailData = td;
            }
        }
        if (trailData != null) {
            trailData.trailObject.SetActive(true);
            trailData.positions.Clear();
            Vector3 start = transform.InverseTransformPoint(interactorPosition);
            trailData.positions.Add(start);

            trailData.lifeTime = 10f;
            trailData.trailTransform.localPosition = start;
            trailData.trailLineRenderer.positionCount = 1;
            trailData.trailLineRenderer.SetPosition(0, start);
        }
    }

    private void UpdateStreamlineField() {
        List<Vector3> seeds = GenerateSeeds();
        bool noSliding = disableSliding != null && disableSliding.isOn;
        int planesCount = noSliding ? numPlanes : 1;
        ManageStreamlinePooling(seeds.Count * planesCount);
        ManageArrowPooling(streamsliceArrowDataList, streamsliceArrowDirections, 0);

        int stepsHalf = streamlineSteps / 2;
        for (int p = 0; p < planesCount; p++)
        {
            float z = (planesCount == 1) ? fieldZcoordinate : Mathf.Lerp(-5f, 5f, (float)p / (planesCount - 1));
            for (int i = 0; i < seeds.Count; i++)
            {
                int streamlineIndex = p * seeds.Count + i;
                StreamlineData streamlineData = streamlineList[streamlineIndex];
                streamlineData.streamlineObject.SetActive(true);
                
                LineRenderer streamlineRenderer = streamlineData.streamlineLineRenderer;
                Vector3 currentPosition = seeds[i];
                streamlineRenderer.positionCount = streamlineSteps;

                Vector3 backwardPosition = currentPosition;
                for (int s = stepsHalf; s >= 0; s--)
                {
                    streamlineRenderer.SetPosition(s, backwardPosition);
                    Vector3 Bfield = magneticFieldProvider.GetMagneticFieldAt(backwardPosition);
                    if (Bfield.sqrMagnitude >= 1e-6f)
                    {
                        backwardPosition -= Bfield.normalized * streamlineSize;
                        backwardPosition.z = z;
                    }
                    else
                    {
                        streamlineRenderer.positionCount = s;
                        break;
                    }
                }
                Vector3 forwardPosition = currentPosition;
                for (int s = stepsHalf + 1; s < streamlineSteps; s++)
                {
                    Vector3 Bfield = magneticFieldProvider.GetMagneticFieldAt(forwardPosition);
                    if (Bfield.sqrMagnitude >= 1e-6f)
                    {
                        forwardPosition += Bfield.normalized * streamlineSize;
                        forwardPosition.z = z;
                    }
                    else
                    {
                        streamlineRenderer.positionCount = s;
                        break;
                    }
                    streamlineRenderer.SetPosition(s, forwardPosition);
                }
                streamlineRenderer.startWidth = streamlineRenderer.endWidth = streamlineWidth;
            }
        }
    }

    private void HandleStreamlineInfo() {
        Vector3 interactorPosition = rightNearFarInteractor.attachTransform.position;
        Vector3 projectedPosition = new Vector3(interactorPosition.x, interactorPosition.y, fieldZcoordinate);
        StreamlineData closestStreamline = null;
        float minimumDistance = 0.5f;

        foreach (StreamlineData streamlineData in streamlineList)
        {
            if (streamlineData.streamlineObject.activeSelf)
            {
                LineRenderer lr = streamlineData.streamlineLineRenderer;
                for (int i = 0; i < lr.positionCount; i++)
                {
                    float distance = Vector3.Distance(projectedPosition, lr.GetPosition(i));
                    if (distance < minimumDistance)
                    {
                        minimumDistance = distance;
                        closestStreamline = streamlineData;
                    }
                }
            }
        }

        if (closestStreamline == null) 
        {
            if (dwStreamlineScript != null && dwStreamlineScript.gameObject.activeSelf)
            {
                LineRenderer[] dwLineRenderers = dwStreamlineScript.gameObject.GetComponentsInChildren<LineRenderer>();
                foreach (LineRenderer lr in dwLineRenderers)
                {
                    for (int i = 0; i < lr.positionCount; i++)
                    {
                        float distance = Vector3.Distance(projectedPosition, lr.GetPosition(i));
                        if (distance < minimumDistance)
                        {
                            minimumDistance = distance;
                            closestStreamline = new StreamlineData(lr.gameObject);
                        }
                    }
                }
            }
        }

        if (closestStreamline != null)
        {
            ShowStreamlineInfo(projectedPosition);
        }
    }

    private void ShowStreamlineInfo(Vector3 position)
    {
        if (currentStreamlineInfo == null)
        {
            currentStreamlineInfo = Instantiate(arrowPrefab, transform);
        }
        currentStreamlineInfo.SetActive(true);
        currentStreamlineInfo.transform.position = position;

        Vector3 Bfield = magneticFieldProvider.GetMagneticFieldAt(position);
        ArrowData arrowData = new ArrowData(currentStreamlineInfo, "SpriteFlecha");
        VRArrowInfo arrowInfo = currentStreamlineInfo.GetComponentInChildren<VRArrowInfo>();
        if (Bfield.sqrMagnitude > 1e-12f)
        {
            arrowData.visualsTransform.localRotation = Quaternion.LookRotation(Vector3.forward, Bfield);
        }
        if (arrowInfo != null)
        {
            arrowInfo.magneticFieldMagnitude = Bfield.magnitude / 1e7f;
            arrowInfo.Bx = Bfield.x / 1e7f;
            arrowInfo.By = Bfield.y / 1e7f;
            arrowInfo.arrowX = position.x;
            arrowInfo.arrowY = position.y;
        }
        float h = 0.95f;
        float b = 4*h - 1;
        float c = (-4)*h + 2;
        float normalizedMagnitude = Bfield.magnitude / 150f;
        float t = b * normalizedMagnitude + c * normalizedMagnitude * normalizedMagnitude;
        Color arrowColor = Color.Lerp(minFieldColor, maxFieldColor, Mathf.Clamp01(t));
        if (arrowData.arrowSpriteRenderer != null) {
            arrowData.visualsTransform.localScale = (Vector3.one * spriteScale) / 2f;
            arrowData.arrowSpriteRenderer.color = arrowColor;
        }
    }

    private void UpdateStreamlineInfo() {
        Vector3 Bfield = magneticFieldProvider.GetMagneticFieldAt(currentStreamlineInfo.transform.position);
        ArrowData arrowData = new ArrowData(currentStreamlineInfo, "SpriteFlecha");
        VRArrowInfo arrowInfo = currentStreamlineInfo.GetComponentInChildren<VRArrowInfo>();
        if (Bfield.sqrMagnitude > 1e-12f)
        {
            arrowData.visualsTransform.localRotation = Quaternion.LookRotation(Vector3.forward, Bfield);
        }
        if (arrowInfo != null)
        {
            arrowInfo.magneticFieldMagnitude = Bfield.magnitude / 1e7f;
            arrowInfo.Bx = Bfield.x / 1e7f;
            arrowInfo.By = Bfield.y / 1e7f;
        }
        float h = 0.95f;
        float b = 4*h - 1;
        float c = (-4)*h + 2;
        float normalizedMagnitude = Bfield.magnitude / 150f;
        float t = b * normalizedMagnitude + c * normalizedMagnitude * normalizedMagnitude;
        Color arrowColor = Color.Lerp(minFieldColor, maxFieldColor, Mathf.Clamp01(t));
        if (arrowData.arrowSpriteRenderer != null) {
            arrowData.arrowSpriteRenderer.color = arrowColor;
        }
    }
    
    private void UpdateStreamsliceField()
    {
        List<Vector3> seeds = GenerateSeeds();
        bool noSliding = disableSliding != null && disableSliding.isOn;
        int planesCount = noSliding ? numPlanes : 1;
        ManageStreamlinePooling(seeds.Count * planesCount);
        ManageArrowPooling(streamsliceArrowDataList, streamsliceArrowDirections, seeds.Count * planesCount);

        float maxFieldMagnitude = 0.1f;

        int index = 0;
        for (int p = 0; p < planesCount; p++)
        {
            float z = (planesCount == 1) ? fieldZcoordinate : Mathf.Lerp(-5, 5, (float)p / (planesCount - 1));
            foreach (Vector3 seedPosition in seeds)
            {

                StreamlineData streamlineData = streamlineList[index];
                streamlineData.streamlineObject.SetActive(true);
                LineRenderer streamlineRenderer = streamlineData.streamlineLineRenderer;
                
                streamlineRenderer.positionCount = (sliceSteps * 2) + 1;
                Vector3 position = new Vector3(seedPosition.x, seedPosition.y, z);

                Vector3 forwardPosition = position;
                for (int s = 0; s <= sliceSteps; s++)
                {
                    streamlineRenderer.SetPosition(sliceSteps + s, forwardPosition);
                    Vector3 Bfield = magneticFieldProvider.GetMagneticFieldAt(forwardPosition);
                    if (Bfield.sqrMagnitude >= 1e-6f)
                    {
                        forwardPosition += Bfield.normalized * sliceSize;
                        forwardPosition.z = z;
                    }

                }
                Vector3 backwardPosition = position;
                for (int s = 0; s <= sliceSteps; s++)
                {
                    streamlineRenderer.SetPosition(sliceSteps - s, backwardPosition);
                    Vector3 Bfield = magneticFieldProvider.GetMagneticFieldAt(backwardPosition);
                    if (Bfield.sqrMagnitude >= 1e-6f)
                    {
                        backwardPosition -= Bfield.normalized * sliceSize;
                        backwardPosition.z = z;
                    }

                }
                streamlineRenderer.startWidth = streamlineRenderer.endWidth = streamsliceWidth;

                Vector3 B = magneticFieldProvider.GetMagneticFieldAt(position);
                maxFieldMagnitude = Mathf.Max(maxFieldMagnitude, B.magnitude);
                ArrowData arrowData = streamsliceArrowDataList[index];
                arrowData.arrowObject.SetActive(true);

                arrowData.arrowTransform.localPosition = position;
                streamsliceArrowDirections[index] = B.normalized;
                if (B.magnitude > 1e-12f)
                {
                    arrowData.visualsTransform.localRotation = Quaternion.LookRotation(B, Vector3.forward);
                }

                float t = Mathf.Clamp01(B.magnitude / (maxFieldMagnitude * 0.8f));
                Color arrowColor = Color.Lerp(minFieldColor, maxFieldColor, t);
                arrowData.arrowSpriteRenderer.color = arrowColor;
                streamlineRenderer.startColor = streamlineRenderer.endColor = arrowColor;
                arrowData.visualsTransform.localScale = Vector3.one * arrowScale * 0.4f * spriteScale;
                index++;
            }
        }

    }

    private void UpdateContourField()
    {
        List<Vector3> seeds = GenerateSeeds();
        bool noSliding = disableSliding != null && disableSliding.isOn;
        int planesCount = noSliding ? numPlanes : 1;
        ManageStreamlinePooling(seeds.Count * planesCount);
        ManageArrowPooling(streamsliceArrowDataList, streamsliceArrowDirections, 0);
        float maxFieldMagnitude = 0.5f;
        foreach (Vector3 seed in seeds)
        {
            Vector3 B = magneticFieldProvider.GetMagneticFieldAt(seed);
            maxFieldMagnitude = Mathf.Max(maxFieldMagnitude, B.magnitude);
        }

        if (contourPoints == null || contourPoints.Length < contourSteps)
        {
            contourPoints = new Vector3[contourSteps];
        }

        Gradient contourGradient = new Gradient();
                    
        contourGradient.SetKeys(
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

        for (int p = 0; p < planesCount; p++)
        {
            float z = (planesCount == 1) ? fieldZcoordinate : Mathf.Lerp(-5f, 5f, (float)p / (planesCount - 1));
            for (int i = 0; i < seeds.Count; i++)
            {
                int streamlineIndex = p * seeds.Count + i;
                StreamlineData streamlineData = streamlineList[streamlineIndex];
                streamlineData.streamlineObject.SetActive(true);
                LineRenderer streamlineRenderer = streamlineData.streamlineLineRenderer; 
                streamlineRenderer.positionCount = contourSteps;
                Vector3 position = seeds[i];
                position.z = z;

                int numSteps = 0;

                for (int s = 0; s < contourSteps; s++)
                {
                    contourPoints[s] = position;
                    numSteps++;
                    Vector3 Bfield = magneticFieldProvider.GetMagneticFieldAt(position);

                    if (Bfield.sqrMagnitude >= 1e-6f)
                    {
                        position += Bfield.normalized * contourStepSize;
                        position.z = z;
                    }
                    else
                    {
                        break;
                    }
                }

                streamlineRenderer.positionCount = numSteps;
                streamlineRenderer.SetPositions(contourPoints);

                if (streamlineRenderer.material != null) {
                    float magnitude = magneticFieldProvider.GetMagneticFieldAt(position).magnitude;
                    float t = Mathf.Clamp01(magnitude / maxFieldMagnitude * 0.95f);
                    Color streamlineColor = contourGradient.Evaluate(t);
                    streamlineRenderer.colorGradient = contourGradient;

                    streamlineData.streamlineRenderer.GetPropertyBlock(streamlineData.propertyBlock);
                    streamlineData.propertyBlock.SetColor("_BaseColor", streamlineColor);
                    streamlineData.streamlineRenderer.SetPropertyBlock(streamlineData.propertyBlock);

                    streamlineRenderer.startColor = streamlineRenderer.endColor = streamlineColor;
                    streamlineRenderer.startWidth = streamlineRenderer.endWidth = contourWidth;
                }
            }
        }
    }

    List<Vector3> GenerateSeeds()
    {
        List<Vector3> seeds = new List<Vector3>();
        float[] r0 = logspace(Mathf.Log10(minr0), Mathf.Log10(maxr0), r0Count);
        float[] theta0 = linspace(0f, 2f * Mathf.PI, theta0Count);
        
        List<Vector2> wirePositions = new List<Vector2>();

        if (doubleWireObject != null && magneticFieldProvider is DoubleWireMagneticField)
        {
            for (int i = 0; i < doubleWireObject.transform.childCount; i++)
            {
                Transform child = doubleWireObject.transform.GetChild(i);
                wirePositions.Add(new Vector2(child.position.x, child.position.y));
            }
        }
        else
        {
            wirePositions.Add(new Vector2(magneticFieldProvider.GetWireCoordinateX(), magneticFieldProvider.GetWireCoordinateY()));
        }

        foreach (Vector2 wirePos in wirePositions)
        {
            float x0 = wirePos.x;
            float y0 = wirePos.y;
            foreach (float r in r0)
            {
                foreach (float angle in theta0)
                {
                    float x = x0 + r * Mathf.Cos(angle);
                    float y = y0 + r * Mathf.Sin(angle);
                    seeds.Add(new Vector3(x, y, fieldZcoordinate));
                }
            }
        }
        return seeds;
    }

    void UpdatePlanes() {
        bool noSliding = disableSliding != null && disableSliding.isOn;
        int planesCount = noSliding ? numPlanes : 1;
        borderRenderer.positionCount = (planesCount == 1) ? 5 : 5 *planesCount;
        int bordersIndex = 0;

        if (slides.Count > planesCount) {
            for (int i = planesCount; i < slides.Count; i++) {
                if (slides[i].activeSelf) {
                    slides[i].SetActive(false);
                }
            }
        }

        if (slides.Count < planesCount) {
            int slidesToCreate = planesCount - slides.Count;
            for (int i = 0; i < slidesToCreate; i++) {
                GameObject newSlide = GameObject.CreatePrimitive(PrimitiveType.Quad);
                newSlide.transform.parent = transform;
                newSlide.SetActive(false);
                slides.Add(newSlide);
            }
        }

        for (int p = 0; p < planesCount; p++) {
            GameObject currentSlide = slides[p];
            float z = (planesCount == 1) ? fieldZcoordinate : Mathf.Lerp(-5f, 5f, (float)p / (planesCount - 1));

            if (!currentSlide.activeSelf) {
                currentSlide.SetActive(true);
            }

            borderRenderer.SetPosition(bordersIndex, new Vector3(-2.6f, 1.9f, z));
            borderRenderer.SetPosition(bordersIndex + 1, new Vector3(2.6f, 1.9f, z));
            borderRenderer.SetPosition(bordersIndex + 2, new Vector3(2.6f, 7.1f, z));
            borderRenderer.SetPosition(bordersIndex + 3, new Vector3(-2.6f, 7.1f, z));
            borderRenderer.SetPosition(bordersIndex + 4, new Vector3(-2.6f, 1.9f, z));
            bordersIndex += 5;
            if (planesCount != 1) {
               currentSlide.SetActive(false);
            }
            else {
                currentSlide.SetActive(true);
            }
            currentSlide.transform.position = new Vector3(0f, 4.5f, z + 0.01f);
            currentSlide.transform.localScale = new Vector3(5.2f, 5.2f, 1f);
            Renderer slideRenderer = currentSlide.GetComponent<Renderer>();
                
            slideRenderer.sharedMaterial = planeMaterial;
            slideRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_BaseColor", Color.black);
            slideRenderer.SetPropertyBlock(propertyBlock);
            
            borderRenderer.enabled = true;
        }
    }

    void ManageArrowPooling(List<ArrowData> dataList, List<Vector3> directions, int totalPointsNeeded) {
        GameObject prefab = (fieldDisplayMode == FieldDisplayMode.Streamslices) ? streamsliceArrowPrefab : arrowPrefab;
        string visuals = (fieldDisplayMode == FieldDisplayMode.Streamslices) ? "SpritePunta" : "SpriteFlecha";
        while (dataList.Count < totalPointsNeeded) {
            GameObject arrowObject = Instantiate(prefab, transform);
            dataList.Add(new ArrowData(arrowObject, visuals));
            directions.Add(Vector3.zero);
        }

        for (int i = 0; i < dataList.Count; i++) {
            if (i >= totalPointsNeeded && dataList[i].arrowObject.activeSelf) {
                dataList[i].arrowObject.SetActive(false);
            }
        }
    }

    void ManageTrailPooling(int totalParticlesNeeded) {
        while (trailDataList.Count < totalParticlesNeeded) {
            GameObject trailObject = Instantiate(trailPrefab, transform);
            trailDataList.Add(new TrailData(trailObject));
        }
    }

    void ManageStreamlinePooling(int totalStreamlinesNeeded)
    {
        while (streamlineList.Count < totalStreamlinesNeeded)
        {
            GameObject streamlineObject = new GameObject("Streamline");
            streamlineObject.transform.parent = transform;
            LineRenderer lineRenderer = streamlineObject.AddComponent<LineRenderer>();
            lineRenderer.material = isosurfaceMaterial;
            lineRenderer.useWorldSpace = false;
            lineRenderer.alignment = LineAlignment.TransformZ;

            lineRenderer.numCornerVertices = 5;
            lineRenderer.numCapVertices = 5;

            streamlineList.Add(new StreamlineData(streamlineObject));
        }
        for (int i = 0; i < streamlineList.Count; i++)
        {
            if (i >= totalStreamlinesNeeded && streamlineList[i].streamlineObject.activeSelf)
            {
                streamlineList[i].streamlineObject.SetActive(false);
            }
        }
    }

    void RespawnParticle(TrailData trailData, float z) {
        float x = Random.Range(-2.5f, 2.5f);
        float y = Random.Range(2f, 7f);
        Vector3 startPosition = new Vector3(x, y, z);
       
        trailData.positions.Clear();
        trailData.positions.Add(startPosition);
        trailData.lifeTime = Random.Range(1.5f, 3f);
        trailData.trailTransform.localPosition = startPosition;
    }


    void SetArrowsActive(bool isActive, List<ArrowData> dataList) {
        foreach (ArrowData arrow in dataList) {
            arrow.arrowObject.SetActive(isActive);
        }
    }

    void SetTrailsActive(bool isActive) {
        foreach (TrailData trail in trailDataList) {
            trail.trailObject.SetActive(isActive);
        }
    }

    void SetStreamlinesActive(bool isActive) {
        foreach (StreamlineData streamline in streamlineList) {
            streamline.streamlineObject.SetActive(isActive);
        }
        if (dwStreamlineScript != null) {
            if (isActive) {
                dwStreamlineScript.gameObject.SetActive(true);
            }
            else {
                dwStreamlineScript.ClearContours();
                dwStreamlineScript.gameObject.SetActive(false);
            }
        }
    }

    void SetParametersChanged() {
        parametersChanged = true;
    }

    public void SetContourWidth(string newContourWidth)
    {
        contourWidth = float.TryParse(newContourWidth, out float parsedValue) ? parsedValue : 0;
        SetParametersChanged();
    }

    public void SetContourSize(string newContourSize)
    {
        contourStepSize = float.TryParse(newContourSize, out float parsedValue) ? parsedValue : 0;
        SetParametersChanged();
    }

    public void SetContourSteps(string newContourSteps)
    {
        int newValue = int.TryParse(newContourSteps, out int parsedValue) ? parsedValue : 0;
        if (contourSteps != newValue)
        {
            contourSteps = newValue;
        }
        SetParametersChanged();
    }

    public void SetStreamlineWidth(string newWidth)
    {
        streamlineWidth = float.TryParse(newWidth, out float parsedValue) ? parsedValue : 0;
        SetParametersChanged();
    }

    public void SetStreamsliceWidth(string newWidth) {
        streamsliceWidth = float.TryParse(newWidth, out float parsedValue) ? parsedValue : 0;
        SetParametersChanged();
    }

    public void SetSpriteScale(float newSpriteScale)
    {
        spriteScale = newSpriteScale;
        SetParametersChanged();
    }

    public void SetArrowScale(float newArrowScale) {
        arrowScale = newArrowScale;
        SetParametersChanged();
    }

    public void SetTheta0Count(string newTheta0Count)
    {
        int newValue = int.TryParse(newTheta0Count, out int parsedValue) ? parsedValue : 0;
        if (theta0Count != newValue) {
            theta0Count = newValue;
        }
        SetParametersChanged();
    }

    public void SetR0Count(string newR0Count)
    {
        int newValue = int.TryParse(newR0Count, out int parsedValue) ? parsedValue : 0;
        if (r0Count != newValue) {
            r0Count = newValue;
        }
        SetParametersChanged();
    }

    public void SetMaxr0(string newMaxr0)
    {
        maxr0 = float.TryParse(newMaxr0, out float parsedValue) ? parsedValue : 0f;
        SetParametersChanged();
    }

    public void SetMinr0(string newMinr0)
    {
        minr0 = float.TryParse(newMinr0, out float parsedValue) ? parsedValue : 0f;
        SetParametersChanged();
    }

    public void SetSliceSize(string newSliceSize)
    {
        sliceSize = float.TryParse(newSliceSize, out float parsedValue) ? parsedValue : 0;
        SetParametersChanged();
    }

    public void SetSliceSteps(string newSliceSteps)
    {
        int newValue = int.TryParse(newSliceSteps, out int parsedValue) ? parsedValue : 0;
        if (sliceSteps != newValue) {
            sliceSteps = newValue;
        }
        SetParametersChanged();
    }

    public void SetStreamlineSize(string newStreamlineSize)
    {
        streamlineSize = float.TryParse(newStreamlineSize, out float parsedValue) ? parsedValue : 0;
        SetParametersChanged();
    }

    public void SetStreamlineSteps(string newStreamlineSteps)
    {
        int newValue = int.TryParse(newStreamlineSteps, out int parsedValue) ? parsedValue : 0;
        if (streamlineSteps != newValue) {
            streamlineSteps = newValue;
        }
        SetParametersChanged();
    }

    public void SetMuR(float newMur) {
        magneticFieldProvider.SetMur(newMur);
        SetParametersChanged();
    }

    public void SetGeometryType(int newGeometry) {
        GeometryType geometryType = (GeometryType)newGeometry;
        if (sourceObject != null) {
            sourceObject.gameObject.SetActive(false);
        }
        switch (geometryType) {
                case GeometryType.InfiniteWire:
                    if (infiniteWireObject != null) {
                        sourceObject = infiniteWireObject.GetComponent<MonoBehaviour>();
                    }
                    break;
                case GeometryType.DoubleWire:
                    if (doubleWireObject != null) {
                        sourceObject = doubleWireObject.GetComponent<MonoBehaviour>();
                    }
                    break;
                case GeometryType.Ring:
                    if (ringObject != null) {
                        sourceObject = ringObject.GetComponents<MonoBehaviour>().FirstOrDefault(monob => monob is IMagneticField);
                        var lr = ringObject.GetComponent<LineRenderer>();
                        if (lr != null) {
                            lr.enabled = true;
                        }
                    }
                    break;
            }
        sourceObject.gameObject.SetActive(true);
        magneticFieldProvider = sourceObject as IMagneticField;
        SetParametersChanged();
    }

    public void SelectWire(int wireIndex, GameObject wireObject) {
        UnselectCurrentWire();
        selectedWireIndex = wireIndex;
        selectedWireObject = wireObject;

        Renderer renderer = selectedWireObject.GetComponent<Renderer>();
        if (renderer != null) {
            renderer.material.EnableKeyword("_EMISSION");
            renderer.material.SetColor("_EmissionColor", Color.gray);
        }
    }

    public void UnselectCurrentWire() {
        if (selectedWireObject != null) {
            Renderer renderer = selectedWireObject.GetComponent<Renderer>();
            if (renderer != null) {
                renderer.material.SetColor("_EmissionColor", Color.black);
            }
        }
    }

    public void SetSelectedWireIntensity(float newIntensity) {
        if (selectedWireIndex != -1) {
             if (switchIntensitySign != null && switchIntensitySign.isOn) {
                newIntensity = -newIntensity;
            }
            if (magneticFieldProvider is InfiniteWireMagneticField wireField) {
                SetParticleSystemVelocity(wireTransform.gameObject, newIntensity);
                wireField.SetWireIntensity(newIntensity);
            }
            else if (magneticFieldProvider is DoubleWireMagneticField doubleWireField) {
                doubleWireField.SetWireIntensity(selectedWireIndex, newIntensity);
                SetParticleSystemVelocity(selectedWireObject, newIntensity);
            }
            SetParametersChanged();
        }
    }

    private void SetParticleSystemVelocity(GameObject wireObject, float intensity)
    {
        if (wireObject != null)
        {
            ParticleSystem particleSystem = wireObject.GetComponentInChildren<ParticleSystem>();
            if (!particleSystem.isPlaying)
            {
                particleSystem.Play();
            }
            if (particleSystem != null)
            {
                ParticleSystem.MainModule main = particleSystem.main;
                main.simulationSpeed = Mathf.Abs(intensity) * 0.25f;

                if (intensity < 0)
                {
                    particleSystem.transform.localPosition = new Vector3(0, 0, -4.9f);
                    particleSystem.transform.localRotation = Quaternion.Euler(0, -90f, 0);
                }
                else
                {
                    particleSystem.transform.localPosition = new Vector3(0, 0, 4.9f);
                    particleSystem.transform.localRotation = Quaternion.Euler(0, 90f, 0);
                }

            }
        }
    }

    public void SetDisplayMode(int mode) {
        fieldDisplayMode = (FieldDisplayMode)mode;
        SetParametersChanged();
    }

    public void ToggleDisableSliding(bool isDisabled) {
        if (isDisabled) {
            disableSliding.isOn = true;
        }
        else {
            disableSliding.isOn = false; 
        }
        SetParametersChanged();
    }

    public void ToggleSwitchIntensitySign(bool switchSign) {
        if (switchIntensitySign != null) {
            if (switchSign) {
                switchIntensitySign.isOn = true;
            }
            else {
                switchIntensitySign.isOn = false;
            }
            float currentIntensity = 0f;
            if (magneticFieldProvider is InfiniteWireMagneticField wireField) {
                currentIntensity = wireField.GetWireIntensity();
                SetParticleSystemVelocity(wireTransform.gameObject, -currentIntensity);
                wireField.SetWireIntensity(-currentIntensity);
            }
            else if (magneticFieldProvider is DoubleWireMagneticField doubleWireField) {
                currentIntensity = doubleWireField.GetWireIntensity(selectedWireIndex);
                SetParticleSystemVelocity(selectedWireObject, -currentIntensity);
                doubleWireField.SetWireIntensity(selectedWireIndex, -currentIntensity);
            }
        }
        SetParametersChanged();
    }

    public void SetDoubleWirePosition(int wireIndex, float newX, float newY, GameObject wireObject, bool updateField)
    {
        if (magneticFieldProvider is DoubleWireMagneticField dwField)
        {
            dwField.SetXCoordinate(wireIndex, newX);
            dwField.SetYCoordinate(wireIndex, newY);

            wireObject.transform.position = new Vector3(newX, newY, 0f);
            UpdateDoubleWireLineRenderer(wireObject.transform);
        }
        if (updateField || (!updateField && fieldDisplayMode == FieldDisplayMode.Streamslices)) {
            SetParametersChanged();
        }
        else if (fieldDisplayMode == FieldDisplayMode.Arrows) {
            SetArrowsActive(false, arrowDataList);
        }
        else if (fieldDisplayMode == FieldDisplayMode.Streamlines || fieldDisplayMode == FieldDisplayMode.Colours) {
            dwStreamlineScript.gameObject.SetActive(false);
        }
    }

    private void UpdateDoubleWireLineRenderer(Transform wireTransform)
    {
        LineRenderer lr = wireTransform.GetComponentInChildren<LineRenderer>();
        if (lr != null)
        {
            lr.SetPosition(0, new Vector3(wireTransform.position.x, wireTransform.position.y, -5f));
            lr.SetPosition(1, new Vector3(wireTransform.position.x, wireTransform.position.y, 5f));
        }
    }

    public void SetWirePositionX(float newx0) {
        if (magneticFieldProvider is InfiniteWireMagneticField wireField) {
            wireField.x0 = newx0;
            if (wireTransform != null) {
                wireTransform.position = new Vector3(newx0, wireField.y0, wireTransform.position.z);
                LineRenderer lr = wireTransform.GetComponentInChildren<LineRenderer>();
                if (lr != null) {
                    float halfLength = 5f;
                    Vector3 wireStart = wireTransform.position - wireTransform.forward * halfLength;
                    Vector3 wireEnd = wireTransform.position + wireTransform.forward * halfLength;
                    lr.SetPosition(0, wireStart);
                    lr.SetPosition(1, wireEnd);
                }
            }
            cutPoint = new Vector3(newx0, wireField.y0, fieldZcoordinate);
        }
        SetParametersChanged();
    }

    public void SetWirePositionY(float newy0) {
        if (magneticFieldProvider is InfiniteWireMagneticField wireField) {
            wireField.y0 = newy0;
            if (wireTransform != null) {
                wireTransform.position = new Vector3(wireField.x0, newy0, wireTransform.position.z);
                LineRenderer lr = wireTransform.GetComponentInChildren<LineRenderer>();
                if (lr != null) {
                    float halfLength = 5f;
                    Vector3 wireStart = wireTransform.position - wireTransform.forward * halfLength;
                    Vector3 wireEnd = wireTransform.position + wireTransform.forward * halfLength;
                    lr.SetPosition(0, wireStart);
                    lr.SetPosition(1, wireEnd);
                }
            }
            cutPoint = new Vector3(wireField.x0, newy0, fieldZcoordinate);            
        }
        SetParametersChanged();
    }

    public void SetFieldZCoordinate(float newZ) {
        fieldZcoordinate = newZ;
        if (Mathf.Abs(wireTransform.forward.z) < 1e-6f) {
            cutPoint.z = newZ;
        }
        else {
            Vector3 p = new Vector3(magneticFieldProvider.GetWireCoordinateX(), magneticFieldProvider.GetWireCoordinateY(), 0f);
            Vector3 direction = wireTransform.forward;
            float t = (newZ - p.z) / direction.z;
            cutPoint = p + t * direction;
        }
        SetParametersChanged();
    }

    public void SetGridpoints(float npoints) {
        int newPoints = (int)npoints;
        if (gridpoints != newPoints) {
            gridpoints = newPoints;
        }
        SetParametersChanged();
    }

    float[] linspace(float start, float end, int npoints) {
        if (npoints <= 0) return new float[] {};
        if (npoints == 1) return new float[] {start};
        float[] result = new float[npoints];
        float step = (end - start) / (npoints - 1);
        for (int i = 0; i < npoints; i++) result[i] = start + step * i;
        return result;
    }

    float[] logspace(float start, float end, int npoints)
    {
        if (npoints <= 0) return new float[] {};
        if (npoints == 1) return new float[] {start};
        float[] result = new float[npoints];
        float step = (end - start) / (npoints - 1);
        for (int i = 0; i < npoints; i++) result[i] = Mathf.Pow(10, start + step * i);
        return result;
    }

    float CalculateArrowDistance(float x, float y, float x0, float y0) {
        float dx = x - x0;
        float dy = y - y0;
        return dx * dx + dy * dy;
    }
}
