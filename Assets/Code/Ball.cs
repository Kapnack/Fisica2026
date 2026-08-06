using System;
using UnityEngine;

[Serializable]
public class Ball
{
    [Header("State")]
    [SerializeField] private Vector2 previousPosition;
    [SerializeField] private Vector2 position;
    [SerializeField] private Vector2 velocity;

    [Header("Properties")]
    [SerializeField] private float radius;
    [SerializeField] private float restitution = 0.8f;
    [SerializeField] private float mass = 1f;
    [SerializeField] private float friction = 0.3f;

    public Vector2 Position
    {
        get => position;
        private set
        {
            previousPosition = position;
            position = value;
        }
    }

    public float Radius => radius;
    private float InvMass => (mass <= 0f) ? 0f : 1f / mass;

    private Vector2 Project(Vector2 vector, Vector2 ontoNormal)
    {
        float dot = Physics.Math.Dot(vector, ontoNormal);
        return ontoNormal * dot;
    }

    private Vector2 Reflect(Vector2 vector, Vector2 normal, float bounce)
    {
        Vector2 vNormal = Project(vector, normal);
        Vector2 vTangent = vector - vNormal;

        // Invertimos la normal escalada por rebote y reducimos la tangente por fricción
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
        Vector2 wallVec = wall.pointB - wall.pointA;
        float wallLen = wallVec.magnitude;

        if (wallLen < Mathf.Epsilon)
            return;

        Vector2 wallDir = wallVec / wallLen;
        Vector2 wallNormal = new Vector2(-wallDir.y, wallDir.x);
        float minDist = wall.thickness + radius;

        //Detección de Túnel (CCD) por Planos
        float distPrev = Physics.Math.Dot(previousPosition - wall.pointA, wallNormal);
        float distCurr = Physics.Math.Dot(position - wall.pointA, wallNormal);

        // Si los signos cambian, la bola cruzó el muro en este frame
        if (Mathf.Sign(distPrev) != Mathf.Sign(distCurr) || Mathf.Abs(distCurr) < minDist)
        {
            float collisionTime = 0;
            if (Mathf.Abs(distPrev - distCurr) > Mathf.Epsilon)
            {
                // Interpolación lineal para hallar el punto exacto de contacto (Root Finding)
                collisionTime = (distPrev - (Mathf.Sign(distPrev) * minDist)) / (distPrev - distCurr);
            }

            collisionTime = Mathf.Clamp01(collisionTime);
            Vector2 intersectPoint = Vector2.Lerp(previousPosition, position, collisionTime);

            float projection = Physics.Math.Dot(intersectPoint - wall.pointA, wallDir);

            if (projection >= 0 && projection <= wallLen)
            {
                Vector2 normal = wallNormal * Mathf.Sign(distPrev);
                position = intersectPoint;
                velocity = Reflect(velocity, normal, restitution);
                return;
            }
        }

        // Colisión Estática (Círculo vs Punto más cercano)
        Vector2 closestPointToWall = GetClosestPointOnWall(wall);
        Vector2 delta = position - closestPointToWall;
        float distSqr = delta.sqrMagnitude;

        if (distSqr < minDist * minDist && distSqr > Mathf.Epsilon)
        {
            float dist = Mathf.Sqrt(distSqr);
            Vector2 normal = delta / dist;
            position = closestPointToWall + normal * minDist;
            velocity = Reflect(velocity, normal, restitution);
        }
    }

    private Vector2 GetClosestPointOnWall(Wall wall)
    {
        Vector2 wallVector = wall.pointB - wall.pointA;
        float wallVectorSqrMag = wallVector.sqrMagnitude;

        if (wallVectorSqrMag < Mathf.Epsilon)
            return wall.pointA;

        float ballWallInterpolation = Physics.Math.Dot(position - wall.pointA, wallVector) / wallVectorSqrMag;
        ballWallInterpolation = Mathf.Clamp01(ballWallInterpolation);

        return wall.pointA + wallVector * ballWallInterpolation;
    }

    public void CheckBallCollision(Ball other, float deltaTime)
    {
        Vector2 otherToThisVector = position - other.position;
        float ballsDistance = otherToThisVector.magnitude;
        float minDist = radius + other.radius;

        if (ballsDistance <= Mathf.Epsilon)
            return;

        Vector2 otherToThisNormal = otherToThisVector / ballsDistance;

        float distPrev = Physics.Math.Dot(previousPosition - other.previousPosition, otherToThisNormal);
        float distCurr = Physics.Math.Dot(position - other.position, otherToThisNormal);

        if (Mathf.Sign(distPrev) != Mathf.Sign(distCurr) || Mathf.Abs(distCurr) < minDist)
        {
            float collisionTime = 0;
            if (Mathf.Abs(distPrev - distCurr) > Mathf.Epsilon)
            {
                collisionTime = (distPrev - (Mathf.Sign(distPrev) * minDist)) / (distPrev - distCurr);
            }

            collisionTime = Mathf.Clamp01(collisionTime);
            Vector2 intersectPoint = Vector2.Lerp(previousPosition, position, collisionTime);

            // Verificar que el punto de intersección es válido (las bolas se acercan)
            Vector2 normal = otherToThisNormal * Mathf.Sign(distPrev);
            Vector2 relativeVelocity = velocity - other.velocity;

            if (Physics.Math.Dot(relativeVelocity, normal) > 0)
                return; // se alejan, ignorar

            position = intersectPoint;

            float invMassA = InvMass;
            float invMassB = other.InvMass;
            float denom = invMassA + invMassB;

            if (denom <= Mathf.Epsilon) return;

            float minRestitution = Mathf.Min(restitution, other.restitution);
            float velAlongNormal = Physics.Math.Dot(relativeVelocity, normal);
            float impulseScalar = (-(1 + minRestitution) * velAlongNormal) / denom;

            Vector2 impulse = impulseScalar * normal;
            velocity += impulse * invMassA;
            other.velocity -= impulse * invMassB;

            ApplyBallFriction(other, relativeVelocity, normal, impulseScalar, denom);
            return;
        }

        // --- 2. Fallback estático (overlap) ---
        if (ballsDistance > minDist) 
            return; // recién acá tiene sentido este guard

        ResolveBallOverlap(other, otherToThisNormal, minDist - ballsDistance);
        ApplyBallPhysicsResponse(other, otherToThisNormal);
    }

    public bool SegmentToSegment(Vector2 point1A, Vector2 point1B, Vector2 point2A, Vector2 point2B, out Vector2 intersectPoint)
    {
        intersectPoint = Vector2.zero;
        Vector2 seg1Dir = point1B - point1A;
        Vector2 seg2Dir = point2B - point2A;
        Vector2 vectorAtoA = point1A - point2B;

        float commonDeterminant = Physics.Math.Cross(seg1Dir, seg2Dir);

        if (Mathf.Abs(commonDeterminant) < float.Epsilon)
            return false;

        float detX = Physics.Math.Cross(seg2Dir, vectorAtoA) / commonDeterminant;
        float detY = Physics.Math.Cross(seg1Dir, vectorAtoA) / commonDeterminant;

        bool isThereIntersection = (detX >= 0 && detX <= 1 &&
                                    detY >= 0 && detY <= 1);

        intersectPoint = isThereIntersection ? new Vector2(
            point1A.x + (detX * (point1B.x - point1A.x)),
            point1A.y + (detX * (point1B.y - point1A.y))
            ) : Vector2.zero;

        return isThereIntersection;
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
        float velAlongNormal = Physics.Math.Dot(relativeVelocity, normal);

        // Si se están alejando, no aplicar impulso
        if (velAlongNormal > 0)
            return;

        float invMassA = InvMass;
        float invMassB = other.InvMass;
        float denom = invMassA + invMassB;

        if (denom <= 0 || Mathf.Approximately(denom, 0f)) return;

        float minRestitution = Mathf.Min(restitution, other.restitution);
        // Fórmula de Impulso Normal (j)
        float impulseCorrecction = (-(1 + minRestitution) * velAlongNormal) / denom;

        Vector2 impulse = impulseCorrecction * normal;
        velocity += impulse * invMassA;
        other.velocity -= impulse * invMassB;

        ApplyBallFriction(other, relativeVelocity, normal, impulseCorrecction, denom);
    }

    // Aplica fricción tangencial durante el choque (frena el deslizamiento entre bolas)
    private void ApplyBallFriction(Ball other, Vector2 relativeVelocity, Vector2 normal, float impulseCorrecction, float denom)
    {
        Vector2 tangent = relativeVelocity - Project(relativeVelocity, normal);

        if (tangent.sqrMagnitude > Mathf.Epsilon)
            tangent.Normalize();

        float relativeVelTangent = Physics.Math.Dot(relativeVelocity, tangent);
        float tangencialImpulse = -relativeVelTangent / denom;

        // La fricción no puede ser mayor que la fuerza normal (Ley de Coulomb)
        float coeficientFriction = (friction + other.friction) * 0.5f;
        tangencialImpulse = Mathf.Clamp(tangencialImpulse, -impulseCorrecction * coeficientFriction, impulseCorrecction * coeficientFriction);

        Vector2 frictionImpulse = tangencialImpulse * tangent;
        velocity += frictionImpulse * InvMass;
        other.velocity -= frictionImpulse * other.InvMass;
    }
}