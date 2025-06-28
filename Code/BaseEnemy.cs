using System;
using System.Threading.Tasks;
using Sandbox;
using Sandbox;
// half ai generated content
[Title("Base Enemy AI")]
[Category("AI")]
[Icon("smart_toy")]
public sealed class BaseEnemy : Component
{
[Property, Group("Detection")] 
    public float DetectionRange { get; set; } = 20f;
    
    [Property, Group("Detection")] 
    public float ChaseRange { get; set; } = 30f;
    
    [Property, Group("Movement")] 
    public float MoveSpeed { get; set; } = 200f;
    
    [Property, Group("Movement")] 
    public float RotationSpeed { get; set; } = 180f;
    
    [Property, Group("Patrol")] 
    public float PatrolRadius { get; set; } = 10f;
    
    [Property, Group("Patrol")] 
    public float WaitTimeAtPatrolPoint { get; set; } = 2f;

    // Required components
    [RequireComponent] public NavMeshAgent Agent { get; set; }
    
    // State management
    private AIState currentState = AIState.Patrolling;
    private GameObject targetPlayer;
    private Vector3 lastKnownPlayerPosition;
    private Vector3 patrolCenter;
    private Vector3 currentPatrolTarget;
    private bool isWaitingAtPatrolPoint;
    private TimeSince timeSinceReachedPatrol;
    
    private enum AIState
    {
        Patrolling,
        Chasing,
        Searching
    }

    protected override void OnAwake()
    {
        // Store initial position as patrol center
        patrolCenter = Transform.Position;
        
        // Set up NavMeshAgent
        if (Agent != null)
        {
            Agent.MaxSpeed = MoveSpeed;
        }
        
        // Start patrolling
        SetNewPatrolTarget();
    }

    protected override void OnUpdate()
    {
        if (Agent == null) return;

        // Find player if we don't have one
        if (targetPlayer == null || !targetPlayer.IsValid())
        {
            FindPlayer();
        }

        // Update AI state machine
        UpdateStateMachine();
        
        // Handle rotation towards movement direction
        HandleRotation();
    }

    private void FindPlayer()
    {
        // Find player using PlayerController component
        var playerControllers = Scene.GetAllComponents<PlayerController>();
        if (playerControllers.Any())
        {
            var closestPlayer = playerControllers
                .OrderBy(p => Vector3.DistanceBetween(Transform.Position, p.Transform.Position))
                .FirstOrDefault();
            
            if (closestPlayer != null)
            {
                targetPlayer = closestPlayer.GameObject;
            }
        }
        
        // Alternative: Find by tag if players are tagged with "player"
        if (targetPlayer == null)
        {
            var playerObjects = Scene.GetAllObjects(true)
                .Where(go => go.Tags.Has("player"))
                .FirstOrDefault();
            
            if (playerObjects != null)
            {
                targetPlayer = playerObjects;
            }
        }
    }

    private void UpdateStateMachine()
    {
        if (targetPlayer == null || !targetPlayer.IsValid()) 
        {
            if (currentState != AIState.Patrolling)
            {
                currentState = AIState.Patrolling;
                SetNewPatrolTarget();
            }
            HandlePatrolling();
            return;
        }

        float distanceToPlayer = Vector3.DistanceBetween(Transform.Position, targetPlayer.Transform.Position);
        bool canSeePlayer = CanSeePlayer();

        switch (currentState)
        {
            case AIState.Patrolling:
                if (canSeePlayer && distanceToPlayer <= DetectionRange)
                {
                    currentState = AIState.Chasing;
                    lastKnownPlayerPosition = targetPlayer.Transform.Position;
                }
                else
                {
                    HandlePatrolling();
                }
                break;

            case AIState.Chasing:
                if (canSeePlayer && distanceToPlayer <= ChaseRange)
                {
                    // Continue chasing
                    lastKnownPlayerPosition = targetPlayer.Transform.Position;
                    Agent.MoveTo(targetPlayer.Transform.Position);
                }
                else if (canSeePlayer && distanceToPlayer > ChaseRange)
                {
                    // Player escaped, start searching
                    currentState = AIState.Searching;
                    Agent.MoveTo(lastKnownPlayerPosition);
                }
                else if (!canSeePlayer)
                {
                    // Lost sight, search last known position
                    currentState = AIState.Searching;
                    Agent.MoveTo(lastKnownPlayerPosition);
                }
                break;

            case AIState.Searching:
                // Check if we can see player again
                if (canSeePlayer && distanceToPlayer <= DetectionRange)
                {
                    currentState = AIState.Chasing;
                    lastKnownPlayerPosition = targetPlayer.Transform.Position;
                }
                else
                {
                    // If reached search position and still no player, return to patrol
                    if (Vector3.DistanceBetween(Transform.Position, lastKnownPlayerPosition) < 2f)
                    {
                        currentState = AIState.Patrolling;
                        SetNewPatrolTarget();
                    }
                }
                break;
        }
    }

    private bool CanSeePlayer()
    {
        if (targetPlayer == null || !targetPlayer.IsValid()) return false;

        Vector3 aiPosition = Transform.Position + Vector3.Up * 1.5f; // Eye level
        Vector3 playerPosition = targetPlayer.Transform.Position + Vector3.Up * 1.5f;

        // Perform line-of-sight check using Scene.Trace
        var trace = Scene.Trace.Ray(aiPosition, playerPosition)
            .WithoutTags("ai", "trigger") // Ignore AI entities and triggers
            .Run();

        // If we hit the player or nothing (clear line of sight)
        return !trace.Hit || trace.GameObject == targetPlayer;
    }

    private void HandlePatrolling()
    {
        if (isWaitingAtPatrolPoint)
        {
            // Wait at patrol point
            if (timeSinceReachedPatrol >= WaitTimeAtPatrolPoint)
            {
                isWaitingAtPatrolPoint = false;
                SetNewPatrolTarget();
            }
            return;
        }

        // Move to current patrol target
        if (currentPatrolTarget != Vector3.Zero)
        {
            Agent.MoveTo(currentPatrolTarget);
            
            // Check if reached patrol point
            if (Vector3.DistanceBetween(Transform.Position, currentPatrolTarget) < 2f)
            {
                isWaitingAtPatrolPoint = true;
                timeSinceReachedPatrol = 0;
                Agent.Stop();
            }
        }
    }

    private void SetNewPatrolTarget()
    {
        // Generate random point within patrol radius
        Vector2 randomDirection = Vector2.Random * PatrolRadius;
        Vector3 potentialTarget = patrolCenter + new Vector3(randomDirection.x, randomDirection.y, 0);
        
        // Ensure the patrol target is on valid ground using trace
        var trace = Scene.Trace.Ray(potentialTarget + Vector3.Up * 5f, potentialTarget + Vector3.Down * 10f)
            .WithTag("ground") // Assuming ground is tagged appropriately
            .Run();

        if (trace.Hit)
        {
            currentPatrolTarget = trace.HitPosition;
        }
        else
        {
            // Fallback to patrol center if no valid ground found
            currentPatrolTarget = patrolCenter;
        }
    }

    private void HandleRotation()
    {
        // Only rotate if we have velocity (are moving)
        if (Agent.Velocity.Length > 0.1f)
        {
            Vector3 direction = Agent.Velocity.Normal;
            Rotation targetRotation = Rotation.LookAt(direction, Vector3.Up);
            
            Transform.Rotation = Rotation.Lerp(
                Transform.Rotation,
                targetRotation,
                RotationSpeed * Time.Delta
            );
        }
    }

    // Debug visualization
    protected override void DrawGizmos()
    {
        if (!Gizmo.IsSelected) return;

        // Draw detection range (yellow)
        Gizmo.Draw.Color = Color.Yellow;
        Gizmo.Draw.LineSphere(Transform.Position, DetectionRange);

        // Draw chase range (orange)
        Gizmo.Draw.Color = Color.Orange;
        Gizmo.Draw.LineSphere(Transform.Position, ChaseRange);

        // Draw patrol area (green)
        Gizmo.Draw.Color = Color.Green;
        Gizmo.Draw.LineSphere(patrolCenter, PatrolRadius);

        // Draw current target
        if (currentPatrolTarget != Vector3.Zero)
        {
            Gizmo.Draw.Color = Color.Blue;
            Gizmo.Draw.LineSphere(currentPatrolTarget, 1f);
            Gizmo.Draw.Line(Transform.Position, currentPatrolTarget);
        }

        // Draw line to player if chasing
        if (targetPlayer != null && currentState == AIState.Chasing)
        {
            Gizmo.Draw.Color = Color.Red;
            Gizmo.Draw.Line(Transform.Position, targetPlayer.Transform.Position);
        }
    }
}
