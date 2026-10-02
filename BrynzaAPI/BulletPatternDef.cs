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
}
