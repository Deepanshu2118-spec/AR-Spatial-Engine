using UnityEngine;

public class ARSystemManager : MonoBehaviour
{
    [Header("Telemetry Link")]
    public UDPReceive udpReceiver;

    [Header("Physical Proxies")]
    public Transform proxyHand1;
    public Transform proxyHand2;

    [Header("Spatial Parameters")]
    public float positionScale = 15f;
    public float smoothing = 10f;
    public float rotationSpeed = 250f;

    [Header("Assembly Parameters")]
    public float grabRadius = 3f;
    public float snapDistance = 1.5f;

    // Dynamic Grabbing Memory
    private GameObject grabbedObject1 = null;
    private Vector3 offset1;

    private GameObject grabbedObject2 = null;
    private Vector3 offset2;

    // Dual Manipulation Memory (Tumble/Scale)
    private bool isDualManipulating = false;
    private float initialPinchDistance;
    private Vector3 initialObjectScale;
    private Vector3 lastCenterPosition;
    private Vector3 lastHandVector;

    void Update()
    {
        // Failsafe: Terminate operation if the data payload is null
        if (udpReceiver.currentData == null || udpReceiver.currentData.hands == null) return;

        int handCount = udpReceiver.currentData.hands.Length;

        if (handCount > 0) MoveProxy(proxyHand1, udpReceiver.currentData.hands[0]);
        if (handCount > 1) MoveProxy(proxyHand2, udpReceiver.currentData.hands[1]);

        ProcessStateEngine(handCount);
    }

    void MoveProxy(Transform proxy, UDPReceive.HandData data)
    {
        // X-axis inverted to counteract standard webcam mirroring
        float targetX = -(data.wrist_x - 0.5f) * positionScale; 
        float targetY = -(data.wrist_y - 0.5f) * positionScale;
        Vector3 targetPosition = new Vector3(targetX, targetY, 5f);
        proxy.position = Vector3.Lerp(proxy.position, targetPosition, Time.deltaTime * smoothing);
    }

    void ProcessStateEngine(int handCount)
    {
        // 1. EVALUATE GRAB STATES (What is each hand holding?)
        UpdateGrabState(ref grabbedObject1, ref offset1, proxyHand1, handCount > 0 ? udpReceiver.currentData.hands[0].gesture : "open");
        UpdateGrabState(ref grabbedObject2, ref offset2, proxyHand2, handCount > 1 ? udpReceiver.currentData.hands[1].gesture : "open");

        // 2. ROUTE TO APPROPRIATE LOGIC MATRIX
        if (grabbedObject1 != null && grabbedObject2 != null && grabbedObject1 == grabbedObject2)
        {
            // STATE B: Dual-Hand Manipulation (Both hands grabbed the same assembled unit)
            ExecuteTumbleAndScale(grabbedObject1);
        }
        else
        {
            // Reset manipulation memory if State B is broken
            isDualManipulating = false;

            // STATE A: Independent Translation & Assembly
            if (grabbedObject1 != null) grabbedObject1.transform.position = proxyHand1.position + offset1;
            if (grabbedObject2 != null) grabbedObject2.transform.position = proxyHand2.position + offset2;

            if (grabbedObject1 != null && grabbedObject2 != null)
            {
                CheckMagneticSnap();
            }
        }
    }

    void UpdateGrabState(ref GameObject grabbedObj, ref Vector3 offset, Transform proxy, string gesture)
    {
        if (gesture == "pinch")
        {
            if (grabbedObj == null)
            {
                grabbedObj = GetNearestPart(proxy.position);
                if (grabbedObj != null) offset = grabbedObj.transform.position - proxy.position;
            }
        }
        else
        {
            grabbedObj = null; // Drop object if hand opens
        }
    }

    GameObject GetNearestPart(Vector3 handPosition)
    {
        GameObject[] parts = GameObject.FindGameObjectsWithTag("MechanicalPart");
        GameObject nearest = null;
        float minDistance = grabRadius;

        foreach (GameObject part in parts)
        {
            float distance = Vector3.Distance(handPosition, part.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                // CRITICAL LOGIC: Always return the highest parent. 
                // This ensures that when parts are snapped together, the system treats them as one solid object.
                nearest = part.transform.root.gameObject; 
            }
        }
        return nearest;
    }

    void ExecuteTumbleAndScale(GameObject targetCAD)
    {
        float currentDistance = Vector3.Distance(proxyHand1.position, proxyHand2.position);
        Vector3 currentVector = proxyHand2.position - proxyHand1.position;
        Vector3 currentCenter = (proxyHand1.position + proxyHand2.position) / 2f;

        if (!isDualManipulating)
        {
            initialPinchDistance = currentDistance;
            initialObjectScale = targetCAD.transform.localScale;
            lastCenterPosition = currentCenter;
            lastHandVector = currentVector;
            isDualManipulating = true;
        }
        else
        {
            // ZOOM (Scale)
            float scaleMultiplier = currentDistance / initialPinchDistance;
            targetCAD.transform.localScale = initialObjectScale * scaleMultiplier;

            // TUMBLE (X/Y Rotation)
            Vector3 centerDelta = currentCenter - lastCenterPosition;
            targetCAD.transform.Rotate(Vector3.up, -centerDelta.x * rotationSpeed * Time.deltaTime, Space.World);
            targetCAD.transform.Rotate(Vector3.right, centerDelta.y * rotationSpeed * Time.deltaTime, Space.World);

            // ROLL (Z Rotation)
            float angleDelta = Vector3.SignedAngle(lastHandVector, currentVector, Vector3.forward);
            targetCAD.transform.Rotate(Vector3.forward, angleDelta, Space.World);

            lastCenterPosition = currentCenter;
            lastHandVector = currentVector;
        }
    }

    void CheckMagneticSnap()
    {
        // Prevent an object from trying to snap to itself
        if (grabbedObject1 == grabbedObject2) return;

        if (Vector3.Distance(grabbedObject1.transform.position, grabbedObject2.transform.position) < snapDistance)
        {
            // Lock Object 2 onto Object 1
            grabbedObject2.transform.position = grabbedObject1.transform.position + new Vector3(0, 1.5f, 0);
            grabbedObject2.transform.SetParent(grabbedObject1.transform);

            // Force Hand 2 to drop the object so physics don't fight the new parent-child hierarchy
            grabbedObject2 = null;
            
            Debug.Log("ASSEMBLY COMPLETE: Multi-Part Matrix Locked.");
        }
    }
}