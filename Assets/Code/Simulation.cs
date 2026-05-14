using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[Serializable]
public struct Hole
{
    public Vector2 position;
    public float radius;
}

public class Simulation : MonoBehaviour
{
    [Header("Table Properties")]
    [SerializeField] private float tableRotation;
    [SerializeField] private float floorFriction;

    [Header("Ball Modification")]
    [SerializeField] private int ballIndex;
    [SerializeField] private float aceleration = 0.3f;
    [SerializeField] private float rotation = 0.0f;
    [SerializeField] private bool impulse = false;
    private Vector2 acelerationDir = Vector2.zero;

    [Header("Scene Objects")]
    [SerializeField] private Wall[] walls;
    [SerializeField] private List<Ball> balls = new List<Ball>();
    [SerializeField] private Hole[] holes;

    List<Ball> toRemove = new List<Ball>();

    private const float Gravity = 9.81f;

    private void Awake()
    {
        Application.runInBackground = true;
    }

    void Update()
    {
        foreach (Ball ball in balls)
        {
            ball.Integrate(Time.deltaTime, floorFriction, Gravity);

            foreach (Wall wall in walls)
                ball.CheckWallCollision(wall);


            foreach (Ball other in balls)
            {
                if (other.Equals(ball))
                    continue;

                ball.CheckBallCollision(other);
            }

            if (!IsInsideHole(ball))
                continue;

            toRemove.Add(ball);
        }

        foreach (Ball ballToRemove in toRemove)
            balls.Remove(ballToRemove);
    }

    [ContextMenu("Impulse Selected Ball")]
    private void ImpulseBall()
    {
        if (ballIndex < 0 || ballIndex > balls.Count)
            return;

        balls[ballIndex].Impulse(aceleration, acelerationDir);

        aceleration = 0.0f;
        acelerationDir = Vector2.one;
        rotation = 0.0f;
    }

    [ContextMenu("RotateWalls")]
    private void RotateWalls()
    {
        for (int i = 0; i < walls.Length; ++i)
            walls[i] = RotateWall(walls[i], tableRotation);
    }

    private bool IsInsideHole(Ball ball)
    {
        foreach (Hole hole in holes)
        {
            if (Vector2.SqrMagnitude(ball.Position - hole.position) <= hole.radius * hole.radius)
                return true;
        }

        return false;
    }

    public Wall RotateWall(Wall wall, float angleInDegrees)
    {
        float radians = angleInDegrees * (MathF.PI / 180f);
        float cos = MathF.Cos(radians);
        float sin = MathF.Sin(radians);

        return new Wall
        {
            pointA = RotatePoint(wall.pointA, cos, sin),
            pointB = RotatePoint(wall.pointB, cos, sin),
            thickness = wall.thickness
        };
    }

    private Vector2 RotatePoint(Vector2 p, float cos, float sin)
    {
        return new Vector2(
            p.x * cos - p.y * sin,
            p.x * sin + p.y * cos
        );
    }

    Vector2 RotateVector(Vector2 v, float radians)
    {
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        return new Vector2(
            v.x * cos - v.y * sin,
            v.x * sin + v.y * cos
        );
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.brown;
        foreach (Wall wall in walls)
        {
            Gizmos.DrawLine(wall.pointA, wall.pointB);
        }

        for (int i = 0; i < balls.Count; ++i)
        {
            if (i == ballIndex)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(balls[i].Position, balls[i].Position + (acelerationDir.normalized * aceleration));
            }
            else
                Gizmos.color = Color.white;

            Handles.Label(balls[i].Position + Vector2.up + Vector2.left * 0.5f, "Ball: " + i + ".");
            Gizmos.DrawSphere(balls[i].Position, balls[i].Radius);
        }

        Gizmos.color = Color.black;
        foreach (Hole hole in holes)
            Gizmos.DrawSphere(hole.position, hole.radius);
    }

    private void OnValidate()
    {
        acelerationDir = RotateVector(Vector2.one, rotation);

        if (impulse)
        {
            ImpulseBall();
            impulse = false;
        }
    }
}
