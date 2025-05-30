using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResetUIScales : MonoBehaviour
{
    // 在Start中重置所有UI元素的缩放
    void Start()
    {
        // 重置Canvas的缩放
        transform.localScale = Vector3.one;
        Debug.Log("已重置Canvas的缩放为(1,1,1)");
        
        // 重置所有子UI元素的缩放
        ResetUIElementScale(transform);
        
        // 特别检查并重置关键UI元素
        GameObject actionPanel = GameObject.Find("ActionPanel");
        if (actionPanel != null)
        {
            actionPanel.transform.localScale = Vector3.one;
            Debug.Log($"已重置actionPanel的缩放为(1,1,1)");
            
            // 设置actionPanel的RectTransform
            RectTransform actionPanelRect = actionPanel.GetComponent<RectTransform>();
            if (actionPanelRect != null)
            {
                actionPanelRect.anchorMin = new Vector2(0.5f, 0);
                actionPanelRect.anchorMax = new Vector2(0.5f, 0);
                actionPanelRect.pivot = new Vector2(0.5f, 0);
                actionPanelRect.anchoredPosition = new Vector2(0, 50);
                actionPanelRect.sizeDelta = new Vector2(600, 100);
                Debug.Log($"已设置actionPanel的RectTransform: anchoredPosition={actionPanelRect.anchoredPosition}, sizeDelta={actionPanelRect.sizeDelta}");
            }
        }
        
        GameObject spellPanel = GameObject.Find("SpellPanel");
        if (spellPanel != null)
        {
            spellPanel.transform.localScale = Vector3.one;
            Debug.Log($"已重置spellPanel的缩放为(1,1,1)");
        }
        
        GameObject targetSelectionPanel = GameObject.Find("TargetSelectionPanel");
        if (targetSelectionPanel != null)
        {
            targetSelectionPanel.transform.localScale = Vector3.one;
            Debug.Log($"已重置targetSelectionPanel的缩放为(1,1,1)");
        }
        
        // 确保Canvas设置正确
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvas.pixelPerfect = true;
            Debug.Log($"已设置Canvas的renderMode为ScreenSpaceOverlay，sortingOrder为{canvas.sortingOrder}");
        }
        
        // 确保CanvasScaler设置正确
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            Debug.Log($"已设置CanvasScaler属性: 参考分辨率={scaler.referenceResolution}, 匹配模式={scaler.matchWidthOrHeight}");
        }
    }
    
    // 递归重置UI元素及其子元素的缩放
    private void ResetUIElementScale(Transform parent)
    {
        foreach (Transform child in parent)
        {
            // 重置当前元素的缩放
            child.localScale = Vector3.one;
            
            // 如果是按钮，确保它是可交互的
            Button button = child.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = true;
            }
            
            // 递归处理子元素
            ResetUIElementScale(child);
        }
    }
}
