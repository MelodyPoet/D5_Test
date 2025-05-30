using UnityEngine;

/// <summary>
/// 行走区域配置工具
/// 提供可视化界面来设置和调整行走区域
/// </summary>
public class WalkAreaConfigurator : MonoBehaviour
{
    [Header("行走区域设置")]
    [Tooltip("区域左下角X坐标")]
    public float areaMinX = -29f;

    [Tooltip("区域左下角Y坐标")]
    public float areaMinY = -3.995085f;

    [Tooltip("区域宽度")]
    public float areaWidth = 81f;

    [Tooltip("区域高度")]
    public float areaHeight = 3.5f;

    [Header("快速设置")]
    [Tooltip("使用当前摄像机视野作为区域")]
    public bool useCurrentCameraView = false;

    [Tooltip("区域边距（从摄像机边界向内缩进）")]
    public float cameraViewMargin = 2f;

    [Header("预设区域")]
    [Tooltip("选择预设的区域大小")]
    public AreaPreset selectedPreset = AreaPreset.Custom;

    public enum AreaPreset
    {
        Custom,
        Small,      // 小区域 20x10
        Medium,     // 中等区域 40x15
        Large,      // 大区域 80x20
        ExtraLarge  // 超大区域 120x30
    }

    [Header("实时预览")]
    [Tooltip("是否显示实时预览")]
    public bool showPreview = true;

    [Tooltip("预览颜色")]
    public Color previewColor = new Color(1f, 1f, 0f, 0.3f);

    private void Start()
    {
        // 从当前LevelData读取设置
        LoadCurrentSettings();
    }

    private void Update()
    {
        // 应用预设
        ApplyPreset();

        // 如果启用了摄像机视野设置
        if (useCurrentCameraView)
        {
            SetAreaFromCameraView();
        }
    }

    private void LoadCurrentSettings()
    {
        Rect currentRect = LevelData.WalkRect;
        areaMinX = currentRect.xMin;
        areaMinY = currentRect.yMin;
        areaWidth = currentRect.width;
        areaHeight = currentRect.height;
    }

    private void ApplyPreset()
    {
        switch (selectedPreset)
        {
            case AreaPreset.Small:
                areaWidth = 20f;
                areaHeight = 10f;
                break;
            case AreaPreset.Medium:
                areaWidth = 40f;
                areaHeight = 15f;
                break;
            case AreaPreset.Large:
                areaWidth = 80f;
                areaHeight = 20f;
                break;
            case AreaPreset.ExtraLarge:
                areaWidth = 120f;
                areaHeight = 30f;
                break;
            case AreaPreset.Custom:
                // 保持当前设置
                break;
        }
    }

    private void SetAreaFromCameraView()
    {
        if (Camera.main == null) return;

        // 获取摄像机的世界坐标边界
        Camera cam = Camera.main;
        float camHeight = 2f * cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        Vector3 camPos = cam.transform.position;

        // 设置区域（考虑边距）
        areaMinX = camPos.x - camWidth/2 + cameraViewMargin;
        areaMinY = camPos.y - camHeight/2 + cameraViewMargin;
        areaWidth = camWidth - 2 * cameraViewMargin;
        areaHeight = camHeight - 2 * cameraViewMargin;
    }

    /// <summary>
    /// 应用当前设置到LevelData
    /// </summary>
    [ContextMenu("应用设置")]
    public void ApplySettings()
    {
        // 注意：这只会在运行时生效，不会修改代码文件
        // 要永久保存，需要手动复制数值到LevelData.cs

        Rect newRect = new Rect(areaMinX, areaMinY, areaWidth, areaHeight);

        Debug.Log($"应用新的行走区域设置:");
        Debug.Log($"左下角: ({areaMinX:F2}, {areaMinY:F2})");
        Debug.Log($"右上角: ({areaMinX + areaWidth:F2}, {areaMinY + areaHeight:F2})");
        Debug.Log($"大小: {areaWidth:F2} x {areaHeight:F2}");
        Debug.Log($"请将以下代码复制到 LevelData.cs 第7行:");
        Debug.Log($"public static Rect WalkRect = new Rect({areaMinX}f, {areaMinY}f, {areaWidth}f, {areaHeight}f);");

        // 临时应用（仅运行时有效）
        System.Reflection.FieldInfo field = typeof(LevelData).GetField("WalkRect");
        if (field != null)
        {
            field.SetValue(null, newRect);
            Debug.Log("设置已临时应用（仅在当前运行时有效）");
        }
    }

    /// <summary>
    /// 重置为默认设置
    /// </summary>
    [ContextMenu("重置为默认")]
    public void ResetToDefault()
    {
        areaMinX = -29f;
        areaMinY = -3.995085f;
        areaWidth = 81f;
        areaHeight = 3.5f;
        selectedPreset = AreaPreset.Custom;
        useCurrentCameraView = false;
    }

    /// <summary>
    /// 居中区域到原点
    /// </summary>
    [ContextMenu("居中到原点")]
    public void CenterToOrigin()
    {
        areaMinX = -areaWidth / 2f;
        areaMinY = -areaHeight / 2f;
    }

    private void OnDrawGizmos()
    {
        if (!showPreview) return;

        // 绘制预览区域
        Gizmos.color = previewColor;
        Vector3 center = new Vector3(areaMinX + areaWidth/2, areaMinY + areaHeight/2, 0);
        Vector3 size = new Vector3(areaWidth, areaHeight, 0.1f);
        Gizmos.DrawCube(center, size);

        // 绘制边界线
        Gizmos.color = Color.yellow;
        Vector3 bottomLeft = new Vector3(areaMinX, areaMinY, 0);
        Vector3 bottomRight = new Vector3(areaMinX + areaWidth, areaMinY, 0);
        Vector3 topLeft = new Vector3(areaMinX, areaMinY + areaHeight, 0);
        Vector3 topRight = new Vector3(areaMinX + areaWidth, areaMinY + areaHeight, 0);

        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);

#if UNITY_EDITOR
        // 显示区域信息
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(center, $"行走区域\n{areaWidth:F1} x {areaHeight:F1}\n左下角: ({areaMinX:F1}, {areaMinY:F1})");
#endif
    }

    private void OnGUI()
    {
        if (!Application.isPlaying) return;

        GUILayout.BeginArea(new Rect(10, Screen.height - 200, 400, 190));
        GUILayout.BeginVertical("box");

        GUILayout.Label("行走区域配置工具");

        GUILayout.Label($"当前区域: {areaWidth:F1} x {areaHeight:F1}");
        GUILayout.Label($"左下角: ({areaMinX:F1}, {areaMinY:F1})");
        GUILayout.Label($"右上角: ({areaMinX + areaWidth:F1}, {areaMinY + areaHeight:F1})");

        GUILayout.Space(10);

        if (GUILayout.Button("应用设置"))
        {
            ApplySettings();
        }

        if (GUILayout.Button("重置为默认"))
        {
            ResetToDefault();
        }

        if (GUILayout.Button("居中到原点"))
        {
            CenterToOrigin();
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
