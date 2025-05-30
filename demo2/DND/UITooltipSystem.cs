using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UITooltipSystem : MonoBehaviour
{
    // 单例实例
    public static UITooltipSystem Instance { get; private set; }
    
    [Header("UI引用")]
    public GameObject tooltipPanel;
    public Text tooltipText;
    
    [Header("设置")]
    public float defaultDisplayTime = 3.0f;
    public float fadeInTime = 0.2f;
    public float fadeOutTime = 0.5f;
    
    // 当前协程
    private Coroutine currentTooltipCoroutine;
    
    private void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
        // 初始隐藏提示面板
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }
    
    // 显示提示
    public void ShowTooltip(string message, float displayTime = -1)
    {
        if (tooltipPanel == null || tooltipText == null) return;
        
        // 如果已经有提示在显示，先停止
        if (currentTooltipCoroutine != null)
        {
            StopCoroutine(currentTooltipCoroutine);
        }
        
        // 使用默认显示时间（如果未指定）
        if (displayTime < 0)
        {
            displayTime = defaultDisplayTime;
        }
        
        // 启动显示协程
        currentTooltipCoroutine = StartCoroutine(ShowTooltipCoroutine(message, displayTime));
    }
    
    // 显示提示协程
    private IEnumerator ShowTooltipCoroutine(string message, float displayTime)
    {
        // 设置文本
        tooltipText.text = message;
        
        // 显示面板
        tooltipPanel.SetActive(true);
        
        // 淡入效果
        CanvasGroup canvasGroup = tooltipPanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            float elapsedTime = 0;
            
            while (elapsedTime < fadeInTime)
            {
                canvasGroup.alpha = elapsedTime / fadeInTime;
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            canvasGroup.alpha = 1;
        }
        
        // 显示指定时间
        yield return new WaitForSeconds(displayTime);
        
        // 淡出效果
        if (canvasGroup != null)
        {
            float elapsedTime = 0;
            
            while (elapsedTime < fadeOutTime)
            {
                canvasGroup.alpha = 1 - (elapsedTime / fadeOutTime);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            canvasGroup.alpha = 0;
        }
        
        // 隐藏面板
        tooltipPanel.SetActive(false);
        
        // 清除协程引用
        currentTooltipCoroutine = null;
    }
    
    // 隐藏提示
    public void HideTooltip()
    {
        if (currentTooltipCoroutine != null)
        {
            StopCoroutine(currentTooltipCoroutine);
            currentTooltipCoroutine = null;
        }
        
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }
}
