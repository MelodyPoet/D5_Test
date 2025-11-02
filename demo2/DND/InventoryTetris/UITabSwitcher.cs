// filepath: d:\UnityProject\Archive\Assets\demo2\DND\InventoryTetris\UITabSwitcher.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using demo2.DND.Utility;

namespace demo2.DND.InventoryTetris
{
    /// <summary>
    /// 左侧标签切换：背包/角色属性 面板显隐控制(严格手动挂载)。
    /// 使用方法：
    /// - 将本脚本挂在任意激活的场景对象(例如 UI_Root)
    /// - 在 Inspector 中手动拖入 btnBackpack、btnCharacter、backpackPanel、characterPanel。
    /// - 启动后会尝试自动发现背包面板内的 Next/Prev 按钮并把它们绑定到本脚本的 PanelNext/PanelPrev 路由。
    /// </summary>
    public class UITabSwitcher : MonoBehaviour
    {
        public enum StartPanelState { None, Backpack, Character }

        [Header("按钮(手动拖入场景里的实例)")]
        public Button btnBackpack;
        public Button btnCharacter;
        // Optional: navigation buttons that live inside the backpack panel (auto-discovered or manually assigned)
        public Button panelNextButton;
        public Button panelPrevButton;

        [Header("面板(手动拖入场景里的实例)")]
        public GameObject backpackPanel;
        public GameObject characterPanel;

        [Header("启用显示状态")]
        public StartPanelState startState = StartPanelState.None;

        [Header("Esc 关闭(可选)")]
        public bool enableEscClose = true;

        [Header("调试快捷键 (可选)")]
        [Tooltip("启用后，按键盘左右方向键可触发 PanelNext/PanelPrev，便于无 UI 绑定时调试")]
        public bool enableDebugKeys = false;

        // Cache of the binders last refreshed when showing a panel. Next/Prev will route to these.
        private InventoryUIBinder[] lastRefreshedBinders;

        // Track whether we added runtime listeners so Unwire can remove them without touching persistent bindings
        private bool navListenersWiredNext = false;
        private bool navListenersWiredPrev = false;
        // Remember the Button instances we wired so we can unhook if they are replaced/recreated
        private Button wiredNextTarget = null;
        private Button wiredPrevTarget = null;

        private void OnEnable()
        {
            if (btnBackpack != null) btnBackpack.onClick.AddListener(ToggleBackpack);
            if (btnCharacter != null) btnCharacter.onClick.AddListener(ToggleCharacter);
            // Auto-discover Next/Prev buttons that live under the backpack panel and wire them
            AutoWirePanelNavButtons();
            // Listen for runtime inventory changes to keep binder cache in sync
            try { CharacterInventory.OnAnyInventoryReady += HandleAnyInventoryReady; } catch { }
            try { CharacterInventory.OnAnyInventoryDestroyed += HandleAnyInventoryDestroyed; } catch { }
        }

        private void OnDisable()
        {
            if (btnBackpack != null) btnBackpack.onClick.RemoveListener(ToggleBackpack);
            if (btnCharacter != null) btnCharacter.onClick.RemoveListener(ToggleCharacter);
            UnwirePanelNavButtons();
            try { CharacterInventory.OnAnyInventoryReady -= HandleAnyInventoryReady; } catch { }
            try { CharacterInventory.OnAnyInventoryDestroyed -= HandleAnyInventoryDestroyed; } catch { }
        }

        private void Start()
        {
            switch (startState)
            {
                case StartPanelState.Backpack:
                    ShowBackpack();
                    break;
                case StartPanelState.Character:
                    ShowCharacter();
                    break;
                default:
                    ShowNone();
                    break;
            }
        }

        private void Update()
        {
            if (!enableEscClose) return;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ShowNone();
            }

            // Optional debug keys to test navigation without buttons
            if (enableDebugKeys)
            {
                if (Input.GetKeyDown(KeyCode.RightArrow)) PanelNext();
                if (Input.GetKeyDown(KeyCode.LeftArrow)) PanelPrev();
            }
        }

        public void ShowBackpack()
        {
            // NOTE: Pause handling intentionally decoupled from UITabSwitcher.
            // The Pause button / Space key should only affect game pause state and not control UI visibility here.

            if (backpackPanel != null) backpackPanel.SetActive(true);
            if (characterPanel != null) characterPanel.SetActive(false);
            // Quick runtime check: ensure an EventSystem exists so UI clicks work
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                Debug.LogWarning("[UITabSwitcher] No EventSystem found in scene. UI button clicks may not be received.");
            }
            // Re-wire nav buttons in case they were not present at OnEnable or have changed
            AutoWirePanelNavButtons();
            // 调试/修复：当背包面板显示时，刷新 InventoryUIBinder 确保显示的是当前活跃来源
            RefreshInventoryBinders();
            // 便于诊断：打印面板内 Button 的持久化绑定
            if (DebugLogEnabled) LogPanelButtonBindings(backpackPanel);
            if (enableEscClose && DebugLogEnabled) Debug.Log("[UITabSwitcher] ShowBackpack -> refreshed InventoryUIBinder(s)");
        }

        public void ShowCharacter()
        {
            // NOTE: Pause handling intentionally decoupled from UITabSwitcher。
            if (backpackPanel != null) backpackPanel.SetActive(false);
            if (characterPanel != null) characterPanel.SetActive(true);
            // Re-wire nav buttons (in case character panel contains navigation in some layouts)
            AutoWirePanelNavButtons();
            // 同步刷新，确保属性面板显示一致
            RefreshInventoryBinders();
            if (enableEscClose && DebugLogEnabled) Debug.Log("[UITabSwitcher] ShowCharacter -> refreshed InventoryUIBinder(s)");
        }

        public void ToggleBackpack()
        {
            // NOTE: Do not treat Pause selection as special here; Pause is handled elsewhere.
            bool isActive = backpackPanel != null && backpackPanel.activeSelf;
            if (isActive) ShowNone(); else ShowBackpack();
            if (DebugLogEnabled) Debug.Log($"[UITabSwitcher] ToggleBackpack -> now {(backpackPanel!=null && backpackPanel.activeSelf ? "shown" : "hidden")}");
        }

        public void ToggleCharacter()
        {
            // NOTE: Do not treat Pause selection as special here; Pause is handled elsewhere.

            bool isActive = characterPanel != null && characterPanel.activeSelf;
            if (isActive) ShowNone(); else ShowCharacter();
        }

        public void ShowNone()
        {
            if (backpackPanel != null) backpackPanel.SetActive(false);
            if (characterPanel != null) characterPanel.SetActive(false);
            // clear cached binders when hiding panels
            lastRefreshedBinders = null;
        }

        // Helper: find and refresh all InventoryUIBinder instances in scene
        private bool DebugLogEnabled => true; // set to true to enable debug logs here
        private void RefreshInventoryBinders()
        {
            try
            {
                // Determine which panel is currently visible and collect binders under it.
                Transform panelRoot = null;
                if (backpackPanel != null && backpackPanel.activeInHierarchy) panelRoot = backpackPanel.transform;
                else if (characterPanel != null && characterPanel.activeInHierarchy) panelRoot = characterPanel.transform;

                InventoryUIBinder[] allBinders = FindObjectsOfType<InventoryUIBinder>(true);
                InventoryUIBinder[] binders;

                if (panelRoot != null)
                {
                    // Filter global list to only binders that belong to the visible panel. Some projects place binder components
                    // on objects that are not direct children of the panel, so we also check their gridView (if available) as a hint.
                    var list = new List<InventoryUIBinder>();
                    for (int i = 0; i < allBinders.Length; i++)
                    {
                        var b = allBinders[i];
                        if (b == null) continue;
                        // If binder GameObject is child of panel
                        if (panelRoot != null && b.transform.IsChildOf(panelRoot)) { list.Add(b); continue; }
                        // Otherwise, if it references a gridView that is under the panel, consider it part of panel
                        var gridField = typeof(InventoryUIBinder).GetField("gridView", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                        if (gridField != null)
                        {
                            var gv = gridField.GetValue(b) as UnityEngine.Object;
                            if (gv is Component gvComp && panelRoot != null && gvComp.transform.IsChildOf(panelRoot)) { list.Add(b); continue; }
                        }
                    }
                    binders = list.ToArray();

                    if ((binders == null || binders.Length == 0) && DebugLogEnabled) Debug.Log($"[UITabSwitcher] RefreshInventoryBinders -> panelRoot='{panelRoot.name}' found 0 binders.");
                }
                else
                {
                    binders = allBinders;
                }
                if (binders == null || binders.Length == 0)
                {
                    if (DebugLogEnabled) Debug.Log("[UITabSwitcher] No InventoryUIBinder instances found under visible panel.");
                    // Fallback: use all InventoryUIBinder instances in scene (some projects place binders outside panel hierarchy)
                    if (allBinders != null && allBinders.Length > 0)
                    {
                        if (DebugLogEnabled) Debug.Log($"[UITabSwitcher] Falling back to all {allBinders.Length} InventoryUIBinder instance(s) in scene.");
                        // Ensure each binder attempts to collect sources before caching
                        for (int i = 0; i < allBinders.Length; i++)
                        {
                            try { allBinders[i]?.EnsureCollected(); } catch { }
                        }
                        binders = allBinders;
                    }
                    else
                    {
                        if (DebugLogEnabled) Debug.Log("[UITabSwitcher] No InventoryUIBinder instances exist in scene at all.");
                        lastRefreshedBinders = new InventoryUIBinder[0];
                        return;
                    }
                }

                 // Cache the set we refreshed so navigation buttons route to the visible binders
                 lastRefreshedBinders = binders;
                 if (DebugLogEnabled) Debug.Log($"[UITabSwitcher] RefreshInventoryBinders -> cached {binders.Length} binder(s) for panelRoot={(panelRoot!=null?panelRoot.name:"<none>")}");
                 for (int i = 0; i < binders.Length; i++)
                 {
                     var b = binders[i];
                     if (b == null) continue;
                     try
                     {
                        // Ensure the binder has collected any CharacterInventory sources (some binders collect on OnEnable and may have missed
                        // runtime-created inventories if timings differed). This uses reflection to call the private collector as a safe fallback.
                         try
                         {
                            // Prefer public API to ensure the binder retriggers its internal collection logic
                            b.EnsureCollected();
                            if (DebugLogEnabled) Debug.Log($"[UITabSwitcher] Called EnsureCollected on '{b.gameObject.name}' to populate sources.");
                         }
                         catch (System.Exception _ex)
                         {
                            if (DebugLogEnabled) Debug.LogWarning($"[UITabSwitcher] EnsureCollected call failed: {_ex}");
                         }
                         b.RefreshFromInventory();
                         b.UpdateStatsUI();
                         if (DebugLogEnabled) Debug.Log($"[UITabSwitcher] Refreshed InventoryUIBinder on '{b.gameObject.name}'");
                     }
                     catch (System.Exception ex)
                     {
                         Debug.LogError($"[UITabSwitcher] Error refreshing InventoryUIBinder '{b.gameObject.name}': {ex}");
                     }
                 }
             }
             catch (System.Exception ex)
             {
                 if (DebugLogEnabled) Debug.LogWarning($"[UITabSwitcher] Exception in RefreshInventoryBinders: {ex}");
             }
         }

         // Auto-discover and wire the panel-level Next/Prev navigation buttons
         private void AutoWirePanelNavButtons()
         {
            // Keep a compact log to indicate runtime rebind attempts
             if (DebugLogEnabled) Debug.Log("[UITabSwitcher] AutoWirePanelNavButtons called");

            // If explicitly assigned in the Inspector or found, ensure we have runtime listeners attached
            if (panelNextButton != null)
            {
                try { panelNextButton.interactable = true; panelNextButton.enabled = true; } catch { }
                // Do not touch button.onClick to avoid removing persistent Inspector bindings or interfering with other listeners.
                // Clear any local wiring trackers so we don't attempt to remove listeners later.
                navListenersWiredNext = false;
                wiredNextTarget = null;

                if (DebugLogEnabled) Debug.Log($"[UITabSwitcher] Auto-wired panelNextButton (no runtime listener changes) -> {panelNextButton.gameObject.name}");
                if (DebugLogEnabled) LogButtonBinding(panelNextButton, "panelNextButton (AutoWire)");
            }
            if (panelPrevButton != null)
            {
                try { panelPrevButton.interactable = true; panelPrevButton.enabled = true; } catch { }
                navListenersWiredPrev = false;
                wiredPrevTarget = null;
                if (DebugLogEnabled) Debug.Log($"[UITabSwitcher] Auto-wired panelPrevButton (no runtime listener changes) -> {panelPrevButton.gameObject.name}");
                if (DebugLogEnabled) LogButtonBinding(panelPrevButton, "panelPrevButton (AutoWire)");
            }

            // Otherwise, try to find candidate buttons under the backpackPanel by name and cache references (and wire them)
            if ((panelNextButton == null || panelPrevButton == null) && backpackPanel != null)
            {
                var btns = backpackPanel.GetComponentsInChildren<Button>(true);
                for (int i = 0; i < btns.Length; i++)
                {
                    var b = btns[i];
                    if (b == null) continue;
                    var n = b.gameObject.name.ToLowerInvariant();
                    if (panelNextButton == null && (n.Contains("next") || n.Contains("right") || n.Contains(">")))
                    {
                        panelNextButton = b;
                        // Do not modify button.onClick; expect persistent binding in the Inspector
                         try { panelNextButton.interactable = true; panelNextButton.enabled = true; } catch { }
                         if (DebugLogEnabled) Debug.Log($"[UITabSwitcher] Auto-found & wired panelNextButton -> {b.gameObject.name}");
                         continue;
                    }
                    if (panelPrevButton == null && (n.Contains("prev") || n.Contains("previous") || n.Contains("left") || n.Contains("<")))
                    {
                        panelPrevButton = b;
                        // Do not modify button.onClick; expect persistent binding in the Inspector
                         try { panelPrevButton.interactable = true; panelPrevButton.enabled = true; } catch { }
                         if (DebugLogEnabled) Debug.Log($"[UITabSwitcher] Auto-found & wired panelPrevButton -> {b.gameObject.name}");
                         continue;
                    }
                    if (panelNextButton != null && panelPrevButton != null) break;
                }
            }

            // Final fallback: if still not found, perform a global search across the scene (covers layouts where nav buttons live outside the panel)
            if ((panelNextButton == null || panelPrevButton == null))
            {
                var allBtns = FindObjectsOfType<Button>(true);
                for (int i = 0; i < allBtns.Length; i++)
                {
                    var b = allBtns[i];
                    if (b == null) continue;
                    var n = b.gameObject.name.ToLowerInvariant();
                    if (panelNextButton == null && (n.Contains("next") || n.Contains("right") || n.Contains(">") || n.Contains("navnext") || n.Contains("pagenext")))
                    {
                        panelNextButton = b;
                        // Do not modify button.onClick; expect persistent binding in the Inspector
                         try { panelNextButton.interactable = true; panelNextButton.enabled = true; } catch { }
                         if (DebugLogEnabled) Debug.Log($"[UITabSwitcher] Global-auto-found & wired panelNextButton -> {b.gameObject.name}");
                         continue;
                    }
                    if (panelPrevButton == null && (n.Contains("prev") || n.Contains("previous") || n.Contains("left") || n.Contains("<") || n.Contains("navprev") || n.Contains("pageprev")))
                    {
                        panelPrevButton = b;
                        // Do not modify button.onClick; expect persistent binding in the Inspector
                         try { panelPrevButton.interactable = true; panelPrevButton.enabled = true; } catch { }
                         if (DebugLogEnabled) Debug.Log($"[UITabSwitcher] Global-auto-found & wired panelPrevButton -> {b.gameObject.name}");
                         continue;
                    }
                    if (panelNextButton != null && panelPrevButton != null) break;
                }
            }

            if (panelNextButton == null && DebugLogEnabled) Debug.LogWarning("[UITabSwitcher] panelNextButton not found or assigned.");
            if (panelPrevButton == null && DebugLogEnabled) Debug.LogWarning("[UITabSwitcher] panelPrevButton not found or assigned.");
         }

         // Exposed helper so you can trigger rebinding from the inspector (context menu) or via UI tools
         [ContextMenu("RebindPanelNavButtons")]
         public void RebindPanelNavButtons()
         {
             AutoWirePanelNavButtons();
         }

          // Remove listeners wired by AutoWirePanelNavButtons
          private void UnwirePanelNavButtons()
          {
              // Intentionally do not remove any listeners. Persistent (Inspector) bindings should be left intact.
              // This method only resets internal wiring tracking used previously.
              navListenersWiredNext = false;
              navListenersWiredPrev = false;
              wiredNextTarget = null;
              wiredPrevTarget = null;
          }

         // Handler invoked when the panel Next button is pressed. Routes to cached binders
         public void PanelNext()
         {
            // Ignore invocations that occur immediately after a pause toggle
            if (PauseController.StaticShouldIgnoreUIChanges())
            {
                if (DebugLogEnabled) Debug.Log("[UITabSwitcher] PanelNext ignored due to recent Pause toggle (suppression).");
                return;
            }

            // Diagnostics: print invocation + nav button state + EventSystem selection
            Debug.Log("[UITabSwitcher] PanelNext() invoked");
            if (panelNextButton != null)
            {
                Debug.Log($"[UITabSwitcher] panelNextButton: name={panelNextButton.gameObject.name}, activeInHierarchy={panelNextButton.gameObject.activeInHierarchy}, interactable={panelNextButton.interactable}");
                if (DebugLogEnabled) LogButtonBinding(panelNextButton, "panelNextButton (PanelNext)");
            }
            else
            {
                Debug.LogWarning("[UITabSwitcher] panelNextButton is NULL when PanelNext invoked.");
            }
            var es = UnityEngine.EventSystems.EventSystem.current;
            Debug.Log($"[UITabSwitcher] EventSystem.current?.currentSelectedGameObject = {(es != null && es.currentSelectedGameObject != null ? es.currentSelectedGameObject.name : "<null>")}");
            // Dump binders and panel button bindings to help identify if UI was recreated
            DumpAllBinders();
            if (backpackPanel != null) LogPanelButtonBindings(backpackPanel);

            // Ensure we have the freshest binders and their sources before attempting navigation
            RefreshInventoryBinders();
            var binders = GetBindersForVisiblePanel();
            if (binders == null || binders.Length == 0)
            {
                // As a fallback, use the last cached set
                if (DebugLogEnabled) Debug.Log("[UITabSwitcher] PanelNext: no binders under visible panel after refresh, using cached binders.");
                binders = lastRefreshedBinders ?? new InventoryUIBinder[0];
            }

            // Ensure binders have collected their sources; if none have sources schedule a retry
            bool anyHasSources = false;
            for (int bi = 0; bi < binders.Length; bi++)
            {
                var tb = binders[bi];
                if (tb == null) continue;
                try { tb.EnsureCollected(); } catch { }
                var s = tb.Sources;
                if (s != null && s.Count > 0) { anyHasSources = true; break; }
            }
            if (!anyHasSources)
            {
                if (DebugLogEnabled) Debug.Log("[UITabSwitcher] PanelNext: found binders but none have sources, scheduling retry navigation.");
                try { StartCoroutine(RetryNavigateBinders(binders, true)); } catch { }
                return;
            }

            // Keep cache in sync
            lastRefreshedBinders = binders;

            // Perform deterministic round-robin navigation by computing the next index and explicitly setting it.
            for (int i = 0; i < binders.Length; i++)
            {
                var b = binders[i];
                if (b == null) continue;
                try { b.EnsureCollected(); } catch { }
                var sources = b.Sources;
                int count = sources != null ? sources.Count : 0;
                if (DebugLogEnabled)
                {
                    Debug.Log($"[UITabSwitcher] Binder '{b.gameObject.name}' activeSourceIndex={b.activeSourceIndex}, sourcesCount={count}");
                }
                if (count == 0)
                {
                    if (DebugLogEnabled) Debug.Log($"[UITabSwitcher] Binder '{b.gameObject.name}' has 0 sources now; scheduling retry navigation.");
                    try { StartCoroutine(RetryNavigateBinders(new InventoryUIBinder[] { b }, true)); } catch { }
                    continue;
                }

                try
                {
                    Debug.Log($"[UITabSwitcher] Calling NextSource() on binder '{b.gameObject.name}' (currentIndex={b.activeSourceIndex}, sources={count})");
                    int before = b.activeSourceIndex;
                    b.NextSource();
                    Debug.Log($"[UITabSwitcher] NextSource completed for '{b.gameObject.name}', activeSourceIndex={b.activeSourceIndex}");
                    if (b.activeSourceIndex == before)
                    {
                        // Fallback: force rotation if binder's NextSource did not change the index
                        int forced = (before + 1) % count;
                        Debug.Log($"[UITabSwitcher] NextSource was no-op for '{b.gameObject.name}', forcing SetActiveSourceIndex({forced})");
                        b.SetActiveSourceIndex(forced);
                        Debug.Log($"[UITabSwitcher] Forced navigation completed for '{b.gameObject.name}', activeSourceIndex={b.activeSourceIndex}");
                    }
                 }
                 catch (System.Exception ex)
                 {
                     Debug.LogError($"[UITabSwitcher] Error routing PanelNext to binder '{b.gameObject.name}': {ex}");
                 }
            }

            // Re-wire nav buttons on the next frame because UI may be recreated after SetActiveSourceIndex
            try { StartCoroutine(AutoWireNextFrameCoroutine()); } catch { AutoWirePanelNavButtons(); }

         }

         // Handler invoked when the panel Prev button is pressed. Routes to cached binders
          public void PanelPrev()
           {
            // Ignore invocations that occur immediately after a pause toggle
            if (PauseController.StaticShouldIgnoreUIChanges())
            {
                if (DebugLogEnabled) Debug.Log("[UITabSwitcher] PanelPrev ignored due to recent Pause toggle (suppression).");
                return;
            }

           // Diagnostics: print invocation + nav button state + EventSystem selection
           Debug.Log("[UITabSwitcher] PanelPrev() invoked");
           if (panelPrevButton != null)
           {
               Debug.Log($"[UITabSwitcher] panelPrevButton: name={panelPrevButton.gameObject.name}, activeInHierarchy={panelPrevButton.gameObject.activeInHierarchy}, interactable={panelPrevButton.interactable}");
               if (DebugLogEnabled) LogButtonBinding(panelPrevButton, "panelPrevButton (PanelPrev)");
           }
           else
           {
               Debug.LogWarning("[UITabSwitcher] panelPrevButton is NULL when PanelPrev invoked.");
           }
           var es2 = UnityEngine.EventSystems.EventSystem.current;
           Debug.Log($"[UITabSwitcher] EventSystem.current?.currentSelectedGameObject = {(es2 != null && es2.currentSelectedGameObject != null ? es2.currentSelectedGameObject.name : "<null>")}");
           DumpAllBinders();
           if (backpackPanel != null) LogPanelButtonBindings(backpackPanel);

           // Ensure we have up-to-date binders
           RefreshInventoryBinders();
           var bindersPrev = GetBindersForVisiblePanel();
           if (bindersPrev == null || bindersPrev.Length == 0)
           {
               if (DebugLogEnabled) Debug.Log("[UITabSwitcher] PanelPrev: no binders under visible panel after refresh, using cached binders.");
               bindersPrev = lastRefreshedBinders ?? new InventoryUIBinder[0];
           }

           // If binders exist but none have sources, schedule retry
           bool anyHasSourcesPrev = false;
           for (int bi = 0; bi < bindersPrev.Length; bi++)
           {
               var tb = bindersPrev[bi];
               if (tb == null) continue;
               try { tb.EnsureCollected(); } catch { }
               var s = tb.Sources;
               if (s != null && s.Count > 0) { anyHasSourcesPrev = true; break; }
           }
           if (!anyHasSourcesPrev)
           {
               if (DebugLogEnabled) Debug.Log("[UITabSwitcher] PanelPrev: found binders but none have sources, scheduling retry navigation (prev).");
               try { StartCoroutine(RetryNavigateBinders(bindersPrev, false)); } catch { }
               return;
           }

           lastRefreshedBinders = bindersPrev;
           for (int i = 0; i < bindersPrev.Length; i++)
           {
                 var b = bindersPrev[i];
                  if (b == null) continue;
                 try { b.EnsureCollected(); } catch { }
                  var sources = b.Sources;
                  int count = sources != null ? sources.Count : 0;
                  if (DebugLogEnabled) Debug.Log($"[UITabSwitcher] Binder '{b.gameObject.name}' activeSourceIndex={b.activeSourceIndex}, sourcesCount={count}");
                if (count == 0)
                {
                    if (DebugLogEnabled) Debug.Log($"[UITabSwitcher] Binder '{b.gameObject.name}' has 0 sources now; scheduling retry navigation (prev).");
                    try { StartCoroutine(RetryNavigateBinders(new InventoryUIBinder[] { b }, false)); } catch { }
                    continue;
                }
                try
                {
                    Debug.Log($"[UITabSwitcher] Calling PrevSource() on binder '{b.gameObject.name}' (currentIndex={b.activeSourceIndex}, sources={count})");
                    int before = b.activeSourceIndex;
                    b.PrevSource();
                    Debug.Log($"[UITabSwitcher] PrevSource completed for '{b.gameObject.name}', activeSourceIndex={b.activeSourceIndex}");
                    if (b.activeSourceIndex == before)
                    {
                        int forced = (before - 1 + count) % count;
                        Debug.Log($"[UITabSwitcher] PrevSource was no-op for '{b.gameObject.name}', forcing SetActiveSourceIndex({forced})");
                        b.SetActiveSourceIndex(forced);
                        Debug.Log($"[UITabSwitcher] Forced Prev navigation completed for '{b.gameObject.name}', activeSourceIndex={b.activeSourceIndex}");
                    }
                 }
                 catch (System.Exception ex) { Debug.LogError($"[UITabSwitcher] Error routing PanelPrev to binder '{b.gameObject.name}': {ex}"); }
             }

           // Re-wire nav buttons on the next frame because UI might have been recreated during refresh
           try { StartCoroutine(AutoWireNextFrameCoroutine()); } catch { AutoWirePanelNavButtons(); }

         }

        // Coroutine to call AutoWire on next frame so we bind freshly created buttons
        private IEnumerator AutoWireNextFrameCoroutine()
        {
            // yield a frame to allow any UI recreation to finish
            yield return null;
            AutoWirePanelNavButtons();
            if (DebugLogEnabled)
            {
                Debug.Log($"[UITabSwitcher] AutoWireNextFrameCoroutine completed: panelNextButton.id={(panelNextButton!=null?panelNextButton.GetInstanceID():-1)}, panelPrevButton.id={(panelPrevButton!=null?panelPrevButton.GetInstanceID():-1)}");
            }
        }

        // Retry navigation on binders for a few frames until their Sources are populated, then invoke NextSource/PrevSource
        private IEnumerator RetryNavigateBinders(InventoryUIBinder[] binders, bool next, int maxFrames = 8)
        {
            int attempts = 0;
            while (attempts < maxFrames)
            {
                yield return null; // wait a frame
                attempts++;
                for (int i = 0; i < binders.Length; i++)
                {
                    var b = binders[i];
                    if (b == null) continue;
                    try { b.EnsureCollected(); } catch { }
                    var s = b.Sources;
                    int count = s != null ? s.Count : 0;
                    if (count > 0)
                    {
                        if (DebugLogEnabled) Debug.Log($"[UITabSwitcher] RetryNavigateBinders: binder '{b.gameObject.name}' now has {count} source(s); performing {(next?"Next":"Prev")}.");
                        try
                        {
                            if (next) b.NextSource(); else b.PrevSource();
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogError($"[UITabSwitcher] RetryNavigateBinders: Error invoking navigation on binder '{b.gameObject.name}': {ex}");
                        }
                    }
                }
                // If at least one binder had sources and was invoked, we can stop early
                bool anyNow = false;
                for (int i = 0; i < binders.Length; i++) { var b = binders[i]; if (b==null) continue; var s = b.Sources; if (s!=null && s.Count>0) { anyNow=true; break; } }
                if (anyNow) break;
            }
            if (DebugLogEnabled) Debug.Log($"[UITabSwitcher] RetryNavigateBinders finished after {attempts} frame(s).");
        }

        // Runtime handlers for inventory lifecycle events - keep binder cache in sync
        private void HandleAnyInventoryReady(CharacterInventory inv)
        {
            if (DebugLogEnabled) Debug.Log($"[TabSwitcher] HandleAnyInventoryReady -> { (inv!=null?inv.gameObject.name:"<null>") }");
            RefreshInventoryBinders();
        }

        private void HandleAnyInventoryDestroyed(CharacterInventory inv)
        {
            if (DebugLogEnabled) Debug.Log($"[TabSwitcher] HandleAnyInventoryDestroyed -> { (inv!=null?inv.gameObject.name:"<null>") }");
            RefreshInventoryBinders();
        }

        // Helper: get binders that belong to the currently visible panel (non-destructive)
        private InventoryUIBinder[] GetBindersForVisiblePanel()
        {
            Transform panelRoot = null;
            if (backpackPanel != null && backpackPanel.activeInHierarchy) panelRoot = backpackPanel.transform;
            else if (characterPanel != null && characterPanel.activeInHierarchy) panelRoot = characterPanel.transform;

            var all = FindObjectsOfType<InventoryUIBinder>(true);
            if (panelRoot == null) return all ?? new InventoryUIBinder[0];

            var list = new List<InventoryUIBinder>();
            for (int i = 0; i < all.Length; i++)
            {
                var b = all[i];
                if (b == null) continue;
                // If binder GameObject is child of panel
                if (panelRoot != null && b.transform.IsChildOf(panelRoot)) { list.Add(b); continue; }
                // Otherwise, if it references a gridView that is under the panel, consider it part of panel
                var gridField = typeof(InventoryUIBinder).GetField("gridView", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                if (gridField != null)
                {
                    var gv = gridField.GetValue(b) as UnityEngine.Object;
                    if (gv is Component gvComp && panelRoot != null && gvComp.transform.IsChildOf(panelRoot)) { list.Add(b); continue; }
                }
            }
            return list.ToArray();
        }

        // Debug: dump all binders and their sources to help diagnose issues
        private void DumpAllBinders()
        {
            var allBinders = FindObjectsOfType<InventoryUIBinder>(true);
            Debug.Log($"[UITabSwitcher] DumpAllBinders: found {allBinders.Length} binder(s) in scene.");
            for (int i = 0; i < allBinders.Length; i++)
            {
                var b = allBinders[i];
                if (b == null) continue;
                var sources = b.Sources;
                int count = sources != null ? sources.Count : 0;
                Debug.Log($"[UITabSwitcher]   binder[{i}] '{b.gameObject.name}': activeSourceIndex={b.activeSourceIndex}, sourcesCount={count}");
                if (count > 0)
                {
                    for (int si = 0; si < count; si++)
                    {
                        var inv = sources[si];
                        string invName = (inv != null && inv.gameObject != null) ? inv.gameObject.name : "<null>";
                        int itemsCount = 0;
                        int rows = -1, cols = -1;
                        try { itemsCount = inv != null && inv.Items != null ? inv.Items.Count : 0; } catch { }
                        try { rows = inv != null ? inv.rows : -1; cols = inv != null ? inv.cols : -1; } catch { }
                        Debug.Log($"[UITabSwitcher]     source[{si}] = {invName}, items={itemsCount}, rows={rows}, cols={cols}");
                    }
                }
            }
        }

        // Debug: log the persistent bindings of buttons in a panel (for diagnosis)
        private void LogPanelButtonBindings(GameObject panel)
        {
            if (panel == null) return;
            var btns = panel.GetComponentsInChildren<Button>(true);
            Debug.Log($"[UITabSwitcher] LogPanelButtonBindings: {btns.Length} button(s) found in panel '{panel.name}'.");
            for (int i = 0; i < btns.Length; i++)
            {
                var b = btns[i];
                if (b == null) continue;
                int persistent = b.onClick.GetPersistentEventCount();
                Debug.Log($"[UITabSwitcher]   button[{i}] '{b.gameObject.name}': persistentBindings={persistent}, interactable={b.interactable}, activeInHierarchy={b.gameObject.activeInHierarchy}");
                for (int pi = 0; pi < persistent; pi++)
                {
                    var target = b.onClick.GetPersistentTarget(pi) as UnityEngine.Object;
                    var method = b.onClick.GetPersistentMethodName(pi);
                    string targetName = target != null ? target.name : "<null>";
                    string targetType = target != null ? target.GetType().FullName : "<null>";
                    Debug.Log($"[UITabSwitcher]     persistent[{pi}] -> targetName={targetName}, targetType={targetType}, method={method}");
                }
            }
        }

        // Debug helper: log a single Button's persistent bindings and its full hierarchy path
        private void LogButtonBinding(Button b, string label)
        {
            if (b == null)
            {
                Debug.Log($"[UITabSwitcher] {label}: <null>");
                return;
            }
            // Full path
            var names = new List<string>();
            var t = b.gameObject.transform as Transform;
            while (t != null)
            {
                names.Add(t.name);
                t = t.parent;
            }
            names.Reverse();
            string path = string.Join("/", names);

            int persistent = b.onClick.GetPersistentEventCount();
            Debug.Log($"[UITabSwitcher] {label}: name={b.gameObject.name}, path={path}, persistentBindings={persistent}, interactable={b.interactable}, activeInHierarchy={b.gameObject.activeInHierarchy}");
            for (int pi = 0; pi < persistent; pi++)
            {
                var target = b.onClick.GetPersistentTarget(pi) as UnityEngine.Object;
                var method = b.onClick.GetPersistentMethodName(pi);
                string targetName = target != null ? target.name : "<null>";
                string targetType = target != null ? target.GetType().FullName : "<null>";
                Debug.Log($"[UITabSwitcher]   persistent[{pi}] -> targetName={targetName}, targetType={targetType}, method={method}");
            }
            // Also check whether the button is a child of the known backpackPanel
            if (backpackPanel != null)
            {
                bool isChild = b.transform.IsChildOf(backpackPanel.transform);
                Debug.Log($"[UITabSwitcher] {label}: IsChildOf(backpackPanel) = {isChild}");
            }
        }
    }
}
