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

        if (ballsDistance <= Mathf.Epsilon || ballsDistance > minDist)
            return;

        Vector2 otherToThisNormal = otherToThisVector / ballsDistance;
        Vector2 otherBallNormal = other.velocity.normalized;

        Vector2 otherFuturePos = other.position + other.velocity * deltaTime;
        Vector2 otherFutureCollisionCheck = otherFuturePos + -velocity.normalized * radius;
        otherFutureCollisionCheck = new Vector2(otherFutureCollisionCheck.x, otherFutureCollisionCheck.y);

        float distPrev = Physics.Math.Dot(previousPosition - otherFutureCollisionCheck, otherFutureCollisionCheck);
        float distCurr = Physics.Math.Dot(position - otherFutureCollisionCheck, otherFutureCollisionCheck);
        //TODO: FIX THIS AAAAAAAAAAAAAAAAAAAAAA.
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

            float projection = Physics.Math.Dot(intersectPoint - other.position, otherFutureCollisionCheck);

            if (projection >= 0 && projection <= other.velocity.magnitude)
            {
                Vector2 normal = otherBallNormal * Mathf.Sign(distPrev);
                position = intersectPoint;

                float minRestitution = Mathf.Min(restitution, other.restitution);

                velocity = Reflect(velocity, normal, minRestitution);
                other.velocity = Reflect(other.velocity, normal, minRestitution);

                return;
            }

        //Vector2 otherFuturePos = other.Position * other.velocity * deltaTime;
        //
        //if (SegmentToSegment(previousPosition, Position, other.Position, otherFuturePos, out Vector2 intersectPoint))
        //{
        //    Vector2 otherBallFutDir = (otherFuturePos - other.position).normalized;
        //    Vector2 normal = new Vector2(-otherBallFutDir.y, otherBallFutDir.x);
        //
        //    if (Vector2.Dot(normal, previousPosition - intersectPoint) < 0)
        //        normal = -normal;
        //
        //    float combinedRadius = other.radius + radius;
        //
        //    Position = intersectPoint + (normal * combinedRadius);
        //    velocity = Reflect(velocity, normal, restitution);
        //
        //    other.Position = intersectPoint + (-normal * combinedRadius);
        //    other.velocity = Reflect(velocity, -normal, restitution);
        //
        //    ApplyBallPhysicsResponse(other, normal);
        //
        //    return;
        //}

        if (lineCircleCollision(previousPosition, position, other.Position, other.Radius))
        {
            Vector2 relativeVelocity = -velocity;
            float velAlongNormal = Physics.Math.Dot(relativeVelocity, otherToThisNormal);

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

            Vector2 impulse = impulseCorrecction * otherToThisNormal;
            velocity += impulse * invMassA;
            other.velocity -= impulse * invMassB;

            ApplyBallFriction(other, relativeVelocity, otherToThisNormal, impulseCorrecction, denom);

            return;
        }
    }

    ResolveBallOverlap(other, otherToThisNormal, minDist - ballsDistance);
    ApplyBallPhysicsResponse(other, otherToThisNormal);
}

bool lineCircleCollision(Vector2 pointA, Vector2 pointB, Vector2 circlePos, float r)
{

    // is either end INSIDE the circle?
    // if so, return true immediately
    bool inside1 = pointCircle(pointA, circlePos, r);
    bool inside2 = pointCircle(pointB, circlePos, r);

    if (inside1 || inside2)
        return true;

    float distX = pointA.x - pointB.x;
    float distY = pointA.y - pointB.y;
    float len = Mathf.Sqrt((distX * distX) + (distY * distY));


    float dot = Physics.Math.Dot(circlePos, pointA) / Mathf.Pow(len, 2);

    // find the closest point on the line
    float closestX = pointA.x + (dot * (pointB.x - pointA.x));
    float closestY = pointA.y + (dot * (pointB.y - pointA.y));

    // is this point actually on the line segment?
    // if so keep going, but if not, return false
    bool onSegment = linePoint(pointA, pointB, new Vector2(closestX, closestY));
    if (!onSegment) return false;

    // get distance to closest point
    distX = closestX - circlePos.x;
    distY = closestY - circlePos.y;
    float distance = Mathf.Sqrt((distX * distX) + (distY * distY));

    if (distance <= r)
    {
        return true;
    }
    return false;
}

bool linePoint(Vector2 pointA, Vector2 pointB, Vector2 proyected)
{

    // get distance from the point to the two ends of the line
    float d1 = Vector2.Distance(proyected, pointA);
    float d2 = Vector2.Distance(proyected, pointB);

    // get the length of the line
    float lineLen = Vector2.Distance(pointA, pointB);

    // since floats are so minutely accurate, add
    // a little buffer zone that will give collision
    float buffer = 0.1f;    // higher # = less accurate

    // if the two distances are equal to the line's
    // length, the point is on the line!
    // note we use the buffer here to give a range,
    // rather than one #
    if (d1 + d2 >= lineLen - buffer && d1 + d2 <= lineLen + buffer)
    {
        return true;
    }
    return false;
}

bool pointCircle(Vector2 point, Vector2 circlePos, float r)
{
    // if the distance is less than the circle's
    // radius the point is inside!
    return Vector2.Distance(point, circlePos) <= r;
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