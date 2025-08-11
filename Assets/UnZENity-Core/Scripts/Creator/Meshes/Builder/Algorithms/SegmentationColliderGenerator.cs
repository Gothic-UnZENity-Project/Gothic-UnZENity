using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GUZ.Core.Creator.Meshes.Builder.Algorithms
{
    public static class SegmentationColliderGenerator
    {
        [System.Serializable]
        public class WidthSegment
        {
            public float minHeight, maxHeight;  // Height bounds along the weapon's main axis
            public float averageWidth;          // Average width at this height
            public List<Vector3> vertices;      // Vertices in this segment
            public Vector3 center;             // Center point
            public Vector3 size;               // Bounding box size
            public ColliderType suggestedType; // Suggested collider type
            
            public WidthSegment()
            {
                vertices = new List<Vector3>();
            }
        }
        
        public enum ColliderType
        {
            Box,
            Capsule
        }
        
        /// <summary>
        /// Generate colliders based on width variations along the weapon's main axis
        /// Automatically detects weapon orientation and segments from handle to tip
        /// </summary>
        public static void GenerateWeaponColliders(GameObject weaponObj, Mesh mesh,
            float widthThreshold = 0.4f, int minVerticesPerSegment = 3, int samples = 50)
        {
            if (mesh == null || mesh.vertices.Length == 0)
                return;
        
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            var bounds = mesh.bounds;
    
            // Step 1: Determine the weapon's main axis and orientation
            var weaponOrientation = DetermineWeaponOrientation(vertices, bounds);

            // Step 2: Create width profile along the main axis using triangles
            var widthProfile = CreateWidthProfile(weaponObj, vertices, triangles, bounds, weaponOrientation, samples);
            
            // Step 3: Segment based on significant width changes
            var segments = CreateWidthBasedSegments(vertices, triangles, widthProfile, weaponOrientation, widthThreshold, minVerticesPerSegment);
            
            // Step 4: Create appropriate colliders for each segment
            CreateCollidersFromSegments(weaponObj, segments, weaponOrientation);
            
            Debug.Log($"Created {segments.Count} colliders for {weaponObj.name} using {weaponOrientation.mainAxis} axis (bounds: {bounds.size})");
        }
        
        private struct WeaponOrientation
        {
            public int mainAxis;           // 0=X, 1=Y, 2=Z
            public Vector3 axisDirection;  // Direction vector along main axis
            public float minValue;         // Minimum value along main axis
            public float maxValue;         // Maximum value along main axis
            public float length;           // Length along main axis
        }
        
        private static WeaponOrientation DetermineWeaponOrientation(Vector3[] vertices, Bounds bounds)
        {
            var orientation = new WeaponOrientation();
            var size = bounds.size;
            
            // Find the longest axis - this should be the weapon's main axis
            if (size.x >= size.y && size.x >= size.z)
            {
                orientation.mainAxis = 0; // X-axis
                orientation.axisDirection = Vector3.right;
                orientation.minValue = bounds.min.x;
                orientation.maxValue = bounds.max.x;
                orientation.length = size.x;
            }
            else if (size.y >= size.x && size.y >= size.z)
            {
                orientation.mainAxis = 1; // Y-axis
                orientation.axisDirection = Vector3.up;
                orientation.minValue = bounds.min.y;
                orientation.maxValue = bounds.max.y;
                orientation.length = size.y;
            }
            else
            {
                orientation.mainAxis = 2; // Z-axis
                orientation.axisDirection = Vector3.forward;
                orientation.minValue = bounds.min.z;
                orientation.maxValue = bounds.max.z;
                orientation.length = size.z;
            }
            
            // Additional validation: For weapons, length should be significantly longer than width
            float maxWidth = orientation.mainAxis == 0 ? Mathf.Max(size.y, size.z) : 
                            orientation.mainAxis == 1 ? Mathf.Max(size.x, size.z) : 
                                                        Mathf.Max(size.x, size.y);
            
            float aspectRatio = orientation.length / maxWidth;
            
            if (aspectRatio < 1.5f)
            {
                Debug.LogWarning($"Weapon aspect ratio is low ({aspectRatio:F2}). This might not be a typical weapon shape.");
            }
            
            Debug.Log($"Weapon orientation: {orientation.mainAxis} axis, length: {orientation.length:F3}, aspect ratio: {aspectRatio:F2}");
            
            return orientation;
        }
        
        private static List<float> CreateWidthProfile(GameObject go, Vector3[] vertices, int[] triangles, Bounds bounds, WeaponOrientation orientation, int samples)
        {
            var profile = new List<float>(samples);
            float step = orientation.length / samples;
            
            for (int i = 0; i < samples; i++)
            {
                float currentPos = orientation.minValue + (i * step);
                float nextPos = orientation.minValue + ((i + 1) * step);
                
// Create box collider at current position
// var colliderObj = new GameObject($"SliceCollider_{i}");
// colliderObj.transform.SetParent(go.transform, false);
// var boxCollider = colliderObj.AddComponent<BoxCollider>();
// boxCollider.center = new Vector3(currentPos, 0, 0);
// boxCollider.size = new Vector3(0f, 0.1f, 0.1f);
                
                // Find all triangles that intersect with this slice
                var intersectionPoints = GetSliceTriangleIntersections(vertices, triangles, orientation.mainAxis, currentPos, nextPos);
                
                if (intersectionPoints.Count == 0)
                {
                    // Use interpolated value from neighboring slices if no intersections found
                    float interpolatedWidth = InterpolateWidth(profile, i);
                    profile.Add(interpolatedWidth);
                    continue;
                }
                
                // Calculate width at this slice using intersection points
                float width = CalculateSliceWidthFromPoints(intersectionPoints, orientation.mainAxis);
                profile.Add(width);
            }
            
            // Smooth the profile to reduce noise
            SmoothProfile(profile);
            
            return profile;
        }

        private static List<Vector3> GetSliceTriangleIntersections(Vector3[] vertices, int[] triangles, int mainAxis, float sliceStart, float sliceEnd)
        {
            var intersectionPoints = new List<Vector3>();
            
            // Process triangles in groups of 3
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var v1 = vertices[triangles[i]];
                var v2 = vertices[triangles[i + 1]];
                var v3 = vertices[triangles[i + 2]];
                
                // Check if triangle intersects with the slice
                var trianglePoints = new List<Vector3> { v1, v2, v3 };
                var sliceIntersections = GetTriangleSliceIntersection(trianglePoints, mainAxis, sliceStart, sliceEnd);
                intersectionPoints.AddRange(sliceIntersections);
            }
            
            return intersectionPoints;
        }

        private static List<Vector3> GetTriangleSliceIntersection(List<Vector3> triangle, int mainAxis, float sliceStart, float sliceEnd)
        {
            var intersectionPoints = new List<Vector3>();
            
            // Add vertices that fall within the slice
            foreach (var vertex in triangle)
            {
                float axisValue = GetAxisValue(vertex, mainAxis);
                if (axisValue >= sliceStart && axisValue < sliceEnd)
                {
                    intersectionPoints.Add(vertex);
                }
            }
            
            // Find edge intersections with slice boundaries
            for (int i = 0; i < triangle.Count; i++)
            {
                var v1 = triangle[i];
                var v2 = triangle[(i + 1) % triangle.Count];
                
                float v1Axis = GetAxisValue(v1, mainAxis);
                float v2Axis = GetAxisValue(v2, mainAxis);
                
                // Check intersection with slice start
                var startIntersection = GetLineSliceIntersection(v1, v2, v1Axis, v2Axis, sliceStart);
                if (startIntersection.HasValue)
                    intersectionPoints.Add(startIntersection.Value);
                
                // Check intersection with slice end
                var endIntersection = GetLineSliceIntersection(v1, v2, v1Axis, v2Axis, sliceEnd);
                if (endIntersection.HasValue)
                    intersectionPoints.Add(endIntersection.Value);
            }
            
            return intersectionPoints;
        }

        private static Vector3? GetLineSliceIntersection(Vector3 v1, Vector3 v2, float v1Axis, float v2Axis, float slicePosition)
        {
            // Check if the edge crosses the slice plane
            if ((v1Axis <= slicePosition && v2Axis >= slicePosition) || 
                (v1Axis >= slicePosition && v2Axis <= slicePosition))
            {
                if (Mathf.Abs(v2Axis - v1Axis) < 0.001f) return null; // Avoid division by zero
                
                // Linear interpolation to find intersection point
                float t = (slicePosition - v1Axis) / (v2Axis - v1Axis);
                return Vector3.Lerp(v1, v2, t);
            }
            
            return null;
        }

        private static float CalculateSliceWidthFromPoints(List<Vector3> points, int mainAxis)
        {
            if (points.Count == 0) return 0f;
            
            // Remove duplicate points
            var uniquePoints = points.Distinct().ToList();
            if (uniquePoints.Count == 0) return 0f;
            
            // Calculate width in the two perpendicular axes
            var perpValues1 = new List<float>();
            var perpValues2 = new List<float>();
            
            foreach (var point in uniquePoints)
            {
                switch (mainAxis)
                {
                    case 0: // Main axis is X, so measure Y and Z
                        perpValues1.Add(point.y);
                        perpValues2.Add(point.z);
                        break;
                    case 1: // Main axis is Y, so measure X and Z
                        perpValues1.Add(point.x);
                        perpValues2.Add(point.z);
                        break;
                    case 2: // Main axis is Z, so measure X and Y
                        perpValues1.Add(point.x);
                        perpValues2.Add(point.y);
                        break;
                }
            }
            
            float width1 = perpValues1.Count > 0 ? perpValues1.Max() - perpValues1.Min() : 0f;
            float width2 = perpValues2.Count > 0 ? perpValues2.Max() - perpValues2.Min() : 0f;
            
            return Mathf.Max(width1, width2);
        }
        
        private static float GetAxisValue(Vector3 vertex, int axis)
        {
            return axis == 0 ? vertex.x : (axis == 1 ? vertex.y : vertex.z);
        }
        
        private static float InterpolateWidth(List<float> profile, int index)
        {
            if (profile.Count == 0) return 0f;
            
            // Find the last valid width value
            for (int i = index - 1; i >= 0; i--)
            {
                if (profile[i] > 0f)
                    return profile[i];
            }
            
            return 0f;
        }
        
        private static void SmoothProfile(List<float> profile, int smoothingPasses = 1)
        {
            for (int pass = 0; pass < smoothingPasses; pass++)
            {
                var smoothed = new List<float>(profile.Count);
                
                for (int i = 0; i < profile.Count; i++)
                {
                    float sum = profile[i];
                    int count = 1;
                    
                    // Add neighboring values
                    if (i > 0 && profile[i - 1] > 0)
                    {
                        sum += profile[i - 1];
                        count++;
                    }
                    if (i < profile.Count - 1 && profile[i + 1] > 0)
                    {
                        sum += profile[i + 1];
                        count++;
                    }
                    
                    smoothed.Add(sum / count);
                }
                
                profile.Clear();
                profile.AddRange(smoothed);
            }
        }
        
        private static float CalculateSliceWidth(List<Vector3> sliceVertices, int mainAxis)
        {
            if (sliceVertices.Count == 0) return 0f;
            
            // Calculate width in the two perpendicular axes
            var perpValues1 = new List<float>();
            var perpValues2 = new List<float>();
            
            foreach (var vertex in sliceVertices)
            {
                switch (mainAxis)
                {
                    case 0: // Main axis is X, so measure Y and Z
                        perpValues1.Add(vertex.y);
                        perpValues2.Add(vertex.z);
                        break;
                    case 1: // Main axis is Y, so measure X and Z
                        perpValues1.Add(vertex.x);
                        perpValues2.Add(vertex.z);
                        break;
                    case 2: // Main axis is Z, so measure X and Y
                        perpValues1.Add(vertex.x);
                        perpValues2.Add(vertex.y);
                        break;
                }
            }
            
            float width1 = perpValues1.Count > 0 ? perpValues1.Max() - perpValues1.Min() : 0f;
            float width2 = perpValues2.Count > 0 ? perpValues2.Max() - perpValues2.Min() : 0f;
            
            // Return the maximum width (considering the weapon could be rotated around the main axis)
            return Mathf.Max(width1, width2);
        }
        
        private static List<WidthSegment> CreateWidthBasedSegments(Vector3[] vertices, int[] triangles, List<float> widthProfile, 
            WeaponOrientation orientation, float threshold, int minVerticesPerSegment)
        {
            var segments = new List<WidthSegment>();
            
            // Find significant width changes
            var segmentBoundaries = new List<float> { orientation.minValue };
            
            for (int i = 1; i < widthProfile.Count - 1; i++)
            {
                float currentWidth = widthProfile[i];
                float previousWidth = widthProfile[i - 1];
                float nextWidth = widthProfile[i + 1];
                
                if (previousWidth <= 0 || currentWidth <= 0) continue;
                
                // Check for significant width change (shrinking or expanding)
                bool significantChange = false;
                
                float changeRatio = Mathf.Abs(currentWidth - previousWidth) / previousWidth;
                if (changeRatio > threshold)
                {
                    significantChange = true;
                }
                
                // Also check for local minima/maxima (typical at guard/pommel transitions)
                if (currentWidth < previousWidth && currentWidth < nextWidth) // Local minimum
                    significantChange = true;
                if (currentWidth > previousWidth && currentWidth > nextWidth && currentWidth > threshold * 2) // Local maximum
                    significantChange = true;
                
                if (significantChange)
                {
                    float boundary = orientation.minValue + ((float)i / widthProfile.Count) * orientation.length;
                    segmentBoundaries.Add(boundary);
                    
                    Debug.Log($"Found width change at position {boundary:F3}: {previousWidth:F3} -> {currentWidth:F3} (ratio: {changeRatio:F3})");
                }
            }
            
            segmentBoundaries.Add(orientation.maxValue);
            
            // Ensure minimum distance between boundaries
            var filteredBoundaries = FilterCloseSegments(segmentBoundaries, orientation.length * 0.1f);
            
            // Create segments based on boundaries
            for (int i = 0; i < filteredBoundaries.Count - 1; i++)
            {
                var segment = new WidthSegment
                {
                    minHeight = filteredBoundaries[i],
                    maxHeight = filteredBoundaries[i + 1]
                };
    
                // Assign vertices to this segment using triangle-based approach
                var segmentPoints = GetSliceTriangleIntersections(vertices, triangles, orientation.mainAxis, 
                    segment.minHeight, segment.maxHeight);
    
                // Also include original vertices that fall within the segment
                foreach (var vertex in vertices)
                {
                    float axisValue = GetAxisValue(vertex, orientation.mainAxis);
                    if (axisValue >= segment.minHeight && axisValue < segment.maxHeight)
                    {
                        segmentPoints.Add(vertex);
                    }
                }
    
                // Remove duplicates and assign to segment
                segment.vertices = segmentPoints.Distinct().ToList();
    
                // Only keep segments with enough points (lower threshold since we have more points now)
                if (segment.vertices.Count >= minVerticesPerSegment / 3) // Reduced threshold
                {
                    CalculateSegmentProperties(segment, orientation.mainAxis);
                    segments.Add(segment);
                }
            }
            
            return segments;
        }
        
        private static List<float> FilterCloseSegments(List<float> boundaries, float minDistance)
        {
            if (boundaries.Count <= 2) return boundaries;
            
            var filtered = new List<float> { boundaries[0] };
            
            for (int i = 1; i < boundaries.Count - 1; i++)
            {
                if (boundaries[i] - filtered[filtered.Count - 1] >= minDistance)
                {
                    filtered.Add(boundaries[i]);
                }
            }
            
            filtered.Add(boundaries[boundaries.Count - 1]);
            return filtered;
        }
        
        private static void CalculateSegmentProperties(WidthSegment segment, int mainAxis)
        {
            if (segment.vertices.Count == 0) return;
            
            var segmentBounds = CalculateBounds(segment.vertices);
            segment.center = segmentBounds.center;
            segment.size = segmentBounds.size;
            
            // Calculate average width
            segment.averageWidth = CalculateSliceWidth(segment.vertices, mainAxis);
            
            // Determine collider type based on shape
            float height = GetAxisSize(segment.size, mainAxis);
            float maxWidth = GetMaxPerpendicularSize(segment.size, mainAxis);
            
            // Use capsule for long, thin segments (blade), box for wider segments (guard, pommel)
            float aspectRatio = height / (maxWidth + 0.001f); // Avoid division by zero
            segment.suggestedType = aspectRatio > 2.0f ? ColliderType.Capsule : ColliderType.Box;
            
            Debug.Log($"Segment properties: Height={height:F3}, Width={maxWidth:F3}, AspectRatio={aspectRatio:F2}, Type={segment.suggestedType}");
        }
        
        private static float GetAxisSize(Vector3 size, int axis)
        {
            return axis == 0 ? size.x : (axis == 1 ? size.y : size.z);
        }
        
        private static float GetMaxPerpendicularSize(Vector3 size, int mainAxis)
        {
            switch (mainAxis)
            {
                case 0: return Mathf.Max(size.y, size.z);
                case 1: return Mathf.Max(size.x, size.z);
                case 2: return Mathf.Max(size.x, size.y);
                default: return 0f;
            }
        }
        
        private static void CreateCollidersFromSegments(GameObject weaponObj, List<WidthSegment> segments, WeaponOrientation orientation)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                
                // Create a child object for organization
                var segmentObj = new GameObject($"Collider_{i}_{segment.suggestedType}");
                segmentObj.transform.SetParent(weaponObj.transform, false);
                
                if (segment.suggestedType == ColliderType.Capsule)
                {
                    CreateCapsuleCollider(segmentObj, segment, orientation.mainAxis);
                }
                else
                {
                    CreateBoxCollider(segmentObj, segment);
                }
                
                Debug.Log($"Segment {i}: {segment.suggestedType}, Width: {segment.averageWidth:F3}, Height: {segment.maxHeight - segment.minHeight:F3}, Vertices: {segment.vertices.Count}");
            }
        }
        
        private static void CreateBoxCollider(GameObject obj, WidthSegment segment)
        {
            var boxCollider = obj.AddComponent<BoxCollider>();
            boxCollider.center = segment.center;
            boxCollider.size = segment.size;
        }
        
        private static void CreateCapsuleCollider(GameObject obj, WidthSegment segment, int mainAxis)
        {
            var capsuleCollider = obj.AddComponent<CapsuleCollider>();
            
            // Set the capsule direction to match the weapon's main axis
            capsuleCollider.direction = mainAxis;
            capsuleCollider.center = segment.center;
            
            // Set dimensions based on the main axis
            float height = GetAxisSize(segment.size, mainAxis);
            float radius = GetMaxPerpendicularSize(segment.size, mainAxis) * 0.5f;
            
            capsuleCollider.height = height;
            capsuleCollider.radius = radius;
        }
        
        private static Bounds CalculateBounds(List<Vector3> vertices)
        {
            if (vertices.Count == 0) return new Bounds();
            
            var min = vertices[0];
            var max = vertices[0];
            
            foreach (var vertex in vertices)
            {
                min = Vector3.Min(min, vertex);
                max = Vector3.Max(max, vertex);
            }
            
            return new Bounds((min + max) * 0.5f, max - min);
        }
    }
}
