using UnityEngine;
using UnityEditor;

public class WheelPivotFixer : EditorWindow
{
    private GameObject frontLeft;
    private GameObject frontRight;
    private GameObject rearLeft;
    private GameObject rearRight;

    [MenuItem("DreamRacer/Fix Floating Tyres")]
    public static void ShowWindow()
    {
        GetWindow<WheelPivotFixer>("Fix Tyres");
    }

    void OnGUI()
    {
        GUILayout.Label("Automatic Tyre Pivot Fixer", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("If your tyres are floating outside your car, it's because Blender exported them with their origin points at the center of the world instead of the center of the tyre! Assign your 4 floating tyre meshes here and click Fix to instantly correct their pivots.", MessageType.Info);

        frontLeft = (GameObject)EditorGUILayout.ObjectField("Front Left Tyre", frontLeft, typeof(GameObject), true);
        frontRight = (GameObject)EditorGUILayout.ObjectField("Front Right Tyre", frontRight, typeof(GameObject), true);
        rearLeft = (GameObject)EditorGUILayout.ObjectField("Rear Left Tyre", rearLeft, typeof(GameObject), true);
        rearRight = (GameObject)EditorGUILayout.ObjectField("Rear Right Tyre", rearRight, typeof(GameObject), true);

        GUILayout.Space(20);

        if (GUILayout.Button("FIX TYRE PIVOTS!", GUILayout.Height(40)))
        {
            FixTyre(frontLeft, "FrontLeft");
            FixTyre(frontRight, "FrontRight");
            FixTyre(rearLeft, "RearLeft");
            FixTyre(rearRight, "RearRight");
            EditorUtility.DisplayDialog("Success!", "Tyres fixed! Now, go to your PrometeoCarController script and assign the new 'Pivot' objects into the Wheel Mesh slots instead of the old meshes!", "Awesome");
        }
    }

    private void FixTyre(GameObject tyreMesh, string namePrefix)
    {
        if (tyreMesh == null) return;

        // Create a new empty GameObject to act as the true center pivot
        GameObject pivot = new GameObject(namePrefix + "_TruePivot");
        Undo.RegisterCreatedObjectUndo(pivot, "Create Pivot");

        // Put it in the same parent so it stays in the car hierarchy
        pivot.transform.SetParent(tyreMesh.transform.parent);

        // Get the actual visual center of the tyre geometry
        Renderer renderer = tyreMesh.GetComponent<Renderer>();
        if (renderer != null)
        {
            pivot.transform.position = renderer.bounds.center;
        }
        else
        {
            pivot.transform.position = tyreMesh.transform.position;
        }

        // Parent the broken mesh to the perfect pivot
        Undo.SetTransformParent(tyreMesh.transform, pivot.transform, "Parent tyre to pivot");
        
        Debug.Log($"Fixed pivot for {tyreMesh.name}. Use {pivot.name} in your script now!");
    }
}
