using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BrynzaAPI;
[CreateAssetMenu(menuName = "BrynzaAPI/BulletPatternDef")]
public class BulletPatternDef : ScriptableObject // New file for new class in a long time wow you are doing good brynza
{
    public List<Vector2> points = [];
    public Vector2 GetSpreadOffset(int shotIndex)
    {
        if (points == null || points.Count == 0) return Vector2.zero;
        Vector2 normalizedPoint = points[shotIndex % points.Count];
        return new Vector2(normalizedPoint.x, normalizedPoint.y);
    }
    public Vector3 GetAimRay(CharacterBody characterBody, Vector3 aimVector, float spread)
    {
        int currentBulletCount = characterBody.GetBulletCount();
        Vector2 vector2 = GetSpreadOffset(currentBulletCount);
        characterBody.SetBulletCount(currentBulletCount + 1);
        characterBody.SetBulletCountResetTimer(characterBody.GetBulletCountGraceDuration());
        Vector3 vector11 = Vector3.Cross(Vector3.up, aimVector);
        Vector3 vector12 = Vector3.Cross(aimVector, vector11);
        Vector3 vector3 = Quaternion.AngleAxis(vector2.x * (spread / 2f), vector12) * Quaternion.AngleAxis(vector2.y * (spread / 2f), vector11) * aimVector;
        return vector3;
    }
}
