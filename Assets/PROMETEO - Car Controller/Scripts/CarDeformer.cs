using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarDeformer : MonoBehaviour
{
    [Header("Deformation Settings")]
    [Tooltip("The maximum distance vertices can be pushed inwards.")]
    public float maxDeformation = 0.5f;
    
    [Tooltip("How much damage a collision does (multiplied by impact velocity).")]
    public float damageMultiplier = 0.05f;
    
    [Tooltip("The minimum collision velocity required to cause damage.")]
    public float minDamageVelocity = 3.0f;
    
    [Tooltip("The radius of the damage area around the impact point.")]
    public float deformationRadius = 0.5f;

    [Tooltip("Key to press to repair the car instantly.")]
    public KeyCode repairKey = KeyCode.R;

    // A class to store the original mesh data so we can repair the car
    private class MeshData
    {
        public MeshFilter filter;
        public Mesh mesh;
        public Vector3[] originalVertices;
        public Vector3[] currentVertices;
    }

    private List<MeshData> deformableMeshes = new List<MeshData>();

    void Start()
    {
        // Find all MeshFilters on the car and its children
        MeshFilter[] allMeshFilters = GetComponentsInChildren<MeshFilter>();

        foreach (MeshFilter mf in allMeshFilters)
        {
            // We want to skip wheels. Prometeo usually names wheels with "Wheel" or "Tyre"
            string lowerName = mf.gameObject.name.ToLower();
            if (lowerName.Contains("wheel") || lowerName.Contains("tyre") || lowerName.Contains("tire"))
            {
                continue;
            }

            // Create a unique instance of the mesh so we don't deform the original project asset
            Mesh instancedMesh = mf.mesh; 
            if (instancedMesh == null) continue;

            MeshData data = new MeshData();
            data.filter = mf;
            data.mesh = instancedMesh;
            data.originalVertices = instancedMesh.vertices;
            // Clone the vertices array for our working copy
            data.currentVertices = instancedMesh.vertices; 

            // Only add meshes that actually have vertices
            if (data.originalVertices.Length > 0)
            {
                deformableMeshes.Add(data);
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(repairKey))
        {
            RepairCar();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Calculate the impact speed
        float impactSpeed = collision.relativeVelocity.magnitude;

        // If the impact is too light, don't deform
        if (impactSpeed < minDamageVelocity) return;

        // Determine how much damage this crash should cause
        float damageAmount = impactSpeed * damageMultiplier;

        // Go through all the contact points of the crash
        foreach (ContactPoint contact in collision.contacts)
        {
            DeformMeshes(contact.point, contact.normal, damageAmount);
        }
    }

    private void DeformMeshes(Vector3 point, Vector3 normal, float damageAmount)
    {
        foreach (MeshData data in deformableMeshes)
        {
            bool meshChanged = false;

            // We need to work in the local space of the mesh, so convert the world impact point
            Vector3 localPoint = data.filter.transform.InverseTransformPoint(point);
            // Convert the world normal to local normal as well
            Vector3 localNormal = data.filter.transform.InverseTransformDirection(normal).normalized;

            for (int i = 0; i < data.currentVertices.Length; i++)
            {
                // Calculate distance from impact point to this vertex
                float distance = Vector3.Distance(localPoint, data.currentVertices[i]);

                if (distance < deformationRadius)
                {
                    // Use a smoother quadratic falloff instead of linear
                    float falloff = 1.0f - (distance / deformationRadius);
                    falloff = falloff * falloff; 

                    // Generate Perlin noise based on vertex position to simulate random metal crumpling
                    // Scale it so the noise isn't too large or small relative to the car's size
                    float noiseX = data.currentVertices[i].x * 3.0f + point.y;
                    float noiseY = data.currentVertices[i].z * 3.0f + point.x;
                    float noise = Mathf.PerlinNoise(noiseX, noiseY);
                    
                    // Map noise to make some parts buckle more and others less (e.g. 0.3x to 1.3x)
                    float crumpleMultiplier = noise + 0.3f;

                    // Apply the deformation with the crumple multiplier
                    Vector3 deformationVector = localNormal * (falloff * damageAmount * crumpleMultiplier);

                    // Apply the deformation to our working copy
                    data.currentVertices[i] += deformationVector;

                    // Ensure we don't push the vertex further than maxDeformation from its original position
                    Vector3 offsetFromOriginal = data.currentVertices[i] - data.originalVertices[i];
                    if (offsetFromOriginal.magnitude > maxDeformation)
                    {
                        data.currentVertices[i] = data.originalVertices[i] + (offsetFromOriginal.normalized * maxDeformation);
                    }

                    meshChanged = true;
                }
            }

            // If we actually bent something, update the physical mesh
            if (meshChanged)
            {
                data.mesh.vertices = data.currentVertices;
                data.mesh.RecalculateNormals(); // Fix lighting
                data.mesh.RecalculateBounds(); // Fix culling

                // If the mesh has a MeshCollider, we should update that too so physics match the bent metal
                MeshCollider mc = data.filter.GetComponent<MeshCollider>();
                if (mc != null)
                {
                    // Toggling the shared mesh forces Unity to rebuild the collider
                    mc.sharedMesh = null;
                    mc.sharedMesh = data.mesh;
                }
            }
        }
    }

    public void RepairCar()
    {
        foreach (MeshData data in deformableMeshes)
        {
            // Copy the original vertices back into our working array
            System.Array.Copy(data.originalVertices, data.currentVertices, data.originalVertices.Length);
            
            // Apply to the mesh
            data.mesh.vertices = data.currentVertices;
            data.mesh.RecalculateNormals();
            data.mesh.RecalculateBounds();

            MeshCollider mc = data.filter.GetComponent<MeshCollider>();
            if (mc != null)
            {
                mc.sharedMesh = null;
                mc.sharedMesh = data.mesh;
            }
        }
        
        Debug.Log("Car Repaired!");
    }
}
