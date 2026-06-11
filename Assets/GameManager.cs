using UnityEngine;

public class GameManager : MonoBehaviour
{
    private void Awake()
    {
        Screen.SetResolution(1200, 1200, FullScreenMode.Windowed);
        Application.runInBackground = true;
        Application.targetFrameRate = 60;
    }

    void Start()
    {
        Application.runInBackground = true;
        Application.targetFrameRate = 60;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
