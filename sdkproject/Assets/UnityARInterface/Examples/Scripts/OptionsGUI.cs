using UnityEngine;
using UnityARInterface;

public class OptionsGUI : MonoBehaviour
{
    [SerializeField]
    private GUISkin m_GuiSkin;

    private void DoButton<T>(float y, string name) where T : MonoBehaviour
    {
        var buttonWidth = 400;
        var buttonHeight = Screen.height / 8;
        var component = GetComponent<T>();
        var rect = new Rect(0, y * buttonHeight, buttonWidth, buttonHeight);
        var text = string.Format("{0} {1}", (component.enabled ? "Hide" : "Show"), name);
        if (GUI.Button(rect, text, m_GuiSkin.button))
            component.enabled = !component.enabled;
    }

    private void OnGUI()
    {
        DoButton<ARPlaneVisualizer>(0f, "Planes");
        DoButton<ARPointCloudVisualizer>(1f, "Pointcloud");
    }
}
