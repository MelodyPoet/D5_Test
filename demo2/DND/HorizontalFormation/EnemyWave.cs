using UnityEngine;

namespace demo2.DND.HorizontalFormation
{
    [CreateAssetMenu(fileName = "NewEnemyWave", menuName = "DND/Enemy Wave")]
    public class EnemyWave : ScriptableObject
    {
        [Header("敌人阵型预制体")]
        [Tooltip("敌人阵型6个位置的角色预制体，按索引顺序配置")]
        [SerializeField] private GameObject[] enemyFormationPrefabs = new GameObject[6];

        public GameObject[] GetEnemyFormation()
        {
            return enemyFormationPrefabs;
        }
    }
}

