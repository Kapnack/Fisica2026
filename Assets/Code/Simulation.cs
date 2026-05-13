using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public struct Hole
{
    public Vector2 position;
    public float radius;
}

public class Simulation : MonoBehaviour
{
    [SerializeField] private float tableRotation;
    [SerializeField] private float floorFriction;
    [SerializeField] private Wall[] walls;
    [SerializeField] private List<Ball> balls = new List<Ball>();
    [SerializeField] private Hole[] holes;

    List<Ball> toRemove = new List<Ball>();

    void Update()
    {
        foreach (Ball ball in balls)
        {
            ball.Integrate(Time.deltaTime, floorFriction);

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

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.brown;
        foreach (Wall wall in walls)
        {
            Gizmos.DrawLine(wall.pointA, wall.pointB);
        }

        Gizmos.color = Color.white;
        foreach (Ball ball in balls)
        {
            Gizmos.DrawSphere(ball.Position, ball.Radius);
        }

        Gizmos.color = Color.black;
        foreach (Hole hole in holes)
            Gizmos.DrawSphere(hole.position, hole.radius);
    }
}
