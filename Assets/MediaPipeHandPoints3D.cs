using UnityEngine;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Unity.Sample.HandLandmarkDetection;

public class MediaPipeHandPoints3D : MonoBehaviour
{
    public Transform pointPrefab;

    [Header("Placement")]
    public float xScale = 5f;
    public float yScale = 3f;
    public float zScale = 5f;
    public Vector3 offset;

    [Header("Points")]
    public bool showPoints = true;
    public float autoSphereSize = 0.035f;

    [Header("Hand Lines")]
    public bool showLines = true;
    public float lineWidth = 0.015f;
    public Material lineMaterial;

    private Transform[] points;
    private LineRenderer[] lines;

    private Vector3[] latestPositions;
    private Vector3[] writePositions;
    private bool hasNewData;
    private readonly object dataLock = new object();
    private bool pointsVisibilityInitialized;
    private bool linesVisibilityInitialized;
    private bool lineWidthInitialized;
    private bool layerInitialized;
    private bool lastShowPoints;
    private bool lastShowLines;
    private float lastLineWidth;
    private int lastLayer;

    private static readonly int[,] handConnections =
    {
        // Thumb
        {0,1},{1,2},{2,3},{3,4},

        // Index
        {0,5},{5,6},{6,7},{7,8},

        // Middle
        {0,9},{9,10},{10,11},{11,12},

        // Ring
        {0,13},{13,14},{14,15},{15,16},

        // Pinky
        {0,17},{17,18},{18,19},{19,20},

        // Palm
        {5,9},{9,13},{13,17},{17,0}
    };

    void OnEnable()
    {
        HandLandmarkerRunner.OnHandResult += OnHandResult;
    }

    void OnDisable()
    {
        HandLandmarkerRunner.OnHandResult -= OnHandResult;
    }

    void OnHandResult(HandLandmarkerResult result)
    {
        if (result.handLandmarks == null || result.handLandmarks.Count == 0)
            return;

        var landmarks = result.handLandmarks[0].landmarks;
        if (landmarks == null) return;

        EnsureWriteBuffer(landmarks.Count);

        for (int i = 0; i < landmarks.Count; i++)
        {
            var lm = landmarks[i];

            float x = (lm.x - 0.5f) * xScale;
            float y = -(lm.y - 0.5f) * yScale;
            float z = -lm.z * zScale;

            writePositions[i] = new Vector3(x, y, z) + offset;
        }

        lock (dataLock)
        {
            if (latestPositions == null || latestPositions.Length != landmarks.Count)
                latestPositions = new Vector3[landmarks.Count];

            var temp = latestPositions;
            latestPositions = writePositions;
            writePositions = temp;
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

        if (lines == null)
            CreateLines();

        UpdatePoints(positions);
        UpdateLines(positions);
    }

    void CreatePoints(int count)
    {
        points = new Transform[count];
        pointsVisibilityInitialized = false;
        layerInitialized = false;

        for (int i = 0; i < count; i++)
        {
            Transform p;

            if (pointPrefab != null)
            {
                p = Instantiate(pointPrefab, transform);
            }
            else
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.SetParent(transform, false);
                sphere.transform.localScale = Vector3.one * autoSphereSize;
                sphere.layer = gameObject.layer;
                p = sphere.transform;
            }

            p.name = "HandPoint_" + i;
            p.gameObject.layer = gameObject.layer;
            points[i] = p;
        }
    }

    void CreateLines()
    {
        lines = new LineRenderer[handConnections.GetLength(0)];
        linesVisibilityInitialized = false;
        lineWidthInitialized = false;

        for (int i = 0; i < lines.Length; i++)
        {
            GameObject obj = new GameObject("HandLine_" + i);
            obj.transform.SetParent(transform, false);
            obj.layer = gameObject.layer;

            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.numCapVertices = 2;

            if (lineMaterial != null)
                line.material = lineMaterial;
            else
                line.material = new Material(Shader.Find("Sprites/Default"));

            lines[i] = line;
        }
    }

    void UpdatePoints(Vector3[] positions)
    {
        bool pointsVisibilityChanged = !pointsVisibilityInitialized || lastShowPoints != showPoints;
        bool layerChanged = !layerInitialized || lastLayer != gameObject.layer;

        for (int i = 0; i < positions.Length; i++)
        {
            if (points[i] == null) continue;

            points[i].localPosition = positions[i];

            if (pointsVisibilityChanged)
                points[i].gameObject.SetActive(showPoints);

            if (layerChanged)
                points[i].gameObject.layer = gameObject.layer;
        }

        lastShowPoints = showPoints;
        lastLayer = gameObject.layer;
        pointsVisibilityInitialized = true;
        layerInitialized = true;
    }

    void UpdateLines(Vector3[] positions)
    {
        bool linesVisibilityChanged = !linesVisibilityInitialized || lastShowLines != showLines;
        bool widthChanged = !lineWidthInitialized || !Mathf.Approximately(lastLineWidth, lineWidth);
        bool applyWidth = widthChanged || linesVisibilityChanged;

        for (int i = 0; i < lines.Length; i++)
        {
            LineRenderer line = lines[i];

            if (linesVisibilityChanged)
                line.enabled = showLines;

            if (!showLines) continue;

            int a = handConnections[i, 0];
            int b = handConnections[i, 1];

            if (applyWidth)
            {
                line.startWidth = lineWidth;
                line.endWidth = lineWidth;
            }

            line.SetPosition(0, positions[a]);
            line.SetPosition(1, positions[b]);
        }

        lastShowLines = showLines;
        lastLineWidth = lineWidth;
        linesVisibilityInitialized = true;
        lineWidthInitialized = true;
    }

    void EnsureWriteBuffer(int count)
    {
        if (writePositions == null || writePositions.Length != count)
            writePositions = new Vector3[count];
    }
}
