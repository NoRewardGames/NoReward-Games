using UnityEngine;
using System.IO;
using System;

#if UNITY_EDITOR
public class DeveloperTools : MonoBehaviour
{
    public static DeveloperTools Instance { get; private set; }

    [Header("Screenshot Settings")]
    [SerializeField] private KeyCode screenshotKey = KeyCode.F12;
    [SerializeField] private string screenshotFolder = "Screenshots";

    private int screenshotCount = 0;

    private void Awake()
    {
        screenshotCount = PlayerPrefs.GetInt("DevTools_ScreenshotCount", 0);

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(screenshotKey))
        {
            TakeScreenshot();
        }
    }

    private void TakeScreenshot()
    {
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string projectFolder = "Case File NV-51";
        string folderPath = Path.Combine(desktopPath, projectFolder, screenshotFolder);

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string fileName = $"Screenshot_{screenshotCount:D4}.png";
        string fullPath = Path.Combine(folderPath, fileName);

        ScreenCapture.CaptureScreenshot(fullPath);

        screenshotCount++;

        PlayerPrefs.SetInt("DevTools_ScreenshotCount", screenshotCount);
        PlayerPrefs.Save();

        Debug.Log($"Screenshot saved: {fullPath}");
    }
}

#endif