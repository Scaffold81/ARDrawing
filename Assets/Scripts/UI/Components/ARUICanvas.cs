using UnityEngine;
using UnityEngine.UI;

namespace ARDrawing.UI.Components
{
    /// <summary>
    /// ARUICanvas - упрощенный Canvas с поддержкой Editor + AR.
    /// В Editor использует GraphicRaycaster, в AR - OVRRaycaster.
    /// ARUICanvas - simplified Canvas with Editor + AR support.
    /// Uses GraphicRaycaster in Editor, OVRRaycaster in AR.
    /// </summary>
    public class ARUICanvas : MonoBehaviour
    {
        #region Unity Lifecycle
        
        private void Awake()
        {
            SetupCanvas();
        }
        
        #endregion
        
        #region Canvas Setup
        
        private void SetupCanvas()
        {
            // Get or add Canvas
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }
            
            // Configure for World Space
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            
            // Get or add CanvasScaler
            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
            }
            
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(600, 400);
            
            // Add appropriate Raycaster based on platform
            SetupRaycaster();
        }
        
        private void SetupRaycaster()
        {
#if UNITY_EDITOR
            // В Editor используем GraphicRaycaster для mouse input
            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }
#else
            // В билде используем OVRRaycaster для Hand Tracking
            if (GetComponent<OVRRaycaster>() == null)
            {
                gameObject.AddComponent<OVRRaycaster>();
            }
#endif
        }
        
        #endregion
    }
}
