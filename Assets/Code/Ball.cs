using UnityEngine;

public class Ball : MonoBehaviour
{
    public Vector2 Position;
    public Vector2 Velocity;
    public Vector2 Acceleration;

    public float Mass;
    public float Radius;
    public float Restitution;

    public bool IsActive = true;

    public Ball(Vector2 position, float radius, float mass, float restitution)
    {
        Position = position;
        Radius = radius;
        Mass = mass;
        Restitution = restitution;

        Velocity = Vector2.Zero;
        Acceleration = Vector2.Zero;
    }


    public void Update(float dt, float frictionCoefficient, float gravity = 9.81f)
    {
        if (!IsActive) return;

        ApplyFriction(dt, frictionCoefficient, gravity);

        Velocity += Acceleration * dt;
        Position += Velocity * dt;

        Acceleration = Vector2.Zero;

        if (Velocity.LengthSquared() < Mathf.Epsilon)
            Velocity = Vector2.Zero;
    }

    private void ApplyFriction(float dt, float mu, float g)
    {
        if (Velocity.LengthSquared() <= 0) return;

        Vector2 frictionDir = -Vector2.Normalize(Velocity);
        Vector2 friction = frictionDir * mu * g;

        Velocity += friction * dt;

        if (Vector2.Dot(Velocity, frictionDir) > 0)
            Velocity = Vector2.Zero;
    }

    public void AddForce(Vector2 force)
    {
        Acceleration += force / Mass;
    }

    public void AddImpulse(Vector2 impulse)
    {
        Velocity += impulse / Mass;
    }

    public static void ResolveCollision(Ball a, Ball b)
    {
        if (!a.IsActive || !b.IsActive) return;

        Vector2 normal = b.Position - a.Position;
        float distance = normal.Length();

        float minDistance = a.Radius + b.Radius;

        if (distance == 0 || distance >= minDistance)
            return;

        normal /= distance;

        Vector2 relativeVelocity = b.Velocity - a.Velocity;

        float velAlongNormal = Vector2.Dot(relativeVelocity, normal);

        if (velAlongNormal > 0)
            return;

        float e = MathF.Min(a.Restitution, b.Restitution);

        float j = -(1 + e) * velAlongNormal;
        j /= (1 / a.Mass + 1 / b.Mass);

        Vector2 impulse = j * normal;

        a.Velocity -= impulse / a.Mass;
        b.Velocity += impulse / b.Mass;

        float penetration = minDistance - distance;
        float percent = 0.8f;
        float slop = 0.01f;

        Vector2 correction = normal *
            (MathF.Max(penetration - slop, 0) / (1 / a.Mass + 1 / b.Mass)) * percent;

        a.Position -= correction / a.Mass;
        b.Position += correction / b.Mass;
    }

    public void ResolveWallCollision(float minX, float maxX, float minY, float maxY)
    {
        if (!IsActive) return;

        if (Position.X - Radius < minX)
        {
            Position.X = minX + Radius;
            Velocity.X *= -Restitution;
        }

        if (Position.X + Radius > maxX)
        {
            Position.X = maxX - Radius;
            Velocity.X *= -Restitution;
        }

        if (Position.Y - Radius < minY)
        {
            Position.Y = minY + Radius;
            Velocity.Y *= -Restitution;
        }

        if (Position.Y + Radius > maxY)
        {
            Position.Y = maxY - Radius;
            Velocity.Y *= -Restitution;
        }
    }

    public void CheckHole(Vector2 holePosition, float holeRadius)
    {
        if (!IsActive) return;

        float distance = Vector2.Distance(Position, holePosition);

        if (distance < holeRadius)
        {
            IsActive = false;
        }
    }
}
