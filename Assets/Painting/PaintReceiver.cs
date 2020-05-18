using System.Collections.Generic;
using System.IO;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class PaintReceiver : MonoBehaviour
{
    [SerializeField]
    private int m_Width = 28;
    [SerializeField]
    private int m_Height = 28;
    [SerializeField]
    private Color m_InitialColor = Color.black;

    private Texture2D m_NewTexture;
    private Color32[] m_CurrentTexture;
    
    private bool m_Dirty = false;

    public event System.Action<Texture2D> OnUpdatedTexture;

    [Header("Test Set")]
    [SerializeField]
    private string m_TestSetDir;

    private Dictionary<string, List<string>> m_TestTextures = new Dictionary<string, List<string>>();

    private void Awake()
    {
        ResetColor();
    }

    public void InitializeTestImages(string[] labels)
    {
        foreach(string label in labels)
            m_TestTextures[label] = new List<string>();

        foreach (string file in Directory.GetFiles(Path.Combine(Application.dataPath, m_TestSetDir)))
        {
            if (Path.GetExtension(file) != ".png")
                continue;

            foreach (string label in labels)
            {                
                if (file.Contains(label))
                {
                    m_TestTextures[label].Add(file);
                }
            }
        }
    }

    private void LateUpdate()
    {
        if (m_Dirty)
        {
            m_NewTexture.SetPixels32(m_CurrentTexture);
            m_NewTexture.Apply();

            OnUpdatedTexture?.Invoke(m_NewTexture);

            m_Dirty = false;
        }
    }

    public void CreateSplash(Vector2 uvPosition, Color color, float distance, float strength)
    {
        PaintOver(distance, strength, (Color32)color, uvPosition);
    }

    public void DrawLine(Vector2 startUVPosition, Vector2 endUVPosition, float startStampRotation, float endStampRotation, Color color, float distance, float strength, float spacing)
    {
        Vector2 uvDistance = endUVPosition - startUVPosition;

        Vector2 pixelDistance = new Vector2(Mathf.Abs(uvDistance.x) * m_Width, Mathf.Abs(uvDistance.y) * m_Height);

        int stampsNo = Mathf.FloorToInt((pixelDistance.magnitude / distance) / spacing) + 1;

        for (int i = 0; i <= stampsNo; i++)
        {
            float lerp = i / (float)stampsNo;

            Vector2 uvPosition = Vector2.Lerp(startUVPosition, endUVPosition, lerp);
            
            PaintOver(distance, strength, color, uvPosition);
        }
    }

    private void PaintOver(float distance, float strength, Color32 color, Vector2 uvPosition)
    {
        float centerX = uvPosition.x * m_Width;
        float centerY = uvPosition.y * m_Height;
        int paintStartPositionX = Mathf.RoundToInt(centerX - distance);
        int paintStartPositionY = Mathf.RoundToInt(centerY -distance);

        // Checking manually if int is bigger than 0 is faster than using Mathf.Clamp
        int paintStartPositionXClamped = paintStartPositionX;
        if (paintStartPositionXClamped < 0)
            paintStartPositionXClamped = 0;
        int paintStartPositionYClamped = paintStartPositionY;
        if (paintStartPositionYClamped < 0)
            paintStartPositionYClamped = 0;

        // Check manually if end position doesn't exceed texture size
        int paintEndPositionXClamped = Mathf.FloorToInt(centerX + distance * 2);
        if (paintEndPositionXClamped >= m_Width)
            paintEndPositionXClamped = m_Width;
        int paintEndPositionYClamped = Mathf.FloorToInt(centerY + distance * 2);
        if (paintEndPositionYClamped >= m_Height)
            paintEndPositionYClamped = m_Height;

        int totalWidth = paintEndPositionXClamped - paintStartPositionXClamped;
        int totalHeight = paintEndPositionYClamped - paintStartPositionYClamped;

        Color32 newColor = new Color32();
        Color32 textureColor;
        float alpha;
        int aChannel;

        for (int x = 0; x < totalWidth; x++)
        {
            for (int y = 0; y < totalHeight; y++)
            {
                float distanceFromCenter = Vector2.Distance(new Vector2(paintStartPositionXClamped + x, paintStartPositionYClamped + y), new Vector2(centerX, centerY));
                alpha = 1 - distanceFromCenter / distance;

                // There is no need to do further calculations if this stamp pixel is transparent
                if (alpha < 0.001f)
                    continue;

                alpha *= strength;

                int texturePosition = paintStartPositionXClamped + x + (paintStartPositionYClamped + y) * m_Width;
                
                aChannel = (int)(alpha * 255f);

                textureColor = m_CurrentTexture[texturePosition];

                newColor.r = (byte)(color.r * aChannel / 255 + textureColor.r * textureColor.a * (255 - aChannel) / (255 * 255));
                newColor.g = (byte)(color.g * aChannel / 255 + textureColor.g * textureColor.a * (255 - aChannel) / (255 * 255));
                newColor.b = (byte)(color.b * aChannel / 255 + textureColor.b * textureColor.a * (255 - aChannel) / (255 * 255));
                newColor.a = (byte)(aChannel + textureColor.a * (255 - aChannel) / 255);

                m_CurrentTexture[texturePosition] = newColor;
            }
        }

        m_Dirty = true;
    }

    public void ResetColor()
    {
        m_NewTexture = new Texture2D(m_Width, m_Height, TextureFormat.RGBA32, false, true);

        for (int y = 0; y < m_Height; y++)
        {
            for (int x = 0; x < m_Width; x++)
            {
                m_NewTexture.SetPixel(x, y, m_InitialColor);
            }
        }

        m_CurrentTexture = new Color32[m_Width * m_Height];
        m_NewTexture.GetPixels32().CopyTo(m_CurrentTexture, 0);

        m_NewTexture.filterMode = FilterMode.Point;

        GetComponent<MeshRenderer>().material.mainTexture = m_NewTexture;

        m_Dirty = true;
    }

    public void SetToTestTexture(string labelName)
    {
        if(m_TestTextures.ContainsKey(labelName) && m_TestTextures[labelName].Count > 0)
        {
            string randomTexture = m_TestTextures[labelName][Random.Range(0, m_TestTextures[labelName].Count)];
            if (!File.Exists(randomTexture))
                return;

            m_NewTexture = new Texture2D(m_Width, m_Height);
            
            m_NewTexture.LoadImage(File.ReadAllBytes(randomTexture));
            m_NewTexture.Apply();
            m_CurrentTexture = new Color32[m_Width * m_Height];
            m_NewTexture.GetPixels32().CopyTo(m_CurrentTexture, 0);

            m_NewTexture.filterMode = FilterMode.Point;

            GetComponent<MeshRenderer>().material.mainTexture = m_NewTexture;

            m_Dirty = true;
        }
    }
}
