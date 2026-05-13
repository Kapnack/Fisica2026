using System;
using UnityEngine;

[Serializable]
public class Ball
{
    [SerializeField] private Vector2 position;
    [SerializeField] private float radius;
    [SerializeField] private Vector2 velocity;
    [SerializeField] private float restitution = 0.8f;
    [SerializeField] private float mass = 1f;
    [SerializeField] private float friction = 0.3f;

    [SerializeField] private float aceleration = 0.3f;
    [SerializeField] private Vector2 acelerationDir;

    public Vector2 Position => position;
    public float Radius => radius;
    private float InvMass => (mass <= 0f) ? 0f : 1f / mass;

    [ContextMenu("Impulse")]
    private void Impulse()
    {
        velocity += aceleration * acelerationDir.normalized;
    }

    public void Integrate(float deltaTime, float floorFriction)
    {
        position += velocity * deltaTime;

        if (velocity.sqrMagnitude > Mathf.Epsilon)
            velocity += -velocity.normalized * (floorFriction * deltaTime);
        else
            velocity = Vector2.zero;
    }

    public void CheckWallCollision(Wall wall)
    {
        Vector2 closestPointToWall = GetClosestPointOnWall(wall);
        Vector2 delta = position - closestPointToWall;
        float dist = delta.magnitude;
        float minDist = wall.thickness + radius;

        if (dist > minDist || dist < Mathf.Epsilon)
            return;

        Vector2 normal = delta / dist;

        ResolveWallOverlap(closestPointToWall, normal, minDist);
        ApplyWallResponse(normal);
    }

    private Vector2 GetClosestPointOnWall(Wall wall)
    {
        Vector2 wallVector = wall.pointB - wall.pointA;
        float wallVectorSqrMag = wallVector.sqrMagnitude;

        if (wallVectorSqrMag < Mathf.Epsilon) return wall.pointA;

        float ballWallInterpolation = DotProduct(position - wall.pointA, wallVector) / wallVectorSqrMag;
        ballWallInterpolation = Mathf.Clamp01(ballWallInterpolation);

        return wall.pointA + wallVector * ballWallInterpolation;
    }

    private void ResolveWallOverlap(Vector2 closestPointToWall, Vector2 normal, float minDist)
    {
        position = closestPointToWall + normal * minDist;
    }

    private void ApplyWallResponse(Vector2 normal)
    {
        Vector2 vNormal = DotProduct(velocity, normal) * normal;
        Vector2 vTangent = velocity - vNormal;

        vNormal = -vNormal * restitution;
        vTangent *= (1.0f - friction);

        velocity = vNormal + vTangent;
    }

    public void CheckBallCollision(Ball other)
    {
        Vector2 otherToThisVector = position - other.position;
        float ballsDistance = otherToThisVector.magnitude;
        float minDist = radius + other.radius;

        if (ballsDistance <= Mathf.Epsilon || ballsDistance > minDist)
            return;

        Vector2 normal = otherToThisVector / ballsDistance;

        ResolveBallOverlap(other, normal, minDist - ballsDistance);
        ApplyBallPhysicsResponse(other, normal);
    }

    private void ResolveBallOverlap(Ball other, Vector2 normal, float penetration)
    {
        Vector2 correction = normal * (penetration * 0.5f);
        position += correction;
        other.position -= correction;
    }

    private void ApplyBallPhysicsResponse(Ball other, Vector2 normal)
    {
        Vector2 relativeVelocity = velocity - other.velocity;
        float velAlongNormal = DotProduct(relativeVelocity, normal);

        if (velAlongNormal > 0)
            return;

        float invMassA = InvMass;
        float invMassB = other.InvMass;
        float denom = invMassA + invMassB;

        if (denom <= 0 || Mathf.Approximately(denom, 0f))
            return;

        float minRestitution = Mathf.Min(restitution, other.restitution);
        float impulseCorrecction = (-(1 + minRestitution) * velAlongNormal) / denom;

        Vector2 impulse = impulseCorrecction * normal;
        velocity += impulse * invMassA;
        other.velocity -= impulse * invMassB;

        ApplyBallFriction(other, relativeVelocity, normal, impulseCorrecction, denom);
    }

    private void ApplyBallFriction(Ball other, Vector2 relativeVelocity, Vector2 normal, float impulseCorrecction, float denom)
    {
        Vector2 tangent = relativeVelocity - (DotProduct(relativeVelocity, normal) * normal);

        if (tangent.sqrMagnitude > Mathf.Epsilon)
            tangent.Normalize();

        float relativeVelTangent = DotProduct(relativeVelocity, tangent);
        float tangencialImpulse = -relativeVelTangent / denom;

        float coeficientFriction = (friction + other.friction) * 0.5f;
        tangencialImpulse = Mathf.Clamp(tangencialImpulse, -impulseCorrecction * coeficientFriction, impulseCorrecction * coeficientFriction);

        Vector2 frictionImpulse = tangencialImpulse * tangent;
        velocity += frictionImpulse * InvMass;
        other.velocity -= frictionImpulse * other.InvMass;
    }

    private float DotProduct(Vector2 a, Vector2 b)
    {
        return a.x * b.x + a.y * b.y;
    }

    public static float Cross(Vec2 a, Vec2 b)
       => a.x * b.y - a.y * b.x;
}