/*
 * Copyright (C) 2024 David Shelley <davidsmithshelley@gmail.com>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License 
 * along with this program. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D; //Point3D stuff
using System.Windows.Media; //SolidColorBrush

namespace ModelLoader
{
    internal static class CollisionUtils
    {
        internal static (List<GeometryModel3D>, Vector3D) GetRayFromViewport(
                                    System.Windows.Point mousePosition,
                                    PerspectiveCamera camera,
                                    double viewportWidth,
                                    double viewportHeight)
        {
            // Get the camera's position and look direction
            Point3D cameraPosition = camera.Position;
            Vector3D cameraLookDirection = camera.LookDirection;
            cameraLookDirection.Normalize();

            double xPercentage = (mousePosition.X / viewportWidth);
            double yPercentage = (mousePosition.Y / viewportHeight);

            double nearPlaneDistance = camera.NearPlaneDistance;

            double fovHorzRadians = camera.FieldOfView * (Math.PI / 180.0);

            double halfWidth = Math.Tan(fovHorzRadians / 2.0) * nearPlaneDistance;
            double wholeWidth = halfWidth * 2.0;

            // The width of the near clipping plane is calculated based on the
            // height(which is determined by the FieldOfView) and the aspect ratio of the
            // viewport(i.e., ActualWidth / ActualHeight).
            //double aspectRatio = viewportWidth / viewportHeight;
            //double wholeHeight = wholeWidth / aspectRatio;
            double wholeHeight = (wholeWidth * viewportHeight) / viewportWidth;
            double halfHeight = wholeHeight * 0.5;

            Vector3D cameraRightDirection = Vector3D.CrossProduct(cameraLookDirection, camera.UpDirection);
            cameraRightDirection.Normalize();
            Vector3D cameraUpDirection = Vector3D.CrossProduct(cameraRightDirection, cameraLookDirection);
            cameraUpDirection.Normalize();

            // Near plane center point in 3D space (along the camera's look direction)
            Point3D nearPlaneCenter = cameraPosition + (cameraLookDirection * nearPlaneDistance);

            double leftRightDistFromCenter = (xPercentage * wholeWidth) - halfWidth;
            double upDownDistanceFromCenter = halfHeight - (yPercentage * wholeHeight);

            // Step 5: Offset the near plane center using the normalizedX and normalizedY
            Point3D clickPointOnNearPlane = nearPlaneCenter
                + (cameraRightDirection * leftRightDistFromCenter)
                + (cameraUpDirection * upDownDistanceFromCenter);

            Vector3D rayVector = clickPointOnNearPlane - cameraPosition;
            rayVector.Normalize();
            Point3D aLineUsingRayVector = cameraPosition + (rayVector * 20);

            List<GeometryModel3D> theLines = new List<GeometryModel3D>();

            theLines.Add(GeometryUtils.CreateLine(aLineUsingRayVector, cameraPosition, 0.05, Colors.Red));

            return (theLines, rayVector);
        }


        // Implements the Möller-Trumbore algorithm for ray-triangle intersection.
        internal static bool RayIntersectsTriangle(Point3D rayOrigin,
                                                Vector3D rayDirection,
                                                Point3D vertex1,
                                                Point3D vertex2,
                                                Point3D vertex3,
                                                out Point3D intersectionPoint)
        {
            intersectionPoint = new Point3D();

            // Normalize the ray direction to avoid precision issues
            rayDirection.Normalize();

            const double EPSILON = 1e-8;

            // Find vectors for two edges sharing vertex1
            Vector3D edge1 = vertex2 - vertex1;
            Vector3D edge2 = vertex3 - vertex1;

            // Begin calculating determinant - also used to calculate U parameter
            Vector3D pvec = Vector3D.CrossProduct(rayDirection, edge2);

            // If determinant is near 0, ray lies in plane of triangle
            double det = Vector3D.DotProduct(edge1, pvec);

            if (det > -EPSILON && det < EPSILON)
            {
                return false;
            }

            double invDet = 1.0 / det;

            // Calculate distance from vertex1 to ray origin
            Vector3D tvec = rayOrigin - vertex1;

            // Calculate U parameter and test bounds
            double u = Vector3D.DotProduct(tvec, pvec) * invDet;
            if (u < 0 || u > 1)
            {
                return false;
            }

            // Prepare to test V parameter
            Vector3D qvec = Vector3D.CrossProduct(tvec, edge1);

            // Calculate V parameter and test bounds
            double v = Vector3D.DotProduct(rayDirection, qvec) * invDet;
            if (v < 0 || u + v > 1)
            {
                return false;
            }

            // Calculate t, the distance along the ray to the intersection point
            double t = Vector3D.DotProduct(edge2, qvec) * invDet;

            // If t is less than 0, the intersection is behind the ray origin
            if (t < 0)
            {
                return false;
            }

            // Calculate the intersection point
            intersectionPoint = rayOrigin + t * rayDirection;
            return true;
        }

        internal static bool GetClosestVisibleHitGivenRay(GeometryModel3D theModel,
                                Vector3D rayDirection,
                                PerspectiveCamera camera,
                                out Point3D intersectionPoint)
        {
            bool hitSomething = false;
            double closestHitDistance = 0.0;

            MeshGeometry3D mesh = (MeshGeometry3D)theModel.Geometry;
            if (mesh != null)
            {
                Point3DCollection positions = mesh.Positions;
                Int32Collection triangles = mesh.TriangleIndices;

                for (int j = 0; j < triangles.Count; j += 3)
                {
                    // Get the three vertices of the triangle
                    Point3D vertex1 = positions[triangles[j]];
                    Point3D vertex2 = positions[triangles[j + 1]];
                    Point3D vertex3 = positions[triangles[j + 2]];

                    // Check for intersection
                    bool intersected = RayIntersectsTriangle(
                        camera.Position,       // Origin of the ray
                        rayDirection,          // Direction of the ray
                        vertex1,               // First vertex of the triangle
                        vertex2,               // Second vertex of the triangle
                        vertex3,               // Third vertex of the triangle
                        out Point3D curIntersectionPoint  // Output intersection point
                    );

                    if (intersected)
                    {
                        Vector3D differenceVector = camera.Position - curIntersectionPoint;
                        double curDistance = differenceVector.Length;

                        //If this is our first hit
                        if (!hitSomething)
                        {
                            closestHitDistance = curDistance;
                            hitSomething = true;
                            intersectionPoint = curIntersectionPoint;
                        }
                        else if (curDistance < closestHitDistance)
                        {
                            closestHitDistance = curDistance;
                            intersectionPoint = curIntersectionPoint;
                        }
                    }
                }
            }

            return hitSomething;
        }

        internal static bool GetClosestVisibleHitGivenRay(Model3DGroup sceneGroup,
                                Dictionary<int, Material> theHiddenMeshes,
                                Vector3D rayDirection,
                                PerspectiveCamera camera,
                                out Point3D intersectionPoint,
                                out int childIndex)
        {

            bool hitSomething = false;
            childIndex = -1;
            double closestHitDistance = 0.0;

            for (int i = sceneGroup.Children.Count - 1; i >= 0; i--)
            {
                if (sceneGroup.Children[i] is GeometryModel3D theModel)
                {
                    //Skip if mesh index is hidden
                    bool hitIsVisible = theHiddenMeshes != null ? !theHiddenMeshes.ContainsKey(i) : false;
                    if (!hitIsVisible)
                    {
                        continue;
                    }


                    if (theModel != null)
                    {
                        bool intersected = GetClosestVisibleHitGivenRay(
                                        theModel,
                                        rayDirection,
                                        camera,
                                        out Point3D tempIntersectionPoint);
                        if (intersected)
                        {
                            Vector3D differenceVector = camera.Position - tempIntersectionPoint;
                            double distance = differenceVector.Length;

                            //If this is our first hit
                            if (!hitSomething)
                            {
                                closestHitDistance = distance;
                                intersectionPoint = tempIntersectionPoint;
                                childIndex = i;
                                hitSomething = true;
                            }
                            else if (distance < closestHitDistance)
                            {
                                intersectionPoint = tempIntersectionPoint;
                                closestHitDistance = distance;
                                childIndex = i;
                            }
                        }
                    }
                }
            }

            return hitSomething;
        }


        internal static bool CheckAxisMeshIndexForHit(List<int> meshIndexList,
                                Model3DGroup sceneGroup,
                                Vector3D rayDirection,
                                PerspectiveCamera camera)
        {
            //Check if draggable x axis was hit
            foreach (int curIndex in meshIndexList)
            {
                if (sceneGroup.Children[curIndex] is GeometryModel3D theModel)
                {
                    bool hitAxis = CollisionUtils.GetClosestVisibleHitGivenRay(theModel,
                                        rayDirection,
                                        camera,
                                        out Point3D intersectionPoint);
                    if (hitAxis)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        internal static Axis GetHitAxisGivenRay(Model3DGroup sceneGroup,
                        Vector3D rayDirection,
                        PerspectiveCamera camera,
                        List<int> theXAxisMeshIndexList,
                        List<int> theYAxisMeshIndexList,
                        List<int> theZAxisMeshIndexList)
        {
            if (CollisionUtils.CheckAxisMeshIndexForHit(theXAxisMeshIndexList,
                                        sceneGroup,
                                        rayDirection,
                                        camera))
            {
                return Axis.X;
            }

            if (CollisionUtils.CheckAxisMeshIndexForHit(theYAxisMeshIndexList,
                            sceneGroup,
                            rayDirection,
                            camera))
            {
                return Axis.Y;
            }

            if (CollisionUtils.CheckAxisMeshIndexForHit(theZAxisMeshIndexList,
                sceneGroup,
                rayDirection,
                camera))
            {
                return Axis.Z;
            }


            return Axis.None;
        }

    }
}
