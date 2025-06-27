using Sandbox;
using Sandbox.Citizen;

public sealed class PlayerComponent : Component
{
    
	[Property]
	[Category( "Components" )]
	public PlayerController Controller { get; set; }

	[Property]
	[Category( "Components" )]
	public SkinnedModelRenderer ModelRenderer { get; set; }

	[Property]
	[Category( "Components" )]
	public CitizenAnimationHelper animationHelper { get; set; }
	[Property]
	[Category( "Components" )]
	public EntityComponent entityComponent { get; set; }

	[Property]
	[Category( "Components" )]
	public SkinnedModelRenderer weaponModelrend { get; set; }

	[Property]
	[Category( "Components" )]
	public CitizenAnimationHelper wepAnimHelper { get; set; }

	[Property]
	[Category( "Stats" )]
	[Range( 50f, 200f, 10f )]
	public float PunchRange { get; set; } = 100f;

	[Property]
	[Category( "Stats" )]
	[Range( 5f, 50f, 5f )]
	public float PunchDamage { get; set; } = 10f;

	[Property]
	[Category( "Stats" )]
	[Range( 0f, 2f, 0.1f )]
	public float PunchCooldown { get; set; } = 0.5f;

	public TimeUntil NextPunch;
	public bool WepDeployed { get; set; } = false;
	public int CurCombo { get; set; } = 0;
	public bool CanCombo { get; set; } = false;
	private ModelPhysics _ragdoll;
	private Vector3 _spawnPosition;
	private TimeUntil _resetPose;

	protected override void OnStart()
	{
		ModelRenderer.OnGenericEvent += HandleGenericEvent;
		ModelRenderer.OnAnimTagEvent += HandleAnimTagEvent;
	}

	private void HandleGenericEvent(SceneModel.GenericEvent eventData)
	{
		if (eventData.Type == "change_deploy")
		{
			WepDeployed = !WepDeployed;

			SharedParams("sword_deployed", WepDeployed);
		}
		else if (eventData.Type == "change_combo")
		{
			if (CurCombo == 3)
				{
				CurCombo = 0;
				}
			else
				{
				CurCombo += 1;
				}
		}
	}

	private void HandleAnimTagEvent( SceneModel.AnimTagEvent tagEvent )
	{
		if ( tagEvent.Name == "continue_combo" )
		{
			if ( tagEvent.Status == SceneModel.AnimTagStatus.Start )
			{
				CanCombo = true;
			}
			else if ( tagEvent.Status == SceneModel.AnimTagStatus.End )
			{
				CanCombo = false;
			}
		}
	}

	public void SharedParams(string parameter, object value)
	{
		switch (value)  
		{  
			case int number:  
				ModelRenderer.Set( parameter, number );
				weaponModelrend.Set( parameter, number );
				break;  

			//case string text: 
				//ModelRenderer.Set( parameter, text );
				//weaponModelrend.Set( parameter, text );  
				//break;  

			case bool flag:
				ModelRenderer.Set( parameter, flag );
				weaponModelrend.Set( parameter, flag );   
				break;

			case float fnumber:
				ModelRenderer.Set( parameter, fnumber );
				weaponModelrend.Set( parameter, fnumber );   
				break;

			case null:  
				break;  

			default:  
				break;  
		} 
	}

    public void Punch()
	{
		SharedParams("b_attack", true);
		_resetPose = 3f;

		var punchDirection = Controller.EyeAngles.Forward;
		var punchStart = Controller.EyePosition;
		var punchEnd = punchStart + punchDirection * PunchRange;

		var punchTrace = Scene.Trace.Ray( punchStart, punchEnd )
			.Radius( 20f )
			.WithoutTags( "player" )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		if ( !punchTrace.Hit ) return;
		if ( !punchTrace.GameObject.Components.TryGet<EntityComponent>( out var unit ) ) return;
		if ( unit.Team == entityComponent.Team ) return;

		unit.Damage( PunchDamage );
	}

    public void DeploySword()
	{
		SharedParams("b_deploy", true);
	}

    [Button]
	public void Ragdoll()
	{
		if ( !ModelRenderer.IsValid() ) return;
		if ( _ragdoll.IsValid() ) return;

		_ragdoll = AddComponent<ModelPhysics>();
		_ragdoll.Renderer = ModelRenderer;
		_ragdoll.Model = ModelRenderer.Model;

		Controller.UseInputControls = false;
	}

    [Button]
	public void Unragdoll()
	{
		if ( !ModelRenderer.IsValid() ) return;
		if ( !_ragdoll.IsValid() ) return;

		_ragdoll.Destroy();
		Controller.UseInputControls = true;
	}

    public void Respawn()
	{
		Unragdoll();
		WorldPosition = _spawnPosition;
		ModelRenderer.Tint = ModelRenderer.Tint.WithAlpha( 1f );
	}

	private void UpdateAnimation()
	{
		if ( animationHelper == null )
			return;

		animationHelper.WithVelocity( Controller.Velocity );
		animationHelper.WithWishVelocity( WorldPosition.Normal );
		//animationHelper.AimAngle = Head.Transform.Rotation;
    	animationHelper.IsGrounded = Controller.IsOnGround;

		if ( wepAnimHelper == null )
			return;
		
		wepAnimHelper.WithVelocity( Controller.Velocity );
		wepAnimHelper.WithWishVelocity( WorldPosition.Normal );
		//animationHelper.AimAngle = Head.Transform.Rotation;
    	wepAnimHelper.IsGrounded = Controller.IsOnGround;
    
	}

	protected override void OnFixedUpdate()
	{
		if ( Input.Down( "attack1" ) )
			{
			if (!NextPunch && CanCombo)
				{
					SharedParams("combo", CurCombo);
					Punch();
					NextPunch = PunchCooldown;
				}
			else if (NextPunch)
				{
					CurCombo = 0;
					SharedParams("combo", CurCombo);
					Punch();
					NextPunch = PunchCooldown;
				}
			}

		if ( Input.Down( "reload" ) && NextPunch )
		{
			DeploySword();
			NextPunch = PunchCooldown;
		}
		//if ( _resetPose )
			//ModelRenderer.Set( "holdtype", 0 );
		UpdateAnimation();
	}
}
