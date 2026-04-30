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
    [SerializeField] private Wall[] walls;
    [SerializeField] private List<Ball> balls = new List<Ball>();
    [SerializeField] private Hole[] holes;

    private const float Gravity = 9.81f;

    List<Ball> toRemove = new List<Ball>();

    void Update()
    {
        foreach (Ball ball in balls)
        {
            ball.Integrate(Time.deltaTime);

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

    private bool IsInsideHole(Ball ball)
    {
        foreach (Hole hole in holes)
        {
            if (Vector2.SqrMagnitude(ball.Position - hole.position) <= hole.radius * hole.radius)
                return true;
        }

        return false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.brown;
        foreach (Wall wall in walls)
        {
            //Gizmos.DrawCube(((wall.pointB + wall.pointA) * 0.5f), new Vector3(wall.thickness, Vector3.Distance(wall.pointB, wall.pointA)));
            Gizmos.DrawLine(wall.pointA, wall.pointB);
        }

        Gizmos.color = Color.white;
        foreach (Ball ball in balls)
            Gizmos.DrawSphere(ball.Position, ball.Radius);

        Gizmos.color = Color.black;
        foreach (Hole hole in holes)
            Gizmos.DrawSphere(hole.position, hole.radius);
    }
}
