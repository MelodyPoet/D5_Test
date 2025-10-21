using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace demo2.DND.HorizontalFormation
{
    public class HealthBarUIManager : MonoBehaviour
    {
        // 当角色在受击时其血条尚未注册，记录到该集合并在随后帧重试刷新
        private HashSet<CharacterStats> pendingRefresh = new HashSet<CharacterStats>();
        private Coroutine pendingFlushCoroutine = null;

        public static HealthBarUIManager Instance { get; private set; }

        [Header("\u8840\u6761UI\u9884\u5236\u4f53")]
        public GameObject healthBarPrefab;
        [Header("\u73a9\u5bb6\u8840\u6761\u5bb9\u5668")]
        public Transform playerHealthBarContainer;
        [Header("\u654c\u4eba\u8840\u6761\u5bb9\u5668")]
        public Transform enemyHealthBarContainer;
        private Dictionary<CharacterStats, UI_HealthBar> healthBarMap = new Dictionary<CharacterStats, UI_HealthBar>();

        [Header("Safety")]
        [Tooltip("当为 true 时允许清理玩家侧血条；默认 false 可防止战斗时意外被清空")]
        public bool allowPlayerBarClear = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            Debug.Log($"[HealthBarUIManager] Awake: playerContainer={(playerHealthBarContainer!=null?playerHealthBarContainer.name:"null")}, enemyContainer={(enemyHealthBarContainer!=null?enemyHealthBarContainer.name:"null")}");

            // Ensure there is a Canvas to parent UI containers under
            Canvas canvas = FindObjectOfType<Canvas>();
            Transform canvasTransform = canvas != null ? canvas.transform : null;

            // If containers are missing, create them under Canvas (or root) to avoid null-parent issues
            if (playerHealthBarContainer == null)
            {
                GameObject playerContainerGO = new GameObject("PlayerHealthBarContainer");
                playerContainerGO.layer = (canvas != null) ? canvas.gameObject.layer : 0;
                playerHealthBarContainer = playerContainerGO.transform;
                if (canvasTransform != null)
                    playerHealthBarContainer.SetParent(canvasTransform, false);
                Debug.LogWarning("[HealthBarUIManager] playerHealthBarContainer was null - created PlayerHealthBarContainer under Canvas (or root).");
            }

            if (enemyHealthBarContainer == null)
            {
                GameObject enemyContainerGO = new GameObject("EnemyHealthBarContainer");
                enemyContainerGO.layer = (canvas != null) ? canvas.gameObject.layer : 0;
                enemyHealthBarContainer = enemyContainerGO.transform;
                if (canvasTransform != null)
                    enemyHealthBarContainer.SetParent(canvasTransform, false);
                Debug.LogWarning("[HealthBarUIManager] enemyHealthBarContainer was null - created EnemyHealthBarContainer under Canvas (or root).");
            }

            // Persist manager across scenes
            DontDestroyOnLoad(gameObject);

            // 尝试查找 HorizontalBattleFormationManager 并对比容器引用（常见 inspector 绑定错误）
            var formationManager = FindObjectOfType<HorizontalBattleFormationManager>();
            if (formationManager != null)
            {
                try
                {
                    var fmPlayer = typeof(HorizontalBattleFormationManager).GetField("playerHealthBarContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                    var fmEnemy = typeof(HorizontalBattleFormationManager).GetField("enemyHealthBarContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                    var fmPlayerVal = fmPlayer?.GetValue(formationManager) as Transform;
                    var fmEnemyVal = fmEnemy?.GetValue(formationManager) as Transform;

                    Debug.Log($"[HealthBarUIManager] Found HorizontalBattleFormationManager: playerContainer={(fmPlayerVal!=null?fmPlayerVal.name:"null")}, enemyContainer={(fmEnemyVal!=null?fmEnemyVal.name:"null")}");

                    if (fmPlayerVal != playerHealthBarContainer)
                    {
                        Debug.LogWarning("[HealthBarUIManager] playerHealthBarContainer 不匹配 HorizontalBattleFormationManager 中的引用，可能导致血条被创建在不同的父物体或被错误清理，请在 Inspector 中统一设置。");
                    }
                    if (fmEnemyVal != enemyHealthBarContainer)
                    {
                        Debug.LogWarning("[HealthBarUIManager] enemyHealthBarContainer 不匹配 HorizontalBattleFormationManager 中的引用，可能导致血条被创建在不同的父物体或被错误清理，请在 Inspector 中统一设置。");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[HealthBarUIManager] Awake: 比较容器引用时发生异常: {ex}");
                }
            }
        }

        // 调试输出当前映射和容器状态
        public void DumpStatus(string tag = "")
        {
            int playerChildren = playerHealthBarContainer != null ? playerHealthBarContainer.childCount : -1;
            int enemyChildren = enemyHealthBarContainer != null ? enemyHealthBarContainer.childCount : -1;
            int mapCount = healthBarMap.Count;
            Debug.Log($"[HealthBarUIManager] DumpStatus {tag}: playerChildren={playerChildren}, enemyChildren={enemyChildren}, mapCount={mapCount}");
        }

        // 调试：输出映射详细信息
        public void DumpMapDetails(string tag = "")
        {
            Debug.Log($"[HealthBarUIManager] DumpMapDetails {tag}: mapCount={healthBarMap.Count}");
            foreach (var kv in healthBarMap)
            {
                string charName = kv.Key != null ? kv.Key.GetDisplayName() : "null";
                int charId = kv.Key != null ? kv.Key.GetInstanceID() : 0;
                string side = kv.Key != null ? kv.Key.battleSide.ToString() : "null";
                string barName = kv.Value != null ? kv.Value.gameObject.name : "null";
                int barId = kv.Value != null ? kv.Value.gameObject.GetInstanceID() : 0;
                Debug.Log($"[HealthBarUIManager]  - Key: {charName} (ID:{charId}) side={side} => Bar: {barName} (ID:{barId})");
            }
        }

        // 初始化血条：只为传入列表中尚未注册的角色创建血条，避免清空已有血条
        public void InitializeBars(List<CharacterStats> activeCharacters)
        {
            DumpStatus("InitializeBars - before");
            if (activeCharacters == null) return;

            foreach (var character in activeCharacters)
            {
                if (character == null) continue;

                // 如果已经存在映射，则确保 owner 已正确设置并跳过创建
                if (healthBarMap.TryGetValue(character, out var existingBar) && existingBar != null)
                {
                    existingBar.SetOwner(character);
                    continue;
                }

                // 否则根据阵营创建新的血条
                Transform container = character.battleSide == BattleSide.Player ? playerHealthBarContainer : enemyHealthBarContainer;
                if (container == null)
                {
                    Debug.LogWarning($"HealthBarUIManager.InitializeBars: container 未配置 for {character.battleSide}");
                    continue;
                }

                if (healthBarPrefab == null)
                {
                    Debug.LogError("HealthBarUIManager.InitializeBars: healthBarPrefab 未设置，无法创建血条");
                    return;
                }

                GameObject go = Instantiate(healthBarPrefab, container);
                UI_HealthBar bar = go.GetComponent<UI_HealthBar>();
                if (bar == null)
                {
                    Debug.LogError("HealthBarUIManager.InitializeBars: healthBarPrefab 缺少 UI_HealthBar 组件");
                    Destroy(go);
                    continue;
                }

                // 注册并初始化
                healthBarMap[character] = bar;
                try
                {
                    bar.SetOwner(character);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"HealthBarUIManager.InitializeBars: SetOwner 发生异常 - {ex}");
                }
            }
            DumpStatus("InitializeBars - after");

            // 调试输出映射详细信息
            DumpMapDetails("InitializeBars");
        }

        // 刷新血条
        public void RefreshBar(CharacterStats character)
        {
            if (healthBarMap.TryGetValue(character, out var bar))
            {
                if (bar != null)
                    bar.RefreshDisplay();
                else
                    healthBarMap.Remove(character);
            }
            else
            {
                // 如果尚未注册对应血条，将请求放入待处理集合，并尝试短时重试
                if (character != null)
                {
                    if (!pendingRefresh.Contains(character))
                    {
                        pendingRefresh.Add(character);
                        Debug.LogWarning($"HealthBarUIManager.RefreshBar: 收到未注册角色的刷新请求，已加入 pendingRefresh: {character.GetDisplayName()}");
                    }

                    if (pendingFlushCoroutine == null)
                    {
                        pendingFlushCoroutine = StartCoroutine(TryFlushPendingRefreshes());
                    }
                }
            }
        }

        /// <summary>
        /// 在外部创建血条对象后注册到管理器（例如 HorizontalBattleFormationManager 在 Instantiate 后调用）
        /// </summary>
        public void RegisterBar(CharacterStats character, UI_HealthBar bar)
        {
            if (character == null || bar == null)
            {
                Debug.LogWarning("HealthBarUIManager.RegisterBar: character 或 bar 为 null，忽略注册。");
                return;
            }

            // 将 bar 的父对象设置为本管理器中对应阵营的容器，保证统一管理
            Transform targetContainer = character.battleSide == BattleSide.Player ? playerHealthBarContainer : enemyHealthBarContainer;
            if (targetContainer != null && bar.transform.parent != targetContainer)
            {
                bar.transform.SetParent(targetContainer, true); // 保持世界坐标
                Debug.Log($"HealthBarUIManager.RegisterBar: Reparented {bar.gameObject.name} to {targetContainer.name}");
            }

            if (healthBarMap.ContainsKey(character))
            {
                healthBarMap[character] = bar;
            }
            else
            {
                healthBarMap.Add(character, bar);
            }

            // 确保血条已设置 owner（在父级确定后设置）
            try
            {
                bar.SetOwner(character);

                // 立即校验绑定是否成功（使用 UI_HealthBar 提供的 Owner 访问器）
                if (bar.Owner != character)
                {
                    Debug.LogWarning($"HealthBarUIManager.RegisterBar: 绑定后 bar.Owner != character. bar.Owner={(bar.Owner!=null?bar.Owner.GetDisplayName():"null")}, expected={character.GetDisplayName()}");
                    // 如遇到时序问题，启动重试协程，在后续几帧内尝试再次绑定并刷新
                    StartCoroutine(EnsureOwnerBinding(character, bar));
                }

                // 立即刷新显示，作为保险（避免因订阅时序错过第一次更新）
                bar.RefreshDisplay();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"HealthBarUIManager.RegisterBar: SetOwner/Refresh 发生异常 - {ex}");
            }

            // 调试输出映射详细信息
            DumpMapDetails("RegisterBar");

            // 如果该 character 曾经处于 pendingRefresh 中，立即刷新并移除 pending 状态
            if (pendingRefresh.Contains(character))
            {
                pendingRefresh.Remove(character);
                try
                {
                    bar.RefreshDisplay();
                    Debug.Log($"HealthBarUIManager.RegisterBar: 已处理 pending refresh for {character.GetDisplayName()}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"HealthBarUIManager.RegisterBar: 处理 pending refresh 时发生异常 - {ex}");
                }
            }
        }

        /// <summary>
        /// 注销血条（清理用）
        /// </summary>
        public void UnregisterBar(CharacterStats character)
        {
            if (character == null) return;
            if (healthBarMap.ContainsKey(character))
            {
                healthBarMap.Remove(character);
            }
        }

        /// <summary>
        /// 移除并销毁指定角色的血条（安全检查后执行）。
        /// </summary>
        public void RemoveBarFor(CharacterStats character)
        {
            if (character == null) return;
            if (healthBarMap.TryGetValue(character, out var bar))
            {
                if (bar != null && bar.gameObject != null)
                {
                    Destroy(bar.gameObject);
                }
                healthBarMap.Remove(character);
                DumpStatus($"RemoveBarFor({character.GetDisplayName()})");
            }
        }

        // 显式清理玩家血条容器（当你想完全重建时调用）
        public void ClearPlayerHealthBars()
        {
            if (!allowPlayerBarClear)
            {
                Debug.LogWarning("HealthBarUIManager.ClearPlayerHealthBars 被调用，但 allowPlayerBarClear 为 false，已跳过以保护玩家血条不被意外清除。");
                DumpStatus("ClearPlayerHealthBars - skipped");
                DumpMapDetails("ClearPlayerHealthBars - skipped");
                return;
            }

            if (healthBarMap == null) return;

            var toRemoveKeys = new List<CharacterStats>();
            var toDestroy = new List<GameObject>();

            // 仅销毁那些 owner 已经为 null 或 owner.gameObject 已经被销毁的条目
            foreach (var kv in healthBarMap)
            {
                CharacterStats owner = kv.Key;
                UI_HealthBar bar = kv.Value;

                bool ownerMissing = (owner == null) || (owner.gameObject == null);

                if (ownerMissing)
                {
                    if (bar != null)
                    {
                        toDestroy.Add(bar.gameObject);
                    }
                    toRemoveKeys.Add(owner);
                }
            }

            // 如果没有找到明确要销毁的条目，输出警告并跳过全部销毁以防误删
            if (toDestroy.Count == 0)
            {
                Debug.LogWarning("HealthBarUIManager.ClearPlayerHealthBars: allowPlayerBarClear 为 true，但未找到明确可以销毁的玩家血条（所有条目仍有关联 owner）。已跳过，以防误删。");
                DumpStatus("ClearPlayerHealthBars - nothing to destroy");
                DumpMapDetails("ClearPlayerHealthBars - nothing to destroy");
                return;
            }

            // 销毁 UI 对象
            foreach (var go in toDestroy)
            {
                if (go != null) Destroy(go);
            }

            // 从映射中移除
            foreach (var k in toRemoveKeys) healthBarMap.Remove(k);

            // 作为兼容/调试，再输出容器状态
            DumpStatus("ClearPlayerHealthBars - after");

            // 调试输出映射详细信息
            DumpMapDetails("ClearPlayerHealthBars");
        }

        /// <summary>
        /// 强制清理玩家血条（绕过 allowPlayerBarClear）
        /// </summary>
        public void ForceClearPlayerHealthBars()
        {
            bool prev = allowPlayerBarClear;
            allowPlayerBarClear = true;
            ClearPlayerHealthBars();
            allowPlayerBarClear = prev;
        }

        // 显式清理敌人血条容器
        public void ClearEnemyHealthBars()
        {
            if (healthBarMap == null) return;

            var toRemoveKeys = new List<CharacterStats>();
            var toDestroy = new List<GameObject>();

            // 复制当前映射快照，避免遍历中修改
            var snapshot = new List<KeyValuePair<CharacterStats, UI_HealthBar>>(healthBarMap);
            foreach (var kv in snapshot)
            {
                CharacterStats owner = kv.Key;
                UI_HealthBar bar = kv.Value;

                bool isEnemyEntry = false;

                // 情况1：owner 仍存在，且为敌方
                if (owner != null)
                {
                    if (owner.battleSide == BattleSide.Enemy)
                    {
                        isEnemyEntry = true;
                    }
                }
                else
                {
                    // 情况2：owner 已被销毁（Unity下等同于null），尝试通过bar的父容器判断是否为敌方血条
                    if (bar != null && enemyHealthBarContainer != null)
                    {
                        try
                        {
                            if (bar.transform != null && bar.transform.parent != null)
                            {
                                if (bar.transform.IsChildOf(enemyHealthBarContainer))
                                {
                                    isEnemyEntry = true;
                                }
                            }
                        }
                        catch { }
                    }
                }

                if (isEnemyEntry)
                {
                    if (bar != null)
                    {
                        toDestroy.Add(bar.gameObject);
                    }
                    // 使用原始owner引用（即使已被销毁，引用仍可用于从字典移除）
                    toRemoveKeys.Add(kv.Key);
                }
            }

            foreach (var go in toDestroy)
            {
                if (go != null) Destroy(go);
            }

            foreach (var k in toRemoveKeys)
            {
                healthBarMap.Remove(k);
            }

            DumpStatus("ClearEnemyHealthBars - after");
            DumpMapDetails("ClearEnemyHealthBars");
        }

        /// <summary>
        /// 当初次绑定失败时重试 SetOwner/Refresh（缓解时序问题）
        /// </summary>
        private IEnumerator EnsureOwnerBinding(CharacterStats character, UI_HealthBar bar)
        {
            const int maxAttempts = 8;
            const int waitFrames = 1;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                // 等待若干帧
                for (int w = 0; w < waitFrames; w++) yield return null;

                if (bar == null)
                {
                    Debug.LogWarning($"EnsureOwnerBinding: bar 已销毁或为 null，停止重试 for {character?.GetDisplayName()}");
                    yield break;
                }

                try
                {
                    if (bar.Owner == character)
                    {
                        // 已成功绑定，刷新并退出
                        bar.RefreshDisplay();
                        Debug.Log($"EnsureOwnerBinding: 绑定成功 after {attempt} attempts for {character.GetDisplayName()}");
                        yield break;
                    }

                    // 再次尝试设置 owner
                    bar.SetOwner(character);

                    if (bar.Owner == character)
                    {
                        bar.RefreshDisplay();
                        Debug.Log($"EnsureOwnerBinding: 绑定成功 after SetOwner retry {attempt} for {character.GetDisplayName()}");
                        yield break;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"EnsureOwnerBinding: attempt {attempt} 异常 - {ex}");
                }
            }

            Debug.LogError($"EnsureOwnerBinding: 绑定失败（超时） for {character?.GetDisplayName()} after {maxAttempts} attempts. 请检查创建/注册时序和 prefab 配置。");
        }

        // 在短时间内多帧尝试将 pendingRefresh 刷新到已注册的条目
        private IEnumerator TryFlushPendingRefreshes()
        {
            const int maxAttempts = 10;
            const int framesBetween = 1;

            for (int attempt = 0; attempt < maxAttempts && pendingRefresh.Count > 0; attempt++)
            {
                // 等待若干帧
                for (int f = 0; f < framesBetween; f++) yield return null;

                var toRemove = new List<CharacterStats>();
                foreach (var character in pendingRefresh)
                {
                    if (character == null)
                    {
                        toRemove.Add(character);
                        continue;
                    }

                    if (healthBarMap.TryGetValue(character, out var bar) && bar != null)
                    {
                        try
                        {
                            bar.RefreshDisplay();
                            toRemove.Add(character);
                            Debug.Log($"TryFlushPendingRefreshes: 刷新成功 for {character.GetDisplayName()} on attempt {attempt}");
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogWarning($"TryFlushPendingRefreshes: 刷新时异常 for {character.GetDisplayName()} - {ex}");
                        }
                    }
                }

                foreach (var c in toRemove) pendingRefresh.Remove(c);
            }

            if (pendingRefresh.Count > 0)
            {
                Debug.LogWarning($"TryFlushPendingRefreshes: 部分 pendingRefresh 在重试后仍未得到处理 count={pendingRefresh.Count}. 可能需要检查注册/创建时序。");
            }

            pendingFlushCoroutine = null;
        }
    }
}
