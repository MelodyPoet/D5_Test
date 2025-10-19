using UnityEngine;

namespace demo2.DND.HorizontalFormation
{
    [System.Serializable]
    public class EnemyWaveConfig
    {
        [Tooltip("该波次敌人阵型6个位置的角色预制体，按索引顺序配置")]
        public GameObject[] enemyFormationPrefabs = new GameObject[6];
    }

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

        [Header("敌人波次配置")]
        [Tooltip("配置每个波次的敌人阵型")]
        [SerializeField] private EnemyWaveConfig[] enemyWaves;

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
        /// 获取指定波次的敌人阵型所有预制体
        /// </summary>
        /// <param name="waveIndex">波次索引</param>
        /// <returns>敌人阵型预制体数组</returns>
        public GameObject[] GetEnemyFormation(int waveIndex)
        {
            if (enemyWaves == null || waveIndex < 0 || waveIndex >= enemyWaves.Length)
            {
                Debug.LogWarning($"敌人波次索引无效或未配置: {waveIndex}");
                return new GameObject[6]; // 返回一个空阵型避免后续逻辑报错
            }
            return enemyWaves[waveIndex].enemyFormationPrefabs;
        }

        /// <summary>
        /// 获取总的敌人波次数
        /// </summary>
        public int GetEnemyWaveCount()
        {
            return enemyWaves?.Length ?? 0;
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
        [System.Obsolete("敌人阵型已由EnemyWave配置，此方法不再适用")]
        public void SetEnemyPrefab(int positionIndex, GameObject prefab)
        {
            Debug.LogWarning("SetEnemyPrefab 已过时，请直接配置 EnemyWave ScriptableObject。");
        }
    }
}
