using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 伤害数字管理器 - 负责在角色头上显示伤害数字和Miss效果
/// </summary>
public class DamageNumberManager : MonoBehaviour
{
    [Header("伤害数字预制体")]
    public GameObject damageNumberPrefab;  // 伤害数字预制体
    public GameObject missTextPrefab;      // Miss文本预制体
    public GameObject healNumberPrefab;    // 治疗数字预制体（可选）

    [Header("显示设置")]
    public Canvas targetCanvas;            // 目标Canvas（用于显示伤害数字）
    public Camera mainCamera;              // 主摄像机
    public Vector3 worldOffset = new Vector3(0, 2f, 0);  // 世界坐标偏移（角色头上的位置）

    [Header("动画设置")]
    public float animationDuration = 1.5f; // 动画持续时间
    public float moveDistance = 100f;      // 向上移动的距离（像素）
    public AnimationCurve moveCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1)); // 移动曲线
    public AnimationCurve fadeCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0));  // 淡出曲线

    [Header("颜色设置")]
    public Color damageColor = Color.red;     // 伤害数字颜色
    public Color healColor = Color.green;     // 治疗数字颜色
    public Color missColor = Color.gray;      // Miss文本颜色
    public Color criticalColor = Color.yellow; // 暴击伤害颜色

    // 单例模式
    public static DamageNumberManager Instance { get; private set; }

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
            return;
        }

        // 自动获取主摄像机
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
            }
        }

        // 自动查找Canvas
        if (targetCanvas == null)
        {
            // 优先查找名为"DamageNumbers"的Canvas
            GameObject canvasObj = GameObject.Find("DamageNumbers");
            if (canvasObj != null)
            {
                targetCanvas = canvasObj.GetComponent<Canvas>();
            }

            // 如果没找到，查找任何Canvas
            if (targetCanvas == null)
            {
                targetCanvas = FindObjectOfType<Canvas>();
            }
        }
    }

    /// <summary>
    /// 显示伤害数字
    /// </summary>
    /// <param name="target">目标角色</param>
    /// <param name="damage">伤害值</param>
    /// <param name="isCritical">是否暴击</param>
    public void ShowDamageNumber(Transform target, int damage, bool isCritical = false)
    {
        if (damageNumberPrefab == null || target == null)
        {
            Debug.LogWarning("DamageNumberManager: 缺少必要的组件或目标");
            return;
        }

        Color textColor = isCritical ? criticalColor : damageColor;
        string text = damage.ToString();

        if (isCritical)
        {
            text = $"<size=120%>{damage}!</size>"; // 暴击使用更大字体和感叹号
        }

        ShowFloatingText(target, text, textColor, damageNumberPrefab);
    }

    /// <summary>
    /// 显示治疗数字
    /// </summary>
    /// <param name="target">目标角色</param>
    /// <param name="healing">治疗值</param>
    public void ShowHealingNumber(Transform target, int healing)
    {
        if (target == null) return;

        GameObject prefab = healNumberPrefab != null ? healNumberPrefab : damageNumberPrefab;
        string text = $"+{healing}";

        ShowFloatingText(target, text, healColor, prefab);
    }

    /// <summary>
    /// 显示Miss文本
    /// </summary>
    /// <param name="target">目标角色</param>
    public void ShowMissText(Transform target)
    {
        if (target == null) return;

        GameObject prefab = missTextPrefab != null ? missTextPrefab : damageNumberPrefab;
        ShowFloatingText(target, "MISS", missColor, prefab);
    }

    /// <summary>
    /// 显示浮动文本的核心方法
    /// </summary>
    /// <param name="target">目标角色</param>
    /// <param name="text">显示的文本</param>
    /// <param name="color">文本颜色</param>
    /// <param name="prefab">使用的预制体</param>
    private void ShowFloatingText(Transform target, string text, Color color, GameObject prefab)
    {
        if (targetCanvas == null || mainCamera == null || prefab == null)
        {
            Debug.LogWarning("DamageNumberManager: 缺少必要的组件");
            return;
        }

        // 计算世界位置
        Vector3 worldPosition = target.position + worldOffset;

        // 转换为屏幕坐标
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

        // 检查是否在屏幕内
        if (screenPosition.z < 0 || screenPosition.x < 0 || screenPosition.x > Screen.width ||
            screenPosition.y < 0 || screenPosition.y > Screen.height)
        {
            Debug.Log("目标在屏幕外，不显示伤害数字");
            return;
        }

        // 实例化预制体
        GameObject floatingTextObj = Instantiate(prefab, targetCanvas.transform);

        // 设置位置
        RectTransform rectTransform = floatingTextObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // 将屏幕坐标转换为Canvas坐标
            Vector2 canvasPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                targetCanvas.transform as RectTransform,
                screenPosition,
                targetCanvas.worldCamera,
                out canvasPosition);

            rectTransform.localPosition = canvasPosition;
        }

        // 设置文本内容和颜色
        Text textComponent = floatingTextObj.GetComponent<Text>();
        if (textComponent != null)
        {
            textComponent.text = text;
            textComponent.color = color;
        }

        // 启动动画
        StartCoroutine(AnimateFloatingText(floatingTextObj, rectTransform));
    }

    /// <summary>
    /// 浮动文本动画协程
    /// </summary>
    /// <param name="textObj">文本对象</param>
    /// <param name="rectTransform">RectTransform组件</param>
    private IEnumerator AnimateFloatingText(GameObject textObj, RectTransform rectTransform)
    {
        if (textObj == null || rectTransform == null) yield break;

        Vector3 startPosition = rectTransform.localPosition;
        Vector3 endPosition = startPosition + Vector3.up * moveDistance;

        Text textComponent = textObj.GetComponent<Text>();
        CanvasGroup canvasGroup = textObj.GetComponent<CanvasGroup>();

        // 如果没有CanvasGroup，添加一个
        if (canvasGroup == null)
        {
            canvasGroup = textObj.AddComponent<CanvasGroup>();
        }

        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            float normalizedTime = elapsedTime / animationDuration;

            // 位置动画
            Vector3 currentPosition = Vector3.Lerp(startPosition, endPosition, moveCurve.Evaluate(normalizedTime));
            rectTransform.localPosition = currentPosition;

            // 透明度动画
            float alpha = fadeCurve.Evaluate(normalizedTime);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 销毁对象
        Destroy(textObj);
    }

    /// <summary>
    /// 设置Canvas（如果需要动态更改）
    /// </summary>
    /// <param name="canvas">新的Canvas</param>
    public void SetCanvas(Canvas canvas)
    {
        targetCanvas = canvas;
        Debug.Log($"DamageNumberManager Canvas设置为: {(canvas != null ? canvas.name : "null")}");
    }

    /// <summary>
    /// 设置主摄像机（如果需要动态更改）
    /// </summary>
    /// <param name="camera">新的摄像机</param>
    public void SetMainCamera(Camera camera)
    {
        mainCamera = camera;
        Debug.Log($"DamageNumberManager Camera设置为: {(camera != null ? camera.name : "null")}");
    }
}
