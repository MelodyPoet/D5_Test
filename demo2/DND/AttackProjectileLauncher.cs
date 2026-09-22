using UnityEngine;
using DG.Tweening;

namespace demo2.DND
{
    /// <summary>
    /// 弹道特效示例实现：实现 IProjectileLauncher，被适配器在释放帧调用 Launch()。
    /// 行为：从起点直线飞向目标点，到达后（可选播放命中特效）并自毁。
    /// 用法：把这个脚本挂到你的箭矢 / 法术弹道 prefab 根节点即可。
    /// </summary>
    public class AttackProjectileLauncher : MonoBehaviour, IProjectileLauncher
    {
        [Header("飞行")]
        [Tooltip("飞行速度（单位/秒）。")]
        public float speed = 18f;
        [Tooltip("到达目标后延迟销毁时间（秒）。")]
        public float destroyDelay = 0.05f;
        [Tooltip("命中时生成的特效 prefab（可选，如爆点/火花）。")]
        public GameObject hitEffectPrefab;
        [Tooltip("飞行中是否始终朝向运动方向。")]
        public bool faceMovement = true;

        public void Launch(Vector3 targetWorldPosition, Vector3 fromWorldPosition)
        {
            // 从生成点出发（适配器已在此处实例化，这里再对齐一次以防万一）
            transform.position = fromWorldPosition;

            float distance = Vector3.Distance(fromWorldPosition, targetWorldPosition);
            float duration = speed > 0f ? distance / speed : 0.3f;

            if (faceMovement)
            {
                Vector3 dir = (targetWorldPosition - fromWorldPosition).normalized;
                if (dir != Vector3.zero)
                    transform.rotation = Quaternion.LookRotation(dir);
            }

            transform.DOMove(targetWorldPosition, duration)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    if (hitEffectPrefab != null)
                    {
                        Instantiate(hitEffectPrefab, targetWorldPosition, Quaternion.identity);
                    }
                    Destroy(gameObject, destroyDelay);
                });
        }
    }
}
