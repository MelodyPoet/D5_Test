using UnityEngine;

public class ScrollLayer : MonoBehaviour {
    [Header("滚动参数")]
    [Tooltip("滚动速度")]
    public float scrollSpeed = 2f;
    [Tooltip("滚动方向 (1=向右滚动, -1=向左滚动)")]
    public int scrollDirection = 1;

    // 组件引用
    private Material material;

    // 缓存纹理属性ID
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

    // 滚动状态
    private Vector2 uvOffset;
    private bool isScrolling;
    private float inspectorSpeed;

    // 如果脚本为 SpriteRenderer 创建了材质实例，则记录以便在 OnDestroy 中销毁
    private bool materialInstanceCreated;

    void Awake() {
        // 在Awake阶段存储在Inspector中配置的初始速度
        inspectorSpeed = scrollSpeed;
    }

    void Start() {
        if (!InitializeUVScroll()) {
            // 如果初始化失败，禁用此组件以停止后续操作
            Debug.LogError($"ScrollLayer: 在 {gameObject.name} 上初始化UV滚动失败，脚本已禁用。");
            enabled = false;
            return;
        }

        // 移除自动开始滚动的逻辑，完全由外部控制
        // if (scrollSpeed > 0) {
        //     SetScrollSpeed(scrollSpeed);
        // }
    }

    void Update() {
        if (!isScrolling || material == null) return;

        // 根据时间和速度计算新的UV偏移量
        uvOffset.x += scrollSpeed * scrollDirection * Time.deltaTime;

        // 应用UV偏移
        material.SetTextureOffset(MainTexId, uvOffset);
    }

    /// <summary>
    /// 初始化UV滚动（统一处理Sprite和Mesh）
    /// </summary>
    private bool InitializeUVScroll() {
        var spriteRenderer = GetComponent<SpriteRenderer>();
        var meshRenderer = GetComponent<Renderer>();

        if (spriteRenderer != null) {
            // 检查当前材质是否为默认材质或不支持UV滚动
            if (spriteRenderer.material.shader.name.Contains("Default")) {
                Debug.Log($"ScrollLayer: 在 {gameObject.name} 上检测到默认Sprite材质，正在创建支持UV滚动的材质实例...");
                // 关键修复：创建一个使用Unlit/Transparent的新材质，它支持Alpha通道
                var newMaterial = new Material(Shader.Find("Unlit/Transparent"));

                // 关键：将原始Sprite的纹理赋给新材质
                if (spriteRenderer.sprite != null) {
                    newMaterial.mainTexture = spriteRenderer.sprite.texture;
                    // 关键修复：将过滤模式设置为Point，避免像素模糊和接缝问题
                    newMaterial.mainTexture.filterMode = FilterMode.Point;
                } else {
                    Debug.LogError($"ScrollLayer: {gameObject.name} 上的 SpriteRenderer 没有分配Sprite，无法获取纹理！");
                    return false;
                }

                // 应用新材质
                spriteRenderer.material = newMaterial;
                material = newMaterial;
                materialInstanceCreated = true;
            } else {
                // 如果已经是自定义材质，直接使用其实例
                material = spriteRenderer.material;
            }
        } else if (meshRenderer != null) {
            // 对于MeshRenderer，直接获取其材质实例
            material = meshRenderer.material;
        } else {
            Debug.LogError($"ScrollLayer: 在 {gameObject.name} 上没有找到 SpriteRenderer 或 MeshRenderer 组件。");
            return false;
        }

        // 最后检查最终的材质是否支持UV滚动
        if (material != null && material.HasProperty(MainTexId)) {
            uvOffset = material.GetTextureOffset(MainTexId);
            // 同样在此处为已存在的自定义材质设置过滤模式
            if (material.mainTexture != null) {
                material.mainTexture.filterMode = FilterMode.Point;
            }
            Debug.Log($"ScrollLayer: 在 {gameObject.name} 上成功初始化UV滚动模式。");
            return true;
        }

        Debug.LogWarning($"ScrollLayer: 最终材质 '{material?.name}' 不支持UV滚动 (_MainTex 属性缺失)。");
        return false;
    }

    /// <summary>
    /// 使用在Inspector中配置的初始速度来开始或恢复滚动
    /// </summary>
    public void StartScrollingWithInspectorValue() {
        scrollSpeed = inspectorSpeed; // 恢复为初始速度
        isScrolling = true;
        Debug.Log($"ScrollLayer: {gameObject.name} 开始UV滚动，使用初始速度: {scrollSpeed}");
    }

    /// <summary>
    /// 设置滚动速度（供外部调用）
    /// </summary>
    public void SetScrollSpeed(float speed) {
        scrollSpeed = speed;
        isScrolling = true;
        Debug.Log($"ScrollLayer: {gameObject.name} 开始UV滚动，速度: {speed}");
    }

    /// <summary>
    /// 停止滚动
    /// </summary>
    public void StopScrolling() {
        isScrolling = false;
        Debug.Log($"ScrollLayer: {gameObject.name} 停止滚动");
    }

    /// <summary>
    /// 检查是否正在滚动
    /// </summary>
    public bool IsScrolling() {
        return isScrolling;
    }

    /// <summary>
    /// 重置滚动状态
    /// </summary>
    public void ResetScroll() {
        uvOffset = Vector2.zero;
        if (material != null && material.HasProperty(MainTexId)) {
            material.SetTextureOffset(MainTexId, uvOffset);
        }
    }

    private void OnDestroy() {
        // 如果我们创建了材质实例，需要在销毁时释放它，避免内存泄漏
        if (materialInstanceCreated && material != null) {
            Destroy(material);
        }
    }
}
