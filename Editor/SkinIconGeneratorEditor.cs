using UnityEngine;
using UnityEditor;
using Spine.Unity;
using Spine;
using System.IO;
using System.Collections.Generic;

namespace demo2.DND.Editor
{
    /// <summary>
    /// 编辑器窗口，用于从 SkeletonDataAsset 自动为 SkinConfig 生成皮肤部件的预览图标。
    /// </summary>
    public class SkinIconGeneratorEditor : EditorWindow
    {
        private SkinConfig skinConfig;
        private SkeletonDataAsset skeletonDataAsset;
        private Vector2 scrollPosition;
        private static readonly Vector2 RenderSize = new Vector2(150, 150);
        private const string IconsSavePath = "Assets/GeneratedIcons";
        private bool forceRegenerate = false;

        [MenuItem("Tools/Skin Icon Generator")]
        public static void ShowWindow()
        {
            GetWindow<SkinIconGeneratorEditor>("Skin Icon Generator");
        }

        private void OnGUI()
        {
            GUILayout.Label("Skin Icon Generator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("此工具可自动为 SkinConfig 中的皮肤部件生成预览图标。\n" +
                                    "1. 拖入 SkinConfig 和对应的 SkeletonDataAsset。\n" +
                                    "2. 点击 'Generate Icons' 按钮。\n" +
                                    "3. 图标将保存在 'Assets/GeneratedIcons' 目录下，并自动关联。", MessageType.Info);

            skinConfig = (SkinConfig)EditorGUILayout.ObjectField("Skin Config", skinConfig, typeof(SkinConfig), false);
            skeletonDataAsset = (SkeletonDataAsset)EditorGUILayout.ObjectField("SkeletonData Asset", skeletonDataAsset, typeof(SkeletonDataAsset), false);

            // 添加强制重新生成选项
            forceRegenerate = EditorGUILayout.Toggle("Force Regenerate", forceRegenerate);

            if (GUILayout.Button("Generate Icons"))
            {
                if (skinConfig == null || skeletonDataAsset == null)
                {
                    EditorUtility.DisplayDialog("错误", "请先提供 SkinConfig 和 SkeletonDataAsset。", "确定");
                    return;
                }
                GenerateIcons();
            }

            // 显示 SkinConfig 中的内容以供预览
            if (skinConfig != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("SkinConfig 内容预览:", EditorStyles.boldLabel);
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

                foreach (var part in skinConfig.GetSkinParts())
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(part.skinID, GUILayout.Width(200));
                    EditorGUILayout.ObjectField(part.previewIcon, typeof(Sprite), false, GUILayout.Width(100));
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void GenerateIcons()
        {
            // 确保IconGenerator层存在
            if (LayerMask.NameToLayer("IconGenerator") == -1)
            {
                 EditorUtility.DisplayDialog("错误", "请先在 'Tags and Layers' 设置中添加一个名为 'IconGenerator' 的新 Layer。", "确定");
                 return;
            }

            if (!Directory.Exists(IconsSavePath))
            {
                Directory.CreateDirectory(IconsSavePath);
            }

            // 1. 创建临时渲染环境
            var cameraGO = new GameObject("IconGeneratorCamera");
            var camera = cameraGO.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 100; // 初始大小，后面会调整
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.cullingMask = 1 << LayerMask.NameToLayer("IconGenerator");

            var skeletonGO = new GameObject("IconGeneratorSkeleton");
            skeletonGO.layer = LayerMask.NameToLayer("IconGenerator");
            var skeletonAnimation = skeletonGO.AddComponent<SkeletonAnimation>();
            skeletonAnimation.skeletonDataAsset = skeletonDataAsset;
            skeletonAnimation.Initialize(false);

            var skeleton = skeletonAnimation.Skeleton;
            var skeletonData = skeleton.Data;

            // 2. 遍历所有皮肤部件
            int generatedCount = 0;
            var skinParts = skinConfig.GetSkinParts();
            for (int i = 0; i < skinParts.Count; i++)
            {
                var part = skinParts[i];
                EditorUtility.DisplayProgressBar("Generating Icons", $"Processing: {part.skinID}", (float)i / skinParts.Count);

                // 检查是否需要生成 (如果图标为空或强制重新生成)
                if (part.previewIcon != null && !forceRegenerate) continue;

                var skinToRender = skeletonData.FindSkin(part.skinID);
                if (skinToRender == null)
                {
                    Debug.LogWarning($"在 SkeletonDataAsset 中未找到皮肤: {part.skinID}");
                    continue;
                }

                // 3. "只穿一件"
                skeleton.SetSkin(skinToRender);
                skeleton.SetSlotsToSetupPose();
                skeletonAnimation.Initialize(true); // 强制重新生成网格

                // 4. 调整相机以适应部件 (使用 MeshRenderer 的可靠方法)
                var meshRenderer = skeletonGO.GetComponent<MeshRenderer>();
                var bounds = meshRenderer.bounds;

                if (bounds.size.x == 0 || bounds.size.y == 0)
                {
                     Debug.LogWarning($"皮肤 '{part.skinID}' 的边界为空，跳过图标生成。这可能是���个空的或无效的皮肤。");
                     continue;
                }

                cameraGO.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10);
                camera.orthographicSize = Mathf.Max(bounds.size.x, bounds.size.y) * 0.6f; // 留出一些边距

                // 5. "拍照" 到 RenderTexture
                var renderTexture = new RenderTexture((int)RenderSize.x, (int)RenderSize.y, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = renderTexture;
                camera.Render();

                // 6. 转换并保存
                var texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                texture.Apply();
                RenderTexture.active = null;

                byte[] bytes = texture.EncodeToPNG();
                string safeFileName = part.skinID.Replace('/', '_').Replace('\\', '_');
                string filePath = $"{IconsSavePath}/{safeFileName}.png";
                File.WriteAllBytes(filePath, bytes);

                // 7. 导入资源并创建 Sprite
                AssetDatabase.ImportAsset(filePath);
                var importer = AssetImporter.GetAtPath(filePath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.SaveAndReimport();
                }

                var newSprite = AssetDatabase.LoadAssetAtPath<Sprite>(filePath);
                if (newSprite != null)
                {
                    part.previewIcon = newSprite;
                    generatedCount++;
                }

                // 清理
                Object.DestroyImmediate(renderTexture);
                Object.DestroyImmediate(texture);
            }

            // 8. 清理临时对象
            EditorUtility.ClearProgressBar();
            Object.DestroyImmediate(cameraGO);
            Object.DestroyImmediate(skeletonGO);

            // 9. 保存 SkinConfig 更改
            EditorUtility.SetDirty(skinConfig);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("完成", $"成功生成了 {generatedCount} 个新图标。", "确定");
        }
    }
}

