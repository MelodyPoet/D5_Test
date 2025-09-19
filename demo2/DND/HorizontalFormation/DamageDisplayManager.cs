using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace demo2.DND.HorizontalFormation
{
    /// <summary>
    /// 伤害数字显示管理器 - 使用DOTween替代协程
    /// 单例模式，统一管理所有伤害数字的显示
    /// </summary>
    public class DamageDisplayManager : MonoBehaviour
    {
        [Header("预制体配置")]
        [Tooltip("伤害数字预制体")]
        public GameObject damageNumberPrefab;
        [Tooltip("MISS显示预制体")]
        public GameObject missPrefab;

        [Header("UI设置")]
        [Tooltip("UI画布 - 伤害数字显示的画布")]
        public Canvas uiCanvas;
        [Tooltip("头部偏移 - 伤害数字相对于角色的位置偏移")]
        public Vector3 headOffset = new Vector3(0, 2f, 0);

        [Header("动画设置")]
        [Tooltip("伤害数字显示时长")]
        public float displayDuration = 2f;
        [Tooltip("向上移动距离")]
        public float moveUpDistance = 100f;
        [Tooltip("淡出开始时间（相对于总时长的百分比）")]
        [Range(0f, 1f)] public float fadeStartPercent = 0.5f;

        // 单例模式
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
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // 初始化检查
            if (damageNumberPrefab == null || uiCanvas == null)
            {
                Debug.LogWarning("DamageDisplayManager: 缺少必要的组件引用");
            }
        }

        /// <summary>
        /// 显示伤害数字
        /// </summary>
        public void ShowDamageNumber(Transform character, int damage, bool isDamage = true)
        {
            if (character == null || damage < 0) return;

            GameObject damageObj = CreateDamageUI(character, damageNumberPrefab);
            if (damageObj == null) return;

            // 配置伤害文本
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

            // 使用DOTween播放动画
            PlayDamageAnimationDOTween(damageObj, textComponent);
        }

        /// <summary>
        /// 显示MISS
        /// </summary>
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

            GameObject prefabToUse = missPrefab != null ? missPrefab : damageNumberPrefab;
            if (prefabToUse == null)
            {
                Debug.LogError("DamageDisplayManager: 缺少预制体配置，请在Inspector中配置damageNumberPrefab或missPrefab");
                return;
            }

            GameObject missObj = CreateDamageUI(character, prefabToUse);
            if (missObj == null)
            {
                Debug.LogError("DamageDisplayManager.ShowMiss: 创建UI对象失败");
                return;
            }

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

            // 使用DOTween播放动画
            PlayDamageAnimationDOTween(missObj, textComponent);
        }

        /// <summary>
        /// 使用DOTween播放伤害动画 - 替代协程
        /// </summary>
        private void PlayDamageAnimationDOTween(GameObject damageObj, Text textComponent)
        {
            if (damageObj == null) return;

            RectTransform rectTransform = damageObj.GetComponent<RectTransform>();
            if (rectTransform == null) return;

            Vector2 startPos = rectTransform.anchoredPosition;
            Vector2 endPos = startPos + Vector2.up * moveUpDistance;

            // 创建DOTween序列
            Sequence damageSequence = DOTween.Sequence();

            // 向上移动动画
            damageSequence.Append(rectTransform.DOAnchorPos(endPos, displayDuration).SetEase(Ease.OutCubic));

            // 淡出动画（在指定时间点开始）
            if (textComponent != null)
            {
                float fadeStartTime = displayDuration * fadeStartPercent;
                float fadeOutDuration = displayDuration * (1f - fadeStartPercent);

                damageSequence.Insert(fadeStartTime, textComponent.DOFade(0f, fadeOutDuration));
            }

            // 动画完成后销毁对象
            damageSequence.OnComplete(() => {
                if (damageObj != null)
                {
                    Destroy(damageObj);
                }
            });
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

            // 坐标转换：Screen Space - Overlay模式
            Vector2 uiPos;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                uiCanvas.transform as RectTransform,
                screenPos,
                uiCanvas.worldCamera,
                out uiPos))
            {
                rectTransform.anchoredPosition = uiPos;
            }
            else
            {
                Debug.LogWarning($"无法为角色 {character.name} 转换UI坐标");
                Destroy(uiObj);
                return null;
            }

            return uiObj;
        }
    }
}
