#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 行走区域编辑器工具
/// 提供Unity编辑器窗口来设置行走区域
/// </summary>
public class WalkAreaEditor : EditorWindow
{
    private float areaMinX = -29f;
    private float areaMinY = -3.995085f;
    private float areaWidth = 81f;
    private float areaHeight = 3.5f;
    
    private bool useCurrentCameraView = false;
    private float cameraViewMargin = 2f;
    
    private enum AreaPreset
    {
        Custom,
        Small,      // 20x10
        Medium,     // 40x15
        Large,      // 80x20
        ExtraLarge  // 120x30
    }
    
    private AreaPreset selectedPreset = AreaPreset.Custom;

    [MenuItem("DND Tools/行走区域编辑器")]
    public static void ShowWindow()
    {
        WalkAreaEditor window = GetWindow<WalkAreaEditor>("行走区域编辑器");
        window.LoadCurrentSettings();
        window.Show();
    }

    private void LoadCurrentSettings()
    {
        Rect currentRect = LevelData.WalkRect;
        areaMinX = currentRect.xMin;
        areaMinY = currentRect.yMin;
        areaWidth = currentRect.width;
        areaHeight = currentRect.height;
    }

    private void OnGUI()
    {
        GUILayout.Label("行走区域设置", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 当前设置显示
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("当前设置", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("左下角", $"({areaMinX:F2}, {areaMinY:F2})");
        EditorGUILayout.LabelField("右上角", $"({areaMinX + areaWidth:F2}, {areaMinY + areaHeight:F2})");
        EditorGUILayout.LabelField("大小", $"{areaWidth:F2} x {areaHeight:F2}");
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // 预设选择
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("快速预设", EditorStyles.boldLabel);
        
        AreaPreset newPreset = (AreaPreset)EditorGUILayout.EnumPopup("预设大小", selectedPreset);
        if (newPreset != selectedPreset)
        {
            selectedPreset = newPreset;
            ApplyPreset();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // 摄像机视野设置
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("摄像机视野设置", EditorStyles.boldLabel);
        useCurrentCameraView = EditorGUILayout.Toggle("使用当前摄像机视野", useCurrentCameraView);
        if (useCurrentCameraView)
        {
            cameraViewMargin = EditorGUILayout.FloatField("边距", cameraViewMargin);
            if (GUILayout.Button("应用摄像机视野"))
            {
                SetAreaFromCameraView();
            }
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // 手动设置
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("手动设置", EditorStyles.boldLabel);
        areaMinX = EditorGUILayout.FloatField("左下角 X", areaMinX);
        areaMinY = EditorGUILayout.FloatField("左下角 Y", areaMinY);
        areaWidth = EditorGUILayout.FloatField("宽度", areaWidth);
        areaHeight = EditorGUILayout.FloatField("高度", areaHeight);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // 操作按钮
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("居中到原点"))
        {
            CenterToOrigin();
        }
        if (GUILayout.Button("重置为默认"))
        {
            ResetToDefault();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // 应用按钮
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("应用设置", EditorStyles.boldLabel);
        
        if (GUILayout.Button("保存到 LevelData.cs", GUILayout.Height(30)))
        {
            SaveToLevelData();
        }
        
        EditorGUILayout.HelpBox("这将直接修改 LevelData.cs 文件中的 WalkRect 值", MessageType.Info);
        EditorGUILayout.EndVertical();

        // 代码预览
        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("代码预览", EditorStyles.boldLabel);
        string codePreview = $"public static Rect WalkRect = new Rect({areaMinX}f, {areaMinY}f, {areaWidth}f, {areaHeight}f);";
        EditorGUILayout.SelectableLabel(codePreview, EditorStyles.textField, GUILayout.Height(20));
        EditorGUILayout.EndVertical();
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
        }
    }

    private void SetAreaFromCameraView()
    {
        if (Camera.main == null)
        {
            EditorUtility.DisplayDialog("错误", "场景中没有找到主摄像机", "确定");
            return;
        }

        Camera cam = Camera.main;
        float camHeight = 2f * cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        Vector3 camPos = cam.transform.position;

        areaMinX = camPos.x - camWidth/2 + cameraViewMargin;
        areaMinY = camPos.y - camHeight/2 + cameraViewMargin;
        areaWidth = camWidth - 2 * cameraViewMargin;
        areaHeight = camHeight - 2 * cameraViewMargin;
    }

    private void CenterToOrigin()
    {
        areaMinX = -areaWidth / 2f;
        areaMinY = -areaHeight / 2f;
    }

    private void ResetToDefault()
    {
        areaMinX = -29f;
        areaMinY = -3.995085f;
        areaWidth = 81f;
        areaHeight = 3.5f;
        selectedPreset = AreaPreset.Custom;
        useCurrentCameraView = false;
    }

    private void SaveToLevelData()
    {
        string levelDataPath = "Assets/LevelData.cs";
        
        if (!File.Exists(levelDataPath))
        {
            EditorUtility.DisplayDialog("错误", "找不到 LevelData.cs 文件", "确定");
            return;
        }

        try
        {
            string[] lines = File.ReadAllLines(levelDataPath);
            
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("public static Rect WalkRect"))
                {
                    lines[i] = $"    public static Rect WalkRect = new Rect({areaMinX}f, {areaMinY}f, {areaWidth}f, {areaHeight}f);";
                    break;
                }
            }
            
            File.WriteAllLines(levelDataPath, lines);
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("成功", "行走区域设置已保存到 LevelData.cs", "确定");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"保存失败: {e.Message}", "确定");
        }
    }
}
#endif
