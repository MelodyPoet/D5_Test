using UnityEngine;

namespace demo2.DND
{
    /// <summary>
    /// 跨场景角色定制桥接单例。
    ///
    /// 设计定位（方案 X：模板只读 + 实例跨场景）：
    ///   模板（如 PlayerTemplate01.prefab）保持纯净、永不改写。玩家在 SpineAni_test0516
    ///   场景中基于模板实例化并定制（外观/属性/装备/状态），得到"玩家角色实例"。
    ///   确认后该实例交给本桥接器（DontDestroyOnLoad 跨场景存活）。
    ///
    ///   战斗场景 DND_Test 生成玩家阵型时，通过 SourcePrefab 引用在 FormationContainer
    ///   的 playerFormationPrefabs 数组中动态定位"玩家主控槽位"（玩家可把 PlayerTemplate01
    ///   拖到任意阵型位置），再用本桥接器提供的实例替换该槽位默认生成的角色。
    ///
    /// 这样：
    ///   - 模板 prefab 不被污染，可反复用于不同玩家/重新定制；
    ///   - 玩家随时改变阵型中 PlayerTemplate01 的位置，战斗逻辑自动对齐，无需写死索引；
    ///   - 实例在战斗中随 RPG 成长不断更新，与模板解耦。
    /// </summary>
    public class CharacterCustomizationBridge : MonoBehaviour
    {
        public static CharacterCustomizationBridge Instance { get; private set; }

        [Header("调试")]
        [SerializeField] private bool dontDestroy = true;

        /// <summary>
        /// 定制后的玩家角色实例（已包含属性/外观/装备，带有完整战斗组件）。
        /// 战斗场景消费前有效。
        /// </summary>
        private GameObject playerCharacterInstance;

        /// <summary>
        /// 该实例所基于的模板 prefab 引用（如 PlayerTemplate01）。
        /// 战斗场景用它来在 FormationContainer 的 prefab 数组中定位玩家主控槽位。
        /// </summary>
        private GameObject sourcePrefab;

        /// <summary>
        /// 本次是否有自定义角色待消费。
        /// </summary>
        public bool HasPlayer => playerCharacterInstance != null && sourcePrefab != null;

        /// <summary>
        /// 获取自定义角色实例（消费方直接复用，不再 Instantiate 该实例本身）。
        /// </summary>
        public GameObject PlayerInstance => playerCharacterInstance;

        /// <summary>
        /// 获取模板 prefab 引用，用于在阵型 prefab 数组中定位玩家主控槽位。
        /// </summary>
        public GameObject SourcePrefab => sourcePrefab;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (dontDestroy)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        /// <summary>
        /// 由自定义面板在确认时调用：提交定制后的角色实例及其来源模板 prefab。
        /// </summary>
        /// <param name="instance">承载定制结果的角色 GameObject</param>
        /// <param name="templatePrefab">该角色所基于的模板 prefab（如 PlayerTemplate01）</param>
        public void SetPlayer(GameObject instance, GameObject templatePrefab)
        {
            playerCharacterInstance = instance;
            sourcePrefab = templatePrefab;
        }

        /// <summary>
        /// 战斗场景消费完毕后调用，复位桥接状态。
        /// </summary>
        public void Clear()
        {
            playerCharacterInstance = null;
            sourcePrefab = null;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
