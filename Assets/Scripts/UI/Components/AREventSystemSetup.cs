using UnityEngine;
using UnityEngine.EventSystems;

namespace ARDrawing.UI.Components
{
    /// <summary>
    /// AREventSystemSetup - автоматически настраивает EventSystem для Editor и AR.
    /// AREventSystemSetup - automatically configures EventSystem for Editor and AR.
    /// </summary>
    public class AREventSystemSetup : MonoBehaviour
    {
        private void Awake()
        {
            SetupEventSystem();
        }
        
        private void SetupEventSystem()
        {
            // Найти или создать EventSystem
            var eventSystem = FindObjectOfType<EventSystem>();
            if (eventSystem == null)
            {
                var go = new GameObject("EventSystem");
                eventSystem = go.AddComponent<EventSystem>();
            }
            
#if UNITY_EDITOR
            // В Editor используем StandaloneInputModule для mouse
            var standaloneInput = eventSystem.GetComponent<StandaloneInputModule>();
            if (standaloneInput == null)
            {
                standaloneInput = eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            }
            
            // Удаляем OVRInputModule в Editor если есть
            var ovrInput = eventSystem.GetComponent<OVRInputModule>();
            if (ovrInput != null)
            {
                DestroyImmediate(ovrInput);
            }
#else
            // В билде используем OVRInputModule для Hand Tracking
            var ovrInput = eventSystem.GetComponent<OVRInputModule>();
            if (ovrInput == null)
            {
                ovrInput = eventSystem.gameObject.AddComponent<OVRInputModule>();
            }
            
            // Удаляем StandaloneInputModule в билде
            var standaloneInput = eventSystem.GetComponent<StandaloneInputModule>();
            if (standaloneInput != null)
            {
                DestroyImmediate(standaloneInput);
            }
#endif
            
            Debug.Log($"[AREventSystemSetup] EventSystem configured for {(Application.isEditor ? "Editor" : "AR Build")}");
        }
    }
}
