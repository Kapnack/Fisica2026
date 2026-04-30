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

    public Vector2 Position => position;
    public float Radius => radius;

    float InvMass => (mass <= 0f) ? 0f : 1f / mass;

    public void Integrate(float deltaTime)
    {
        position += velocity * deltaTime;
    }

    public void CheckWallCollision(Wall wall)
    {
        Vector2 wallVector = wall.pointB - wall.pointA;
        float wallVectorSqrMag = Vector2.SqrMagnitude(wallVector);

        if (wallVectorSqrMag <= Mathf.Epsilon)
            return;

        Vector2 ballPointAVector = position - wall.pointA;

        float ballWallInterpolation = Vector2.Dot(ballPointAVector, wallVector) / wallVectorSqrMag;
        ballWallInterpolation = Mathf.Clamp01(ballWallInterpolation);

        Vector2 closestPointToWall = wall.pointA + wallVector * ballWallInterpolation;

        Vector2 delta = position - closestPointToWall;
        float dist = delta.magnitude;

        float minDist = wall.thickness + radius;

        if (dist > minDist)
            return;

        if (dist <= Mathf.Epsilon)
            return;

        Vector2 normal = delta / dist;

        position = closestPointToWall + normal * minDist;

        Vector2 vNormal = Vector2.Dot(velocity, normal) * normal;
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

        float penetration = minDist - ballsDistance;

        Vector2 correction = normal * (penetration * 0.5f);
        position += correction;
        other.position -= correction;

        Vector2 relativeVelocity = velocity - other.velocity;
        float velAlongNormal = Vector2.Dot(relativeVelocity, normal);

        if (velAlongNormal > 0)
            return;

        float minRestitution = Mathf.Min(restitution, other.restitution);

        float invMassA = InvMass;
        float invMassB = other.InvMass;

        float impulseCorrecction = -(1 + minRestitution) * velAlongNormal;

        float denom = invMassA + invMassB;
        if (denom < 0f || Mathf.Approximately(denom, 0.0f))
            return;

        impulseCorrecction /= denom;

        Vector2 impulse = impulseCorrecction * normal;

        velocity += impulse * invMassA;
        other.velocity -= impulse * invMassB;

        relativeVelocity = velocity - other.velocity;

        Vector2 tangent = (relativeVelocity - Vector2.Dot(relativeVelocity, normal) * normal);
        if (tangent.sqrMagnitude > Mathf.Epsilon)
            tangent.Normalize();

        float relativeVelTangent = Vector2.Dot(relativeVelocity, tangent);

        float tangencialImpulse = -relativeVelTangent;
        tangencialImpulse /= denom;

        float coeficientFriction = (friction + other.friction) * 0.5f;

        tangencialImpulse = Mathf.Clamp(tangencialImpulse, -impulseCorrecction * coeficientFriction, impulseCorrecction * coeficientFriction);

        Vector2 frictionImpulse = tangencialImpulse * tangent;

        velocity += frictionImpulse * invMassA;
        other.velocity -= frictionImpulse * invMassB;
    }
}