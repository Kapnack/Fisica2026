using System.Collections.Generic;
using UnityEngine;

public class Proyect : MonoBehaviour
{
    [SerializeField] List<Ball> balls = new List<Ball>();

    private void Awake()
    {
        balls[0].AddForce(new Vector2(20.0f, 0.0f));
    }

    void Start()
    {

    }

    void Update()
    {
        foreach (Ball ball in balls)
        {
            ball.Tick();

            foreach (Ball collitionBall in balls)
                if (!collitionBall.Equals(ball))
                    Ball.ResolveCollision(ball, collitionBall);
        }
    }
}
