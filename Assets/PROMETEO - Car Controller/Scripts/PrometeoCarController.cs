/*
MESSAGE FROM CREATOR: This script was coded by Mena. You can use it in your games either these are commercial or
personal projects. You can even add or remove functions as you wish. However, you cannot sell copies of this
script by itself, since it is originally distributed as a free product.
I wish you the best for your project. Good luck!

P.S: If you need more cars, you can check my other vehicle assets on the Unity Asset Store, perhaps you could find
something useful for your game. Best regards, Mena.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Fusion;

public class PrometeoCarController : NetworkBehaviour
{

    //CAR SETUP

      [Space(20)]
      //[Header("CAR SETUP")]
      [Space(10)]
      [Range(20, 190)]
      public int maxSpeed = 90; //The maximum speed that the car can reach in km/h.
      [Range(10, 120)]
      public int maxReverseSpeed = 45; //The maximum speed that the car can reach while going on reverse in km/h.
      [Range(1, 10)]
      public int accelerationMultiplier = 2; // How fast the car can accelerate. 1 is a slow acceleration and 10 is the fastest.
      [Space(10)]
      [Range(10, 45)]
      public int maxSteeringAngle = 27; // The maximum angle that the tires can reach while rotating the steering wheel.
      [Range(0.1f, 1f)]
      public float steeringSpeed = 0.5f; // How fast the steering wheel turns.
      [Space(10)]
      [Range(100, 600)]
      public int brakeForce = 350; // The strength of the wheel brakes.
      [Range(1, 10)]
      public int decelerationMultiplier = 2; // How fast the car decelerates when the user is not using the throttle.
      [Range(1, 10)]
      public int handbrakeDriftMultiplier = 5; // How much grip the car loses when the user hit the handbrake.
      [Space(10)]
      public Vector3 bodyMassCenter; // This is a vector that contains the center of mass of the car. I recommend to set this value
                                    // in the points x = 0 and z = 0 of your car. You can select the value that you want in the y axis,
                                    // however, you must notice that the higher this value is, the more unstable the car becomes.
                                    // Usually the y value goes from 0 to 1.5.

    //GEAR SYSTEM

      public enum TransmissionMode { Automatic, SequentialGear, ManualClutch }

      [Space(20)]
      //[Header("GEAR SYSTEM")]
      [Space(10)]
      public TransmissionMode transmissionMode = TransmissionMode.Automatic;
      public int numberOfGears = 6;
      public float[] gearRatios = new float[] { 3.5f, 2.5f, 1.8f, 1.4f, 1.1f, 0.9f };
      public float reverseGearRatio = 3.2f;
      public float finalDriveRatio = 3.5f;
      [Range(4000, 12000)]
      public float maxEngineRPM = 8000f;
      [Range(500, 1500)]
      public float idleRPM = 800f;
      [Range(4000, 10000)]
      public float shiftUpRPM = 6500f;    // Automatic shifts up at this RPM
      [Range(1000, 4000)]
      public float shiftDownRPM = 2500f;  // Automatic shifts down at this RPM

    //WHEELS

      //[Header("WHEELS")]

      /*
      The following variables are used to store the wheels' data of the car. We need both the mesh-only game objects and wheel
      collider components of the wheels. The wheel collider components and 3D meshes of the wheels cannot come from the same
      game object; they must be separate game objects.
      */
      public GameObject frontLeftMesh;
      public WheelCollider frontLeftCollider;
      [Space(10)]
      public GameObject frontRightMesh;
      public WheelCollider frontRightCollider;
      [Space(10)]
      public GameObject rearLeftMesh;
      public WheelCollider rearLeftCollider;
      [Space(10)]
      public GameObject rearRightMesh;
      public WheelCollider rearRightCollider;



    //CAR DATA

      [HideInInspector]
      public float carSpeed; // Used to store the speed of the car.
      [HideInInspector]
      public bool isDrifting; // Used to know whether the car is drifting or not.
      [HideInInspector]
      public bool isTractionLocked; // Used to know whether the traction of the car is locked or not.

    //GEAR DATA (public read-only properties for UIManager)

      private int currentGear = 1;        // 0 = neutral, -1 = reverse, 1-6 = forward gears
      private float engineRPM;
      private bool clutchEngaged = true;   // true = clutch released (power flows), false = clutch pressed (disengaged)
      private float clutchInput;           // 0 = clutch released, 1 = clutch fully pressed
      private float shiftCooldown = 0.5f;

      // Public read-only properties for UIManager
      public int CurrentGear => currentGear;
      public float EngineRPM => engineRPM;
      public TransmissionMode CurrentTransmissionMode => transmissionMode;
      public bool ClutchEngaged => clutchEngaged;

    //PRIVATE VARIABLES

      /*
      IMPORTANT: The following variables should not be modified manually since their values are automatically given via script.
      */
      Rigidbody carRigidbody; // Stores the car's rigidbody.
      float steeringAxis; // Used to know whether the steering wheel has reached the maximum value. It goes from -1 to 1.
      float throttleAxis; // Used to know whether the throttle has reached the maximum value. It goes from -1 to 1.
      float driftingAxis;
      float localVelocityZ;
      float localVelocityX;
      bool deceleratingCar;
      /*
      The following variables are used to store information about sideways friction of the wheels (such as
      extremumSlip,extremumValue, asymptoteSlip, asymptoteValue and stiffness). We change this values to
      make the car to start drifting.
      */
      WheelFrictionCurve FLwheelFriction;
      float FLWextremumSlip;
      WheelFrictionCurve FRwheelFriction;
      float FRWextremumSlip;
      WheelFrictionCurve RLwheelFriction;
      float RLWextremumSlip;
      WheelFrictionCurve RRwheelFriction;
      float RRWextremumSlip;

      [Space(10)]
      public GameObject localCamera; // Assign your Cinemachine camera here in the Inspector

      private CarControls controls;
      private bool prevShiftUp;
      private bool prevShiftDown;
      private float lastShiftTime;

      [Networked] public float NetCarSpeed { get; set; }
      [Networked] public float NetEngineRPM { get; set; }
      [Networked] public int NetCurrentGear { get; set; }
      [Networked] public bool NetClutchEngaged { get; set; }
      [Networked] public float NetWheelRPM { get; set; }
      [Networked] public float NetSteeringAngle { get; set; }

      // Race freeze state — prevents all input and movement during countdown
      private bool isFrozen = false;

      /// <summary>
      /// Freezes or unfreezes the car. Used by RaceManager during countdown.
      /// When frozen, the car cannot move or accept input.
      /// </summary>
      public void SetFrozen(bool frozen)
      {
          isFrozen = frozen;
          if (carRigidbody != null)
          {
              if (frozen)
              {
                  carRigidbody.linearVelocity = Vector3.zero;
                  carRigidbody.angularVelocity = Vector3.zero;
                  carRigidbody.constraints = RigidbodyConstraints.FreezeAll;
              }
              else
              {
                  carRigidbody.constraints = RigidbodyConstraints.None;
              }
          }
      }

    void Awake()
    {
        controls = new CarControls();
        carRigidbody = gameObject.GetComponent<Rigidbody>();
        
        TryAutoAssignWheelReferences();

        if(frontLeftCollider == null || frontRightCollider == null || rearLeftCollider == null || rearRightCollider == null){
          Debug.LogError("PrometeoCarController is missing one or more WheelCollider references.");
          enabled = false;
          return;
        }

        carRigidbody.centerOfMass = bodyMassCenter;

        FLwheelFriction = new WheelFrictionCurve ();
        FLwheelFriction.extremumSlip = frontLeftCollider.sidewaysFriction.extremumSlip;
        FLWextremumSlip = frontLeftCollider.sidewaysFriction.extremumSlip;
        FLwheelFriction.extremumValue = frontLeftCollider.sidewaysFriction.extremumValue;
        FLwheelFriction.asymptoteSlip = frontLeftCollider.sidewaysFriction.asymptoteSlip;
        FLwheelFriction.asymptoteValue = frontLeftCollider.sidewaysFriction.asymptoteValue;
        FLwheelFriction.stiffness = frontLeftCollider.sidewaysFriction.stiffness;
      FRwheelFriction = new WheelFrictionCurve ();
        FRwheelFriction.extremumSlip = frontRightCollider.sidewaysFriction.extremumSlip;
        FRWextremumSlip = frontRightCollider.sidewaysFriction.extremumSlip;
        FRwheelFriction.extremumValue = frontRightCollider.sidewaysFriction.extremumValue;
        FRwheelFriction.asymptoteSlip = frontRightCollider.sidewaysFriction.asymptoteSlip;
        FRwheelFriction.asymptoteValue = frontRightCollider.sidewaysFriction.asymptoteValue;
        FRwheelFriction.stiffness = frontRightCollider.sidewaysFriction.stiffness;
      RLwheelFriction = new WheelFrictionCurve ();
        RLwheelFriction.extremumSlip = rearLeftCollider.sidewaysFriction.extremumSlip;
        RLWextremumSlip = rearLeftCollider.sidewaysFriction.extremumSlip;
        RLwheelFriction.extremumValue = rearLeftCollider.sidewaysFriction.extremumValue;
        RLwheelFriction.asymptoteSlip = rearLeftCollider.sidewaysFriction.asymptoteSlip;
        RLwheelFriction.asymptoteValue = rearLeftCollider.sidewaysFriction.asymptoteValue;
        RLwheelFriction.stiffness = rearLeftCollider.sidewaysFriction.stiffness;
      RRwheelFriction = new WheelFrictionCurve ();
        RRwheelFriction.extremumSlip = rearRightCollider.sidewaysFriction.extremumSlip;
        RRWextremumSlip = rearRightCollider.sidewaysFriction.extremumSlip;
        RRwheelFriction.extremumValue = rearRightCollider.sidewaysFriction.extremumValue;
        RRwheelFriction.asymptoteSlip = rearRightCollider.sidewaysFriction.asymptoteSlip;
        RRwheelFriction.asymptoteValue = rearRightCollider.sidewaysFriction.asymptoteValue;
        RRwheelFriction.stiffness = rearRightCollider.sidewaysFriction.stiffness;



        SyncWheelMeshesToColliders();
        SnapCarToGround();
        ResetWheelPhysicsState();

    }

    public override void Spawned()
    {
        if (localCamera != null)
        {
            localCamera.SetActive(HasStateAuthority);
        }
        if (HasStateAuthority)
        {
            controls.Enable();
            Minimap.LocalPlayer = this.transform;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        controls.Disable();
    }

    void TryAutoAssignWheelReferences(){
      Transform collidersRoot = transform.Find("Colliders");
      if(collidersRoot == null){
        collidersRoot = transform.Find("Colliders (2)");
      }

      if(frontLeftCollider == null) frontLeftCollider = FindWheelCollider(collidersRoot, "front", "left");
      if(frontRightCollider == null) frontRightCollider = FindWheelCollider(collidersRoot, "front", "right");
      if(rearLeftCollider == null) rearLeftCollider = FindWheelCollider(collidersRoot, "rear", "left");
      if(rearRightCollider == null) rearRightCollider = FindWheelCollider(collidersRoot, "rear", "right");

      if(frontLeftMesh == null) frontLeftMesh = FindWheelMesh("front", "left");
      if(frontRightMesh == null) frontRightMesh = FindWheelMesh("front", "right");
      if(rearLeftMesh == null) rearLeftMesh = FindWheelMesh("rear", "left");
      if(rearRightMesh == null) rearRightMesh = FindWheelMesh("rear", "right");
    }

    WheelCollider FindWheelCollider(Transform collidersRoot, string axle, string side){
      if(collidersRoot == null){
        return null;
      }

      WheelCollider[] colliders = collidersRoot.GetComponentsInChildren<WheelCollider>(true);
      foreach(WheelCollider collider in colliders){
        string name = collider.gameObject.name.ToLowerInvariant();
        if(name.Contains(axle) && name.Contains(side)){
          return collider;
        }
      }

      return null;
    }

    GameObject FindWheelMesh(string axle, string side){
      MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);
      foreach(MeshRenderer renderer in renderers){
        string name = renderer.gameObject.name.ToLowerInvariant();
        if((name.Contains("wheel") || name.Contains("tyre") || name.Contains("tire"))
           && name.Contains(axle) && name.Contains(side)){
          return renderer.gameObject;
        }
      }

      return null;
    }

    void ResetWheelPhysicsState(){
      ReleaseAllBrakes();
      isTractionLocked = false;
      driftingAxis = 0f;
      if(carRigidbody != null){
        carRigidbody.WakeUp();
      }
    }

    void ReleaseAllBrakes(){
      if(frontLeftCollider != null) frontLeftCollider.brakeTorque = 0;
      if(frontRightCollider != null) frontRightCollider.brakeTorque = 0;
      if(rearLeftCollider != null) rearLeftCollider.brakeTorque = 0;
      if(rearRightCollider != null) rearRightCollider.brakeTorque = 0;
    }

    void SnapCarToGround(){
      WheelCollider[] wheels = {
        frontLeftCollider,
        frontRightCollider,
        rearLeftCollider,
        rearRightCollider
      };

      float maxLift = float.NegativeInfinity;
      int groundedWheelCount = 0;

      foreach(WheelCollider wheel in wheels){
        if(wheel == null){
          continue;
        }

        Vector3 wheelBottom = wheel.transform.TransformPoint(wheel.center + new Vector3(0f, -wheel.radius, 0f));
        float rayStartHeight = wheel.suspensionDistance + wheel.radius + 1f;
        Vector3 rayOrigin = wheelBottom + Vector3.up * rayStartHeight;

        if(Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayStartHeight + 2f)){
          float lift = hit.point.y - wheelBottom.y;
          maxLift = Mathf.Max(maxLift, lift);
          groundedWheelCount++;
        }
      }

      if(groundedWheelCount > 0 && maxLift > float.NegativeInfinity){
        transform.position += Vector3.up * maxLift;
      }
    }

    void SyncWheelMeshesToColliders(){
      SyncWheelMeshToCollider(frontLeftCollider, frontLeftMesh);
      SyncWheelMeshToCollider(frontRightCollider, frontRightMesh);
      SyncWheelMeshToCollider(rearLeftCollider, rearLeftMesh);
      SyncWheelMeshToCollider(rearRightCollider, rearRightMesh);
    }

    static void SyncWheelMeshToCollider(WheelCollider collider, GameObject wheelMesh){
      if(collider == null || wheelMesh == null){
        return;
      }

      wheelMesh.transform.position = collider.transform.TransformPoint(collider.center);
      wheelMesh.transform.rotation = collider.transform.rotation;
    }

    float SimulationDeltaTime => Time.fixedDeltaTime;

    // FixedUpdate is called by Unity's physics tick
    void FixedUpdate()
    {
      if (!HasStateAuthority)
      {
        return;
      }

      // If the car is frozen (e.g. race countdown), skip all input and physics
      if (isFrozen)
      {
        return;
      }

      //CAR DATA
      // We determine the speed of the car.
      carSpeed = (2 * Mathf.PI * frontLeftCollider.radius * frontLeftCollider.rpm * 60) / 1000;
      // Save the local velocity of the car in the x axis. Used to know if the car is drifting.
      localVelocityX = transform.InverseTransformDirection(carRigidbody.linearVelocity).x;
      // Save the local velocity of the car in the z axis. Used to know if the car is going forward or backwards.
      localVelocityZ = transform.InverseTransformDirection(carRigidbody.linearVelocity).z;

      // Update engine RPM and gear system
      UpdateGearSystem();

      //CAR PHYSICS
      bool isAccelerating = false;
      bool isReversing = false;
      bool isBraking = false;
      float steeringValue = 0f;
      bool shiftUpInput = false;
      bool shiftDownInput = false;
      bool clutchHeld = false;

      isAccelerating = controls.Driving.Accelerate.IsPressed();
      isReversing = controls.Driving.Reverse.IsPressed();
      isBraking = controls.Driving.HandBrake.IsPressed();
      steeringValue = controls.Driving.Steer.ReadValue<float>();
      
      bool currentShiftUp = controls.Driving.ShiftUp.IsPressed();
      bool currentShiftDown = controls.Driving.ShiftDown.IsPressed();
      
      shiftUpInput = !prevShiftUp && currentShiftUp;
      shiftDownInput = !prevShiftDown && currentShiftDown;
      clutchHeld = controls.Driving.Clutch.IsPressed();

      prevShiftUp = currentShiftUp;
      prevShiftDown = currentShiftDown;

      // Handle gear shifting based on transmission mode
      HandleGearInput(shiftUpInput, shiftDownInput, clutchHeld);

      bool intendedForward = (currentGear >= 1);
      bool intendedReverse = (currentGear == -1);

      if (transmissionMode == TransmissionMode.Automatic) {
        if(isAccelerating){
          deceleratingCar = false;
          if (localVelocityZ < -1f) Brakes(); // moving backward, brake
          else GoForward(); // auto-shifts to 1
        }
        if(isReversing){
          deceleratingCar = false;
          if (localVelocityZ > 1f) Brakes();
          else GoReverse(); // auto-shifts to -1
        }
      } else {
        if(isAccelerating){
          deceleratingCar = false;

          if (intendedForward) {
            if (localVelocityZ < -1f) Brakes(); // moving backward, brake
            else GoForward();
          } else if (intendedReverse) {
            if (localVelocityZ > 1f) Brakes(); // moving forward, brake
            else GoReverse();
          } else {
            ThrottleOff();
          }
        }

        if(isReversing){
          deceleratingCar = false;
          // In manual mode, 'S' is strictly a brake
          Brakes();
        }
      }

      if(steeringValue < -0.1f){
        TurnLeft();
      }
      if(steeringValue > 0.1f){
        TurnRight();
      }
      if(isBraking){
        deceleratingCar = false;
        Handbrake();
      }
      if((!isReversing && !isAccelerating)){
        ThrottleOff();
      }
      if((!isReversing && !isAccelerating) && !isBraking && !deceleratingCar){
        DecelerateCar();
        deceleratingCar = true;
      }
      if(steeringValue > -0.1f && steeringValue < 0.1f && steeringAxis != 0f){
        ResetSteeringAngle();
      }

      if (isTractionLocked && !isBraking)
      {
        RecoverTraction();
      }

      // Sync telemetry for remote proxies only; predicting peers use local values.
      if (HasStateAuthority)
      {
        NetCarSpeed = carSpeed;
        NetEngineRPM = engineRPM;
        NetCurrentGear = currentGear;
        NetClutchEngaged = clutchEngaged;
        NetWheelRPM = frontLeftCollider.rpm;
        NetSteeringAngle = frontLeftCollider.steerAngle;
      }

      // Push telemetry data to UIManager singleton for the local player
      if(HasStateAuthority && UIManager.Instance != null){
        UIManager.Instance.UpdateCarUI(
          carSpeed, currentGear, engineRPM,
          maxEngineRPM, clutchEngaged, transmissionMode
        );
      }

    }

    void LateUpdate()
    {
      AnimateWheelMeshes();
    }



    //
    //STEERING METHODS
    //

    //The following method turns the front car wheels to the left. The speed of this movement will depend on the steeringSpeed variable.
    public void TurnLeft(){
      steeringAxis = steeringAxis - (SimulationDeltaTime * 10f * steeringSpeed);
      if(steeringAxis < -1f){
        steeringAxis = -1f;
      }
      var steeringAngle = steeringAxis * maxSteeringAngle;
      frontLeftCollider.steerAngle = Mathf.Lerp(frontLeftCollider.steerAngle, steeringAngle, steeringSpeed);
      frontRightCollider.steerAngle = Mathf.Lerp(frontRightCollider.steerAngle, steeringAngle, steeringSpeed);
    }

    //The following method turns the front car wheels to the right. The speed of this movement will depend on the steeringSpeed variable.
    public void TurnRight(){
      steeringAxis = steeringAxis + (SimulationDeltaTime * 10f * steeringSpeed);
      if(steeringAxis > 1f){
        steeringAxis = 1f;
      }
      var steeringAngle = steeringAxis * maxSteeringAngle;
      frontLeftCollider.steerAngle = Mathf.Lerp(frontLeftCollider.steerAngle, steeringAngle, steeringSpeed);
      frontRightCollider.steerAngle = Mathf.Lerp(frontRightCollider.steerAngle, steeringAngle, steeringSpeed);
    }

    //The following method takes the front car wheels to their default position (rotation = 0). The speed of this movement will depend
    // on the steeringSpeed variable.
    public void ResetSteeringAngle(){
      if(steeringAxis < 0f){
        steeringAxis = steeringAxis + (SimulationDeltaTime * 10f * steeringSpeed);
      }else if(steeringAxis > 0f){
        steeringAxis = steeringAxis - (SimulationDeltaTime * 10f * steeringSpeed);
      }
      if(Mathf.Abs(frontLeftCollider.steerAngle) < 1f){
        steeringAxis = 0f;
      }
      var steeringAngle = steeringAxis * maxSteeringAngle;
      frontLeftCollider.steerAngle = Mathf.Lerp(frontLeftCollider.steerAngle, steeringAngle, steeringSpeed);
      frontRightCollider.steerAngle = Mathf.Lerp(frontRightCollider.steerAngle, steeringAngle, steeringSpeed);
    }

    // This method matches both the position and rotation of the WheelColliders with the WheelMeshes.
    // A 180° Y rotation offset is applied to correct for wheel meshes that face the opposite direction.
    private static readonly Quaternion wheelRotationOffset = Quaternion.Euler(0, 180, 0);

    private float clientWheelSpin = 0f;

    void AnimateWheelMeshes(){
      try{
        if (HasStateAuthority || HasInputAuthority) {
            Quaternion FLWRotation;
            Vector3 FLWPosition;
            frontLeftCollider.GetWorldPose(out FLWPosition, out FLWRotation);
            frontLeftMesh.transform.position = FLWPosition;
            frontLeftMesh.transform.rotation = FLWRotation * wheelRotationOffset;

            Quaternion FRWRotation;
            Vector3 FRWPosition;
            frontRightCollider.GetWorldPose(out FRWPosition, out FRWRotation);
            frontRightMesh.transform.position = FRWPosition;
            frontRightMesh.transform.rotation = FRWRotation * wheelRotationOffset;

            Quaternion RLWRotation;
            Vector3 RLWPosition;
            rearLeftCollider.GetWorldPose(out RLWPosition, out RLWRotation);
            rearLeftMesh.transform.position = RLWPosition;
            rearLeftMesh.transform.rotation = RLWRotation * wheelRotationOffset;

            Quaternion RRWRotation;
            Vector3 RRWPosition;
            rearRightCollider.GetWorldPose(out RRWPosition, out RRWRotation);
            rearRightMesh.transform.position = RRWPosition;
            rearRightMesh.transform.rotation = RRWRotation * wheelRotationOffset;
        } else {
            // Client manually spins the wheels based on networked RPM and steering
            clientWheelSpin += NetWheelRPM * 6f * Time.deltaTime;
            
            Vector3 pos; Quaternion rot;
            
            frontLeftCollider.GetWorldPose(out pos, out rot);
            frontLeftMesh.transform.position = pos;
            frontLeftMesh.transform.rotation = rot * Quaternion.Euler(clientWheelSpin, NetSteeringAngle, 0) * wheelRotationOffset;
            
            frontRightCollider.GetWorldPose(out pos, out rot);
            frontRightMesh.transform.position = pos;
            frontRightMesh.transform.rotation = rot * Quaternion.Euler(clientWheelSpin, NetSteeringAngle, 0) * wheelRotationOffset;
            
            rearLeftCollider.GetWorldPose(out pos, out rot);
            rearLeftMesh.transform.position = pos;
            rearLeftMesh.transform.rotation = rot * Quaternion.Euler(clientWheelSpin, 0, 0) * wheelRotationOffset;
            
            rearRightCollider.GetWorldPose(out pos, out rot);
            rearRightMesh.transform.position = pos;
            rearRightMesh.transform.rotation = rot * Quaternion.Euler(clientWheelSpin, 0, 0) * wheelRotationOffset;
        }
      }catch(Exception ex){
        Debug.LogWarning(ex);
      }
    }

    //
    //ENGINE AND BRAKING METHODS
    //

    // Returns the torque multiplier based on the current gear ratio.
    // In Automatic mode, gears shift automatically. In other modes,
    // the player controls shifting. Clutch affects torque in ManualClutch mode.
    float GetGearTorqueMultiplier(){
      float gearRatio;
      if(currentGear == 0){
        // Neutral — no torque
        return 0f;
      } else if(currentGear == -1){
        gearRatio = reverseGearRatio;
      } else {
        int gearIndex = Mathf.Clamp(currentGear - 1, 0, gearRatios.Length - 1);
        gearRatio = gearRatios[gearIndex];
      }

      float torqueMultiplier = gearRatio * finalDriveRatio;

      // In ManualClutch mode, torque is scaled by how much the clutch is released
      if(transmissionMode == TransmissionMode.ManualClutch){
        float clutchFactor = 1f - clutchInput; // 0 input = full power, 1 input = no power
        torqueMultiplier *= clutchFactor;
      }

      // Normalize so gear 1 ratio doesn't produce wildly different torque from the original flat value.
      // The original torque was (accelerationMultiplier * 50f). We scale relative to gear 1 ratio.
      if(gearRatios.Length > 0 && gearRatios[0] > 0f){
        torqueMultiplier /= gearRatios[0];
      }

      return torqueMultiplier;
    }

    // This method apply positive torque to the wheels in order to go forward.
    public void GoForward(){
      isDrifting = Mathf.Abs(localVelocityX) > 2.5f;
      // The following part sets the throttle power to 1 smoothly.
      throttleAxis = throttleAxis + (SimulationDeltaTime * 3f);
      if(throttleAxis > 1f){
        throttleAxis = 1f;
      }

      // Ensure we are in a forward gear (auto-shift from reverse/neutral when accelerating)
      if(currentGear <= 0 && transmissionMode == TransmissionMode.Automatic){
        currentGear = 1;
      }

      //If the car is going backwards, then apply brakes in order to avoid strange
      //behaviours. If the local velocity in the 'z' axis is less than -1f, then it
      //is safe to apply positive torque to go forward.
      if(localVelocityZ < -1f){
        Brakes();
      }else{
        if(engineRPM >= maxEngineRPM - 10f){
          // Rev limiter hit
          frontLeftCollider.motorTorque = 0;
          frontRightCollider.motorTorque = 0;
          rearLeftCollider.motorTorque = 0;
          rearRightCollider.motorTorque = 0;
        }else if(Mathf.RoundToInt(carSpeed) < maxSpeed){
          float gearMultiplier = GetGearTorqueMultiplier();
          float motorTorque = (accelerationMultiplier * 50f) * throttleAxis * gearMultiplier;
          //Apply positive torque in all wheels to go forward if maxSpeed has not been reached.
          frontLeftCollider.brakeTorque = 0;
          frontLeftCollider.motorTorque = motorTorque;
          frontRightCollider.brakeTorque = 0;
          frontRightCollider.motorTorque = motorTorque;
          rearLeftCollider.brakeTorque = 0;
          rearLeftCollider.motorTorque = motorTorque;
          rearRightCollider.brakeTorque = 0;
          rearRightCollider.motorTorque = motorTorque;
        }else {
          // If the maxSpeed has been reached, then stop applying torque to the wheels.
          // IMPORTANT: The maxSpeed variable should be considered as an approximation; the speed of the car
          // could be a bit higher than expected.
    			frontLeftCollider.motorTorque = 0;
    			frontRightCollider.motorTorque = 0;
          rearLeftCollider.motorTorque = 0;
    			rearRightCollider.motorTorque = 0;
    		}
      }
    }

    // This method apply negative torque to the wheels in order to go backwards.
    public void GoReverse(){
      isDrifting = Mathf.Abs(localVelocityX) > 2.5f;
      // The following part sets the throttle power to -1 smoothly.
      throttleAxis = throttleAxis - (SimulationDeltaTime * 3f);
      if(throttleAxis < -1f){
        throttleAxis = -1f;
      }

      // Ensure we are in reverse gear (auto-shift when reversing)
      if(currentGear != -1 && transmissionMode == TransmissionMode.Automatic){
        currentGear = -1;
      }

      //If the car is still going forward, then apply brakes in order to avoid strange
      //behaviours. If the local velocity in the 'z' axis is greater than 1f, then it
      //is safe to apply negative torque to go reverse.
      if(localVelocityZ > 1f){
        Brakes();
      }else{
        if(engineRPM >= maxEngineRPM - 10f){
          frontLeftCollider.motorTorque = 0;
          frontRightCollider.motorTorque = 0;
          rearLeftCollider.motorTorque = 0;
          rearRightCollider.motorTorque = 0;
        }else if(Mathf.Abs(Mathf.RoundToInt(carSpeed)) < maxReverseSpeed){
          float gearMultiplier = GetGearTorqueMultiplier();
          float motorTorque = (accelerationMultiplier * 50f) * throttleAxis * Mathf.Abs(gearMultiplier);
          //Apply negative torque in all wheels to go in reverse if maxReverseSpeed has not been reached.
          frontLeftCollider.brakeTorque = 0;
          frontLeftCollider.motorTorque = motorTorque;
          frontRightCollider.brakeTorque = 0;
          frontRightCollider.motorTorque = motorTorque;
          rearLeftCollider.brakeTorque = 0;
          rearLeftCollider.motorTorque = motorTorque;
          rearRightCollider.brakeTorque = 0;
          rearRightCollider.motorTorque = motorTorque;
        }else {
          //If the maxReverseSpeed has been reached, then stop applying torque to the wheels.
          // IMPORTANT: The maxReverseSpeed variable should be considered as an approximation; the speed of the car
          // could be a bit higher than expected.
    			frontLeftCollider.motorTorque = 0;
    			frontRightCollider.motorTorque = 0;
          rearLeftCollider.motorTorque = 0;
    			rearRightCollider.motorTorque = 0;
    		}
      }
    }

    //The following function set the motor torque to 0 (in case the user is not pressing either W or S).
    public void ThrottleOff(){
      frontLeftCollider.motorTorque = 0;
      frontRightCollider.motorTorque = 0;
      rearLeftCollider.motorTorque = 0;
      rearRightCollider.motorTorque = 0;
    }

    // The following method decelerates the speed of the car according to the decelerationMultiplier variable, where
    // 1 is the slowest and 10 is the fastest deceleration. This method is called by the function InvokeRepeating,
    // usually every 0.1f when the user is not pressing W (throttle), S (reverse) or Space bar (handbrake).
    public void DecelerateCar(){
      isDrifting = Mathf.Abs(localVelocityX) > 2.5f;
      // The following part resets the throttle power to 0 smoothly.
      if(throttleAxis != 0f){
        if(throttleAxis > 0f){
          throttleAxis = throttleAxis - (SimulationDeltaTime * 10f);
        }else if(throttleAxis < 0f){
            throttleAxis = throttleAxis + (SimulationDeltaTime * 10f);
        }
        if(Mathf.Abs(throttleAxis) < 0.15f){
          throttleAxis = 0f;
        }
      }
      carRigidbody.linearVelocity = carRigidbody.linearVelocity * (1f / (1f + (0.025f * decelerationMultiplier)));
      // Since we want to decelerate the car, we are going to remove the torque from the wheels of the car.
      frontLeftCollider.motorTorque = 0;
      frontRightCollider.motorTorque = 0;
      rearLeftCollider.motorTorque = 0;
      rearRightCollider.motorTorque = 0;
      // If the magnitude of the car's velocity is less than 0.25f (very slow velocity), then stop the car completely and
      // also cancel the invoke of this method.
      if(carRigidbody.linearVelocity.magnitude < 0.25f){
        carRigidbody.linearVelocity = Vector3.zero;
      }
    }

    // This function applies brake torque to the wheels according to the brake force given by the user.
    public void Brakes(){
      frontLeftCollider.brakeTorque = brakeForce;
      frontRightCollider.brakeTorque = brakeForce;
      rearLeftCollider.brakeTorque = brakeForce;
      rearRightCollider.brakeTorque = brakeForce;

      frontLeftCollider.motorTorque = 0;
      frontRightCollider.motorTorque = 0;
      rearLeftCollider.motorTorque = 0;
      rearRightCollider.motorTorque = 0;
    }

    // This function is used to make the car lose traction. By using this, the car will start drifting. The amount of traction lost
    // will depend on the handbrakeDriftMultiplier variable. If this value is small, then the car will not drift too much, but if
    // it is high, then you could make the car to feel like going on ice.
    public void Handbrake(){
      // FIX: Actually apply brake torque to rear wheels so the handbrake stops the car!
      rearLeftCollider.brakeTorque = brakeForce;
      rearRightCollider.brakeTorque = brakeForce;
      // We are going to start losing traction smoothly, there is were our 'driftingAxis' variable takes
      // place. This variable will start from 0 and will reach a top value of 1, which means that the maximum
      // drifting value has been reached. It will increase smoothly by using the variable Time.deltaTime.
      driftingAxis = driftingAxis + SimulationDeltaTime;
      float secureStartingPoint = driftingAxis * FLWextremumSlip * handbrakeDriftMultiplier;

      if(secureStartingPoint < FLWextremumSlip){
        driftingAxis = FLWextremumSlip / (FLWextremumSlip * handbrakeDriftMultiplier);
      }
      if(driftingAxis > 1f){
        driftingAxis = 1f;
      }
      if(Mathf.Abs(localVelocityX) > 2.5f){
        isDrifting = true;
      }else{
        isDrifting = false;
      }

      if(driftingAxis < 1f){
        FLwheelFriction.extremumSlip = FLWextremumSlip * handbrakeDriftMultiplier * driftingAxis;
        frontLeftCollider.sidewaysFriction = FLwheelFriction;

        FRwheelFriction.extremumSlip = FRWextremumSlip * handbrakeDriftMultiplier * driftingAxis;
        frontRightCollider.sidewaysFriction = FRwheelFriction;

        RLwheelFriction.extremumSlip = RLWextremumSlip * handbrakeDriftMultiplier * driftingAxis;
        rearLeftCollider.sidewaysFriction = RLwheelFriction;

        RRwheelFriction.extremumSlip = RRWextremumSlip * handbrakeDriftMultiplier * driftingAxis;
        rearRightCollider.sidewaysFriction = RRwheelFriction;
      }

      isTractionLocked = true;
    }



    // This function is used to recover the traction of the car when the user has stopped using the car's handbrake.
    public void RecoverTraction(){
      // FIX: Release the handbrake brake torque
      rearLeftCollider.brakeTorque = 0;
      rearRightCollider.brakeTorque = 0;
      driftingAxis = driftingAxis - (SimulationDeltaTime / 1.5f);
      if(driftingAxis < 0f){
        driftingAxis = 0f;
      }

      if(FLwheelFriction.extremumSlip > FLWextremumSlip){
        FLwheelFriction.extremumSlip = FLWextremumSlip * handbrakeDriftMultiplier * driftingAxis;
        frontLeftCollider.sidewaysFriction = FLwheelFriction;

        FRwheelFriction.extremumSlip = FRWextremumSlip * handbrakeDriftMultiplier * driftingAxis;
        frontRightCollider.sidewaysFriction = FRwheelFriction;

        RLwheelFriction.extremumSlip = RLWextremumSlip * handbrakeDriftMultiplier * driftingAxis;
        rearLeftCollider.sidewaysFriction = RLwheelFriction;

        RRwheelFriction.extremumSlip = RRWextremumSlip * handbrakeDriftMultiplier * driftingAxis;
        rearRightCollider.sidewaysFriction = RRwheelFriction;
      } else {
        FLwheelFriction.extremumSlip = FLWextremumSlip;
        frontLeftCollider.sidewaysFriction = FLwheelFriction;

        FRwheelFriction.extremumSlip = FRWextremumSlip;
        frontRightCollider.sidewaysFriction = FRwheelFriction;

        RLwheelFriction.extremumSlip = RLWextremumSlip;
        rearLeftCollider.sidewaysFriction = RLwheelFriction;

        RRwheelFriction.extremumSlip = RRWextremumSlip;
        rearRightCollider.sidewaysFriction = RRwheelFriction;

        driftingAxis = 0f;
        isTractionLocked = false;
      }
    }

    //
    //GEAR SYSTEM METHODS
    //

    // Handles gear shift input based on the current transmission mode.
    void HandleGearInput(bool shiftUp, bool shiftDown, bool clutchHeld){
      // Update clutch state
      clutchInput = clutchHeld ? 1f : 0f;
      clutchEngaged = !clutchHeld; // clutch engaged = power flows = clutch pedal NOT pressed

      switch(transmissionMode){
        case TransmissionMode.Automatic:
          // Automatic mode: no manual shifting needed, handled in UpdateGearSystem()
          break;

        case TransmissionMode.SequentialGear:
          // Sequential: shift up/down without clutch
          if(shiftUp){
            ShiftUp();
          }
          if(shiftDown){
            ShiftDown();
          }
          break;

        case TransmissionMode.ManualClutch:
          // Manual: must hold clutch to shift
          if(clutchHeld){
            if(shiftUp){
              ShiftUp();
            }
            if(shiftDown){
              ShiftDown();
            }
          }
          break;
      }
    }

    // Shifts up one gear, clamped to numberOfGears.
    void ShiftUp(){
      if(currentGear < numberOfGears){
        currentGear++;
        lastShiftTime = Time.time;
      }
    }

    // Shifts down one gear. Minimum is -1 (reverse).
    void ShiftDown(){
      if(currentGear > -1){
        currentGear--;
        lastShiftTime = Time.time;
      }
    }

    // Computes engine RPM from wheel speed and current gear ratio,
    // and handles automatic shifting when in Automatic mode.
    void UpdateGearSystem(){
      // Compute engine RPM from average rear wheel RPM
      float averageWheelRPM = (Mathf.Abs(rearLeftCollider.rpm) + Mathf.Abs(rearRightCollider.rpm)) / 2f;

      float currentRatio;
      if(currentGear == 0){
        currentRatio = 0f;
      } else if(currentGear == -1){
        currentRatio = reverseGearRatio;
      } else {
        int gearIndex = Mathf.Clamp(currentGear - 1, 0, gearRatios.Length - 1);
        currentRatio = gearRatios[gearIndex];
      }

      if(currentRatio > 0f){
        engineRPM = averageWheelRPM * currentRatio * finalDriveRatio;
      } else {
        engineRPM = idleRPM;
      }

      // Clamp RPM between idle and max
      engineRPM = Mathf.Clamp(engineRPM, idleRPM, maxEngineRPM);



      // Automatic shifting logic
      if(transmissionMode == TransmissionMode.Automatic && currentGear >= 1){
        if (Time.time - lastShiftTime > shiftCooldown) {
          if(engineRPM >= shiftUpRPM && currentGear < numberOfGears){
            currentGear++;
            lastShiftTime = Time.time;
          } else if(engineRPM <= shiftDownRPM && currentGear > 1){
            currentGear--;
            lastShiftTime = Time.time;
          }
        }
      }
    }

}
