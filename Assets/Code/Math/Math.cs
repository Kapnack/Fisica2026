using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Physics
{
    public class Math
    {
        static public float Dot(Vector2 a, Vector2 b) => a.x * b.x + a.y * b.y;

        static public float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        static public Vector2 Project(Vector2 vector, Vector2 ontoNormal)
        {
            float dot = Dot(vector, ontoNormal);
            return ontoNormal * dot;
        }

        static public Vector2 Reflect(Vector2 vector, Vector2 normal, float bounce)
        {
            Vector2 vNormal = Project(vector, normal);
            return (-vNormal * bounce);
        }

        static public bool DoSegmentCollide(Vector2 point1A, Vector2 point1B, Vector2 point2A, Vector2 point2B, out Vector2 intersectPoint)
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
    }
}
