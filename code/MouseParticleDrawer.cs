using System.Threading.Tasks;
using Sandbox;

[Title("Mouse Particle Drawer")]
[Category("Effects")]
[Icon("mouse")]
public sealed class MouseParticleDrawer : Component
{
    [Property] public GameObject ParticlePrefab { get; set; }
    [Property] public float ParticleLifetime { get; set; } = 1f;
    [Property, Range(0.01f, 1f)] public float SpawnRate { get; set; } = 0.05f; 

    private bool isLocked = false;
    private float spawnTimer;

    protected override void OnUpdate()
    {
        HandleCursorToggle();
        HandleParticleSpawning();
    }

    private void HandleCursorToggle()
    {
        if (Input.Pressed("Use")) 
        {
            Log.Info("E key was pressed.");
            isLocked = !isLocked;
        }

        if (isLocked)
        {
            Mouse.Visibility = MouseVisibility.Visible;

        }
        else
        {
            Mouse.Visibility = MouseVisibility.Hidden;
       }
    }

    private void HandleParticleSpawning()
    {
        if (Input.Down("attack1")) 
        {
            spawnTimer -= Time.Delta;
            if (spawnTimer <= 0f)
            {
                SpawnParticleAtMouse();
                spawnTimer = SpawnRate;
            }
        }
        else
        {
            spawnTimer = 0f;
        }
    }

    private void SpawnParticleAtMouse()
    {
        var camera = Scene.GetAllComponents<CameraComponent>()
            .FirstOrDefault(c => c.IsMainCamera);
        
        if (camera == null) return;

        Vector2 mousePos = Mouse.Position;
        Ray ray = camera.ScreenPixelToRay(mousePos);

        var trace = Scene.Trace.Ray(ray.Position, ray.Position + ray.Forward * 1000f)
            .Run();

        if (trace.Hit)
        {

            if (ParticlePrefab != null)
            {
                var particle = ParticlePrefab.Clone();
                particle.WorldPosition = trace.HitPosition;
                particle.WorldRotation = Rotation.Identity;

                _ = DestroyAfterDelay(particle, ParticleLifetime);
            }
        }
    }

	private async Task DestroyAfterDelay( GameObject obj, float delay )
	{
		await Task.Delay( (int)(delay * 1000) ); 

		if ( obj.IsValid() ) 
		{
			obj.Destroy();
		}
    }
}
