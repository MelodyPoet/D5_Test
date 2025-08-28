using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrollLayer : MonoBehaviour {
    [Header("滚动模式选择")]
    [Tooltip("滚动实现方式")]
    public ScrollMode scrollMode = ScrollMode.AutoDetect;
    [Tooltip("强制使用Transform移动（跳过UV检测）")]
    public bool forceTransformMove = false;

    [Header("滚动参数")]
    [Tooltip("滚动速度")]
    public float scrollSpeed = 2f;
    [Tooltip("滚动方向 (1=向右滚动, -1=向左滚动)")]
    public int scrollDirection = 1; [Header("Transform滚动设置（兼容旧版）")]
    private float startX;
    public float startCameraX;
    [Range(-1, 1)]
    public float moveSpeed = -0.5f;

    // 组件引用
    private SpriteRenderer spriteRenderer;
    private Renderer meshRenderer;
    private Material material;

    // 滚动状态
    private Vector2 uvOffset = Vector2.zero;
    private bool isScrolling = false;
    private ScrollMode actualScrollMode;

    public enum ScrollMode {
        AutoDetect,      // 自动检测最佳方式
        UVScroll,        // UV滚动（MeshRenderer）
        SpriteUVScroll,  // Sprite UV滚动（SpriteRenderer）
        TransformMove    // Transform移动（兼容模式）
    }

    void Start() {
        startX = transform.position.x;
        InitializeScrolling();

        // 添加立即测试滚动的选项
        Debug.Log($"🔍 ScrollLayer在 {gameObject.name} 上初始化完成");
        Debug.Log($"🔍 当前滚动模式: {actualScrollMode}");
        Debug.Log($"🔍 滚动状态: isScrolling = {isScrolling}");

        // 如果设置了自动开始滚动，立即开始（用于测试）
        if (scrollSpeed > 0) {
            Debug.Log("🚀 ScrollLayer: 启动时立即开始滚动测试");
            SetScrollSpeed(scrollSpeed);
        }
    }

    void Update() {
        // 添加调试信息（每60帧输出一次）
        if (Time.frameCount % 60 == 0 && isScrolling) {
            Debug.Log($"🔄 ScrollLayer更新: 模式={actualScrollMode}, 速度={scrollSpeed}, UV偏移={uvOffset}");
        }

        switch (actualScrollMode) {
            case ScrollMode.UVScroll:
                UpdateUVScroll();
                break;
            case ScrollMode.SpriteUVScroll:
                UpdateSpriteUVScroll();
                break;
            case ScrollMode.TransformMove:
                UpdateTransformMove();
                break;
        }
    }

    /// <summary>
    /// 初始化滚动系统
    /// </summary>
    private void InitializeScrolling() {
        // 获取组件引用
        spriteRenderer = GetComponent<SpriteRenderer>();
        meshRenderer = GetComponent<Renderer>();

        // 确定实际滚动模式
        if (forceTransformMove) {
            actualScrollMode = ScrollMode.TransformMove;
            Debug.Log("🎬 ScrollLayer: 强制使用Transform移动模式");
        }
        else if (scrollMode == ScrollMode.AutoDetect) {
            if (spriteRenderer != null) {
                actualScrollMode = ScrollMode.SpriteUVScroll;
                Debug.Log("🎬 ScrollLayer: 检测到SpriteRenderer，使用Sprite UV滚动模式");
            }
            else if (meshRenderer != null) {
                actualScrollMode = ScrollMode.UVScroll;
                Debug.Log("🎬 ScrollLayer: 检测到MeshRenderer，使用标准UV滚动模式");
            }
            else {
                actualScrollMode = ScrollMode.TransformMove;
                Debug.Log("🎬 ScrollLayer: 未检测到Renderer，使用Transform移动模式");
            }
        }
        else {
            actualScrollMode = scrollMode;
        }

        // 初始化对应模式
        switch (actualScrollMode) {
            case ScrollMode.UVScroll:
                InitializeUVScroll();
                break;
            case ScrollMode.SpriteUVScroll:
                InitializeSpriteUVScroll();
                break;
            case ScrollMode.TransformMove:
                // Transform模式无需特殊初始化
                Debug.Log("✅ Transform移动模式已准备就绪");
                break;
        }
    }

    /// <summary>
    /// 初始化标准UV滚动
    /// </summary>
    private void InitializeUVScroll() {
        if (meshRenderer != null) {
            material = meshRenderer.material;
            Debug.Log("✅ UV滚动材质初始化完成");
        }
        else {
            Debug.LogWarning("⚠️ 未找到MeshRenderer，切换到Transform移动模式");
            actualScrollMode = ScrollMode.TransformMove;
        }
    }

    /// <summary>
    /// 初始化Sprite UV滚动
    /// </summary>
    private void InitializeSpriteUVScroll() {
        if (spriteRenderer != null) {
            // 为SpriteRenderer创建可修改的材质副本
            material = new Material(spriteRenderer.material);
            spriteRenderer.material = material;

            // 🎯 重要：检查材质是否支持UV偏移
            if (!material.HasProperty("_MainTex") && !material.HasProperty("_BaseMap")) {
                Debug.LogWarning($"⚠️ 材质 {material.name} 不支持UV偏移，自动切换到Transform移动模式");
                actualScrollMode = ScrollMode.TransformMove;
                return;
            }

            Debug.Log($"✅ SpriteRenderer UV滚动材质初始化完成: {material.name}");
        }
        else {
            Debug.LogWarning("⚠️ 未找到SpriteRenderer，切换到Transform移动模式");
            actualScrollMode = ScrollMode.TransformMove;
        }
    }

    /// <summary>
    /// 更新标准UV滚动
    /// </summary>
    private void UpdateUVScroll() {
        if (material == null || !isScrolling) return;

        uvOffset.x += scrollSpeed * scrollDirection * Time.deltaTime;
        material.SetTextureOffset("_MainTex", uvOffset);
    }

    /// <summary>
    /// 更新Sprite UV滚动
    /// </summary>
    private void UpdateSpriteUVScroll() {
        if (material == null || !isScrolling) {
            if (Time.frameCount % 120 == 0) {
                Debug.Log($"🔍 SpriteUVScroll检查: material={material != null}, isScrolling={isScrolling}");
            }
            return;
        }

        uvOffset.x += scrollSpeed * scrollDirection * Time.deltaTime;

        // 尝试不同的纹理属性名称
        bool success = false;
        if (material.HasProperty("_MainTex")) {
            material.SetTextureOffset("_MainTex", uvOffset);
            success = true;
        }
        else if (material.HasProperty("_BaseMap")) {
            material.SetTextureOffset("_BaseMap", uvOffset);
            success = true;
        }

        if (!success) {
            // 如果都不支持，立即切换到Transform移动
            Debug.LogWarning("⚠️ UV偏移不支持，立即切换到Transform移动模式");
            actualScrollMode = ScrollMode.TransformMove;
            // 重置UV偏移
            uvOffset = Vector2.zero;
        }
        else if (Time.frameCount % 180 == 0) {
            Debug.Log($"✅ SpriteUV滚动: uvOffset={uvOffset}, 材质={material.name}");

            // 🎯 检查UV偏移是否真的在工作（检查材质当前偏移）
            Vector2 currentOffset = material.GetTextureOffset("_MainTex");
            if (Mathf.Approximately(currentOffset.x, 0f) && uvOffset.x > 1f) {
                Debug.LogWarning("⚠️ 检测到UV偏移无效，切换到Transform移动模式");
                actualScrollMode = ScrollMode.TransformMove;
            }
        }
    }    /// <summary>
         /// 更新Transform移动
         /// </summary>
    private void UpdateTransformMove() {
        if (!isScrolling) return;

        Vector3 pos = transform.position;
        pos.x += scrollSpeed * scrollDirection * Time.deltaTime;
        transform.position = pos;

        if (Time.frameCount % 120 == 0) {
            Debug.Log($"🚀 Transform移动: 新位置={transform.position}");
        }
    }

    /// <summary>
    /// 设置滚动速度（供外部调用）
    /// </summary>
    public void SetScrollSpeed(float speed) {
        scrollSpeed = speed;
        isScrolling = true;
        Debug.Log($"🎬 背景开始滚动，模式: {actualScrollMode}，速度: {speed}");
    }

    /// <summary>
    /// 停止滚动
    /// </summary>
    public void StopScrolling() {
        isScrolling = false;
        Debug.Log("🛑 背景停止滚动");
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
        if (material != null) {
            if (material.HasProperty("_MainTex")) {
                material.SetTextureOffset("_MainTex", uvOffset);
            }
            else if (material.HasProperty("_BaseMap")) {
                material.SetTextureOffset("_BaseMap", uvOffset);
            }
        }

        // 重置Transform位置
        transform.position = new Vector3(startX, transform.position.y, transform.position.z);
    }
}
