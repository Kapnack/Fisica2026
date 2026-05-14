using System;
using UnityEngine;

[Serializable]
public class Ball
{
    [SerializeField] private Vector2 previousPosition;
    [SerializeField] private Vector2 position;
    [SerializeField] private float radius;
    [SerializeField] private Vector2 velocity;
    [SerializeField] private float restitution = 0.8f;
    [SerializeField] private float mass = 1f;
    [SerializeField] private float friction = 0.3f;

    public Vector2 Position
    {
        get => position;
        private set
        {
            previousPosition = Position;
            position = value;
        }
    }

    public float Radius => radius;
    private float InvMass => (mass <= 0f) ? 0f : 1f / mass;

    private float Dot(Vector2 a, Vector2 b) => a.x * b.x + a.y * b.y;

    private float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

    private Vector2 Project(Vector2 vector, Vector2 ontoNormal)
    {
        float dot = Dot(vector, ontoNormal);
        return ontoNormal * dot;
    }

    private Vector2 Reflect(Vector2 vector, Vector2 normal, float bounce)
    {
        Vector2 vNormal = Project(vector, normal);
        Vector2 vTangent = vector - vNormal;

        return (-vNormal * bounce) + (vTangent * (1.0f - friction));
    }

    public void Impulse(float aceleration, Vector2 dir)
    {
        velocity += aceleration * dir.normalized;
    }

    public void Integrate(float deltaTime, float floorFriction, float gravity)
    {
        Position += velocity * deltaTime;

        if (velocity.sqrMagnitude > Mathf.Epsilon * Mathf.Epsilon)
            velocity += -velocity.normalized * (floorFriction * gravity * deltaTime);
        else
            velocity = Vector2.zero;
    }

    public void CheckWallCollision(Wall wall)
    {
        if (SegmentToSegment(previousPosition, Position, wall.pointA, wall.pointB, out Vector2 intersectPoint))
        {
            Vector2 wallDir = (wall.pointB - wall.pointA).normalized;
            Vector2 normal = new Vector2(-wallDir.y, wallDir.x);

            if (Vector2.Dot(normal, previousPosition - intersectPoint) < 0)
                normal = -normal;

            float combinedRadius = wall.thickness + radius;

            Position = intersectPoint + (normal * combinedRadius);

            velocity = Reflect(velocity, normal, restitution);

            return;
        }

        Vector2 closestPointToWall = GetClosestPointOnWall(wall);
        Vector2 delta = Position - closestPointToWall;
        float dist = delta.magnitude;
        float minDist = wall.thickness + radius;

        if (dist < minDist && dist > Mathf.Epsilon)
        {
            Vector2 normal = delta / dist;
            ResolveWallOverlap(closestPointToWall, normal, minDist);
            velocity = Reflect(velocity, normal, restitution);
        }
    }

    public bool SegmentToSegment(Vector2 point1A, Vector2 point1B, Vector2 point2A, Vector2 point2B, out Vector2 intersectPoint)
    {
        intersectPoint = Vector2.zero;
        Vector2 seg1Dir = point1B - point1A;
        Vector2 seg2Dir = point2B - point2A;
        Vector2 vectorAtoA = point1A - point2B;

        float commonDeterminant = Cross(seg1Dir, seg2Dir);

        if (Mathf.Abs(commonDeterminant) < float.Epsilon)
            return false;

        float detX = Cross(seg2Dir, vectorAtoA) / commonDeterminant;
        float detY = Cross(seg1Dir, vectorAtoA) / commonDeterminant;

        bool isThereIntersection = (detX >= 0 && detX <= 1 &&
                                    detY >= 0 && detY <= 1);

        intersectPoint = isThereIntersection ? new Vector2(
            point1A.x + (detX * (point1B.x - point1A.x)),
            point1A.y + (detX * (point1B.y - point1A.y))
            ) : Vector2.zero;

        return isThereIntersection;
    }

    private Vector2 GetClosestPointOnWall(Wall wall)
    {
        Vector2 wallVector = wall.pointB - wall.pointA;
        float wallVectorSqrMag = wallVector.sqrMagnitude;

        if (wallVectorSqrMag < Mathf.Epsilon)
            return wall.pointA;

        float ballWallInterpolation = Dot(Position - wall.pointA, wallVector) / wallVectorSqrMag;
        ballWallInterpolation = Mathf.Clamp01(ballWallInterpolation);

        return wall.pointA + wallVector * ballWallInterpolation;
    }

    private void ResolveWallOverlap(Vector2 closestPointToWall, Vector2 normal, float minDist)
    {
        Position = closestPointToWall + normal * minDist;
    }

    public void CheckBallCollision(Ball other)
    {
        Vector2 otherToThisVector = Position - other.Position;
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
        Position += correction;
        other.Position -= correction;
    }

    private void ApplyBallPhysicsResponse(Ball other, Vector2 normal)
    {
        Vector2 relativeVelocity = velocity - other.velocity;
        float velAlongNormal = Dot(relativeVelocity, normal);

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
        Vector2 tangent = relativeVelocity - Project(relativeVelocity, normal);

        if (tangent.sqrMagnitude > Mathf.Epsilon)
            tangent.Normalize();

        float relativeVelTangent = Dot(relativeVelocity, tangent);
        float tangencialImpulse = -relativeVelTangent / denom;

        float coeficientFriction = (friction + other.friction) * 0.5f;
        tangencialImpulse = Mathf.Clamp(tangencialImpulse, -impulseCorrecction * coeficientFriction, impulseCorrecction * coeficientFriction);

        Vector2 frictionImpulse = tangencialImpulse * tangent;
        velocity += frictionImpulse * InvMass;
        other.velocity -= frictionImpulse * other.InvMass;
    }
}