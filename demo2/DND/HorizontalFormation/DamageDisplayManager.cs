using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

namespace demo2.DND.HorizontalFormation
{
    /// <summary>
    /// 伤害显示管理器 - 统一管理所有角色的伤害数字显示
    /// 直接使用UI预制体，无需额外组件依赖
    /// </summary>
    public class DamageDisplayManager : MonoBehaviour
    {
        [Header("预制体配置")]
        [Tooltip("伤害数字UI预制体 - 应该包含Text组件")]
        [SerializeField] private GameObject damageNumberPrefab;

        [Tooltip("Miss显示UI预制体 - 应该包含Text组件")]
        [SerializeField] private GameObject missPrefab;

        [Header("显示设置")]
        [Tooltip("UI Canvas")]
        [SerializeField] private Canvas uiCanvas;

        [Tooltip("角色头部偏移量")]
        [SerializeField] private Vector3 headOffset = new Vector3(0, 2f, 0);

        [Tooltip("随机偏移范围")]
        [SerializeField] private Vector2 randomOffset = new Vector2(0.5f, 0.3f);

        [Header("动画设置")]
        [Tooltip("向上浮动距离")]
        [SerializeField] private float floatHeight = 50f;

        [Tooltip("动画总时长")]
        [SerializeField] private float animationDuration = 1.5f;

        [Tooltip("淡出时长")]
        [SerializeField] private float fadeOutDuration = 0.5f;

        private static DamageDisplayManager instance;
        public static DamageDisplayManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<DamageDisplayManager>();
                }
                return instance;
            }
        }

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 显示伤害数字
        /// </summary>
        /// <param name="character">角色Transform</param>
        /// <param name="damage">伤害值</param>
        /// <param name="isDamage">是否为伤害（true）还是治疗（false）</param>
        public void ShowDamageNumber(Transform character, int damage, bool isDamage = true)
        {
            if (character == null || damageNumberPrefab == null || uiCanvas == null)
            {
                Debug.LogWarning("DamageDisplayManager: 缺少必要的组件引用");
                return;
            }

            // 创建伤害显示UI
            GameObject damageObj = CreateDamageUI(character, damageNumberPrefab);
            if (damageObj == null) return;

            // 配置文本内容和颜色
            Text textComponent = damageObj.GetComponentInChildren<Text>();
            if (textComponent != null)
            {
                textComponent.text = damage.ToString();
                textComponent.color = isDamage ? Color.red : Color.green;
            }
            else
            {
                Debug.LogWarning("伤害数字预制体中没有找到Text组件");
                Destroy(damageObj);
                return;
            }

            // 播放动画
            StartCoroutine(PlayDamageAnimation(damageObj, textComponent));
        }

        /// <summary>
        /// 显示MISS
        /// </summary>
        /// <param name="character">角色Transform</param>
        public void ShowMiss(Transform character)
        {
            if (character == null)
            {
                Debug.LogError("DamageDisplayManager.ShowMiss: character参数为空");
                return;
            }

            if (uiCanvas == null)
            {
                Debug.LogError("DamageDisplayManager.ShowMiss: uiCanvas未设置，请在Inspector中拖入Canvas");
                return;
            }

            // 优先使用MISS预制体，如果没有则使用伤害数字预制体
            GameObject prefabToUse = missPrefab != null ? missPrefab : damageNumberPrefab;

            // 如果都没有预制体，创建一个简单的文本显示
            if (prefabToUse == null)
            {
                Debug.LogWarning("没有配置预制体，创建默认MISS文本显示");
                ShowMissWithFallback(character);
                return;
            }

            Debug.Log($"显示MISS - 使用预制体: {prefabToUse.name}，角色: {character.name}");

            // 创建MISS显示UI
            GameObject missObj = CreateDamageUI(character, prefabToUse);
            if (missObj == null)
            {
                Debug.LogError("DamageDisplayManager.ShowMiss: 创建UI对象失败，使用备用方案");
                ShowMissWithFallback(character);
                return;
            }

            // 配置MISS文本
            Text textComponent = missObj.GetComponentInChildren<Text>();
            if (textComponent != null)
            {
                textComponent.text = "MISS";
                textComponent.color = Color.yellow;
                Debug.Log($"MISS文本设置成功: {textComponent.text}");
            }
            else
            {
                Debug.LogError($"MISS预制体 '{prefabToUse.name}' 中没有找到Text组件，使用备用方案");

                // 尝试查找所有可能的文本组件
                Text[] allTexts = missObj.GetComponentsInChildren<Text>(true);
                if (allTexts.Length > 0)
                {
                    Debug.Log($"找到 {allTexts.Length} 个Text组件，使用第一个");
                    textComponent = allTexts[0];
                    textComponent.text = "MISS";
                    textComponent.color = Color.yellow;
                }
                else
                {
                    Debug.LogError("预制体中完全没有找到可用的文本组件");
                    Destroy(missObj);
                    ShowMissWithFallback(character);
                    return;
                }
            }

            // 播放动画
            StartCoroutine(PlayDamageAnimation(missObj, textComponent));
        }

        /// <summary>
        /// 备用MISS显示方案 - 创建简单的文本显示
        /// </summary>
        private void ShowMissWithFallback(Transform character)
        {
            // 创建一个简单的文本GameObject
            GameObject missTextObj = new GameObject("MISS_Text");
            missTextObj.transform.SetParent(uiCanvas.transform, false);

            // 添加文本组件
            Text textComponent = missTextObj.AddComponent<Text>();
            textComponent.text = "MISS";
            textComponent.color = Color.yellow;
            textComponent.fontSize = 24;

            // 修复字体引用问题
            try
            {
                textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch
            {
                textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            textComponent.alignment = TextAnchor.MiddleCenter;

            // 设置RectTransform
            RectTransform rectTransform = missTextObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(100, 50);

            // 计算位置
            Vector3 worldPos = character.position + headOffset;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

            // 转换为UI坐标
            Vector2 uiPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                uiCanvas.transform as RectTransform,
                screenPos,
                uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main,
                out uiPos
            );

            rectTransform.anchoredPosition = uiPos;

            Debug.Log("使用备用方案创建MISS显示");

            // 播放动画
            StartCoroutine(PlayDamageAnimation(missTextObj, textComponent));
        }

        /// <summary>
        /// 创建伤害显示UI对象
        /// </summary>
        private GameObject CreateDamageUI(Transform character, GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("CreateDamageUI: 预制体为空");
                return null;
            }

            if (uiCanvas == null)
            {
                Debug.LogError("CreateDamageUI: uiCanvas为空");
                return null;
            }

            if (Camera.main == null)
            {
                Debug.LogError("CreateDamageUI: 主摄像机未找到，请确保场景中有标记为MainCamera的摄像机");
                return null;
            }

            try
            {
                // 计算世界位置
                Vector3 worldPos = character.position + headOffset;
                Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

                // 检查屏幕坐标是否有效
                if (screenPos.z < 0)
                {
                    Debug.LogWarning($"角色 {character.name} 在摄像机后方，屏幕坐标无效");
                    return null;
                }

                Debug.Log($"原始坐标 - 世界坐标: {worldPos}, 屏幕坐标: {screenPos}");

                // 创建UI对象
                GameObject uiObj = Instantiate(prefab, uiCanvas.transform);
                if (uiObj == null)
                {
                    Debug.LogError("Instantiate返回null，预制体创建失败");
                    return null;
                }

                RectTransform rectTransform = uiObj.GetComponent<RectTransform>();
                if (rectTransform == null)
                {
                    Debug.LogError($"预制体 {prefab.name} 没有RectTransform组件，无法用作UI");
                    Destroy(uiObj);
                    return null;
                }

                // 重置预制体的本地变换，确保从0开始
                rectTransform.localPosition = Vector3.zero;
                rectTransform.localRotation = Quaternion.identity;
                rectTransform.localScale = Vector3.one;

                Vector2 uiPos = Vector2.zero;

                // 根据Canvas的渲染模式选择不同的坐标转换方法
                if (uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    // Screen Space - Overlay模式：正确的坐标转换方法
                    RectTransform canvasRect = uiCanvas.transform as RectTransform;
                    Vector2 canvasPos;

                    // 使用Unity官方的坐标转换方法
                    bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRect,
                        screenPos,
                        null, // Screen Space - Overlay模式传null
                        out canvasPos
                    );

                    uiPos = canvasPos;
                    Debug.Log($"Screen Space - Overlay坐标转换: {canvasPos}");
                }
                else if (uiCanvas.renderMode == RenderMode.ScreenSpaceCamera)
                {
                    // Screen Space - Camera模式：需要考虑Canvas的相机
                    Camera canvasCamera = uiCanvas.worldCamera != null ? uiCanvas.worldCamera : Camera.main;

                    // 转换为UI坐标
                    bool converted = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        uiCanvas.transform as RectTransform,
                        screenPos,
                        canvasCamera,
                        out uiPos
                    );

                    if (!converted)
                    {
                        Debug.LogWarning("Screen Space - Camera模式坐标转换失败，使用屏幕坐标");
                        uiPos = screenPos;
                    }
                    Debug.Log("使用Screen Space - Camera模式");
                }
                else if (uiCanvas.renderMode == RenderMode.WorldSpace)
                {
                    // World Space模式：直接转换世界坐标
                    Vector3 worldToCanvasPos = uiCanvas.transform.InverseTransformPoint(worldPos);
                    uiPos = new Vector2(worldToCanvasPos.x, worldToCanvasPos.y);
                    Debug.Log("使用World Space模式");
                }

                // 添加随机偏移（针对Screen Space - Overlay使用合适的偏移量）
                float offsetMultiplier = 25f; // 为Screen Space - Overlay模式调整偏移量
                uiPos.x += Random.Range(-randomOffset.x, randomOffset.x) * offsetMultiplier;
                uiPos.y += Random.Range(-randomOffset.y, randomOffset.y) * offsetMultiplier;

                // 统一使用anchoredPosition设置UI坐标
                rectTransform.anchoredPosition = uiPos;

                Debug.Log($"UI对象创建成功 - Canvas模式: {uiCanvas.renderMode}, 最终UI坐标: {uiPos}, 实际位置: {rectTransform.position}");

                return uiObj;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"创建UI对象时发生异常: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// 播放伤害动画
        /// </summary>
        private IEnumerator PlayDamageAnimation(GameObject damageObj, Text textComponent)
        {
            RectTransform rectTransform = damageObj.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                Destroy(damageObj);
                yield break;
            }

            // 初始状态
            Vector3 startPos = rectTransform.anchoredPosition;
            Vector3 endPos = startPos + Vector3.up * floatHeight;

            // 向上浮动动画
            rectTransform.DOAnchorPos(endPos, animationDuration).SetEase(Ease.OutQuart);

            // 等待延迟后开始淡出
            yield return new WaitForSeconds(animationDuration - fadeOutDuration);

            // 淡出动画
            if (textComponent != null)
            {
                textComponent.DOFade(0f, fadeOutDuration);
            }

            // 等待淡出完成
            yield return new WaitForSeconds(fadeOutDuration);

            // 销毁对象
            if (damageObj != null)
            {
                Destroy(damageObj);
            }
        }

        /// <summary>
        /// 清理所有显示中的伤害数字
        /// </summary>
        public void ClearAllDamageNumbers()
        {
            // 查找所有由这个管理器创建的UI对象并销毁
            if (uiCanvas != null)
            {
                for (int i = uiCanvas.transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = uiCanvas.transform.GetChild(i);
                    if (child.name.Contains("(Clone)"))
                    {
                        Destroy(child.gameObject);
                    }
                }
            }
        }

        void OnValidate()
        {
            // 自动查找Canvas
            if (uiCanvas == null)
            {
                uiCanvas = FindObjectOfType<Canvas>();
            }
        }
    }
}
