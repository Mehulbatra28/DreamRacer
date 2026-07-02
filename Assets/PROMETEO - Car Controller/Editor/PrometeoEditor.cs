using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;

[CustomEditor(typeof(PrometeoCarController))]
[System.Serializable]
public class PrometeoEditor : Editor{

  enum displayFieldType {DisplayAsAutomaticFields, DisplayAsCustomizableGUIFields}
  displayFieldType DisplayFieldType;

  private PrometeoCarController prometeo;
  private SerializedObject SO;
  //
  //
  //CAR SETUP
  //
  //
  private SerializedProperty maxSpeed;
  private SerializedProperty maxReverseSpeed;
  private SerializedProperty accelerationMultiplier;
  private SerializedProperty maxSteeringAngle;
  private SerializedProperty steeringSpeed;
  private SerializedProperty brakeForce;
  private SerializedProperty decelerationMultiplier;
  private SerializedProperty handbrakeDriftMultiplier;
  private SerializedProperty bodyMassCenter;
  //
  //
  //WHEELS VARIABLES
  //
  //
  private SerializedProperty frontLeftMesh;
  private SerializedProperty frontLeftCollider;
  private SerializedProperty frontRightMesh;
  private SerializedProperty frontRightCollider;
  private SerializedProperty rearLeftMesh;
  private SerializedProperty rearLeftCollider;
  private SerializedProperty rearRightMesh;
  private SerializedProperty rearRightCollider;

  //GEAR SYSTEM VARIABLES
  //
  //
  private SerializedProperty transmissionMode;
  private SerializedProperty numberOfGears;
  private SerializedProperty gearRatios;
  private SerializedProperty reverseGearRatio;
  private SerializedProperty finalDriveRatio;
  private SerializedProperty maxEngineRPM;
  private SerializedProperty idleRPM;
  private SerializedProperty shiftUpRPM;
  private SerializedProperty shiftDownRPM;

  private void OnEnable(){
    prometeo = (PrometeoCarController)target;
    SO = new SerializedObject(target);

    maxSpeed = SO.FindProperty("maxSpeed");
    maxReverseSpeed = SO.FindProperty("maxReverseSpeed");
    accelerationMultiplier = SO.FindProperty("accelerationMultiplier");
    maxSteeringAngle = SO.FindProperty("maxSteeringAngle");
    steeringSpeed = SO.FindProperty("steeringSpeed");
    brakeForce = SO.FindProperty("brakeForce");
    decelerationMultiplier = SO.FindProperty("decelerationMultiplier");
    handbrakeDriftMultiplier = SO.FindProperty("handbrakeDriftMultiplier");
    bodyMassCenter = SO.FindProperty("bodyMassCenter");

    frontLeftMesh = SO.FindProperty("frontLeftMesh");
    frontLeftCollider = SO.FindProperty("frontLeftCollider");
    frontRightMesh = SO.FindProperty("frontRightMesh");
    frontRightCollider = SO.FindProperty("frontRightCollider");
    rearLeftMesh = SO.FindProperty("rearLeftMesh");
    rearLeftCollider = SO.FindProperty("rearLeftCollider");
    rearRightMesh = SO.FindProperty("rearRightMesh");
    rearRightCollider = SO.FindProperty("rearRightCollider");


    transmissionMode = SO.FindProperty("transmissionMode");
    numberOfGears = SO.FindProperty("numberOfGears");
    gearRatios = SO.FindProperty("gearRatios");
    reverseGearRatio = SO.FindProperty("reverseGearRatio");
    finalDriveRatio = SO.FindProperty("finalDriveRatio");
    maxEngineRPM = SO.FindProperty("maxEngineRPM");
    idleRPM = SO.FindProperty("idleRPM");
    shiftUpRPM = SO.FindProperty("shiftUpRPM");
    shiftDownRPM = SO.FindProperty("shiftDownRPM");

  }

  public override void OnInspectorGUI(){

    SO.Update();

    GUILayout.Space(25);
    GUILayout.Label("CAR SETUP", EditorStyles.boldLabel);
    GUILayout.Space(10);
    //
    //
    //CAR SETUP
    //
    //
    //
    maxSpeed.intValue = EditorGUILayout.IntSlider("Max Speed:", maxSpeed.intValue, 20, 190);
    maxReverseSpeed.intValue = EditorGUILayout.IntSlider("Max Reverse Speed:", maxReverseSpeed.intValue, 10, 120);
    accelerationMultiplier.intValue = EditorGUILayout.IntSlider("Acceleration Multiplier:", accelerationMultiplier.intValue, 1, 10);
    maxSteeringAngle.intValue = EditorGUILayout.IntSlider("Max Steering Angle:", maxSteeringAngle.intValue, 10, 45);
    steeringSpeed.floatValue = EditorGUILayout.Slider("Steering Speed:", steeringSpeed.floatValue, 0.1f, 1f);
    brakeForce.intValue = EditorGUILayout.IntSlider("Brake Force:", brakeForce.intValue, 100, 600);
    decelerationMultiplier.intValue = EditorGUILayout.IntSlider("Deceleration Multiplier:", decelerationMultiplier.intValue, 1, 10);
    handbrakeDriftMultiplier.intValue = EditorGUILayout.IntSlider("Drift Multiplier:", handbrakeDriftMultiplier.intValue, 1, 10);
    EditorGUILayout.PropertyField(bodyMassCenter, new GUIContent("Mass Center of Car: "));

    //
    //
    //WHEELS
    //
    //

    GUILayout.Space(25);
    GUILayout.Label("WHEELS", EditorStyles.boldLabel);
    GUILayout.Space(10);

    EditorGUILayout.PropertyField(frontLeftMesh, new GUIContent("Front Left Mesh: "));
    EditorGUILayout.PropertyField(frontLeftCollider, new GUIContent("Front Left Collider: "));

    EditorGUILayout.PropertyField(frontRightMesh, new GUIContent("Front Right Mesh: "));
    EditorGUILayout.PropertyField(frontRightCollider, new GUIContent("Front Right Collider: "));

    EditorGUILayout.PropertyField(rearLeftMesh, new GUIContent("Rear Left Mesh: "));
    EditorGUILayout.PropertyField(rearLeftCollider, new GUIContent("Rear Left Collider: "));

    EditorGUILayout.PropertyField(rearRightMesh, new GUIContent("Rear Right Mesh: "));
    EditorGUILayout.PropertyField(rearRightCollider, new GUIContent("Rear Right Collider: "));

    //
    //
    //GEAR SYSTEM
    //
    //

    GUILayout.Space(25);
    GUILayout.Label("GEAR SYSTEM", EditorStyles.boldLabel);
    GUILayout.Space(10);

    EditorGUILayout.PropertyField(transmissionMode, new GUIContent("Transmission Mode: "));
    GUILayout.Space(5);

    numberOfGears.intValue = EditorGUILayout.IntSlider("Number of Gears:", numberOfGears.intValue, 1, 8);
    EditorGUILayout.PropertyField(gearRatios, new GUIContent("Gear Ratios: "), true);
    EditorGUILayout.PropertyField(reverseGearRatio, new GUIContent("Reverse Gear Ratio: "));
    EditorGUILayout.PropertyField(finalDriveRatio, new GUIContent("Final Drive Ratio: "));

    GUILayout.Space(10);
    maxEngineRPM.floatValue = EditorGUILayout.Slider("Max Engine RPM:", maxEngineRPM.floatValue, 4000f, 12000f);
    idleRPM.floatValue = EditorGUILayout.Slider("Idle RPM:", idleRPM.floatValue, 500f, 1500f);

    // Only show auto-shift thresholds when in Automatic mode
    if(transmissionMode.enumValueIndex == 0){ // 0 = Automatic
      shiftUpRPM.floatValue = EditorGUILayout.Slider("Shift Up RPM:", shiftUpRPM.floatValue, 4000f, 10000f);
      shiftDownRPM.floatValue = EditorGUILayout.Slider("Shift Down RPM:", shiftDownRPM.floatValue, 1000f, 4000f);
    } else {
      GUILayout.Space(5);
      EditorGUILayout.HelpBox(
        transmissionMode.enumValueIndex == 1
          ? "Sequential Mode: Player shifts with E/Q (keyboard) or Bumpers (gamepad). No clutch needed."
          : "Manual Mode: Player must hold Clutch (Left Shift / A button) while shifting with E/Q or Bumpers.",
        MessageType.Info);
    }

    //END

    GUILayout.Space(10);
    SO.ApplyModifiedProperties();

  }

}
