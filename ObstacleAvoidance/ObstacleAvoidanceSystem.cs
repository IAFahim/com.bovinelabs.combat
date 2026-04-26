using BovineLabs.Combat.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace BovineLabs.Combat.ObstacleAvoidance
{
    /// <summary>
    /// Obstacle avoidance behavior system.
    /// Generates a fan of rays ahead of the agent and computes avoidance forces
    /// from detected obstacle hits. Uses wall-sliding for smooth navigation along surfaces.
    /// 
    /// Note: This system computes the avoidance force math but does NOT perform actual
    /// physics raycasts (that requires PhysicsWorld access). Instead, it expects
    /// hit data to be populated by a separate raycast system or job. For standalone
    /// testing, the system outputs forces based on the math API.
    /// 
    /// For production use, pair with a raycast job that fills hitDistances/hitNormals
    /// before this system runs.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSteeringGroup))]
    public partial struct ObstacleAvoidanceSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ObstacleAvoidanceParams>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (avoidParams, stats, transform, steering) in
                SystemAPI.Query<
                    RefRO<ObstacleAvoidanceParams>,
                    RefRO<MovementStats>,
                    RefRO<LocalTransform>,
                    RefRW<SteeringForce>>())
            {
                var currentPos = transform.ValueRO.Position.xz;
                var facingAngle = stats.ValueRO.FacingAngle;
                var rayCount = avoidParams.ValueRO.RaycastCount;
                var rayLength = avoidParams.ValueRO.RaycastLength;
                var slideStrength = avoidParams.ValueRO.WallSlideStrength;

                if (rayCount <= 0)
                    continue;

                // Generate ray fan
                var rayAngles = new NativeArray<float>(rayCount, Allocator.Temp);
                var rayDirections = ObstacleAvoidanceMath.RaycastFan(
                    currentPos, facingAngle, rayCount, rayLength, rayAngles);

                // Hit data arrays - in production these would be filled by a raycast job.
                // Here we initialize them as "no hits" (far distance, zero normals).
                var hitDistances = new NativeArray<float>(rayCount, Allocator.Temp);
                var hitNormals = new NativeArray<float2>(rayCount, Allocator.Temp);

                for (int i = 0; i < rayCount; i++)
                {
                    hitDistances[i] = float.MaxValue; // No hit
                    hitNormals[i] = float2.zero;
                }

                // Compute avoidance force from hits
                var desiredVel = steering.ValueRO.Linear;
                var avoidanceForce = ObstacleAvoidanceMath.ComputeObstacleForce(
                    currentPos,
                    desiredVel,
                    hitDistances,
                    hitNormals,
                    rayDirections,
                    slideStrength);

                // Only output if we have a meaningful avoidance force
                if (math.lengthsq(avoidanceForce) > 0.0001f)
                {
                    var limitedForce = SteeringMath.LimitMagnitude(avoidanceForce, stats.ValueRO.MaxSpeed);
                    steering.ValueRW.Linear = limitedForce;
                    steering.ValueRW.Priority = 2f;
                    steering.ValueRW.Weight = 1f;
                    steering.ValueRW.BehaviorType = SteeringBehaviorType.ObstacleAvoidance;
                }

                rayAngles.Dispose();
                rayDirections.Dispose();
                hitDistances.Dispose();
                hitNormals.Dispose();
            }
        }
    }
}
