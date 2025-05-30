using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class UIAnimationHelper : MonoBehaviour
{
    [Header("按钮动画设置")]
    public float hoverScale = 1.1f;
    public float hoverDuration = 0.2f;
    public Ease hoverEase = Ease.OutBack;
    
    public float clickPunchStrength = 0.2f;
    public float clickPunchDuration = 0.3f;
    public int clickPunchVibrato = 1;
    public float clickPunchElasticity = 0.5f;
    
    [Header("面板动画设置")]
    public float panelFadeDuration = 0.3f;
    public Ease panelFadeEase = Ease.OutQuad;
    
    public float panelSlideDuration = 0.5f;
    public Ease panelSlideEase = Ease.OutQuint;
    public Vector2 panelSlideOffset = new Vector2(0, 50);
    
    // 为按钮添加悬停动画
    public void SetupButtonHoverAnimation(Button button)
    {
        if (button == null) return;
        
        // 添加事件触发器组件（如果没有）
        UnityEngine.EventSystems.EventTrigger trigger = button.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        }
        
        // 创建鼠标进入事件
        UnityEngine.EventSystems.EventTrigger.Entry enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
        enterEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => {
            button.transform.DOScale(hoverScale, hoverDuration).SetEase(hoverEase);
        });
        trigger.triggers.Add(enterEntry);
        
        // 创建鼠标退出事件
        UnityEngine.EventSystems.EventTrigger.Entry exitEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
        exitEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => {
            button.transform.DOScale(1f, hoverDuration).SetEase(hoverEase);
        });
        trigger.triggers.Add(exitEntry);
    }
    
    // 为按钮添加点击动画
    public void SetupButtonClickAnimation(Button button)
    {
        if (button == null) return;
        
        // 添加点击事件
        button.onClick.AddListener(() => {
            button.transform.DOPunchScale(Vector3.one * clickPunchStrength, clickPunchDuration, clickPunchVibrato, clickPunchElasticity);
        });
    }
    
    // 为面板添加淡入动画
    public void PlayPanelFadeIn(CanvasGroup panel)
    {
        if (panel == null) return;
        
        panel.alpha = 0;
        panel.gameObject.SetActive(true);
        panel.DOFade(1, panelFadeDuration).SetEase(panelFadeEase);
    }
    
    // 为面板添加淡出动画
    public void PlayPanelFadeOut(CanvasGroup panel)
    {
        if (panel == null) return;
        
        panel.DOFade(0, panelFadeDuration).SetEase(panelFadeEase).OnComplete(() => {
            panel.gameObject.SetActive(false);
        });
    }
    
    // 为面板添加滑入动画
    public void PlayPanelSlideIn(RectTransform panel, bool fromTop = true)
    {
        if (panel == null) return;
        
        // 设置初始位置
        Vector2 targetPosition = panel.anchoredPosition;
        Vector2 startPosition = targetPosition + (fromTop ? new Vector2(0, panelSlideOffset.y) : new Vector2(panelSlideOffset.x, 0));
        panel.anchoredPosition = startPosition;
        
        // 设置初始透明度
        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.DOFade(1, panelFadeDuration).SetEase(panelFadeEase);
        }
        
        // 执行滑入动画
        panel.gameObject.SetActive(true);
        panel.DOAnchorPos(targetPosition, panelSlideDuration).SetEase(panelSlideEase);
    }
    
    // 为面板添加滑出动画
    public void PlayPanelSlideOut(RectTransform panel, bool toTop = true)
    {
        if (panel == null) return;
        
        // 设置目标位置
        Vector2 startPosition = panel.anchoredPosition;
        Vector2 targetPosition = startPosition + (toTop ? new Vector2(0, panelSlideOffset.y) : new Vector2(panelSlideOffset.x, 0));
        
        // 设置透明度动画
        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.DOFade(0, panelFadeDuration).SetEase(panelFadeEase);
        }
        
        // 执行滑出动画
        panel.DOAnchorPos(targetPosition, panelSlideDuration).SetEase(panelSlideEase).OnComplete(() => {
            panel.gameObject.SetActive(false);
            panel.anchoredPosition = startPosition;
        });
    }
    
    // 为所有子按钮添加动画
    public void SetupAllButtonsInPanel(Transform panel)
    {
        if (panel == null) return;
        
        // 获取所有按钮
        Button[] buttons = panel.GetComponentsInChildren<Button>(true);
        
        // 为每个按钮添加动画
        foreach (var button in buttons)
        {
            SetupButtonHoverAnimation(button);
            SetupButtonClickAnimation(button);
        }
    }
}
