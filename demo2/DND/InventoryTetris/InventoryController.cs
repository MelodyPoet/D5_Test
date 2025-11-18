using UnityEngine;
using demo2.DND.Core.Events.Channels;
using demo2.DND.Core.Events.Data; // 重新加入以解析 InventoryEquipRequest / InventoryEquipAction
using System.Linq; // 为 IReadOnlyList<T>.Contains 提供扩展
using demo2.DND.HorizontalFormation; // 添加：用于 IdleGameManager / HorizontalBattleFormationManager / HorizontalFormation 相关类型
using System; // 添加以解析 Enum 等类型

namespace demo2.DND.InventoryTetris
{
    /// <summary>
    /// 背包/装备 控制器（全局）：订阅请求事件，执行业务逻辑，广播变更事件。
    /// 建议挂载在场景的 IdleGameSystem 节点下。
    /// </summary>
    public class InventoryController : MonoBehaviour
    {
        [Header("请求通道（拖入资产）")]
        [SerializeField] private RequestEquipItemChannel_SO requestEquipItemChannel;

        [Header("输出通道（拖入资产）")]
        [SerializeField] private InventoryChangedChannel_SO inventoryChangedChannel;

        [Header("拾取请求通道（拖入资产）")]
        [SerializeField] private RequestAddItemChannel_SO requestAddItemChannel; // 去除冗余限定符

        private void OnEnable()
        {
            if (requestEquipItemChannel != null)
            {
                requestEquipItemChannel.OnEventRaised += HandleEquipRequest;
            }
            else
            {
                Debug.LogWarning("[InventoryController] 未配置 RequestEquipItemChannel 资产，无法接收装备请求。");
            }

            if (requestAddItemChannel != null)
            {
                requestAddItemChannel.OnEventRaised += HandleAddItemRequest;
            }
            else
            {
                Debug.LogWarning("[InventoryController] 未配置 RequestAddItemChannel 资产，无法接收新增物品请求。");
            }
        }

        private void OnDisable()
        {
            if (requestEquipItemChannel != null)
            {
                requestEquipItemChannel.OnEventRaised -= HandleEquipRequest;
            }

            if (requestAddItemChannel != null)
            {
                requestAddItemChannel.OnEventRaised -= HandleAddItemRequest;
            }
        }

        private void HandleEquipRequest(InventoryEquipRequest req)
        {
            // 战斗中禁止装备/卸下
            var idle = FindObjectOfType<IdleGameManager>(); // 精简限定符
            if (idle != null && idle.IsInBattle)
            {
                Debug.LogWarning("[InventoryController] 当前处于战斗中，禁止执行装备/卸下操作（请求已忽略）。");
                return;
            }

            if (req.inventory == null || req.item == null)
            {
                Debug.LogWarning("[InventoryController] 收到无效的装备请求（inventory/item 为空）。");
                return;
            }

            // 防守：确保该物品属于该背包
            if (!req.inventory.Items.Contains(req.item))
            {
                Debug.LogWarning("[InventoryController] 目标物品不属于目标背包，拒绝执行装备请求。");
                return;
            }

            var eq = req.inventory.GetComponent<CharacterEquipment>()
                     ?? req.inventory.GetComponentInParent<CharacterEquipment>()
                     ?? req.inventory.GetComponentInChildren<CharacterEquipment>(true);
            if (eq == null)
            {
                Debug.LogWarning("[InventoryController] 未找到 CharacterEquipment 组件，无法执行装备/卸下。");
                return;
            }

            bool result = false;
            switch (req.action)
            {
                case InventoryEquipAction.Toggle:
                    result = eq.ToggleEquip(req.item);
                    break;
                case InventoryEquipAction.Equip:
                    if (req.item.data.isWeapon) result = eq.EquipToSlot(EquipmentSlot.MainHand, req.item);
                    else if (req.item.data.isArmor) result = eq.EquipToSlot(EquipmentSlot.Armor, req.item);
                    else if (req.item.data.isShield) result = eq.EquipToSlot(EquipmentSlot.OffHand, req.item);
                    break;
                case InventoryEquipAction.Unequip:
                    if (eq.IsEquipped(req.item))
                    {
                        // Attempt to find the slot where the instance is equipped and unequip that slot.
                        foreach (EquipmentSlot s in Enum.GetValues(typeof(EquipmentSlot)))
                        {
                            var inst = eq.GetEquipped(s);
                            if (ReferenceEquals(inst, req.item))
                            {
                                result = eq.UnequipSlot(s);
                                break;
                            }
                        }
                    }
                    break;
            }

            if (!result)
            {
                Debug.LogWarning("[InventoryController] 装备请求执行失败（可能是不兼容、未熟练或槽位状态不允许）。");
            }

            // 广播背包变更（供视图层刷新）
            if (inventoryChangedChannel != null)
            {
                inventoryChangedChannel.RaiseEvent(req.inventory);
            }
        }

        private void HandleAddItemRequest(InventoryAddItemRequest req)
        {
            if (req.inventory == null || req.item == null)
            {
                Debug.LogWarning("[InventoryController] 收到无效的添加物品请求（inventory/item 为空）。");
                return;
            }

            int amount = req.amount <= 0 ? 1 : req.amount;
            int added = 0;
            for (int i = 0; i < amount; i++)
            {
                // 构造实例时捕获异常（构造函数不会返回 null，移除恒为 false 的 null 检查）
                ItemInstance inst;
                try {
                    inst = new ItemInstance(req.item);
                }
                catch (Exception ex) {
                    Debug.LogWarning("[InventoryController] 创建物品实例失败：" + req.item.name + " -> " + ex.Message);
                    continue;
                }
                req.inventory.AddInstance(inst);
                added++;
            }

            if (added > 0)
            {
                if (inventoryChangedChannel != null)
                {
                    inventoryChangedChannel.RaiseEvent(req.inventory);
                }
#if UNITY_EDITOR
                Debug.Log($"[InventoryController] 已向 {req.inventory.gameObject.name} 背包添加 {added}/{amount} 个 '{req.item.displayName}'。");
#endif
            }
        }

        /// <summary>
        /// 示例：根据当前波次生成战利品（真实项目中应引用掉落表）。
        /// 自动选择第一个玩家角色的背包作为目标并发布添加请求事件。
        /// </summary>
        public void GenerateWaveLootExample()
        {
            var formation = FindObjectOfType<HorizontalBattleFormationManager>(); // 精简限定符
            if (formation == null) return;
            var playerChars = formation.GetAllAliveCharacters(BattleSide.Player); // 修复 BattleSide 引用
            if (playerChars == null || playerChars.Count == 0) return;
            var inv = playerChars[0].GetComponent<CharacterInventory>();
            if (inv == null) return;

            // 示例：这里直接从背包初始物品或项目中任意可用物品 SO 中挑选（实际应从掉落系统传入）
            // 为简化，仅在项目中查找任意一个 ItemBaseSO 资源（运行时需要可访问）——保持占位逻辑
            // 若无法自动采集，未来改由外部系统调用 RequestAddItemChannel.RaiseEvent

#if UNITY_EDITOR
            var allItemSOs = UnityEditor.AssetDatabase.FindAssets("t:ItemBaseSO");
            int added = 0;
            foreach (var guid in allItemSOs)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var so = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemBaseSO>(path);
                if (so == null) continue;
                if (requestAddItemChannel != null)
                {
                    requestAddItemChannel.RaiseEvent(new InventoryAddItemRequest(inv, so)); // 精简限定符
                    added++;
                }
                if (added >= 2) break; // 控制示例数量
            }
            if (added == 0)
            {
                Debug.LogWarning("[InventoryController] GenerateWaveLootExample: 未找到任何 ItemBaseSO 资产用于示例掉落。");
            }
            else
            {
                Debug.Log($"[InventoryController] GenerateWaveLootExample: 已发布 {added} 个物品拾取请求。");
            }
#endif
        }
    }
}
