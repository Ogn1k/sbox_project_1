using Sandbox;

public sealed class TrailComponent : Component
{

	[Property]
	[Category( "Start Params" )]
	public float height { get; set; } = 1f;

	[Property]
	[Category( "Start Params" )]
	public float trangle { get; set; } = 0f;

	[Property]
	[Category( "Start Params" )]
	public float offangle { get; set; } = 90f;

	[Property]
	[Category( "Start Params" )]
	public float livetime { get; set; } = 0.3f;

	[Property]
	[Category( "Start Params" )]
	public float peak { get; set; } = 0.6f;

	[Property]
	[Category( "Start Params" )]
	public float rotSpeed { get; set; } = 1f;

	public TimeUntil DieTime;
	private SkinnedModelRenderer TrailRender;
	public float RotatePerFrame { get; set; } = 0f;

	protected override void OnAwake()
	{
		TrailRender = Components.Get<SkinnedModelRenderer>();
	}

	protected override void OnStart()
	{
		if ( !GameObject.IsValid() ) return;
		var vect = new Vector3(0,0,height);
		GameObject.LocalPosition += vect;

		var newAng = new Angles(0,offangle,trangle);
		GameObject.LocalRotation = newAng;

		TrailRender.Set( "speed", rotSpeed );

		DieTime = livetime;
	}

	protected override void OnUpdate()
	{
		if ( !GameObject.IsValid() ) return;

		float trailopen = DieTime.Passed/livetime * (1/peak);

		TrailRender.Set( "open_cycle", trailopen );

		if (DieTime)
			GameObject.Destroy();
	}
}
