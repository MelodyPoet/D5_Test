using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 简单的UI缩放修复工具，将此脚本附加到任何GameObject上
/// 它会自动查找并修复所有UI元素的缩放问题
/// </summary>
public class UIScaleFixer : MonoBehaviour
{
    [Tooltip("是否在每一帧都检查UI缩放")]
    public bool checkEveryFrame = false;
    
    [Tooltip("检查间隔帧数（仅当checkEveryFrame为true时有效）")]
    public int checkInterval = 300;
    
    [Tooltip("要设置的缩放值")]
    public Vector3 targetScale = Vector3.one;
    
    [Tooltip("是否在控制台输出调试信息")]
    public bool debugLog = true;
    
    // 在Start中修复所有UI元素的缩放
    void Start()
    {
        FixAllUIScales();
    }
    
    // 如果启用了每帧检查，则在Update中定期检查UI缩放
    void Update()
    {
        if (checkEveryFrame && Time.frameCount % checkInterval == 0)
        {
            FixAllUIScales();
        }
    }
    
    // 修复所有UI元素的缩放
    void FixAllUIScales()
    {
        // 修复所有Canvas
        FixCanvasScales();
        
        // 修复所有RectTransform
        FixRectTransformScales();
    }
    
    // 修复所有Canvas的缩放
    void FixCanvasScales()
    {
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        int fixedCount = 0;
        
        foreach (Canvas canvas in allCanvases)
        {
            // 检查是否需要修复缩放
            if (NeedsScaleFix(canvas.transform))
            {
                // 修复缩放
                canvas.transform.localScale = targetScale;
                fixedCount++;
                
                if (debugLog)
                    Debug.Log($"UIScaleFixer: 修复了Canvas '{canvas.name}' 的缩放为 {targetScale}");
            }
        }
        
        if (debugLog && fixedCount > 0)
            Debug.Log($"UIScaleFixer: 共修复了 {fixedCount} 个Canvas的缩放");
    }
    
    // 修复所有RectTransform的缩放
    void FixRectTransformScales()
    {
        RectTransform[] allRectTransforms = FindObjectsOfType<RectTransform>();
        int fixedCount = 0;
        
        foreach (RectTransform rect in allRectTransforms)
        {
            // 跳过Canvas的RectTransform，因为已经在FixCanvasScales中处理过
            if (rect.GetComponent<Canvas>() != null)
                continue;
            
            // 检查是否需要修复缩放
            if (NeedsScaleFix(rect))
            {
                // 修复缩放
                rect.localScale = targetScale;
                fixedCount++;
                
                if (debugLog)
                    Debug.Log($"UIScaleFixer: 修复了RectTransform '{rect.name}' 的缩放为 {targetScale}");
                
                // 如果是敌人状态UI，确保所有子对象都是激活的
                if (IsEnemyStatusUI(rect.gameObject))
                {
                    ActivateAllChildren(rect);
                }
            }
        }
        
        if (debugLog && fixedCount > 0)
            Debug.Log($"UIScaleFixer: 共修复了 {fixedCount} 个RectTransform的缩放");
    }
    
    // 检查是否需要修复缩放
    bool NeedsScaleFix(Transform transform)
    {
        // 如果缩放值不是(1,1,1)，则需要修复
        return transform.localScale != targetScale;
    }
    
    // 检查是否是敌人状态UI
    bool IsEnemyStatusUI(GameObject obj)
    {
        return obj.CompareTag("EnemyStatus") || 
               obj.name.Contains("EnemyStatus") || 
               obj.name.Contains("StatusTemplate");
    }
    
    // 激活所有子对象
    void ActivateAllChildren(Transform parent)
    {
        foreach (Transform child in parent)
        {
            if (!child.gameObject.activeSelf)
            {
                child.gameObject.SetActive(true);
                
                if (debugLog)
                    Debug.Log($"UIScaleFixer: 激活了 '{child.name}' 对象");
            }
            
            // 递归处理子对象
            ActivateAllChildren(child);
        }
    }
}
