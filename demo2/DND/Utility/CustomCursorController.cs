using UnityEngine;
using UnityEngine.UI;

namespace demo2.DND.Utility
{
    /// <summary>
    /// Applies a custom cursor image using a software-drawn RawImage.
    /// </summary>
    public class CustomCursorController : MonoBehaviour
    {
        [Header("Cursor Settings")]
        public Texture2D cursorTexture;

        [Tooltip("Hotspot in pixels, relative to the top-left of the cursor texture.")]
        public Vector2 hotspot = Vector2.zero;

        [Tooltip("Scale applied to the cursor image (1 = original size).")]
        [Min(0.1f)]
        public float cursorScale = 1f;

        [Tooltip("Optional canvas to host the software cursor. If null, one will be created.")]
        public Canvas softwareCursorCanvas;

        private Canvas runtimeCanvas;
        private RawImage softwareCursorImage;
        private bool usingSoftwareCursor;

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                CleanupEditorResiduals();
                return;
            }
            ApplyCursor();
        }

        private void OnDisable()
        {
            HideSoftwareCursor();
            Cursor.visible = true;
            if (!Application.isPlaying)
            {
                CleanupEditorResiduals();
            }
        }

        private void Update()
        {
            if (!usingSoftwareCursor || softwareCursorImage == null) return;
            UpdateSoftwareCursorPosition();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying) return;
            if (cursorTexture == null) return;
            hotspot.x = Mathf.Clamp(hotspot.x, 0, cursorTexture.width);
            hotspot.y = Mathf.Clamp(hotspot.y, 0, cursorTexture.height);
            cursorScale = Mathf.Max(0.1f, cursorScale);
            ApplyCursor();
        }

        public void ApplyCursor()
        {
            if (!Application.isPlaying)
            {
                HideSoftwareCursor();
                Cursor.visible = true;
                return;
            }

            if (cursorTexture == null)
            {
                HideSoftwareCursor();
                Cursor.visible = true;
                return;
            }

            EnsureSoftwareCursorImage();
            if (softwareCursorImage == null) return;

            usingSoftwareCursor = true;
            Cursor.visible = false;

            softwareCursorImage.texture = cursorTexture;
            softwareCursorImage.raycastTarget = false;

            if (cursorTexture != null)
            {
                cursorTexture.filterMode = FilterMode.Point;
            }

            var rt = softwareCursorImage.rectTransform;
            rt.sizeDelta = new Vector2(cursorTexture.width * cursorScale, cursorTexture.height * cursorScale);
            rt.pivot = new Vector2(0f, 1f); // top-left for hotspot math
            UpdateSoftwareCursorPosition();
        }

        private void UpdateSoftwareCursorPosition()
        {
            var rt = softwareCursorImage.rectTransform;
            float scale = cursorScale <= 0.01f ? 0.01f : cursorScale;
            Vector2 offset = new Vector2(-hotspot.x * scale, hotspot.y * scale);
            rt.position = (Vector2)Input.mousePosition + offset;
        }

        private void HideSoftwareCursor()
        {
            if (softwareCursorImage != null) softwareCursorImage.enabled = false;
            usingSoftwareCursor = false;
            if (runtimeCanvas != null && softwareCursorCanvas == null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(runtimeCanvas.gameObject);
                }
                else
#endif
                {
                    Destroy(runtimeCanvas.gameObject);
                }
                runtimeCanvas = null;
            }
        }

        private void EnsureSoftwareCursorImage()
        {
            if (!Application.isPlaying) return;
            Canvas hostCanvas = softwareCursorCanvas != null ? softwareCursorCanvas : runtimeCanvas;
            if (hostCanvas == null)
            {
                var go = new GameObject("SoftwareCursorCanvas");
                go.transform.SetParent(transform, false);
                runtimeCanvas = go.AddComponent<Canvas>();
                runtimeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                go.AddComponent<CanvasScaler>();
                go.AddComponent<GraphicRaycaster>();
                hostCanvas = runtimeCanvas;
            }

            if (softwareCursorImage == null)
            {
                var imgGo = new GameObject("SoftwareCursor");
                imgGo.transform.SetParent(hostCanvas.transform, false);
                softwareCursorImage = imgGo.AddComponent<RawImage>();
                softwareCursorImage.raycastTarget = false;
            }

            softwareCursorImage.enabled = true;
        }

        private void CleanupEditorResiduals()
        {
#if UNITY_EDITOR
            if (Application.isPlaying) return;
            foreach (Transform child in transform)
            {
                if (child != null && child.name == "SoftwareCursorCanvas")
                {
                    DestroyImmediate(child.gameObject);
                }
            }
#endif
        }
    }
}
