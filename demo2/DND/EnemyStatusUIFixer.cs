using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 专门用于修复敌人状态UI的缩放和位置问题
/// 将此脚本附加到任何GameObject上
/// </summary>
public class EnemyStatusUIFixer : MonoBehaviour
{
    [Tooltip("是否在每一帧都检查敌人状态UI")]
    public bool checkEveryFrame = true;
    
    [Tooltip("检查间隔帧数（仅当checkEveryFrame为true时有效）")]
    public int checkInterval = 60;
    
    [Tooltip("要设置的缩放值")]
    public Vector3 targetScale = Vector3.one;
    
    [Tooltip("敌人头顶上方的偏移量")]
    public float heightOffset = 1.0f;
    
    [Tooltip("是否在控制台输出调试信息")]
    public bool debugLog = true;
    
    // 在Start中修复敌人状态UI
    void Start()
    {
        FixEnemyStatusUIs();
    }
    
    // 如果启用了每帧检查，则在Update中定期检查敌人状态UI
    void Update()
    {
        if (checkEveryFrame && Time.frameCount % checkInterval == 0)
        {
            FixEnemyStatusUIs();
        }
    }
    
    // 修复所有敌人状态UI
    void FixEnemyStatusUIs()
    {
        // 查找所有敌人状态UI
        GameObject[] enemyStatusUIs = GameObject.FindGameObjectsWithTag("EnemyStatus");
        
        if (enemyStatusUIs.Length == 0)
        {
            // 如果没有找到敌人状态UI，尝试通过名称查找
            Canvas[] allCanvases = FindObjectsOfType<Canvas>();
            foreach (Canvas canvas in allCanvases)
            {
                if (IsEnemyStatusUI(canvas.gameObject))
                {
                    FixEnemyStatusUI(canvas.gameObject);
                }
            }
            
            // 尝试通过RectTransform查找
            RectTransform[] allRectTransforms = FindObjectsOfType<RectTransform>();
            foreach (RectTransform rect in allRectTransforms)
            {
                if (IsEnemyStatusUI(rect.gameObject))
                {
                    FixEnemyStatusUI(rect.gameObject);
                }
            }
        }
        else
        {
            // 修复每个敌人状态UI
            foreach (GameObject statusUI in enemyStatusUIs)
            {
                FixEnemyStatusUI(statusUI);
            }
        }
    }
    
    // 修复单个敌人状态UI
    void FixEnemyStatusUI(GameObject statusUI)
    {
        // 修复缩放
        statusUI.transform.localScale = targetScale;
        
        if (debugLog)
            Debug.Log($"EnemyStatusUIFixer: 修复了 '{statusUI.name}' 的缩放为 {targetScale}");
        
        // 确保所有子对象都是激活的
        ActivateAllChildren(statusUI.transform);
        
        // 尝试修复位置
        TryFixPosition(statusUI);
        
        // 确保Canvas设置正确
        Canvas canvas = statusUI.GetComponent<Canvas>();
        if (canvas != null)
        {
            // 确保Canvas的排序顺序正确
            if (canvas.sortingOrder != 90)
            {
                canvas.sortingOrder = 90; // 比主UI的排序顺序(100)低，确保不会拦截主UI的点击事件
                
                if (debugLog)
                    Debug.Log($"EnemyStatusUIFixer: 设置 '{statusUI.name}' 的Canvas排序顺序为 90");
            }
            
            // 禁用GraphicRaycaster，防止拦截点击事件
            GraphicRaycaster raycaster = statusUI.GetComponent<GraphicRaycaster>();
            if (raycaster != null && raycaster.enabled)
            {
                raycaster.enabled = false;
                
                if (debugLog)
                    Debug.Log($"EnemyStatusUIFixer: 禁用了 '{statusUI.name}' 的GraphicRaycaster");
            }
        }
    }
    
    // 尝试修复敌人状态UI的位置
    void TryFixPosition(GameObject statusUI)
    {
        // 尝试找到关联的敌人
        Transform parent = statusUI.transform.parent;
        if (parent != null && parent.CompareTag("Enemy"))
        {
            // 获取敌人位置
            Vector3 enemyPosition = parent.position;
            
            // 设置UI位置在敌人头顶上方
            statusUI.transform.position = new Vector3(enemyPosition.x, enemyPosition.y + heightOffset, enemyPosition.z);
            
            if (debugLog)
                Debug.Log($"EnemyStatusUIFixer: 设置 '{statusUI.name}' 的位置在敌人 '{parent.name}' 头顶上方");
        }
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
                    Debug.Log($"EnemyStatusUIFixer: 激活了 '{child.name}' 对象");
            }
            
            // 递归处理子对象
            ActivateAllChildren(child);
        }
    }
}
