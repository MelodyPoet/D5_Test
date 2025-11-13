// filepath: d:\UnityProject\Archive\Assets\demo2\DND\InventoryTetris\InventoryUIBinder.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using demo2.DND.HorizontalFormation;
using demo2.DND.Utility; // add reference to PauseController namespace
using demo2.DND.Core.Events.Channels; // added for event channels
using demo2.DND.Core.Events.Data; // added for InventoryAddItemRequest

namespace demo2.DND.InventoryTetris
{
    /// <summary>
    /// 供属性UI绑定器实现的最小接口（避免在此处强依赖具体类名，便于Inspector手动挂载任意实现）。
    /// </summary>
    public interface ICharacterStatsUIBinder
    {
        void Bind(CharacterStats stats);
        void Unbind();
    }

    /// <summary>
    /// 背包 UI 绑定器：将 CharacterInventory 的运行时物品实例“落地”到 InventoryGridView。
    /// 严格手动挂载：需在 Inspector 中手动将多个“角色预制件或实例”上的 CharacterInventory 拖入 sourceInventories。
    /// 不做 AddComponent；不包含任何战斗/装备逻辑。
    /// 说明：本组件不再支持任何全局 PlayerData 或“单一默认来源”。
    /// </summary>
    // Implement PauseController.IExcludableFromPause so PauseController.excludedComponents can detect and call OnPauseExcluded
    public class InventoryUIBinder : MonoBehaviour, PauseController.IExcludableFromPause
    {
        [Header("网格视图（手动挂载）")]
        public InventoryGridView gridView;

        [Header("角色属性UI（手动挂载，可选）")]
        [Tooltip("可选：若提供，将在切换活动角色时刷新属性显示（字段应挂载一个实现了 ICharacterStatsUIBinder 的组件）。")]
        public MonoBehaviour statsUIBinder; // 运行时转换为 ICharacterStatsUIBinder

        [Header("绑定选项")]
        public bool clearAndRebuildOnBind = true;
        public bool bindOnStart = true;

        [Header("网格显示模式")]
        [Tooltip("启用后，使用固定的格子大小/间距/内边距；不同角色仅改变格子数量（rows/cols），图标大小保持一致。建议配合 ScrollRect，使内容可滚动。")]
        public bool useUniformCellSize = true;
        public Vector2 uniformCellSize = new Vector2(96, 96);
        public Vector2 uniformSpacing = new Vector2(8, 8);
        public Vector2 uniformPadding = new Vector2(8, 8);

        [Header("切换策略（运行时自动收集）")]
        [Tooltip("当自动收集到第一个可用背包，且当前无活动来源时，是否自动切换为该背包。")]
        public bool autoSwitchToFirstReady = true;

        [Header("自动装备（可选)")]
        [Tooltip("启用后，在将物品落地到网格前，如果角色装备栏的主手/护甲/盾牌为空，将自动把背包中的第一件同类物品装备到对应槽位（仅当 CanEquip 为真）。")]
        public bool autoEquipOnBind;

        [Header("调试（可选）")]
        public bool debugLogs;

        [Header("事件通道（可选，事件化绑定刷新）")]
        [Tooltip("可选：拖入 InventoryChangedChannel 资产；当任意背包通过控制器广播变化时（例如装备变更），若与当前激活来源匹配则刷新 UI。")]
        [SerializeField] private InventoryChangedChannel_SO inventoryChangedChannel; // simplified qualifier
        [Tooltip("可选：拖入 ActiveCharacterChangedChannel 资产；当当前角色切换时，若该角色具有可用背包則自動切换激活來源。")]
        [SerializeField] private ActiveCharacterChangedChannel_SO activeCharacterChangedChannel; // simplified qualifier
        [Tooltip("是否启用事件通道驱动刷新（为 false 则仅使用直接订阅 CharacterInventory.OnInventoryChanged）。")]
        [SerializeField] private bool enableEventChannels = true;
        [Tooltip("当收到 ActiveCharacterChanged 事件且角色对应背包尚未收集时，是否自动收集并添加。")]
        [SerializeField] private bool autoCollectOnActiveCharacterEvent = true;

        [Header("拾取事件通道（可选）")]
        [SerializeField] private RequestAddItemChannel_SO requestAddItemChannel; // simplified qualifier
        [Tooltip("是否由 UI 直接处理拾取事件并尝试落地（一般不需要；由控制器处理以避免重复添加）")]
        [SerializeField] private bool handleAddItemEvents = false;

        // Navigation buttons removed: navigation is now owned by UITabSwitcher (single-responsibility).
        // nextButton/prevButton and acceptExternalNavigation have been removed intentionally.

        // 内部数据源：仅运行时维护
        private List<CharacterInventory> sourceInventories = new List<CharacterInventory>();
        public IReadOnlyList<CharacterInventory> Sources => sourceInventories;
        public int activeSourceIndex;

        private bool autoSwitchConsumed; // prevent repeated auto-switch when multiple inventories become ready at runtime

        private CharacterInventory ActiveSource
        {
            get
            {
                if (sourceInventories == null || sourceInventories.Count == 0) return null;
                if (activeSourceIndex < 0 || activeSourceIndex >= sourceInventories.Count) return null;
                return sourceInventories[activeSourceIndex];
            }
        }

        private GraphicRaycaster debugRaycaster;
        private EventSystem debugEventSystem;


        private void OnEnable()
        {
            HorizontalBattleFormationManager.OnPlayerFormationGenerated += HandlePlayerFormationGenerated;

            CollectExistingInventoriesInScene();
            UpdateStatsUI();
            TryBindStatsFromExistingFormation();

            CharacterInventory.OnAnyInventoryReady += HandleInventoryReady;
            CharacterInventory.OnAnyInventoryDestroyed += HandleInventoryDestroyed;

            // 事件通道订阅（事件化刷新）
            if (enableEventChannels)
            {
                if (inventoryChangedChannel != null) inventoryChangedChannel.OnEventRaised += HandleInventoryChangedChannel;
                if (activeCharacterChangedChannel != null) activeCharacterChangedChannel.OnEventRaised += HandleActiveCharacterChangedChannel;
                if (handleAddItemEvents && requestAddItemChannel != null) requestAddItemChannel.OnEventRaised += HandleAddItemRequest;
            }

            debugEventSystem = EventSystem.current;
            debugRaycaster = GetComponentInParent<GraphicRaycaster>() ?? FindObjectOfType<GraphicRaycaster>();

            var goName = gameObject != null ? gameObject.name : "<null>";
            var srcCount = sourceInventories != null ? sourceInventories.Count : 0;
            Debug.Log($"[InventoryUIBinder] OnEnable summary -> GO={goName}, sourcesCount={srcCount}");
            if (debugLogs)
            {
                var gridName = gridView != null && gridView.gameObject != null ? gridView.gameObject.name : "<null>";
                Debug.Log($"[InventoryUIBinder] OnEnable -> instanceID={this.GetInstanceID()}, activeInHierarchy={(gameObject!=null?gameObject.activeInHierarchy:false)}, gridView={gridName}, collectedSources={srcCount}");
            }

            if (sourceInventories == null || sourceInventories.Count == 0)
            {
                try
                {
                    var found = FindObjectsByType<CharacterInventory>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    int foundCount = found != null ? found.Length : 0;
                    Debug.Log($"[InventoryUIBinder] OnEnable scan -> found {foundCount} CharacterInventory components in scenes (including inactive)");
                    if (found != null)
                    {
                        for (int i = 0; i < found.Length; i++)
                        {
                            var inv = found[i];
                            if (inv == null) continue;
                            Debug.Log($"[InventoryUIBinder]   scanned[{i}] = {DescribeInventory(inv)}");
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[InventoryUIBinder] OnEnable scan exception: {ex}");
                }
            }
        }

        private void OnDisable()
        {
            CharacterInventory.OnAnyInventoryReady -= HandleInventoryReady;
            CharacterInventory.OnAnyInventoryDestroyed -= HandleInventoryDestroyed;
            HorizontalBattleFormationManager.OnPlayerFormationGenerated -= HandlePlayerFormationGenerated;
            if (enableEventChannels)
            {
                if (inventoryChangedChannel != null) inventoryChangedChannel.OnEventRaised -= HandleInventoryChangedChannel;
                if (activeCharacterChangedChannel != null) activeCharacterChangedChannel.OnEventRaised -= HandleActiveCharacterChangedChannel;
                if (handleAddItemEvents && requestAddItemChannel != null) requestAddItemChannel.OnEventRaised -= HandleAddItemRequest;
            }
        }

        private void Start()
        {
            if (bindOnStart)
            {
                if (debugLogs)
                {
                    string src = ActiveSource != null ? ActiveSource.gameObject.name : "<null>";
                    string grid = gridView != null ? gridView.gameObject.name : "<null>";
                    Debug.Log($"[InventoryUIBinder] Start -> activeSource= {src}, grid= {grid}");
                }
                RefreshFromInventory();
                UpdateStatsUI();
            }
        }

        private void Update()
        {
            if (!debugLogs) return;
            // 调试模式下，在每次鼠标左键按下时输出光线检测结果，帮助判断是否有 UI 遮挡
            if (Input.GetMouseButtonDown(0))
            {
                if (debugEventSystem == null) debugEventSystem = EventSystem.current;
                var ped = new PointerEventData(debugEventSystem) { position = Input.mousePosition };
                var results = new List<RaycastResult>();
                if (debugRaycaster == null) debugRaycaster = FindObjectOfType<GraphicRaycaster>();
                if (debugRaycaster != null)
                {
                    debugRaycaster.Raycast(ped, results);
                }
                Debug.Log($"[UIRaycast] MouseDown at {ped.position}, hits={results.Count}");
                for (int i = 0; i < results.Count; i++)
                {
                    var r = results[i];
                    Debug.Log($"[UIRaycast] #{i}: {r.gameObject.name} (module={r.module?.GetType().Name}, worldPos={r.worldPosition})");
                }

                // Navigation handled externally; no overlap-invoke fallback here.
            }
        }

        // 收集当前场景中已存在的 CharacterInventory（包含已激活/未激活）
        private void CollectExistingInventoriesInScene()
        {
            var arr = FindObjectsByType<CharacterInventory>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            bool hadAny = sourceInventories.Count > 0;
            for (int i = 0; i < arr.Length; i++)
            {
                var inv = arr[i];
                if (inv == null) continue;
                if (!inv.gameObject.scene.IsValid()) continue; // 过滤预制体资源
                if (sourceInventories.Contains(inv)) continue;
                // Avoid adding multiple CharacterInventory components from the same GameObject
                bool sameGoExists = false;
                for (int j = 0; j < sourceInventories.Count; j++)
                {
                    if (sourceInventories[j].gameObject == inv.gameObject)
                    {
                        sameGoExists = true;
                        break;
                    }
                }
                if (sameGoExists)
                {
                    if (debugLogs) Debug.LogWarning($"[InventoryUIBinder] Skipping additional CharacterInventory component on same GameObject: {DescribeInventory(inv)}");
                    continue;
                }
                sourceInventories.Add(inv);
                if (debugLogs) Debug.Log($"[InventoryUIBinder] Collected inventory '{inv.gameObject.name}' (scene={inv.gameObject.scene.name}) -> {DescribeInventory(inv)}");
            }
            if (debugLogs)
            {
                Debug.Log($"[InventoryUIBinder] CollectExistingInventoriesInScene -> totalCollected={sourceInventories.Count}, hadAnyBefore={hadAny}");
                if (sourceInventories.Count > 0)
                {
                    for (int i = 0; i < sourceInventories.Count; i++)
                    {
                        Debug.Log($"[InventoryUIBinder]   source[{i}] = {DescribeInventory(sourceInventories[i])}");
                    }
                }
            }
            if (!hadAny && autoSwitchToFirstReady && sourceInventories.Count > 0)
            {
                SetActiveSourceIndex(0);
                autoSwitchConsumed = true;
            }
        }

        private void HandleInventoryReady(CharacterInventory inv)
        {
            if (inv == null) return;
            if (!inv.gameObject.scene.IsValid()) return;
            if (!sourceInventories.Contains(inv)
            )
            {
                // Avoid adding additional CharacterInventory components from the same GameObject
                bool sameGoExists = false;
                for (int i = 0; i < sourceInventories.Count; i++)
                {
                    if (sourceInventories[i].gameObject == inv.gameObject)
                    {
                        sameGoExists = true;
                        break;
                    }
                }
                if (sameGoExists)
                {
                    if (debugLogs) Debug.LogWarning($"[InventoryUIBinder] HandleInventoryReady: an inventory component on the same GameObject already exists, skipping: {DescribeInventory(inv)}");
                }
                else
                {
                    sourceInventories.Add(inv);
                    if (debugLogs) Debug.Log($"[InventoryUIBinder] 收集到运行时背包: {DescribeInventory(inv)}");
                }
            }

            if (debugLogs) Debug.Log($"[InventoryUIBinder] HandleInventoryReady -> ActiveSourceName={(ActiveSource!=null?DescribeInventory(ActiveSource):"<null>" )}, inv={DescribeInventory(inv)}");

            if (sourceInventories.Count == 1 && autoSwitchToFirstReady && !autoSwitchConsumed)
            {
                SetActiveSourceIndex(0);
                autoSwitchConsumed = true;
                return;
            }

            if (ActiveSource == inv)
            {
                RefreshFromInventory();
                UpdateStatsUI();
            }
        }

        private void HandleInventoryDestroyed(CharacterInventory inv)
        {
            if (inv == null) return;
            int idx = sourceInventories.IndexOf(inv);
            if (idx < 0) return;

            bool wasActive = (ActiveSource == inv);
            sourceInventories.RemoveAt(idx);
            if (wasActive)
            {
                if (sourceInventories.Count == 0)
                {
                    activeSourceIndex = 0;
                }
                else
                {
                    activeSourceIndex = Mathf.Clamp(activeSourceIndex, 0, sourceInventories.Count - 1);
                }
                RefreshFromInventory();
                UpdateStatsUI();
            }
        }

        /// <summary>
        /// 在��来源列表中设置活动索引。
        /// </summary>
        public void SetActiveSourceIndex(int index)
        {
            if (index < 0 || index >= sourceInventories.Count)
            {
                if (debugLogs) Debug.LogWarning($"[InventoryUIBinder] SetActiveSourceIndex invalid: {index}");
                return;
            }

            if (debugLogs) Debug.Log($"[SetActiveSourceIndex] 切换请求 -> index={index}, currentIndex={activeSourceIndex}, sourcesCount={sourceInventories.Count}");

            if (activeSourceIndex == index)
            {
                RefreshFromInventory();
                UpdateStatsUI();
                return;
            }

            var prevActive = ActiveSource;
            activeSourceIndex = index;

            if (debugLogs) Debug.Log($"[SetActiveSourceIndex] 切换完成 -> newActive={DescribeInventory(ActiveSource)}");

            // If the active source did not actually change (e.g. two entries point to same GameObject), dump details
            if (prevActive == ActiveSource)
            {
                if (debugLogs) Debug.LogWarning("[SetActiveSourceIndex] ActiveSource did not change after switching index — possible duplicate entries or same GameObject present multiple times.");
                if (debugLogs)
                {
                    for (int i = 0; i < sourceInventories.Count; i++)
                    {
                        Debug.Log($"[SetActiveSourceIndex]   source[{i}] = {DescribeInventory(sourceInventories[i])}");
                    }
                }
            }

            // Also dump full sources list for clarity
            if (debugLogs)
            {
                for (int i = 0; i < sourceInventories.Count; i++)
                {
                    Debug.Log($"[SetActiveSourceIndex]   source[{i}] = {DescribeInventory(sourceInventories[i])}");
                }
            }

            RefreshFromInventory();
            UpdateStatsUI();
        }

        /// <summary>
        /// �����据源中的 ItemInstance 按首个可用位置落地到网格；
        /// 若 clearAndRebuildOnBind=true，会先清空现有UI再逐一放置，确保映射一致性。
        /// 现已支持按照“活动来源”的 rows/cols ���动配置 GridView。
        /// </summary>
        public void RefreshFromInventory()
        {
            var src = ActiveSource;
            if (src == null || gridView == null)
            {
                if (debugLogs) Debug.LogWarning("[InventoryUIBinder] Refresh aborted: ActiveSource or gridView is null.");
                return;
            }

            if (!src.gameObject.scene.IsValid())
            {
                if (debugLogs)
                {
                    Debug.LogWarning("[InventoryUIBinder] ActiveSource 不在场景中（可能是预制体资产），已跳过刷新；等待运行时实例化与收集。");
                }
                return;
            }

            if (useUniformCellSize && gridView != null)
            {
                gridView.autoFitToContainer = false;
                gridView.autoResizeContainer = true;
                gridView.cellSize = uniformCellSize;
                gridView.spacing = uniformSpacing;
                gridView.padding = uniformPadding;
            }

            gridView.SourceInventory = src;
            // 显式指定装备组件，避免 UI 侧 eq 解析失败
            var eq = src.GetComponent<CharacterEquipment>()
                     ?? src.GetComponentInParent<CharacterEquipment>()
                     ?? src.GetComponentInChildren<CharacterEquipment>(true);
            gridView.OverrideEquipment = eq;
            if (debugLogs && eq == null)
            {
                Debug.LogWarning("[InventoryUIBinder] 未找到 CharacterEquipment（src 自身/父/子），UI 将无法显示‘已装备’状态。");
            }

            // 可选：在生成 UI 之前为空槽位自动装备一件同类物品
            if (autoEquipOnBind)
            {
                TryAutoEquipDefaults(src);
            }

            if (gridView.rows != src.rows || gridView.cols != src.cols)
            {
                gridView.Configure(src.rows, src.cols);
            }
            else if (clearAndRebuildOnBind)
            {
                gridView.ClearAndRebuild();
            }

            gridView.RefreshLayoutSize();

            var items = src.Items;
            if (debugLogs)
            {
                Debug.Log($"[InventoryUIBinder] Refresh items count = {items.Count} from '{src.gameObject.name}'");
            }
            for (int i = 0; i < items.Count; i++)
            {
                var inst = items[i];
                if (inst == null)
                {
                    if (debugLogs) Debug.LogWarning($"[InventoryUIBinder] item[{i}] is null, skip");
                    continue;
                }
                var view = gridView.SpawnInstance(inst);
                if (debugLogs)
                {
                    string itemName = inst.data != null ? inst.data.displayName : inst.instanceId;
                    Debug.Log(view != null ? $"[InventoryUIBinder] Spawned: {itemName}" : $"[InventoryUIBinder] Spawn failed: {itemName}");
                }
            }

            // 新增：统一刷新一次“已装备”标签，避免启动顺序竞态导致首次不显示
            gridView.RefreshAllEquipLabels();
        }

        /// <summary>
        /// 可��：根据当前活动来源所在对象上的 CharacterStats 刷新属性 UI。
        /// </summary>
        public void UpdateStatsUI()
        {
            // 若未手动赋值，尝试自动查找一次（仅在本对象及其子物体中）
            if (statsUIBinder == null)
            {
                if (debugLogs) Debug.Log("[UpdateStatsUI] statsUIBinder 为 null，尝试自动查找 ICharacterStatsUIBinder。");
                var all = GetComponentsInChildren<MonoBehaviour>(true);
                for (int i = 0; i < all.Length; i++)
                {
                    var mb = all[i];
                    if (mb == null) continue;
                    if (mb is ICharacterStatsUIBinder)
                    {
                        statsUIBinder = mb;
                        if (debugLogs)
                        {
                            Debug.Log($"[UpdateStatsUI] 自动绑定 statsUIBinder: {mb.GetType().Name} (GameObject={mb.gameObject.name})");
                        }
                        break;
                    }
                }
                if (statsUIBinder == null)
                {
                    if (debugLogs) Debug.LogWarning("[UpdateStatsUI] 未找到属性UI绑定器（ICharacterStatsUIBinder）。已跳过属性刷新。");
                    return;
                }
            }

            var binder = statsUIBinder as ICharacterStatsUIBinder;
            if (binder == null)
            {
                if (debugLogs) Debug.LogWarning("[UpdateStatsUI] statsUIBinder 类型不匹配，尝试在子物体中修正绑定。");
                // 尝试兜底自动查找一次（即便 statsUIBinder 被错误赋值为其他类型）
                var all = GetComponentsInChildren<MonoBehaviour>(true);
                for (int i = 0; i < all.Length; i++)
                {
                    var mb = all[i];
                    if (mb == null) continue;
                    if (mb is ICharacterStatsUIBinder)
                    {
                        statsUIBinder = mb;
                        binder = (ICharacterStatsUIBinder)mb;
                        if (debugLogs)
                        {
                            Debug.Log($"[UpdateStatsUI] 修正：statsUIBinder 已切换到 {mb.GetType().Name} (GameObject={mb.gameObject.name})");
                        }
                        break;
                    }
                }
                if (binder == null)
                {
                    Debug.LogWarning("statsUIBinder 未实现 ICharacterStatsUIBinder，且未能在子物体中找到可用绑定器。已忽略属性UI刷新。");
                    return;
                }
            }

            var src = ActiveSource;
            CharacterStats stats = null;
            if (src != null)
            {
                // 兼容多种挂载位置：自身 -> 父级 -> 子级（含未激活）
                stats = src.GetComponent<CharacterStats>();
                if (stats == null) stats = src.GetComponentInParent<CharacterStats>();
                if (stats == null) stats = src.GetComponentInChildren<CharacterStats>(true);

                if (debugLogs) Debug.Log($"[UpdateStatsUI] ActiveSource={src.gameObject.name}, FoundStats={(stats!=null?stats.characterName:"null")}");

                if (stats == null && debugLogs)
                {
                    Debug.LogWarning($"[InventoryUIBinder] 未在 ActiveSource('{src.gameObject.name}') 的自身/父级/子级上找到 CharacterStats。");
                }
            }
            else if (debugLogs)
            {
                Debug.LogWarning("[InventoryUIBinder] UpdateStatsUI 时 ActiveSource 为空。");
            }

            if (stats != null)
            {
                binder.Bind(stats);
                if (debugLogs)
                {
                    Debug.Log($"[InventoryUIBinder] 已绑定属性到 UI（角色: {stats.characterName}）。");
                }
            }
            else
            {
                binder.Unbind();
                if (debugLogs)
                {
                    Debug.Log("[InventoryUIBinder] 未找到可绑定的 CharacterStats，已清空属性 UI。");
                }
            }
        }

        /// <summary>
        /// 作为示例的最小“新增”接口：尝试将一个新 SO 物品加入“当前活动背包”。
        /// 仅当能在当前网格找到位置并成功落地时，才真正加入数据源；否则不改动数据源。
        /// </summary>
        public bool TryAddNew(ItemBaseSO so)
        {
            var src = ActiveSource;
            if (so == null || src == null || gridView == null) return false;
            var inst = CharacterInventory.CreateInstance(so);
            if (inst == null) return false;

            var view = gridView.SpawnInstance(inst);
            if (view == null) return false; // 没有空间

            src.AddInstance(inst);
            // 事件驱动刷新：主动广播变更
            if (enableEventChannels && inventoryChangedChannel != null)
            {
                inventoryChangedChannel.RaiseEvent(src);
            }
            else if (debugLogs)
            {
                Debug.LogWarning("[InventoryUIBinder] TryAddNew: 未配置 InventoryChangedChannel，已添加物品但无法事件化刷新（依赖兜底路径）。");
            }
            return true;
        }

        /// <summary>
        /// 从 UI 与“当前活动数据源”同时移除实例。
        /// </summary>
        public bool Remove(ItemInstance inst)
        {
            var src = ActiveSource;
            if (inst == null || src == null || gridView == null) return false;
            bool ok = gridView.Remove(inst);
            bool okData = src.RemoveInstance(inst);
            if (okData && enableEventChannels && inventoryChangedChannel != null)
            {
                inventoryChangedChannel.RaiseEvent(src);
            }
            else if (okData && debugLogs && (!enableEventChannels || inventoryChangedChannel == null))
            {
                Debug.LogWarning("[InventoryUIBinder] Remove: 未配置 InventoryChangedChannel，已移除物品但无法事件化刷新（依赖兜底路径）。");
            }
            return okData && ok;
        }

        // 当玩家阵型根据 FormationContainer 生成完毕时触发
        private void HandlePlayerFormationGenerated(List<CharacterStats> playerStats)
        {
            if (playerStats == null || playerStats.Count == 0)
            {
                if (debugLogs) Debug.LogWarning("[InventoryUIBinder] HandlePlayerFormationGenerated: playerStats is null or empty.");
                return;
            }

            // 尝试自动收集场景中已存在的背包（包括未激活的物体）
            CollectExistingInventoriesInScene();

            // 切换到当前阵型的主角背包
            for (int i = 0; i < playerStats.Count; i++)
            {
                var stats = playerStats[i];
                if (stats == null) continue;
                var inv = stats.GetComponent<CharacterInventory>();
                if (inv == null) continue;
                if (!sourceInventories.Contains(inv)) continue;

                Debug.Log($"[InventoryUIBinder] HandlePlayerFormationGenerated: switching to inventory of {stats.characterName}");
                SetActiveSourceIndex(sourceInventories.IndexOf(inv));
                return;
            }

            Debug.LogWarning("[InventoryUIBinder] HandlePlayerFormationGenerated: No valid CharacterInventory found for the given playerStats.");
        }

        /// <summary>
        /// Advance to the next available source. This wraps SetActiveSourceIndex with extra
        /// logic to avoid no-op when multiple entries reference the same GameObject.
        /// </summary>
        public void NextSource()
        {
            var s = Sources;
            int count = s != null ? s.Count : 0;
            if (count == 0)
            {
                if (debugLogs) Debug.Log("[InventoryUIBinder] NextSource: no sources available.");
                return;
            }
            if (s == null) return; // extra guard for static analyzer
            int start = activeSourceIndex;
            int candidate = (start + 1) % count;
            // Try to find a candidate with a different GameObject than current ActiveSource
            var cur = ActiveSource != null ? ActiveSource.gameObject : null;
            for (int i = 0; i < count; i++)
            {
                var idx = (start + 1 + i) % count;
                var inv = s[idx];
                if (inv == null) continue;
                if (cur == null || inv.gameObject != cur)
                {
                    if (debugLogs) Debug.Log($"[InventoryUIBinder] NextSource -> switching to index {idx}");
                    SetActiveSourceIndex(idx);
                    return;
                }
            }
            // All entries point to same GameObject; still rotate index to next to preserve round-robin behaviour
            if (debugLogs) Debug.Log("[InventoryUIBinder] NextSource: all sources map to same GameObject, rotating index anyway.");
            SetActiveSourceIndex(candidate);
        }

        /// <summary>
        /// Move to the previous available source. Similar protections as NextSource().
        /// </summary>
        public void PrevSource()
        {
            var s = Sources;
            int count = s != null ? s.Count : 0;
            if (count == 0)
            {
                if (debugLogs) Debug.Log("[InventoryUIBinder] PrevSource: no sources available.");
                return;
            }
            if (s == null) return; // extra guard for static analyzer
            int start = activeSourceIndex;
            int candidate = (start - 1 + count) % count;
            var cur = ActiveSource != null ? ActiveSource.gameObject : null;
            for (int i = 0; i < count; i++)
            {
                var idx = (start - 1 - i + count * 1000) % count; // safe modulo
                var inv = s[idx];
                if (inv == null) continue;
                if (cur == null || inv.gameObject != cur)
                {
                    if (debugLogs) Debug.Log($"[InventoryUIBinder] PrevSource -> switching to index {idx}");
                    SetActiveSourceIndex(idx);
                    return;
                }
            }
            if (debugLogs) Debug.Log("[InventoryUIBinder] PrevSource: all sources map to same GameObject, rotating index anyway.");
            SetActiveSourceIndex(candidate);
        }

        // 便捷方法：描述背包内容的简洁字符串（用于调试日志）
        private string DescribeInventory(CharacterInventory inv)
        {
            if (inv == null) return "<null>";
            return $"<CharacterInventory: {inv.gameObject.name}, ItemsCount={inv.Items.Count}, Rows={inv.rows}, Cols={inv.cols}>";
        }

        // 尝试从已存在的阵型中绑定角色属性 UI（仅在场景中已存在的角色）
        private void TryBindStatsFromExistingFormation()
        {
            if (statsUIBinder == null) return;

            var activeSrc = ActiveSource;
            if (activeSrc == null) return;

            // 仅在场景中已存在的角色才尝试绑定属性 UI
            var playerStats = FindObjectsByType<CharacterStats>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < playerStats.Length; i++)
            {
                var stats = playerStats[i];
                if (stats == null) continue;
                if (!stats.gameObject.scene.IsValid()) continue; // 仅限于场景中的实例

                if (debugLogs) Debug.Log($"[InventoryUIBinder] TryBindStatsFromExistingFormation: checking {stats.characterName}");

                // 如果角色的背包就是当前活动背包，则尝试绑定属性 UI
                var inv = stats.GetComponent<CharacterInventory>();
                if (inv != null && inv == activeSrc)
                {
                    if (debugLogs) Debug.Log($"[InventoryUIBinder] TryBindStatsFromExistingFormation: binding stats for {stats.characterName}");
                    var binder = statsUIBinder as ICharacterStatsUIBinder;
                    if (binder != null) binder.Bind(stats);
                    return;
                }
            }

            if (debugLogs) Debug.LogWarning("[InventoryUIBinder] TryBindStatsFromExistingFormation: No valid CharacterStats found in the scene to bind.");
        }

        /// <summary>
        /// Public helper: ensure this binder has collected any CharacterInventory instances in the scene.
        /// This wraps the existing private collection logic so external classes can trigger a retry without using reflection.
        /// </summary>
        public void EnsureCollected()
        {
            try
            {
                CollectExistingInventoriesInScene();
            }
            catch (System.Exception ex)
            {
                if (debugLogs) Debug.LogWarning($"[InventoryUIBinder] EnsureCollected failed: {ex}");
            }
        }

        private void TryAutoEquipDefaults(CharacterInventory src)
        {
            if (src == null) return;
            var eq = src.GetComponent<CharacterEquipment>()
                     ?? src.GetComponentInParent<CharacterEquipment>()
                     ?? src.GetComponentInChildren<CharacterEquipment>(true);
            if (eq == null) return;
            var items = src.Items;
            if (items == null || items.Count == 0) return;

            // 主手武器
            if (eq.mainHand == null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    var it = items[i];
                    if (it?.data != null && it.data.isWeapon && eq.CanEquip(it)) { eq.EquipMainHand(it); break; }
                }
            }
            // 护甲
            if (eq.armor == null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    var it = items[i];
                    if (it?.data != null && it.data.isArmor && eq.CanEquip(it)) { eq.EquipArmor(it); break; }
                }
            }
            // 盾牌
            if (eq.shield == null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    var it = items[i];
                    if (it?.data != null && it.data.isShield && eq.CanEquip(it)) { eq.EquipShield(it); break; }
                }
            }
        }

        // Implement the IExcludableFromPause interface to handle exclusion from pause
        public void OnPauseExcluded()
        {
            // No action needed; inventory UI remains unaffected by pause state
            if (debugLogs)
            {
                Debug.Log("InventoryUIBinder: Ignored pause state change.");
            }
        }

        // 事件通道：InventoryChanged -> 若是当前激活来源则刷新
        private void HandleInventoryChangedChannel(CharacterInventory inv)
        {
            if (!enableEventChannels || inv == null) return;
            if (inv == ActiveSource)
            {
                if (debugLogs) Debug.Log("[InventoryUIBinder] InventoryChangedChannel 命中当前激活来源，刷新网格与属性 UI。");
                RefreshFromInventory();
                UpdateStatsUI();
            }
            else if (debugLogs)
            {
                // 仅调试输出一次来源描述
                if (debugLogs) Debug.Log($"[InventoryUIBinder] 收到 InventoryChangedChannel 但非当前激活来源 -> {DescribeInventory(inv)}");
            }
        }

        // 事件通道：ActiveCharacterChanged -> 尝试切换到该角色的背包
        private void HandleActiveCharacterChangedChannel(CharacterStats stats)
        {
            if (!enableEventChannels || stats == null) return;
            var inv = stats.GetComponent<CharacterInventory>();
            if (inv == null)
            {
                if (debugLogs) Debug.LogWarning($"[InventoryUIBinder] ActiveCharacterChanged: 角色 '{stats.characterName}' 未挂载 CharacterInventory。");
                return;
            }
            // 若未收集过该背包并允许自动收集 -> 收集
            if (!sourceInventories.Contains(inv))
            {
                if (autoCollectOnActiveCharacterEvent)
                {
                    if (debugLogs) Debug.Log($"[InventoryUIBinder] ActiveCharacterChanged: 自动收集新背包 {DescribeInventory(inv)}");
                    // 避免重复：执行最小化收集加入
                    if (inv.gameObject.scene.IsValid()) sourceInventories.Add(inv);
                }
                else
                {
                    if (debugLogs) Debug.LogWarning("[InventoryUIBinder] ActiveCharacterChanged: 背包未在已收集列表中且未启用自动收集，保持现状。");
                    return;
                }
            }
            int idx = sourceInventories.IndexOf(inv);
            if (idx >= 0)
            {
                if (debugLogs) Debug.Log($"[InventoryUIBinder] ActiveCharacterChanged: 切换激活背包 -> index={idx}");
                SetActiveSourceIndex(idx);
            }
            else if (debugLogs)
            {
                Debug.LogWarning("[InventoryUIBinder] ActiveCharacterChanged: 虽已尝试收集但未能在列表中找到该背包。");
            }
        }

        private void HandleAddItemRequest(InventoryAddItemRequest req)
        {
            if (!enableEventChannels) return;
            if (req.item == null)
            {
                if (debugLogs) Debug.LogWarning("[InventoryUIBinder] HandleAddItemRequest: item 为 null，忽略。");
                return;
            }
            var target = req.inventory != null ? req.inventory : ActiveSource;
            if (target == null)
            {
                if (debugLogs) Debug.LogWarning("[InventoryUIBinder] HandleAddItemRequest: 无有效背包（ActiveSource 为空且请求未指定）。");
                return;
            }
            // 仅当请求的背包正是当前激活来源才尝试即时落地；否则忽略（可扩展为延迟列表）
            if (target != ActiveSource)
            {
                if (debugLogs) Debug.Log("[InventoryUIBinder] HandleAddItemRequest: 请求目标不是当前激活背包，已忽略（未来可扩展队伍整体同步）。");
                return;
            }

            int amount = req.amount <= 0 ? 1 : req.amount;
            int placed = 0;
            for (int i = 0; i < amount; i++)
            {
                bool ok = TryAddNew(req.item);
                if (!ok)
                {
                    if (debugLogs)
                    {
                        Debug.LogWarning("[InventoryUIBinder] HandleAddItemRequest: 背包无空间，停止继续放置后续物品。");
                    }
                    break;
                }
                placed++;
            }

            if (debugLogs)
            {
                Debug.Log($"[InventoryUIBinder] HandleAddItemRequest: 请求数量={amount}，成功放置={placed}。");
            }
        }
    }
}
