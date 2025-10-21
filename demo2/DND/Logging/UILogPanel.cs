using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 游戏日志 UI 面板：订阅 GameLog 并在屏幕上显示滚动文本日志。
/// 将该组件挂到主屏幕（有 Canvas 下）某个对象上，指定 ScrollRect 与 Text；
/// 若留空并勾选 autoBuildUIIfMissing，会自动创建一个基础 ScrollView + Text。
/// </summary>
[DisallowMultipleComponent]
public class UILogPanel : MonoBehaviour
{
    [Header("UI 引用（可选）")]
    public ScrollRect scrollRect;
    public Text contentText;
    public InputField searchInput;

    [Header("行为设置")]
    [Tooltip("是否自动创建基础 UI（当未指定 ScrollRect/Text 时）")]
    public bool autoBuildUIIfMissing = true;
    [Tooltip("追加日志时自动滚动到底部")]
    public bool autoScrollToBottom = true;
    [Tooltip("仅当当前滚动条在底部时，追加新日志才自动滚动到底部（防止浏览历史时被强制拉到底）")]
    public bool autoScrollOnlyIfAtBottom = true;
    // 底部判定的容差，避免浮点误差导致刚好在底部却判定失败
    [Range(0f, 0.2f)] public float bottomTolerance = 0.02f;

    [Tooltip("最多保留的可见行数（0 或负数表示不限制）")]
    public int maxVisibleLines = 300;

    [Header("搜索设置")]
    [Tooltip("搜索是否区分大小写（仅在有搜索关键字时生效）")]
    public bool caseSensitiveSearch = false;

    [Header("频道过滤")]
    public bool showSystem = false;
    public bool showExploration = true;
    public bool showInitiative = true;
    public bool showCombatHit = true;
    public bool showCombatDamage = true;
    public bool showAction = true;

    private readonly List<string> _allLines = new List<string>(256);
    private readonly List<string> _lines = new List<string>(256);
    private readonly StringBuilder _sb = new StringBuilder(1024);

    private void Awake()
    {
        EnsureUI();
        if (!autoBuildUIIfMissing && (scrollRect == null || contentText == null))
        {
            Debug.LogWarning("[UILogPanel] 未开启自动构建且未指定 ScrollRect/Content Text 引用。请在 Inspector 赋值，或勾选 autoBuildUIIfMissing。");
        }
        if (searchInput != null)
        {
            searchInput.onValueChanged.AddListener(_ => RebuildFromAll());
        }
    }

    private void OnEnable()
    {
        GameLog.OnEntryAdded += HandleEntry;
        // 回放历史：把订阅前产生的日志也显示出来
        ReplayHistory();
    }

    private void OnDisable()
    {
        GameLog.OnEntryAdded -= HandleEntry;
        if (searchInput != null)
        {
            searchInput.onValueChanged.RemoveAllListeners();
        }
    }

    private void EnsureUI()
    {
        if (scrollRect != null && contentText != null) return;
        if (!autoBuildUIIfMissing) return;

        // 确保场景内有 EventSystem（用于处理输入）
        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            es.transform.SetParent(transform.root, worldPositionStays: false);
        }

        // 查找或创建 Canvas
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            var canvasGO = new GameObject("UILogCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            // 把当前组件放到 Canvas 下
            transform.SetParent(canvasGO.transform, worldPositionStays: false);
        }

        // 创建容器
        GameObject panel = new GameObject("UILogPanel", typeof(RectTransform), typeof(Image));
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.SetParent(canvas.transform, false);
        panelRect.anchorMin = new Vector2(0.65f, 0.05f);
        panelRect.anchorMax = new Vector2(0.98f, 0.45f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        var panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.4f);

        // 顶部搜索条
        GameObject searchBar = new GameObject("SearchBar", typeof(RectTransform), typeof(Image));
        var searchBarRect = searchBar.GetComponent<RectTransform>();
        searchBarRect.SetParent(panelRect, false);
        searchBarRect.anchorMin = new Vector2(0f, 1f);
        searchBarRect.anchorMax = new Vector2(1f, 1f);
        searchBarRect.pivot = new Vector2(0.5f, 1f);
        searchBarRect.sizeDelta = new Vector2(0f, 36f);
        searchBarRect.anchoredPosition = new Vector2(0f, 0f);
        searchBar.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);

        GameObject inputGO = new GameObject("InputField", typeof(RectTransform), typeof(Image), typeof(InputField));
        var inputRect = inputGO.GetComponent<RectTransform>();
        inputRect.SetParent(searchBarRect, false);
        inputRect.anchorMin = new Vector2(0f, 0f);
        inputRect.anchorMax = new Vector2(1f, 1f);
        inputRect.offsetMin = new Vector2(8f, 4f);
        inputRect.offsetMax = new Vector2(-8f, -4f);
        inputGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);

        // Placeholder
        GameObject placeholderGO = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
        var placeholderRect = placeholderGO.GetComponent<RectTransform>();
        placeholderRect.SetParent(inputRect, false);
        placeholderRect.anchorMin = new Vector2(0f, 0f);
        placeholderRect.anchorMax = new Vector2(1f, 1f);
        placeholderRect.offsetMin = new Vector2(8f, 0f);
        placeholderRect.offsetMax = new Vector2(-8f, 0f);
        var placeholderText = placeholderGO.GetComponent<Text>();
        placeholderText.text = "搜索关键词...";
        placeholderText.fontSize = 16;
        placeholderText.alignment = TextAnchor.MiddleLeft;
        placeholderText.color = new Color(1f, 1f, 1f, 0.3f);
        placeholderText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // Text
        GameObject inputTextGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
        var inputTextRect = inputTextGO.GetComponent<RectTransform>();
        inputTextRect.SetParent(inputRect, false);
        inputTextRect.anchorMin = new Vector2(0f, 0f);
        inputTextRect.anchorMax = new Vector2(1f, 1f);
        inputTextRect.offsetMin = new Vector2(8f, 0f);
        inputTextRect.offsetMax = new Vector2(-8f, 0f);
        var inputText = inputTextGO.GetComponent<Text>();
        inputText.text = string.Empty;
        inputText.fontSize = 16;
        inputText.alignment = TextAnchor.MiddleLeft;
        inputText.color = new Color(1f, 1f, 1f, 0.95f);
        inputText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        var input = inputGO.GetComponent<InputField>();
        input.textComponent = inputText;
        input.placeholder = placeholderText;
        searchInput = input;

        // ScrollRect + Mask + Viewport（留出搜索条高度）
        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        var viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.SetParent(panelRect, false);
        viewportRect.anchorMin = new Vector2(0f, 0f);
        viewportRect.anchorMax = new Vector2(1f, 1f);
        viewportRect.offsetMin = new Vector2(8f, 8f);
        viewportRect.offsetMax = new Vector2(-8f, -44f); // 顶部为搜索条预留高度
        viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.2f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        GameObject content = new GameObject("Content", typeof(RectTransform));
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.SetParent(viewportRect, false);
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0f, 1f);
        contentRect.offsetMin = new Vector2(0f, 0f);
        contentRect.offsetMax = new Vector2(0f, 0f);

        GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.SetParent(contentRect, false);
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(0f, 1f);
        textRect.offsetMin = new Vector2(8f, 0f);
        textRect.offsetMax = new Vector2(-8f, 0f);

        var txt = textGO.GetComponent<Text>();
        txt.text = string.Empty;
        txt.fontSize = 18;
        txt.alignment = TextAnchor.UpperLeft;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        // 选一个内置字体（在编辑器/运行时会自动分配默认字体）
        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        var sr = panel.AddComponent<ScrollRect>();
        sr.viewport = viewportRect;
        sr.content = contentRect;
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 30f;

        // 关键：Content 挂载 VerticalLayoutGroup + ContentSizeFitter，让其随子节点(Text)的首选高度自动扩展
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 0f;
        vlg.padding = new RectOffset(0, 0, 0, 0);

        var layout = content.AddComponent<ContentSizeFitter>();
        layout.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        layout.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scrollRect = sr;
        contentText = txt;

        // 将本组件放到面板下面，避免被销毁
        transform.SetParent(panelRect, false);
    }

    private void HandleEntry(GameLog.LogEntry entry)
    {
        if (!PassFilter(entry.Channel)) return;

        _sb.Length = 0;
        _sb.Append('[').Append(entry.Channel.ToString()).Append("] ");
        _sb.Append(entry.Message);
        string line = _sb.ToString();
        _allLines.Add(line);

        // 如果当前没有搜索词，或此行匹配搜索，则追加到可见列表
        if (PassSearch(line))
        {
            _lines.Add(line);
            // 控制最大行数
            if (maxVisibleLines > 0 && _lines.Count > maxVisibleLines)
            {
                int remove = _lines.Count - maxVisibleLines;
                _lines.RemoveRange(0, remove);
            }
            RefreshTextAndScroll();
        }
    }

    private bool PassFilter(GameLog.LogChannel ch)
    {
        switch (ch)
        {
            case GameLog.LogChannel.System: return showSystem;
            case GameLog.LogChannel.Exploration: return showExploration;
            case GameLog.LogChannel.Initiative: return showInitiative;
            case GameLog.LogChannel.CombatHit: return showCombatHit;
            case GameLog.LogChannel.CombatDamage: return showCombatDamage;
            case GameLog.LogChannel.Action: return showAction;
            default: return true;
        }
    }

    private bool PassSearch(string line)
    {
        if (searchInput == null) return true;
        var term = searchInput.text;
        if (string.IsNullOrEmpty(term)) return true;
        if (caseSensitiveSearch)
        {
            return line.Contains(term);
        }
        else
        {
            return line.ToLowerInvariant().Contains(term.ToLowerInvariant());
        }
    }

    private void RefreshTextAndScroll()
    {
        // 在内容更新前记录是否位于底部，用于决定是否进行自动跟随
        bool shouldFollow = false;
        if (scrollRect != null)
        {
            bool atBottomBefore = IsAtBottom();
            shouldFollow = autoScrollToBottom && (!autoScrollOnlyIfAtBottom || atBottomBefore);
        }

        if (contentText != null)
        {
            contentText.text = string.Join("\n", _lines);
            // 先重建 Text 自身的布局，再重建 Content 的布局，确保 Content 尺寸更新
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentText.rectTransform);
            if (scrollRect != null && scrollRect.content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
            }
        }
        if (scrollRect != null && shouldFollow)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private bool IsAtBottom()
    {
        if (scrollRect == null) return false;
        // 若内容尚未超过视口高度，视为在底部（避免内容从不满到溢出时首次不跟随）
        var content = scrollRect.content;
        var viewport = scrollRect.viewport != null ? scrollRect.viewport : (RectTransform)scrollRect.transform;
        if (content != null && viewport != null)
        {
            float contentH = content.rect.height;
            float viewportH = viewport.rect.height;
            if (contentH <= viewportH + 1f)
                return true;
        }
        return scrollRect.verticalNormalizedPosition <= bottomTolerance;
    }

    private void RebuildFromAll()
    {
        _lines.Clear();
        string term = searchInput != null ? searchInput.text : string.Empty;
        for (int i = 0; i < _allLines.Count; i++)
        {
            var line = _allLines[i];
            if (string.IsNullOrEmpty(term) ? true : PassSearch(line))
            {
                _lines.Add(line);
            }
        }
        if (maxVisibleLines > 0 && _lines.Count > maxVisibleLines)
        {
            int remove = _lines.Count - maxVisibleLines;
            _lines.RemoveRange(0, remove);
        }
        RefreshTextAndScroll();
    }

    private void ReplayHistory()
    {
        if (contentText == null) return;
        var buf = new List<GameLog.LogEntry>(128);
        GameLog.GetHistory(buf);
        _allLines.Clear();
        _lines.Clear();
        for (int i = 0; i < buf.Count; i++)
        {
            var e = buf[i];
            if (!PassFilter(e.Channel)) continue;
            _sb.Length = 0;
            _sb.Append('[').Append(e.Channel.ToString()).Append("] ");
            _sb.Append(e.Message);
            string line = _sb.ToString();
            _allLines.Add(line);
            if (PassSearch(line))
            {
                _lines.Add(line);
            }
        }
        if (maxVisibleLines > 0 && _lines.Count > maxVisibleLines)
        {
            int remove = _lines.Count - maxVisibleLines;
            _lines.RemoveRange(0, remove);
        }
        RefreshTextAndScroll();
    }

    public void Clear()
    {
        _allLines.Clear();
        _lines.Clear();
        if (contentText != null) contentText.text = string.Empty;
    }

    /// <summary>
    /// 手动滚动到最底部（例如给按钮/调试调用）。
    /// </summary>
    public void ScrollToBottom()
    {
        if (scrollRect == null) return;
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}
