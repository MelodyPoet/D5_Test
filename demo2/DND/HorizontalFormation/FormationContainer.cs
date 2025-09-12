using UnityEngine;

namespace demo2.DND.HorizontalFormation
{
    /// <summary>
    /// 阵型容器 - 简化预制体配置管理
    /// 使用数组方式统一管理阵型预制体，避免配置复杂度
    /// </summary>
    [CreateAssetMenu(fileName = "NewFormationContainer", menuName = "DND/Formation Container")]
    public class FormationContainer : ScriptableObject
    {
        [Header("阵型配置说明")]
        [TextArea(3, 5)]
        [SerializeField] private string configurationNotes =
            "索引对应关系：\n" +
            "[0]前排左翼 [1]前排中锋 [2]前排右翼\n" +
            "[3]后排左翼 [4]后排中路 [5]后排右翼\n" +
            "null值表示该位置无角色";

        [Header("玩家阵型预制体")]
        [Tooltip("玩家阵型6个位置的角色预制体，按索引顺序配置")]
        [SerializeField] private GameObject[] playerFormationPrefabs = new GameObject[6];

        [Header("敌人阵型预制体")]
        [Tooltip("敌人阵型6个位置的角色预制体，按索引顺序配置")]
        [SerializeField] private GameObject[] enemyFormationPrefabs = new GameObject[6];

        /// <summary>
        /// 获取玩家指定位置的预制体
        /// </summary>
        /// <param name="positionIndex">位置索引 (0-5)</param>
        /// <returns>对应位置的预制体，如果索引无效或位置为空则返回null</returns>
        public GameObject GetPlayerPrefab(int positionIndex)
        {
            if (positionIndex < 0 || positionIndex >= playerFormationPrefabs.Length)
            {
                Debug.LogWarning($"玩家阵型位置索引超出范围: {positionIndex}");
                return null;
            }
            return playerFormationPrefabs[positionIndex];
        }

        /// <summary>
        /// 获取敌人指定位置的预制体
        /// </summary>
        /// <param name="positionIndex">位置索引 (0-5)</param>
        /// <returns>对应位置的预制体，如果索引无效或位置为空则返回null</returns>
        public GameObject GetEnemyPrefab(int positionIndex)
        {
            if (positionIndex < 0 || positionIndex >= enemyFormationPrefabs.Length)
            {
                Debug.LogWarning($"敌人阵型位置索引超出范围: {positionIndex}");
                return null;
            }
            return enemyFormationPrefabs[positionIndex];
        }

        /// <summary>
        /// 获取玩家阵型所有预制体
        /// </summary>
        /// <returns>玩家阵型预制体数组</returns>
        public GameObject[] GetPlayerFormation()
        {
            return playerFormationPrefabs;
        }

        /// <summary>
        /// 获取敌人阵型所有预制体
        /// </summary>
        /// <returns>敌人阵型预制体数组</returns>
        public GameObject[] GetEnemyFormation()
        {
            return enemyFormationPrefabs;
        }

        /// <summary>
        /// 设置玩家指定位置的预制体
        /// </summary>
        /// <param name="positionIndex">位置索引 (0-5)</param>
        /// <param name="prefab">要设置的预制体</param>
        public void SetPlayerPrefab(int positionIndex, GameObject prefab)
        {
            if (positionIndex < 0 || positionIndex >= playerFormationPrefabs.Length)
            {
                Debug.LogWarning($"玩家阵型位置索引超出范围: {positionIndex}");
                return;
            }
            playerFormationPrefabs[positionIndex] = prefab;
        }

        /// <summary>
        /// 设置敌人指定位置的预制体
        /// </summary>
        /// <param name="positionIndex">位置索引 (0-5)</param>
        /// <param name="prefab">要设置的预制体</param>
        public void SetEnemyPrefab(int positionIndex, GameObject prefab)
        {
            if (positionIndex < 0 || positionIndex >= enemyFormationPrefabs.Length)
            {
                Debug.LogWarning($"敌人阵型位置索引超出范围: {positionIndex}");
                return;
            }
            enemyFormationPrefabs[positionIndex] = prefab;
        }

        /// <summary>
        /// 获取位置描述信息
        /// </summary>
        /// <param name="positionIndex">位置索引</param>
        /// <returns>位置描述字符串</returns>
        public string GetPositionDescription(int positionIndex)
        {
            switch (positionIndex)
            {
                case 0: return "前排左翼";
                case 1: return "前排中锋";
                case 2: return "前排右翼";
                case 3: return "后排左翼";
                case 4: return "后排中路";
                case 5: return "后排右翼";
                default: return "无效位置";
            }
        }
    }
}
