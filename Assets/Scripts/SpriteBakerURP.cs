using UnityEngine;
using System.IO;

public class NewMonoBehaviourScript : MonoBehaviour
{

    public int width = 512;
    public int height = 512;
    public string fileName = "FlechaSprite.png";

    [ContextMenu("Capture Screenshot")]
    void CaptureScreenshot() {
        Camera camera = GetComponent<Camera>();
        RenderTexture renderTexture = new RenderTexture(width, height, 32);
        camera.targetTexture = renderTexture;
        Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGBA32, false);
        camera.Render();

        RenderTexture.active = renderTexture;
        screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenShot.Apply();
        camera.targetTexture = null;
        RenderTexture.active = null;
        DestroyImmediate(renderTexture);

        byte[] bytes = screenShot.EncodeToPNG();
        string path = Path.Combine(Application.dataPath, fileName);
        File.WriteAllBytes(path, bytes);
        Debug.Log($"Screenshot saved to: {path}");
        #if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
        #endif
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
