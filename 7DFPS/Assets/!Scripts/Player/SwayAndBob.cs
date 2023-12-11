using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwayAndBob : MonoBehaviour
{
    [Header("External Refs")]
    //Assingables
    [SerializeField] advPlayerMove advMove;
    [SerializeField] Rigidbody rb;

    [Header("Sway")]
    //Sway
    public float step = 0.01f; //its multiplied to the value of the mouse for 1 frame;
    public float maxStepDistance = 0.06f; //max distance from the local origin;
    public float rotationStep = 4f;
    public float maxRotationStep = 5f;
    public float smooth = 10f; //offset
    public float smoothRot = 12f;
    Vector3 swayPos;
    Vector3 swayEulerRot;

    [Header("Bobbing")]
    //Bobing
    public float speedCurve;
    public Vector3 travelLimit = Vector3.one * 0.02f;
    public Vector3 BobLimit = Vector3.one * 0.01f;
    public Vector3 multiplier;
    float curveSin { get => Mathf.Sin(speedCurve); }
    float curveCos { get => Mathf.Cos(speedCurve); }
    Vector3 bobPos;
    Vector3 bobEulerRotation;

    void Update()
    {
        advMove.MyInput();
        Sway();
        SwayRotation();
        BobOffset();
        BobRotation();
        CompositePositionRotation();
    }

    private void Sway()
    {
        //x,y,z pos change as a result of moving mouse;
        
        Vector3 invertLook = advMove.lookInput * -step;
        invertLook.x = Mathf.Clamp(invertLook.x, -maxStepDistance, maxStepDistance);
        invertLook.y = Mathf.Clamp(invertLook.y, -maxStepDistance, maxStepDistance);

        swayPos = invertLook;
    }

    private void SwayRotation()
    {
        Vector3 invertLook = advMove.lookInput * -rotationStep;
        invertLook.x = Mathf.Clamp(invertLook.x, -maxRotationStep, maxRotationStep);
        invertLook.y = Mathf.Clamp(invertLook.y, -maxRotationStep, maxRotationStep);

        swayEulerRot = new Vector3(invertLook.y, invertLook.x, invertLook.x);
    }


    private void BobOffset()
    {
        speedCurve += Time.deltaTime * (advMove.grounded ? rb.velocity.magnitude : 1f) + 0.01f;

        bobPos.x = (curveCos * BobLimit.x * (advMove.grounded ? 1 : 0)) - (advMove.walkInput.x * travelLimit.x);
        bobPos.y = (curveCos * BobLimit.y * (advMove.grounded ? 1 : 0)) - (advMove.walkInput.y * travelLimit.y);
        bobPos.z = -(advMove.walkInput.y * travelLimit.z);
    }
    
    private void BobRotation()
    {
        bobEulerRotation.x = (advMove.walkInput != Vector2.zero ? multiplier.x * (Mathf.Sin(2 * speedCurve)) : multiplier.x * (Mathf.Sin(2 * speedCurve) / 2));
        bobEulerRotation.y = (advMove.walkInput != Vector2.zero ? multiplier.y * curveCos : 0);
        bobEulerRotation.z = (advMove.walkInput != Vector2.zero ? multiplier.z * curveCos * advMove.walkInput.x : 0);
    }
    private void CompositePositionRotation()
    {
        //pos
        transform.localPosition = Vector3.Lerp(transform.localPosition, swayPos + bobPos, Time.deltaTime * smooth);
        //rot
        transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(swayEulerRot) * Quaternion.Euler(bobEulerRotation), Time.deltaTime * smoothRot);
    }
}
