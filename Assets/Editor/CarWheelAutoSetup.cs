using UnityEngine;
using UnityEditor;

public static class CarWheelAutoSetup
{
    [MenuItem("DreamRacer/Fix Car Wheel Setup")]
    public static void FixSelectedCar()
    {
        PrometeoCarController controller = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponentInParent<PrometeoCarController>()
            : null;

        if(controller == null){
            controller = Object.FindFirstObjectByType<PrometeoCarController>();
        }

        if(controller == null){
            EditorUtility.DisplayDialog("Fix Car Wheel Setup", "No PrometeoCarController found in the scene.", "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Fix Car Wheel Setup");
        Apply(controller);
        EditorUtility.SetDirty(controller);
        EditorUtility.DisplayDialog("Fix Car Wheel Setup", "Wheel colliders, meshes, and Prometeo references were updated.", "OK");
    }

    public static void Apply(PrometeoCarController controller)
    {
        Transform root = controller.transform;
        Transform collidersRoot = FindChild(root, "Colliders") ?? FindChild(root, "Colliders (2)");

        AssignWheel(controller, "frontLeft", collidersRoot, "Front_Left", "FrontLeft");
        AssignWheel(controller, "frontRight", collidersRoot, "Front_Right", "FrontRight");
        AssignWheel(controller, "rearLeft", collidersRoot, "Rear_Left", "RearLeft");
        AssignWheel(controller, "rearRight", collidersRoot, "Rear_Right", "RearRight");

        root.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        root.localPosition = new Vector3(0f, 1f, 0f);

        if(collidersRoot != null){
            collidersRoot.localRotation = Quaternion.identity;
            collidersRoot.localPosition = Vector3.zero;
        }

        SetWheelTransform(controller.frontLeftCollider, controller.frontLeftMesh, new Vector3(-0.854f, -1.21f, 0.395f));
        SetWheelTransform(controller.frontRightCollider, controller.frontRightMesh, new Vector3(0.854f, -1.21f, 0.395f));
        SetWheelTransform(controller.rearLeftCollider, controller.rearLeftMesh, new Vector3(-0.854f, 1.5f, 0.395f));
        SetWheelTransform(controller.rearRightCollider, controller.rearRightMesh, new Vector3(0.854f, 1.5f, 0.395f));

        NormalizeCollider(controller.frontLeftCollider);
        NormalizeCollider(controller.frontRightCollider);
        NormalizeCollider(controller.rearLeftCollider);
        NormalizeCollider(controller.rearRightCollider);
    }

    static void SetWheelTransform(WheelCollider collider, GameObject mesh, Vector3 localPosition)
    {
        if(collider != null){
            collider.transform.localPosition = localPosition;
            collider.transform.localRotation = Quaternion.identity;
        }

        if(mesh != null){
            mesh.transform.localPosition = localPosition;
            mesh.transform.localRotation = Quaternion.identity;
        }
    }

    static void AssignWheel(
        PrometeoCarController controller,
        string slotPrefix,
        Transform collidersRoot,
        string sideToken,
        string compactToken)
    {
        WheelCollider collider = FindWheelCollider(collidersRoot, sideToken);
        GameObject mesh = FindWheelMesh(controller.transform, sideToken, compactToken);

        switch(slotPrefix){
            case "frontLeft":
                controller.frontLeftCollider = collider;
                controller.frontLeftMesh = mesh;
                break;
            case "frontRight":
                controller.frontRightCollider = collider;
                controller.frontRightMesh = mesh;
                break;
            case "rearLeft":
                controller.rearLeftCollider = collider;
                controller.rearLeftMesh = mesh;
                break;
            case "rearRight":
                controller.rearRightCollider = collider;
                controller.rearRightMesh = mesh;
                break;
        }
    }

    static Transform FindChild(Transform parent, string childName)
    {
        if(parent == null){
            return null;
        }

        foreach(Transform child in parent){
            if(child.name == childName){
                return child;
            }
        }

        return null;
    }

    static WheelCollider FindWheelCollider(Transform collidersRoot, string sideToken)
    {
        if(collidersRoot == null){
            return null;
        }

        WheelCollider[] colliders = collidersRoot.GetComponentsInChildren<WheelCollider>(true);
        foreach(WheelCollider collider in colliders){
            string name = collider.gameObject.name.ToLowerInvariant();
            if(name.Contains(sideToken.ToLowerInvariant()) || name.Contains(sideToken.Replace("_", "").ToLowerInvariant())){
                return collider;
            }
        }

        return null;
    }

    static GameObject FindWheelMesh(Transform root, string sideToken, string compactToken)
    {
        MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        string side = sideToken.ToLowerInvariant();
        string compact = compactToken.ToLowerInvariant();

        foreach(MeshRenderer renderer in renderers){
            string name = renderer.gameObject.name.ToLowerInvariant();
            if((name.Contains("wheel") || name.Contains("tyre") || name.Contains("tire"))
               && (name.Contains(side) || name.Contains(compact) || name.Contains(side.Replace("_", "")))){
                return renderer.gameObject;
            }
        }

        return null;
    }

    static void NormalizeCollider(WheelCollider collider)
    {
        if(collider == null){
            return;
        }

        collider.center = new Vector3(0f, 0.15f, 0f);
        collider.radius = 0.32f;
    }

    static void SyncMeshesToColliders(PrometeoCarController controller)
    {
        Sync(controller.frontLeftCollider, controller.frontLeftMesh);
        Sync(controller.frontRightCollider, controller.frontRightMesh);
        Sync(controller.rearLeftCollider, controller.rearLeftMesh);
        Sync(controller.rearRightCollider, controller.rearRightMesh);
    }

    static void Sync(WheelCollider collider, GameObject mesh)
    {
        if(collider == null || mesh == null){
            return;
        }

        mesh.transform.position = collider.transform.TransformPoint(collider.center);
        mesh.transform.rotation = collider.transform.rotation;
    }
}
