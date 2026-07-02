using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor wizard to set up a new 3D car model with all the components and configuration
/// matching the Lambo High quality prefab (PrometeoCarController, Rigidbody, CarDeformer,
/// WheelColliders, MeshCollider, etc.).
///
/// Access via: DreamRacer > Car Setup Wizard
/// </summary>
public class DreamRacerCarSetupWizard : EditorWindow
{
    private GameObject carModel;
    private Vector2 scrollPosition;

    // Default values matching Lambo High quality setup
    private int maxSpeed = 190;
    private int maxReverseSpeed = 45;
    private int accelerationMultiplier = 8;
    private int maxSteeringAngle = 30;
    private float steeringSpeed = 0.5f;
    private int brakeForce = 400;
    private int decelerationMultiplier = 2;
    private int handbrakeDriftMultiplier = 5;
    private Vector3 bodyMassCenter = new Vector3(0f, 0.4f, 0f);
    private float vehicleMass = 1500f;

    // Wheel collider defaults
    private float wheelRadius = 0.32f;
    private float suspensionDistance = 0.2f;
    private float springForce = 35000f;
    private float damperForce = 4500f;
    private float forwardExtremumSlip = 0.4f;
    private float forwardExtremumValue = 1f;
    private float sidewaysExtremumSlip = 0.25f;
    private float sidewaysExtremumValue = 1f;

    [MenuItem("DreamRacer/Car Setup Wizard")]
    public static void ShowWindow()
    {
        var window = GetWindow<DreamRacerCarSetupWizard>("Car Setup Wizard");
        window.minSize = new Vector2(400, 600);
    }

    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Space(10);
        EditorGUILayout.LabelField("DreamRacer Car Setup Wizard", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Drag a 3D car model (FBX, Prefab, or scene GameObject) into the field below, " +
            "then click 'Setup Car' to automatically add all required components configured " +
            "like the Lambo High quality prefab.",
            MessageType.Info
        );

        GUILayout.Space(10);
        EditorGUILayout.LabelField("CAR MODEL", EditorStyles.boldLabel);
        carModel = (GameObject)EditorGUILayout.ObjectField("Car Model:", carModel, typeof(GameObject), true);

        GUILayout.Space(15);
        EditorGUILayout.LabelField("CAR SETUP DEFAULTS", EditorStyles.boldLabel);
        maxSpeed = EditorGUILayout.IntSlider("Max Speed:", maxSpeed, 20, 190);
        maxReverseSpeed = EditorGUILayout.IntSlider("Max Reverse Speed:", maxReverseSpeed, 10, 120);
        accelerationMultiplier = EditorGUILayout.IntSlider("Acceleration Multiplier:", accelerationMultiplier, 1, 10);
        maxSteeringAngle = EditorGUILayout.IntSlider("Max Steering Angle:", maxSteeringAngle, 10, 45);
        steeringSpeed = EditorGUILayout.Slider("Steering Speed:", steeringSpeed, 0.1f, 1f);
        brakeForce = EditorGUILayout.IntSlider("Brake Force:", brakeForce, 100, 600);
        decelerationMultiplier = EditorGUILayout.IntSlider("Deceleration Multiplier:", decelerationMultiplier, 1, 10);
        handbrakeDriftMultiplier = EditorGUILayout.IntSlider("Drift Multiplier:", handbrakeDriftMultiplier, 1, 10);
        bodyMassCenter = EditorGUILayout.Vector3Field("Body Mass Center:", bodyMassCenter);
        vehicleMass = EditorGUILayout.FloatField("Vehicle Mass (kg):", vehicleMass);

        GUILayout.Space(15);
        EditorGUILayout.LabelField("WHEEL COLLIDER DEFAULTS", EditorStyles.boldLabel);
        wheelRadius = EditorGUILayout.FloatField("Wheel Radius:", wheelRadius);
        suspensionDistance = EditorGUILayout.FloatField("Suspension Distance:", suspensionDistance);
        springForce = EditorGUILayout.FloatField("Spring Force:", springForce);
        damperForce = EditorGUILayout.FloatField("Damper Force:", damperForce);

        GUILayout.Space(20);

        GUI.enabled = carModel != null;
        if (GUILayout.Button("Setup Car", GUILayout.Height(40)))
        {
            SetupCar();
        }
        GUI.enabled = true;

        EditorGUILayout.EndScrollView();
    }

    void SetupCar()
    {
        if (carModel == null)
        {
            EditorUtility.DisplayDialog("Car Setup Wizard", "Please assign a car model first.", "OK");
            return;
        }

        // If it's a prefab asset (not in scene), instantiate it
        GameObject carInstance;
        if (PrefabUtility.IsPartOfPrefabAsset(carModel))
        {
            carInstance = (GameObject)PrefabUtility.InstantiatePrefab(carModel);
            carInstance.name = carModel.name;
        }
        else if (!carModel.scene.isLoaded)
        {
            // It's an FBX or model asset — instantiate
            carInstance = Instantiate(carModel);
            carInstance.name = carModel.name;
        }
        else
        {
            // Already in scene
            carInstance = carModel;
        }

        Undo.RegisterFullObjectHierarchyUndo(carInstance, "DreamRacer Car Setup");

        // 1. Add Rigidbody
        Rigidbody rb = carInstance.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = carInstance.AddComponent<Rigidbody>();
        }
        rb.mass = vehicleMass;
        rb.linearDamping = 0.01f;
        rb.angularDamping = 0.05f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // 2. Add MeshCollider on the body (find the largest mesh that isn't a wheel)
        AddBodyCollider(carInstance);

        // 3. Create Colliders hierarchy with WheelColliders
        Transform collidersRoot = carInstance.transform.Find("Colliders");
        if (collidersRoot == null)
        {
            GameObject collidersObj = new GameObject("Colliders");
            collidersObj.transform.SetParent(carInstance.transform, false);
            collidersRoot = collidersObj.transform;
        }

        WheelCollider flCollider = CreateOrGetWheelCollider(collidersRoot, "Front_Left");
        WheelCollider frCollider = CreateOrGetWheelCollider(collidersRoot, "Front_Right");
        WheelCollider rlCollider = CreateOrGetWheelCollider(collidersRoot, "Rear_Left");
        WheelCollider rrCollider = CreateOrGetWheelCollider(collidersRoot, "Rear_Right");

        // 4. Try to auto-detect wheel meshes and position colliders at wheel positions
        GameObject flMesh = FindWheelMesh(carInstance.transform, "front", "left");
        GameObject frMesh = FindWheelMesh(carInstance.transform, "front", "right");
        GameObject rlMesh = FindWheelMesh(carInstance.transform, "rear", "left");
        GameObject rrMesh = FindWheelMesh(carInstance.transform, "rear", "right");

        // Also try alternate naming: fl, fr, rl, rr or wheel_0, wheel_1, etc.
        if (flMesh == null) flMesh = FindWheelMeshAlt(carInstance.transform, new string[] { "fl", "wheel_0", "wheelfl", "wheel_fl" });
        if (frMesh == null) frMesh = FindWheelMeshAlt(carInstance.transform, new string[] { "fr", "wheel_1", "wheelfr", "wheel_fr" });
        if (rlMesh == null) rlMesh = FindWheelMeshAlt(carInstance.transform, new string[] { "rl", "wheel_2", "wheelrl", "wheel_rl" });
        if (rrMesh == null) rrMesh = FindWheelMeshAlt(carInstance.transform, new string[] { "rr", "wheel_3", "wheelrr", "wheel_rr" });

        // Position colliders at detected wheel mesh positions
        PositionColliderAtMesh(flCollider, flMesh);
        PositionColliderAtMesh(frCollider, frMesh);
        PositionColliderAtMesh(rlCollider, rlMesh);
        PositionColliderAtMesh(rrCollider, rrMesh);

        // 5. Add PrometeoCarController
        PrometeoCarController controller = carInstance.GetComponent<PrometeoCarController>();
        if (controller == null)
        {
            controller = carInstance.AddComponent<PrometeoCarController>();
        }

        // Configure controller values
        controller.maxSpeed = maxSpeed;
        controller.maxReverseSpeed = maxReverseSpeed;
        controller.accelerationMultiplier = accelerationMultiplier;
        controller.maxSteeringAngle = maxSteeringAngle;
        controller.steeringSpeed = steeringSpeed;
        controller.brakeForce = brakeForce;
        controller.decelerationMultiplier = decelerationMultiplier;
        controller.handbrakeDriftMultiplier = handbrakeDriftMultiplier;
        controller.bodyMassCenter = bodyMassCenter;

        // Assign wheel references
        controller.frontLeftCollider = flCollider;
        controller.frontRightCollider = frCollider;
        controller.rearLeftCollider = rlCollider;
        controller.rearRightCollider = rrCollider;
        controller.frontLeftMesh = flMesh;
        controller.frontRightMesh = frMesh;
        controller.rearLeftMesh = rlMesh;
        controller.rearRightMesh = rrMesh;

        // 6. Add CarDeformer
        CarDeformer deformer = carInstance.GetComponent<CarDeformer>();
        if (deformer == null)
        {
            carInstance.AddComponent<CarDeformer>();
        }

        // Mark dirty
        EditorUtility.SetDirty(carInstance);
        EditorUtility.SetDirty(controller);

        // Build result message
        int wheelsFound = 0;
        if (flMesh != null) wheelsFound++;
        if (frMesh != null) wheelsFound++;
        if (rlMesh != null) wheelsFound++;
        if (rrMesh != null) wheelsFound++;

        string message = $"Car setup complete!\n\n" +
            $"✓ Rigidbody (mass: {vehicleMass}kg)\n" +
            $"✓ PrometeoCarController\n" +
            $"✓ CarDeformer\n" +
            $"✓ 4 WheelColliders created under 'Colliders'\n" +
            $"✓ {wheelsFound}/4 wheel meshes auto-detected\n\n";

        if (wheelsFound < 4)
        {
            message += "⚠ Some wheel meshes could not be auto-detected.\n" +
                "Please assign them manually in the PrometeoCarController inspector.\n" +
                "Wheel mesh names should contain 'wheel'/'tyre'/'tire' + 'front'/'rear' + 'left'/'right'.";
        }

        Selection.activeGameObject = carInstance;
        EditorUtility.DisplayDialog("Car Setup Wizard", message, "OK");
    }

    WheelCollider CreateOrGetWheelCollider(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            WheelCollider wc = existing.GetComponent<WheelCollider>();
            if (wc != null)
            {
                ConfigureWheelCollider(wc);
                return wc;
            }
        }

        GameObject wheelObj = new GameObject(name);
        wheelObj.transform.SetParent(parent, false);
        WheelCollider collider = wheelObj.AddComponent<WheelCollider>();
        ConfigureWheelCollider(collider);
        return collider;
    }

    void ConfigureWheelCollider(WheelCollider wc)
    {
        wc.radius = wheelRadius;
        wc.suspensionDistance = suspensionDistance;
        wc.center = new Vector3(0f, 0.15f, 0f);

        JointSpring spring = wc.suspensionSpring;
        spring.spring = springForce;
        spring.damper = damperForce;
        spring.targetPosition = 0.5f;
        wc.suspensionSpring = spring;

        WheelFrictionCurve forwardFriction = wc.forwardFriction;
        forwardFriction.extremumSlip = forwardExtremumSlip;
        forwardFriction.extremumValue = forwardExtremumValue;
        forwardFriction.asymptoteSlip = 0.8f;
        forwardFriction.asymptoteValue = 0.5f;
        forwardFriction.stiffness = 1f;
        wc.forwardFriction = forwardFriction;

        WheelFrictionCurve sidewaysFriction = wc.sidewaysFriction;
        sidewaysFriction.extremumSlip = sidewaysExtremumSlip;
        sidewaysFriction.extremumValue = sidewaysExtremumValue;
        sidewaysFriction.asymptoteSlip = 0.5f;
        sidewaysFriction.asymptoteValue = 0.75f;
        sidewaysFriction.stiffness = 1f;
        wc.sidewaysFriction = sidewaysFriction;
    }

    void PositionColliderAtMesh(WheelCollider collider, GameObject mesh)
    {
        if (collider == null || mesh == null) return;

        // Convert mesh world position to collider's parent local space
        Vector3 localPos = collider.transform.parent.InverseTransformPoint(mesh.transform.position);
        collider.transform.localPosition = localPos;
        collider.transform.localRotation = Quaternion.identity;
    }

    GameObject FindWheelMesh(Transform root, string axle, string side)
    {
        MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer renderer in renderers)
        {
            string name = renderer.gameObject.name.ToLowerInvariant();
            if ((name.Contains("wheel") || name.Contains("tyre") || name.Contains("tire"))
               && name.Contains(axle) && name.Contains(side))
            {
                return renderer.gameObject;
            }
        }
        return null;
    }

    GameObject FindWheelMeshAlt(Transform root, string[] patterns)
    {
        MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer renderer in renderers)
        {
            string name = renderer.gameObject.name.ToLowerInvariant();
            foreach (string pattern in patterns)
            {
                if (name.Contains(pattern))
                {
                    return renderer.gameObject;
                }
            }
        }
        return null;
    }

    void AddBodyCollider(GameObject car)
    {
        // Find the largest mesh on the car that isn't a wheel to use as body collider
        MeshFilter[] meshFilters = car.GetComponentsInChildren<MeshFilter>(true);
        MeshFilter bestBody = null;
        int bestVertexCount = 0;

        foreach (MeshFilter mf in meshFilters)
        {
            string name = mf.gameObject.name.ToLowerInvariant();
            // Skip wheels
            if (name.Contains("wheel") || name.Contains("tyre") || name.Contains("tire"))
                continue;

            if (mf.sharedMesh != null && mf.sharedMesh.vertexCount > bestVertexCount)
            {
                bestVertexCount = mf.sharedMesh.vertexCount;
                bestBody = mf;
            }
        }

        if (bestBody != null)
        {
            MeshCollider mc = bestBody.GetComponent<MeshCollider>();
            if (mc == null)
            {
                mc = bestBody.gameObject.AddComponent<MeshCollider>();
            }
            mc.convex = true;
        }
    }
}
