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

            // 强制要求预制体配置，移除自动创建逻辑
            GameObject prefabToUse = missPrefab != null ? missPrefab : damageNumberPrefab;
            if (prefabToUse == null)
            {
                Debug.LogError("DamageDisplayManager: 缺少预制体配置，请在Inspector中配置damageNumberPrefab或missPrefab");
                return;
            }

            // 创建MISS显示UI
            GameObject missObj = CreateDamageUI(character, prefabToUse);
            if (missObj == null)
            {
                Debug.LogError("DamageDisplayManager.ShowMiss: 创建UI对象失败");
                return;
            }

            // 配置MISS文本
            Text textComponent = missObj.GetComponentInChildren<Text>();
            if (textComponent != null)
            {
                textComponent.text = "MISS";
                textComponent.color = Color.yellow;
            }
            else
            {
                Debug.LogError($"预制体 '{prefabToUse.name}' 中没有找到Text组件，请检查预制体配置");
                Destroy(missObj);
                return;
            }

            // 播放动画
            StartCoroutine(PlayDamageAnimation(missObj, textComponent));
        }

        /// <summary>
        /// 创建伤害显示UI对象 - 统一的坐标转换方法
        /// </summary>
        private GameObject CreateDamageUI(Transform character, GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("CreateDamageUI: 预制体为空，请在Inspector中配置");
                return null;
            }

            if (uiCanvas == null)
            {
                Debug.LogError("CreateDamageUI: uiCanvas为空，请在Inspector中拖入Canvas");
                return null;
            }

            if (Camera.main == null)
            {
                Debug.LogError("CreateDamageUI: 主摄像机未找到，请确保场景中有标记为MainCamera的摄像机");
                return null;
            }

            // 计算世界位置
            Vector3 worldPos = character.position + headOffset;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

            // 检查屏幕坐标是否有效
            if (screenPos.z < 0)
            {
                Debug.LogWarning($"角色 {character.name} 在摄像机后方，跳过显示");
                return null;
            }

            // 创建UI对象
            GameObject uiObj = Instantiate(prefab, uiCanvas.transform);
            RectTransform rectTransform = uiObj.GetComponent<RectTransform>();

            if (rectTransform == null)
            {
                Debug.LogError($"预制体 {prefab.name} 没有RectTransform组件，无法用作UI");
                Destroy(uiObj);
                return null;
            }

            // 统一的坐标转换方法 - 只支持Screen Space - Overlay模式
            Vector2 uiPos;
            bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                uiCanvas.transform as RectTransform,
                screenPos,
                uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : uiCanvas.worldCamera,
                out uiPos
            );

            if (!success)
            {
                Debug.LogError("坐标转换失败，请检查Canvas配置");
                Destroy(uiObj);
                return null;
            }

            // 添加随机偏移
            uiPos.x += Random.Range(-randomOffset.x, randomOffset.x) * 50f;
            uiPos.y += Random.Range(-randomOffset.y, randomOffset.y) * 50f;

            // 设置UI位置
            rectTransform.anchoredPosition = uiPos;

            return uiObj;
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
