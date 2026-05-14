using System;
using UnityEngine;

[Serializable]
public struct Vec2 : IEquatable<Vec2>
{
    #region Variables

    public float x;
    public float y;

    public float sqrMagnitude => x * x + y * y;

    public float magnitude
    {
        get
        {
            float sqr = sqrMagnitude;
            if (sqr < epsilon) return 0f;
            return MathF.Sqrt(sqr);
        }
    }

    public Vec2 normalized
    {
        get
        {
            float sqr = sqrMagnitude;

            if (sqr < epsilon * epsilon)
                return Zero;

            float invMag = 1.0f / MathF.Sqrt(sqr);
            return new Vec2(x * invMag, y * invMag);
        }
    }

    #endregion

    #region Constants

    public const float epsilon = 1e-6f;

    #endregion

    #region Default Values

    public static Vec2 Zero => new Vec2(0f, 0f);
    public static Vec2 One => new Vec2(1f, 1f);

    #endregion

    #region Constructors

    public Vec2(float x, float y)
    {
        this.x = x;
        this.y = y;
    }

    public Vec2(Vector2 v)
    {
        x = v.x;
        y = v.y;
    }

    #endregion

    #region Operators

    public static Vec2 operator +(Vec2 a, Vec2 b)
        => new Vec2(a.x + b.x, a.y + b.y);

    public static Vec2 operator -(Vec2 a, Vec2 b)
        => new Vec2(a.x - b.x, a.y - b.y);

    public static Vec2 operator -(Vec2 v)
        => new Vec2(-v.x, -v.y);

    public static Vec2 operator *(Vec2 v, float s)
        => new Vec2(v.x * s, v.y * s);

    public static Vec2 operator *(float s, Vec2 v)
        => v * s;

    public static Vec2 operator /(Vec2 v, float s)
    {
        if (MathF.Abs(s) < epsilon)
            return new Vec2(
                v.x * float.PositiveInfinity,
                v.y * float.PositiveInfinity
            );

        return new Vec2(v.x / s, v.y / s);
    }

    public static bool operator ==(Vec2 a, Vec2 b)
        => (a - b).sqrMagnitude < epsilon * epsilon;

    public static bool operator !=(Vec2 a, Vec2 b)
        => !(a == b);

    public static implicit operator Vector2(Vec2 v)
        => new Vector2(v.x, v.y);

    public static implicit operator Vector3(Vec2 v)
    => new Vector3(v.x, v.y);

    #endregion

    #region Functions

    public static float Dot(Vec2 a, Vec2 b)
        => a.x * b.x + a.y * b.y;

    public static float Cross(Vec2 a, Vec2 b)
        => a.x * b.y - a.y * b.x;

    public static float Distance(Vec2 a, Vec2 b)
        => (a - b).magnitude;

    public static Vec2 Project(Vec2 v, Vec2 onNormal)
    {
        float sqr = onNormal.sqrMagnitude;

        if (sqr < epsilon * epsilon)
            return Zero;

        return onNormal * (Dot(v, onNormal) / sqr);
    }

    public static Vec2 Reflect(Vec2 dir, Vec2 normal)
    {
        Vec2 n = normal.normalized;
        return dir - 2f * Dot(dir, n) * n;
    }

    public void Normalize()
    {
        float sqr = sqrMagnitude;

        if (sqr < epsilon * epsilon)
        {
            x = 0f;
            y = 0f;
            return;
        }

        float invMag = 1.0f / MathF.Sqrt(sqr);
        x *= invMag;
        y *= invMag;
    }

    #endregion

    #region Safety Checks

    public bool IsFinite()
    {
        return !(float.IsNaN(x) || float.IsNaN(y) ||
                 float.IsInfinity(x) || float.IsInfinity(y));
    }

    public void Sanitize()
    {
        if (!IsFinite())
        {
            x = 0f;
            y = 0f;
        }
    }

    #endregion

    #region Debug

    public override string ToString()
        => $"({x}, {y})";

    #endregion

    #region Equality

    public override bool Equals(object obj)
        => obj is Vec2 other && this == other;

    public bool Equals(Vec2 other)
        => this == other;

    public override int GetHashCode()
        => x.GetHashCode() ^ (y.GetHashCode() << 2);

    #endregion
}