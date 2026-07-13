using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public static class RebuildCarControls
{
    [MenuItem("Tools/Rebuild CarControls")]
    public static void Rebuild()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = asset.AddActionMap("Driving");

        var acc = map.AddAction("Accelerate", InputActionType.Button);
        acc.AddBinding("<Keyboard>/w");
        acc.AddBinding("<DualShockGamepad>/rightTrigger");
        acc.AddBinding("<XInputController>/rightTrigger");
        acc.AddBinding("<Gamepad>/rightTrigger");

        var steer = map.AddAction("Steer", InputActionType.Value);
        steer.expectedControlType = "Axis";
        steer.AddCompositeBinding("1DAxis")
            .With("negative", "<Keyboard>/a")
            .With("positive", "<Keyboard>/d");
        steer.AddBinding("<Gamepad>/leftStick/x");

        var rev = map.AddAction("Reverse", InputActionType.Button);
        rev.AddBinding("<Keyboard>/s");
        rev.AddBinding("<DualShockGamepad>/leftTrigger");
        rev.AddBinding("<XInputController>/leftTrigger");
        rev.AddBinding("<Gamepad>/leftTrigger");

        var hb = map.AddAction("HandBrake", InputActionType.Button);
        hb.AddBinding("<Keyboard>/space");
        hb.AddBinding("<Gamepad>/buttonSouth");
        hb.AddBinding("<XInputController>/buttonWest");

        var su = map.AddAction("ShiftUp", InputActionType.Button);
        su.AddBinding("<Keyboard>/e");
        su.AddBinding("<Gamepad>/rightShoulder");

        var sd = map.AddAction("ShiftDown", InputActionType.Button);
        sd.AddBinding("<Keyboard>/q");
        sd.AddBinding("<Gamepad>/leftShoulder");

        var clutch = map.AddAction("Clutch", InputActionType.Button);
        clutch.AddBinding("<Keyboard>/leftShift");
        clutch.AddBinding("<DualShockGamepad>/buttonEast");
        clutch.AddBinding("<XInputController>/buttonEast");
        clutch.AddBinding("<Gamepad>/buttonEast");

        var om = map.AddAction("OpenMap", InputActionType.Button);
        om.AddBinding("<Keyboard>/m");
        om.AddBinding("<DualShockGamepad>/touchpadButton");
        om.AddBinding("<Gamepad>/select");

        var mlc = map.AddAction("MapLeftClick", InputActionType.Button);
        mlc.AddBinding("<Mouse>/leftButton");

        var mrc = map.AddAction("MapRightClick", InputActionType.Button);
        mrc.AddBinding("<Mouse>/rightButton");

        var mdw = map.AddAction("MapDeleteWaypoint", InputActionType.Button);
        mdw.AddBinding("<Mouse>/middleButton");

        string path = "Assets/CarControls.inputactions";
        System.IO.File.WriteAllText(path, asset.ToJson());
        AssetDatabase.ImportAsset(path);
        
        Debug.Log("Successfully rebuilt CarControls.inputactions!");
    }
}
