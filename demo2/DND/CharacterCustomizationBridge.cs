using UnityEngine;
using System.Collections.Generic;
using demo2.DND.InventoryTetris; // for ItemBaseSO

namespace demo2.DND
{
    /// <summary>
    /// 单个外观部件条目（用于序列化跨场景传递）。
    /// </summary>
    [System.Serializable]
    public class AppearancePartEntry
    {
        public SkinBodyPartType partType;
        public string skinID;
    }

    /// <summary>
    /// 可序列化物品条目：落盘时用 itemId 重建 ItemBaseSO；内存桥直接持有 runtimeRef 以避免资源扫描。
    /// </summary>
    [System.Serializable]
    public class SerializableItem
    {
        /// <summary>物品模板稳定标识（ItemBaseSO.itemId），用于落盘后反查重建</summary>
        public string itemId;

        /// <summary>仅内存桥使用，不进入 JSON；落盘重新加载时由 ItemDatabase.Get(itemId) 重建</summary>
        [System.NonSerialized]
        public ItemBaseSO runtimeRef;
    }

    /// <summary>
    /// 玩家定制数据包：跨场景传递的纯数据快照。
    /// 战斗场景基于 PlayerTemplate01 模板实例化玩家后，把本数据包"叠加"到实例上
    /// （即把玩家选的各个部件 skin 添加到模板默认底子（如 p7_alignment）之上），
    /// 并应用属性值、职业、种族与初始装备。模板 prefab 保持纯净、不被改写。
    ///
    /// 设计：本类整体 [Serializable]，可直接被 JsonUtility 序列化（落盘接口见 SaveManager）。
    /// 其中 CharacterTemplate / ItemBaseSO 这类 UnityEngine.Object 引用用 [NonSerialized] 排除，
    /// 改以 characterClass / itemId 等字符串/枚举在磁盘侧重建，内存侧则保留 runtimeRef 引用。
    /// </summary>
    [System.Serializable]
    public class CharacterCustomizationData
    {
        /// <summary>外观部件（头发/眼睛/装备外观等），在模板底子之上叠加</summary>
        public List<AppearancePartEntry> appearanceParts = new List<AppearancePartEntry>();

        /// <summary>六维属性（已含种族加成的最终值）</summary>
        public int strength, dexterity, constitution, intelligence, wisdom, charisma;

        /// <summary>角色等级</summary>
        public int level = 1;

        /// <summary>职业（确认的职业属性模板来源）</summary>
        public CharacterClass characterClass;

        /// <summary>种族（当前仅变体人类）</summary>
        public PointBuySystem.RaceType race;

        /// <summary>种族加成自选属性列表（变体人类：两个不同属性各+1）</summary>
        public List<StatType> racialChoices = new List<StatType>();

        /// <summary>初始/确认装备（真实物品：含背包与属性修正）。落盘以 itemId 重建。</summary>
        public List<SerializableItem> initialEquipment = new List<SerializableItem>();

        /// <summary>选中的职业模板（内存桥使用，不落盘；落盘由 characterClass + selectedTemplateName 重建）</summary>
        [System.NonSerialized]
        public CharacterTemplate selectedTemplate;

        /// <summary>选中职业模板名称（落盘用，便于 SaveManager 反查或日志）</summary>
        public string selectedTemplateName;
    }

    /// <summary>
    /// 跨场景角色定制桥接单例。
    ///
    /// 设计定位（方案 X：模板只读 + 数据跨场景）：
    ///   模板（如 PlayerTemplate01.prefab）保持纯净、永不改写。玩家在 SpineAni_test0516
    ///   场景中完成定制（外观/属性），确认时把结果序列化为 CharacterCustomizationData
    ///   交给本桥接器（DontDestroyOnLoad 跨场景存活）。
    ///
    ///   战斗场景 DND_Test 生成玩家阵型时，通过 SourcePrefab 引用在 FormationContainer
    ///   的 playerFormationPrefabs 数组中动态定位"玩家主控槽位"（玩家可把 PlayerTemplate01
    ///   拖到任意阵型位置），然后基于该模板 prefab 实例化玩家，并把数据包的外观/属性
    ///   叠加到实例上，得到"玩家角色实例"。
    ///
    /// 这样：
    ///   - 不再跨场景传递活 GameObject 实例，规避 DontDestroyOnLoad 时序/场景卸载导致的丢失；
    ///   - 模板 prefab 不被污染，可反复用于不同玩家/重新定制；
    ///   - 玩家随时改变阵型中 PlayerTemplate01 的位置，战斗逻辑自动对齐，无需写死索引。
    /// </summary>
    public class CharacterCustomizationBridge : MonoBehaviour
    {
        public static CharacterCustomizationBridge Instance { get; private set; }

        [Header("调试")]
        [SerializeField] private bool dontDestroy = true;

        /// <summary>
        /// 定制数据包（外观 + 属性）。战斗场景消费前有效。
        /// </summary>
        private CharacterCustomizationData customizationData;

        /// <summary>
        /// 该实例所基于的模板 prefab 引用（如 PlayerTemplate01）。
        /// 战斗场景用它来在 FormationContainer 的 prefab 数组中定位玩家主控槽位。
        /// </summary>
        private GameObject sourcePrefab;

        /// <summary>
        /// 本次是否有自定义角色待消费。
        /// </summary>
        public bool HasPlayer => customizationData != null && sourcePrefab != null;

        /// <summary>
        /// 获取定制数据包。
        /// </summary>
        public CharacterCustomizationData Data => customizationData;

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
        /// 由自定义面板在确认时调用：提交定制数据包及其来源模板 prefab。
        /// </summary>
        /// <param name="data">承载定制结果的纯数据（外观部件 + 属性）</param>
        /// <param name="templatePrefab">该角色所基于的模板 prefab（如 PlayerTemplate01）</param>
        public void SetPlayerData(CharacterCustomizationData data, GameObject templatePrefab)
        {
            customizationData = data;
            sourcePrefab = templatePrefab;
        }

        /// <summary>
        /// 战斗场景消费完毕后调用，复位桥接状态。
        /// </summary>
        public void Clear()
        {
            customizationData = null;
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
