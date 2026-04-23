using System;
using UnityEngine;

[Serializable]
struct Wall
{
    public Vector2 pointA;
    public Vector2 pointB;
    public float thickness;
}

public class Ball : MonoBehaviour
{
    private const float Gravity = 9.81f;
    
    [SerializeField] private Wall wall;
    [SerializeField] private Vector2 velocity;
    [SerializeField] private float radius;

    Ball[] balls;

    void Awake()
    {

    }

    void Update()
    {
        float dt = Time.deltaTime;

        velocity += Vector2.down * Gravity * dt;

        Vector2 position = transform.position;
        position += velocity * dt;

        Vector2 AB = wall.pointB - wall.pointA;
        Vector2 AP = position - wall.pointA;

        float t = Vector2.Dot(AP, AB) / Vector2.SqrMagnitude(AB);

        t = Mathf.Clamp01(t);

        Vector2 closest = wall.pointA + AB * t;

        float distance = Vector2.Distance(position, closest);

        if (distance <= wall.thickness + radius)
        {
            Vector2 normal = (position - closest).normalized;

            position = closest + normal * (wall.thickness + radius);

            velocity = Vector2.Reflect(velocity, normal);
        }

       // foreach (Ball other in balls)
       // {
       //     if (other == this) 
       //         continue;
       //
       //     Vector2 delta = (Vector2)transform.position - (Vector2)other.transform.position;
       //     float ballsDistance = delta.magnitude;
       //
       //     float minDistance = radius + other.radius;
       //
       //     if (distance <= minDistance)
       //     {
       //         Vector2 normal = delta.normalized;
       //
       //         float penetration = minDistance - distance;
       //
       //         transform.position += (Vector3)(normal * (penetration * 0.5f));
       //         other.transform.position -= (Vector3)(normal * (penetration * 0.5f));
       //     }
       // }

        transform.position = position;
    }

    void OnDrawGizmos()
    {
        Vector2 position = Application.isPlaying ? (Vector2)transform.position : (Vector2)transform.position;

        Vector2 AB = wall.pointB - wall.pointA;
        Vector2 AP = position - wall.pointA;

        float t = Vector2.Dot(AP, AB) / Vector2.Dot(AB, AB);
        t = Mathf.Clamp01(t);

        Vector2 closest = wall.pointA + AB * t;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(wall.pointA, wall.pointB);

        Gizmos.color = Color.white;
        Gizmos.DrawSphere(position, radius);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(closest, 0.1f);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(wall.pointA, position);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(wall.pointA, closest);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(closest, position);

        Vector2 normal = (position - closest).normalized;
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(closest, closest + normal);
    }
}