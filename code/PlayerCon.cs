using System;
using Sandbox;

[Title("Third Person Controller")]
[Category("Movement")]
[Icon("directions_walk")]
public sealed class ThirdPersonController : Component
{
    [Property] public float MaximumSpeed { get; set; } = 5.0f;
    [Property] public float RotationSpeed { get; set; } = 180.0f;
    [Property] public float JumpSpeed { get; set; } = 8.0f;
    [Property] public float JumpButtonGracePeriod { get; set; } = 0.1f;
    [Property] public GameObject CameraGameObject { get; set; }
    [Property] public GameObject PlayerObj { get; set; }
    protected override void OnUpdate()
    {
        HandleRotation();
    }
    private void HandleRotation()
    {
        Vector3 movementDirection = new Vector3(Input.AnalogMove.x, Input.AnalogMove.y, 0);
        
        if (CameraGameObject != null)
        {
            var cameraRotation = CameraGameObject.WorldRotation;
            movementDirection = cameraRotation * movementDirection;
        }

        if (movementDirection.Length > 0.1f)
        {
            movementDirection = movementDirection.Normal;
            Rotation targetRotation = Rotation.LookAt(movementDirection, Vector3.Up);
            Angles constrainedAngles = new Angles( 0, targetRotation.Yaw(), 0 );
            PlayerObj.WorldRotation = Rotation.Lerp(
                PlayerObj.WorldRotation,
                constrainedAngles, 
                RotationSpeed * Time.Delta
            );
        }
    }
}
