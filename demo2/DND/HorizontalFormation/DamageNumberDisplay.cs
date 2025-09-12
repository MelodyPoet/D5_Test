using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

namespace demo2.DND.HorizontalFormation
{
    /// <summary>
    /// 伤害数字显示组件 - 在角色头上显示伤害数字或MISS
    /// </summary>
    public class DamageNumberDisplay : MonoBehaviour
    {
        [Header("UI组件")]
        [SerializeField] private Text damageText;
        [SerializeField] private Text missText;

        [Header("动画参数")]
        [SerializeField] private float floatHeight = 50f;
        [SerializeField] private float animationDuration = 1.5f;
        [SerializeField] private float fadeOutDuration = 0.5f;

        private RectTransform rectTransform;
        private Canvas parentCanvas;

        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            parentCanvas = GetComponentInParent<Canvas>();

            // 初始化时隐藏所有文本
            if (damageText != null) damageText.gameObject.SetActive(false);
            if (missText != null) missText.gameObject.SetActive(false);
        }

        /// <summary>
        /// 显示伤害数字
        /// </summary>
        /// <param name="damage">伤害值</param>
        /// <param name="isDamage">是否为伤害（true）还是治疗（false）</param>
        public void ShowDamage(int damage, bool isDamage = true)
        {
            if (damageText == null) return;

            // 设置文本内容和颜色
            damageText.text = damage.ToString();
            damageText.color = isDamage ? Color.red : Color.green;

            // 显示伤害文本，隐藏MISS文本
            damageText.gameObject.SetActive(true);
            if (missText != null) missText.gameObject.SetActive(false);

            // 播放动画
            PlayFloatAnimation(damageText);
        }

        /// <summary>
        /// 显示MISS
        /// </summary>
        public void ShowMiss()
        {
            if (missText == null) return;

            // 显示MISS文本，隐藏伤害文本
            missText.gameObject.SetActive(true);
            if (damageText != null) damageText.gameObject.SetActive(false);

            // 播放动画
            PlayFloatAnimation(missText);
        }

        /// <summary>
        /// 播放浮动动画
        /// </summary>
        /// <param name="targetText">目标文本组件</param>
        private void PlayFloatAnimation(Text targetText)
        {
            // 重置初始状态
            targetText.color = new Color(targetText.color.r, targetText.color.g, targetText.color.b, 1f);
            Vector3 startPos = rectTransform.anchoredPosition;
            Vector3 endPos = startPos + Vector3.up * floatHeight;

            // 创建动画序列
            Sequence animSequence = DOTween.Sequence();

            // 向上浮动
            animSequence.Append(rectTransform.DOAnchorPos(endPos, animationDuration).SetEase(Ease.OutQuart));

            // 淡出
            animSequence.Join(targetText.DOFade(0f, fadeOutDuration).SetDelay(animationDuration - fadeOutDuration));

            // 动画完成后销毁
            animSequence.OnComplete(() => {
                Destroy(gameObject);
            });
        }

        /// <summary>
        /// 手动销毁（用于清理）
        /// </summary>
        public void DestroyDisplay()
        {
            DOTween.Kill(rectTransform);
            DOTween.Kill(damageText);
            if (missText != null) DOTween.Kill(missText);
            Destroy(gameObject);
        }
    }
}
