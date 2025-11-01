using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace demo2.DND.InventoryTetris.Tools
{
    /// <summary>
    /// 运行时调试辅助：在每次鼠标左键按下时，打印当前指针下的 UI 射线检测结果、
    /// 并输出 Next/Prev 按钮的可交互状态与父 CanvasGroup 信息，方便定位哪个 UI 元素在拦截点击。
    /// 使用方式：把本脚本挂到场景中任意激活物体（例如 UI Root），在 Inspector 可手动指定 nextButton / prevButton 或启用自动查找。
    /// </summary>
    public class UIBlockerFinder : MonoBehaviour
    {
        [Header("要监控的按钮（可选，未设置时会自动查找名含 next/prev 的 Button）")]
        public Button nextButton;
        public Button prevButton;

        [Header("调试开关")]
        public bool enabledLogging = true;
        public bool autoFindButtons = true;
        [Tooltip("是否在运行时订阅 next/prev 的 onClick 并打印调用日志（临时调试）。")]
        public bool attachClickListener = true;

        private GraphicRaycaster cachedRaycaster;
        private EventSystem evsys;

        // store actions so we can remove them later
        private UnityEngine.Events.UnityAction nextBtnAction;
        private UnityEngine.Events.UnityAction prevBtnAction;

        private void Awake()
        {
            evsys = EventSystem.current;
            if (cachedRaycaster == null)
            {
                cachedRaycaster = FindObjectOfType<GraphicRaycaster>();
            }
            if (autoFindButtons) AutoFindButtons();
            if (attachClickListener)
            {
                if (nextButton != null)
                {
                    nextBtnAction = () => { Debug.Log("[UIBlockerFinder] NextButton.onClick invoked"); };
                    nextButton.onClick.AddListener(nextBtnAction);
                }
                if (prevButton != null)
                {
                    prevBtnAction = () => { Debug.Log("[UIBlockerFinder] PrevButton.onClick invoked"); };
                    prevButton.onClick.AddListener(prevBtnAction);
                }
            }
        }

        private void OnDestroy()
        {
            if (attachClickListener)
            {
                if (nextButton != null && nextBtnAction != null) nextButton.onClick.RemoveListener(nextBtnAction);
                if (prevButton != null && prevBtnAction != null) prevButton.onClick.RemoveListener(prevBtnAction);
            }
        }

        private void AutoFindButtons()
        {
            if (nextButton != null && prevButton != null) return;
            var all = FindObjectsOfType<Button>(true);
            foreach (var b in all)
            {
                if (b == null) continue;
                var n = b.gameObject.name.ToLowerInvariant();
                if (nextButton == null && (n.Contains("next") || n.Contains("right") || n.Contains(">"))) nextButton = b;
                if (prevButton == null && (n.Contains("prev") || n.Contains("previous") || n.Contains("left") || n.Contains("<"))) prevButton = b;
                if (nextButton != null && prevButton != null) break;
            }
        }

        private void Update()
        {
            if (!enabledLogging) return;
            if (evsys == null) evsys = EventSystem.current;
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 pos = Input.mousePosition;
                Debug.Log($"[UIBlockerFinder] MouseDown at {pos}");
                DoRaycastAndDump(pos);
                DumpButtonState(nextButton, "NextButton", pos);
                DumpButtonState(prevButton, "PrevButton", pos);
                Debug.Log($"[UIBlockerFinder] EventSystem.current.selectedGameObject = {(evsys!=null && evsys.currentSelectedGameObject!=null?evsys.currentSelectedGameObject.name:"<null>")}");
            }
        }

        private void DoRaycastAndDump(Vector2 screenPos)
        {
            if (evsys == null)
            {
                Debug.LogWarning("[UIBlockerFinder] EventSystem.current is null.");
                return;
            }

            if (cachedRaycaster == null) cachedRaycaster = FindObjectOfType<GraphicRaycaster>();
            if (cachedRaycaster == null)
            {
                Debug.LogWarning("[UIBlockerFinder] No GraphicRaycaster found in scene.");
                return;
            }

            var ped = new PointerEventData(evsys) { position = screenPos };
            var results = new List<RaycastResult>();
            cachedRaycaster.Raycast(ped, results);

            Debug.Log($"[UIBlockerFinder] Raycast hits = {results.Count}");
            for (int i = 0; i < results.Count; i++)
            {
                var r = results[i];
                var go = r.gameObject;
                string cg = "<none>";
                var cgComp = go.GetComponentInParent<CanvasGroup>();
                if (cgComp != null) cg = $"CanvasGroup (interactable={cgComp.interactable}, blocksRaycasts={cgComp.blocksRaycasts}, alpha={cgComp.alpha})";
                Debug.Log($"[UIBlockerFinder] #{i}: name={go.name}, path={GetPath(go)}, module={r.module?.GetType().Name}, worldPos={r.worldPosition}, canvasGroup={cg}");
            }

            // Also try full scene search for any object named OverlayAutoClose or having CanvasGroup.blocksRaycasts==true covering screen
            var overlay = GameObject.Find("OverlayAutoClose");
            if (overlay != null)
            {
                Debug.Log($"[UIBlockerFinder] Found runtime overlay object: {GetPath(overlay)}");
            }
        }

        private void DumpButtonState(Button btn, string label, Vector2 screenPos)
        {
            if (btn == null)
            {
                Debug.Log($"[UIBlockerFinder] {label} is <null> (未找到或未指定)");
                return;
            }
            var go = btn.gameObject;
            bool active = go.activeInHierarchy;
            bool interactable = btn.interactable;
            var rt = btn.GetComponent<RectTransform>();
            bool contains = rt != null ? RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, null) : false;

            // Check if any raycast hit was the button or its child
            var ped = new PointerEventData(evsys) { position = screenPos };
            var results = new List<RaycastResult>();
            if (cachedRaycaster == null) cachedRaycaster = FindObjectOfType<GraphicRaycaster>();
            if (cachedRaycaster != null) cachedRaycaster.Raycast(ped, results);
            bool hitButton = false;
            foreach (var r in results)
            {
                if (r.gameObject == go || r.gameObject.transform.IsChildOf(go.transform)) { hitButton = true; break; }
            }

            // Gather parent CanvasGroup info along the hierarchy
            var cgInfo = GetCanvasGroupInfo(go);

            Debug.Log($"[UIBlockerFinder] {label}: name={go.name}, path={GetPath(go)}, activeInHierarchy={active}, interactable={interactable}, containsPointer={contains}, raycastHit={hitButton}, canvasGroupInfo={cgInfo}");
        }

        private string GetCanvasGroupInfo(GameObject go)
        {
            var sb = new System.Text.StringBuilder();
            var t = go.transform;
            while (t != null)
            {
                var cg = t.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    sb.Append($"[{GetPath(t.gameObject)}: interactable={cg.interactable}, blocksRaycasts={cg.blocksRaycasts}, alpha={cg.alpha}]");
                }
                t = t.parent;
            }
            if (sb.Length == 0) return "<none>";
            return sb.ToString();
        }

        private string GetPath(GameObject go)
        {
            if (go == null) return "<null>";
            var names = new List<string>();
            var t = go.transform;
            while (t != null)
            {
                names.Add(t.name);
                t = t.parent;
            }
            names.Reverse();
            return string.Join("/", names);
        }
    }
}
