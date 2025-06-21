using System;
using Sandbox;

public sealed class CamComponent : Component
{

	[Property, Group( "References" )]
	public GameObject Orientation { get; set; }
	[Property, Group( "References" )]
	public GameObject Player { get; set; }
	[Property, Group( "References" )]
	public GameObject PlayerObj { get; set; }
	[Property, Range(0f, 20f)]
    public float RotationSpeed { get; set; } = 10f;
	protected override void OnFixedUpdate()
	{
		if ( !Orientation.IsValid() || !Player.IsValid() || !PlayerObj.IsValid() )
			return;

		Vector3 cameraPos = WorldPosition ;
		Vector3 playerPos = Player.WorldPosition ;
		Vector3 viewDir = playerPos - new Vector3( cameraPos.x, playerPos.y, cameraPos.z );

		Orientation.WorldRotation = Rotation.LookAt(viewDir.Normal);

		float horizontalInput = Input.Down( "Left" ) ? -1f : Input.Down( "Right" ) ? 1f : 0f;
		float verticalInput = Input.Down( "Forward" ) ? 1f : Input.Down( "Backward" ) ? -1f : 0f;

		Vector3 forward = Orientation.WorldRotation.Forward;
		Vector3 right = Orientation.WorldRotation.Right;
		Vector3 inputDir = forward * verticalInput + right * horizontalInput;

		if ( !inputDir.IsNearZeroLength )
		{
			//Angles currentAngles = PlayerObj.Transform.Rotation.Angles();

			//float targetYaw = MathF.Atan2(inputDir.x, inputDir.y) * (180f / MathF.PI);
			//Angles targetAngles = new Angles(0, targetYaw, 0);
			//Rotation targetRotation = targetAngles.ToRotation();
			Rotation targetRotation = Rotation.LookAt( inputDir.Normal );
			Angles targetAngles = targetRotation.Angles();
			Angles constrainedAngles = new Angles(0, targetAngles.yaw, 0);
			PlayerObj.WorldRotation = Rotation.Slerp(
				PlayerObj.WorldRotation,
				constrainedAngles.ToRotation(),
				Time.Delta * RotationSpeed
			);
			//PlayerObj.WorldRotation = new Rotation(0, PlayerObj.WorldRotation.y, 0, 0);
        }
	}
}