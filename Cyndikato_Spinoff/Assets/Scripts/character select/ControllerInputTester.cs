using UnityEngine;

/// <summary>
/// Helper script to test controller input mapping.
/// Attach this to a GameObject in your scene to debug controller inputs.
/// This helps identify which buttons correspond to which controller functions.
/// </summary>
public class ControllerInputTester : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool enableDebugLogging = true;
    public int testControllerNumber = 1; // Which controller to test (1-4)
    public bool testAllControllers = false;
    
    [Header("Button Mapping Reference")]
    [TextArea(10, 15)]
    public string buttonReference = 
        "Common Xbox Controller Mapping:\n" +
        "Button 0: A\n" +
        "Button 1: B\n" +
        "Button 2: X\n" +
        "Button 3: Y\n" +
        "Button 4: LB (Left Bumper)\n" +
        "Button 5: RB (Right Bumper)\n" +
        "Button 6: Back/View\n" +
        "Button 7: Start/Menu\n" +
        "Button 8: Left Stick Click\n" +
        "Button 9: Right Stick Click\n" +
        "Button 10: Xbox Guide\n" +
        "Button 11: D-Pad Up\n" +
        "Button 12: D-Pad Down\n" +
        "Button 13: D-Pad Left\n" +
        "Button 14: D-Pad Right";
    
    void Update()
    {
        if (!enableDebugLogging) return;
        
        if (testAllControllers)
        {
            for (int i = 1; i <= 4; i++)
            {
                TestControllerInput(i);
            }
        }
        else
        {
            TestControllerInput(testControllerNumber);
        }
    }
    
    void TestControllerInput(int controllerNum)
    {
        string joystickName = $"joystick {controllerNum}";
        
        // Test all buttons
        for (int i = 0; i < 20; i++)
        {
            if (Input.GetKeyDown($"{joystickName} button {i}"))
            {
                string buttonName = GetButtonName(i);
                Debug.Log($"?? Controller {controllerNum} Button {i} ({buttonName}) pressed");
            }
        }
        
        // Test default axes (only for controller 1 to avoid errors)
        if (controllerNum == 1)
        {
            try
            {
                float horizontal = Input.GetAxis("Horizontal");
                float vertical = Input.GetAxis("Vertical");
                
                if (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f)
                {
                    Debug.Log($"??? Controller {controllerNum} Default Axis - H: {horizontal:F2}, V: {vertical:F2}");
                }
            }
            catch (System.ArgumentException)
            {
                // Ignore axis errors
            }
        }
    }
    
    string GetButtonName(int buttonIndex)
    {
        switch (buttonIndex)
        {
            case 0: return "A";
            case 1: return "B"; 
            case 2: return "X";
            case 3: return "Y";
            case 4: return "LB";
            case 5: return "RB";
            case 6: return "Back/View";
            case 7: return "Start/Menu";
            case 8: return "LS Click";
            case 9: return "RS Click";
            case 10: return "Xbox Guide";
            case 11: return "D-Pad Up";
            case 12: return "D-Pad Down";
            case 13: return "D-Pad Left";
            case 14: return "D-Pad Right";
            default: return "Unknown";
        }
    }
    
    [ContextMenu("Test All Controllers")]
    public void TestAllControllersOnce()
    {
        Debug.Log("=== CONTROLLER CONNECTION TEST ===");
        string[] joystickNames = Input.GetJoystickNames();
        
        for (int i = 0; i < 4; i++)
        {
            if (i < joystickNames.Length && !string.IsNullOrEmpty(joystickNames[i]))
            {
                Debug.Log($"? Controller {i + 1} detected: {joystickNames[i]}");
            }
            else
            {
                Debug.Log($"? Controller {i + 1}: Not connected");
            }
        }
        
        Debug.Log("=== Press buttons on your controllers to test ===");
    }
    
    [ContextMenu("Show Navigation Instructions")]
    public void ShowNavigationInstructions()
    {
        Debug.Log("=== CONTROLLER NAVIGATION SETUP ===");
        Debug.Log("For Character Selection Navigation:");
        Debug.Log("• D-Pad Left/Right: Navigate characters horizontally");
        Debug.Log("• D-Pad Up/Down: Navigate characters vertically OR switch active player");
        Debug.Log("• A Button (Button 0): Lock in character selection");
        Debug.Log("• B Button (Button 1): Cancel/Leave (for non-keyboard players)");
        Debug.Log("• Start Button (Button 7): Alternative lock in");
        Debug.Log("• Left Stick: Alternative navigation (Controller 1 only)");
    }
}