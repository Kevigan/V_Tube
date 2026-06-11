using System.Collections.Generic;
using UnityEngine;
using Mediapipe.Tasks.Vision.FaceLandmarker;
using Mediapipe.Unity.Sample.FaceLandmarkDetection;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MediaPipeFacePoints3D : MonoBehaviour
{
    public Transform pointPrefab;

    [Header("Placement")]
    public float xScale = 5f;
    public float yScale = 3f;
    public float zScale = 5f;
    public Vector3 offset;

    [Header("Points")]
    public bool showPoints = true;
    public float autoSphereSize = 0.025f;

    [Header("Face Surface")]
    public bool showFaceSurface = true;
    public bool doubleSided = true;
    public Material faceMaterial;

    [Header("Feature Lines")]
    public bool showFeatureLines = true;
    public bool showEyeLines = true;
    public bool showEyebrowLines = true;
    public bool showMouthLines = true;
    public float featureLineWidth = 0.01f;
    public Material featureLineMaterial;

    [Header("Hat Tracking")]
    public Transform hatSpawnPoint;
    public Vector3 hatLocalOffset = new Vector3(0f, 0.45f, 0f);
    public Vector3 hatRotationOffset;
    public bool updateHatSpawnPoint = true;

    private LineRenderer[] featureLines;

    private Transform[] points;
    private Mesh faceMesh;
    private MeshRenderer meshRenderer;
    private int[] triangleIndices;

    private Vector3[] latestPositions;
    private bool hasNewData;
    private readonly object dataLock = new object();

    private static readonly int[,] featureConnections =
{
    // Left eye
    {33,7},{7,163},{163,144},{144,145},{145,153},{153,154},{154,155},{155,133},
    {133,173},{173,157},{157,158},{158,159},{159,160},{160,161},{161,246},{246,33},

    // Right eye
    {263,249},{249,390},{390,373},{373,374},{374,380},{380,381},{381,382},{382,362},
    {362,398},{398,384},{384,385},{385,386},{386,387},{387,388},{388,466},{466,263},

    // Eyebrows
    {70,63},{63,105},{105,66},{66,107},
    {336,296},{296,334},{334,293},{293,300},

    // Mouth outer
    {61,146},{146,91},{91,181},{181,84},{84,17},{17,314},{314,405},{405,321},
    {321,375},{375,291},{291,409},{409,270},{270,269},{269,267},{267,0},{0,37},
    {37,39},{39,40},{40,185},{185,61},

    // Mouth inner
    {78,95},{95,88},{88,178},{178,87},{87,14},{14,317},{317,402},{402,318},
    {318,324},{324,308},{308,415},{415,310},{310,311},{311,312},{312,13},
    {13,82},{82,81},{81,80},{80,191},{191,78}
};

    void Awake()
    {
        faceMesh = new Mesh();
        faceMesh.name = "MediaPipe Face Mesh";

        GetComponent<MeshFilter>().mesh = faceMesh;

        meshRenderer = GetComponent<MeshRenderer>();

        Material testMat = new Material(Shader.Find("Standard"));
        testMat = faceMaterial;
        meshRenderer.material = testMat;

        triangleIndices = LoadTrianglesFromResources();
    }

    void OnEnable()
    {
        FaceLandmarkerRunner.OnFaceResult += OnFaceResult;
    }

    void OnDisable()
    {
        FaceLandmarkerRunner.OnFaceResult -= OnFaceResult;
    }

    void OnFaceResult(FaceLandmarkerResult result)
    {
        if (result.faceLandmarks == null || result.faceLandmarks.Count == 0)
            return;

        var landmarks = result.faceLandmarks[0].landmarks;
        if (landmarks == null) return;

        Vector3[] temp = new Vector3[landmarks.Count];

        for (int i = 0; i < landmarks.Count; i++)
        {
            var lm = landmarks[i];

            float x = (lm.x - 0.5f) * xScale;
            float y = -(lm.y - 0.5f) * yScale;
            float z = -lm.z * zScale;

            temp[i] = new Vector3(x, y, z) + offset;
        }

        lock (dataLock)
        {
            latestPositions = temp;
            hasNewData = true;
        }
    }

    void Update()
    {
        Vector3[] positions = null;

        lock (dataLock)
        {
            if (hasNewData && latestPositions != null)
            {
                positions = latestPositions;
                hasNewData = false;
            }
        }

        if (positions == null) return;

        if (points == null || points.Length != positions.Length)
            CreatePoints(positions.Length);

        UpdatePoints(positions);
        UpdateFaceMesh(positions);
        UpdateFeatureLines(positions);
        UpdateHatSpawnPoint(positions);
    }

    void UpdatePoints(Vector3[] positions)
    {
        for (int i = 0; i < positions.Length; i++)
        {
            if (points[i] == null) continue;

            points[i].localPosition = positions[i];
            points[i].gameObject.SetActive(showPoints);
        }
    }

    void UpdateFaceMesh(Vector3[] positions)
    {
        meshRenderer.enabled = showFaceSurface;

        if (!showFaceSurface || triangleIndices == null || triangleIndices.Length < 3)
            return;

        faceMesh.Clear();
        faceMesh.vertices = positions;
        faceMesh.triangles = doubleSided ? MakeDoubleSided(triangleIndices) : triangleIndices;
        faceMesh.RecalculateNormals();
        faceMesh.RecalculateBounds();
    }

    void CreatePoints(int count)
    {
        points = new Transform[count];

        for (int i = 0; i < count; i++)
        {
            Transform p;

            if (pointPrefab != null)
            {
                p = Instantiate(pointPrefab, transform);
                p.gameObject.layer = gameObject.layer;
            }
            else
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.SetParent(transform, false);
                sphere.transform.localScale = Vector3.one * autoSphereSize;
                sphere.layer = gameObject.layer;
                p = sphere.transform;
                p.gameObject.layer = gameObject.layer;
            }

            p.name = "FacePoint_" + i;
            points[i] = p;
        }
    }

    int[] LoadTrianglesFromResources()
    {
        TextAsset txt = Resources.Load<TextAsset>("mediapipe_triangulation");

        if (txt == null)
        {
            Debug.LogError("Could not load Assets/Resources/mediapipe_triangulation.txt");
            return new int[0];
        }

        string text = txt.text;

        int start = text.IndexOf("[");
        int end = text.LastIndexOf("]");

        if (start < 0 || end < 0 || end <= start)
        {
            Debug.LogError("Could not find TRIANGULATION array brackets.");
            return new int[0];
        }

        string arrayOnly = text.Substring(start + 1, end - start - 1);

        string[] parts = arrayOnly.Split(',');

        List<int> values = new List<int>();

        foreach (string part in parts)
        {
            string cleaned = part.Trim();

            if (int.TryParse(cleaned, out int value))
                values.Add(value);
        }

        /*Debug.Log("Loaded triangle indices: " + values.Count);
        Debug.Log("Triangle count: " + values.Count / 3);
        Debug.Log("Triangle remainder: " + (values.Count % 3));*/

        /*if (values.Count > 0)
            Debug.Log("First triangle: " + values[0] + ", " + values[1] + ", " + values[2]);*/

        return values.ToArray();
    }

    int[] MakeDoubleSided(int[] source)
    {
        int validLength = source.Length - (source.Length % 3);

        int[] result = new int[validLength * 2];

        for (int i = 0; i < validLength; i += 3)
        {
            result[i] = source[i];
            result[i + 1] = source[i + 1];
            result[i + 2] = source[i + 2];

            int j = validLength + i;
            result[j] = source[i];
            result[j + 1] = source[i + 2];
            result[j + 2] = source[i + 1];
        }

        return result;
    }

    void UpdateFeatureLines(Vector3[] positions)
    {
        if (featureLines == null)
            CreateFeatureLines();

        for (int i = 0; i < featureLines.Length; i++)
        {
            bool visible = showFeatureLines;

            if (i < 32)
                visible &= showEyeLines;
            else if (i < 40)
                visible &= showEyebrowLines;
            else
                visible &= showMouthLines;

            LineRenderer line = featureLines[i];
            line.enabled = visible;

            if (!visible) continue;

            int a = featureConnections[i, 0];
            int b = featureConnections[i, 1];

            line.startWidth = featureLineWidth;
            line.endWidth = featureLineWidth;

            line.SetPosition(0, positions[a]);
            line.SetPosition(1, positions[b]);
        }
    }

    void CreateFeatureLines()
    {
        featureLines = new LineRenderer[featureConnections.GetLength(0)];

        for (int i = 0; i < featureLines.Length; i++)
        {
            GameObject obj = new GameObject("FeatureLine_" + i);
            obj.transform.SetParent(transform, false);
            obj.layer = gameObject.layer;

            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.startWidth = featureLineWidth;
            line.endWidth = featureLineWidth;
            line.numCapVertices = 2;

            if (featureLineMaterial != null)
                line.material = featureLineMaterial;
            else
                line.material = new Material(Shader.Find("Sprites/Default"));

            featureLines[i] = line;
        }
    }

    void UpdateHatSpawnPoint(Vector3[] positions)
    {
        if (!updateHatSpawnPoint || hatSpawnPoint == null) return;
        if (positions.Length <= 263) return;

        Vector3 leftEye = positions[33];
        Vector3 rightEye = positions[263];
        Vector3 forehead = positions[10];
        Vector3 chin = positions[152];

        Vector3 eyeCenter = (leftEye + rightEye) * 0.5f;

        Vector3 right = (rightEye - leftEye).normalized;
        Vector3 up = (forehead - chin).normalized;
        Vector3 forward = Vector3.Cross(right, up).normalized;

        if (forward == Vector3.zero) return;

        Quaternion faceRot = Quaternion.LookRotation(forward, up);

        hatSpawnPoint.localPosition = forehead + faceRot * hatLocalOffset;
        hatSpawnPoint.localRotation = faceRot * Quaternion.Euler(hatRotationOffset);
    }
}