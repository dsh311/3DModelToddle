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
using System.Linq;
using System.Windows.Media.Media3D; //Point3D stuff
using System.Windows.Media; //SolidColorBrush

namespace ModelLoader
{
    internal static class MeshUtils
    {
        internal static void SelectMesh(Model3DGroup sceneGroup,
                                        PerspectiveCamera camera,
                                        Dictionary<int, Material> theSelectedMeshes,
                                        Dictionary<int, Material> theHiddenMeshes,
                                        List<int> xAxisIndexList,
                                        List<int> yAxisIndexList,
                                        List<int> zAxisIndexList,
                                        int meshIndex,
                                        bool drawAxis = false,
                                        bool keepPrevSelections = true)
        {
            if (sceneGroup.Children != null && sceneGroup.Children.Count > meshIndex)
            {
                // Unselect already selected meshes
                if (!keepPrevSelections)
                {
                    UnSelectAllVisibleMeshes(sceneGroup,
                                        theSelectedMeshes,
                                        theHiddenMeshes,
                                        xAxisIndexList,
                                        yAxisIndexList,
                                        zAxisIndexList);
                }

                bool meshStillExists = (sceneGroup.Children.Count - 1) >= meshIndex;
                // Save the current mesh if not already selected
                // Make sure the mesh still exists because its possible it was deleted agove
                if (!theSelectedMeshes.ContainsKey(meshIndex) && meshStillExists)
                {
                    if (sceneGroup.Children[meshIndex] is GeometryModel3D geometryModel)
                    {
                        // Save the mesh so we can restore it later
                        theSelectedMeshes[meshIndex] = geometryModel.Material;

                        // Set the selected mesh material to blue and transparent
                        Material materialColorized = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(128, 0, 255, 255)));
                        (sceneGroup.Children[meshIndex] as GeometryModel3D).Material = materialColorized;
                    }
                }

                // When the 'move' tool, draw the axis if not drawn
                // This must come after since the above might add the first selected mesh
                //if (_theTool == SelectedTool.move)
                if (drawAxis)
                {
                    AxisUtils.DrawAxisLinesFromSelectedMeshes(sceneGroup,
                                                    camera,
                                                    theSelectedMeshes,
                                                    xAxisIndexList,
                                                    yAxisIndexList,
                                                    zAxisIndexList);
                }
            }
        }

        internal static void UpdateMeshPositions(MeshGeometry3D mesh, double deltaX, double deltaY, double deltaZ)
        {
            for (int i = 0; i < mesh.Positions.Count; i++)
            {
                // Get the current position
                Point3D currentPosition = mesh.Positions[i];

                // Update the X coordinate by adding deltaX
                Point3D updatedPosition = new Point3D(currentPosition.X + deltaX, currentPosition.Y + deltaY, currentPosition.Z + deltaZ);

                // Assign the updated position back to the Positions collection
                mesh.Positions[i] = updatedPosition;
            }
        }

        internal static void MoveMeshIndexlist(List<int> meshIndexList,
                                    Model3DGroup sceneGroup,
                                    double finalX,
                                    double finalY,
                                    double finalZ)
        {
            // Check if draggable x axis was hit
            foreach (int curIndex in meshIndexList)
            {
                if (sceneGroup.Children[curIndex] is GeometryModel3D theModel)
                {
                    MeshGeometry3D mesh = (MeshGeometry3D)theModel.Geometry;
                    if (mesh != null)
                    {
                        MeshUtils.UpdateMeshPositions(mesh, finalX, finalY, finalZ);
                    }
                }
            }
        }

        internal static void MoveSelectionOnAxis(Model3DGroup sceneGroup,
                        PerspectiveCamera camera,
                        Point3D center,
                        double deltaX,
                        double deltaY,
                        Dictionary<int, Material> theSelectedMeshes,
                        Axis draggingAxis,
                        List<int> xAxisIndexList,
                        List<int> yAxisIndexList,
                        List<int> zAxisIndexList)
        {
            double percentageOfViewportForAxisLength = 0.20;
            double axisLength = AxisUtils.CalculateAxisLength(camera, center, percentageOfViewportForAxisLength);
            double desiredPercentOfAxisLength = 0.00005;
            double axisLengthPercentage = axisLength * desiredPercentOfAxisLength;

            double finalX = (draggingAxis == Axis.X) ? deltaX * axisLengthPercentage : 0;
            double finalY = (draggingAxis == Axis.Y) ? deltaY * axisLengthPercentage : 0;
            double finalZ = (draggingAxis == Axis.Z) ? deltaX * axisLengthPercentage : 0;


            // Adjust the direction the model moves based on the camera position
            // This ensure the drag works when on the right of left side of an axis
            bool cameraIsPositiveX = camera.Position.X - center.X >= 0;
            bool cameraIsPositiveZ = camera.Position.Z - center.Z >= 0;
            finalX = cameraIsPositiveZ ? -1 * finalX : finalX;
            finalZ = cameraIsPositiveX ? finalZ : -1 * finalZ;


            // Update selected meshes
            foreach (int key in theSelectedMeshes.Keys)
            {
                if (sceneGroup.Children[key] is GeometryModel3D theModel)
                {
                    MeshGeometry3D mesh = (MeshGeometry3D)theModel.Geometry;
                    if (mesh != null)
                    {
                        MeshUtils.UpdateMeshPositions(mesh, finalX, finalY, finalZ);
                    }
                }
            }

            // Update the X,Y, Z axis position
            MeshUtils.MoveMeshIndexlist(xAxisIndexList, sceneGroup, finalX, finalY, finalZ);
            MeshUtils.MoveMeshIndexlist(yAxisIndexList, sceneGroup, finalX, finalY, finalZ);
            MeshUtils.MoveMeshIndexlist(zAxisIndexList, sceneGroup, finalX, finalY, finalZ);
        }

        internal static (Point3D center, Point3D min, Point3D max) FindCenterOfMeshes(IEnumerable<MeshGeometry3D> meshes)
        {
            if (meshes == null || !meshes.Any())
                throw new ArgumentException("No meshes provided");

            double totalX = 0, totalY = 0, totalZ = 0;
            int vertexCount = 0;

            // Initialize min and max values
            double minX = double.MaxValue, minY = double.MaxValue, minZ = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue, maxZ = double.MinValue;

            foreach (var mesh in meshes)
            {
                for (int i = 0; i < mesh.Positions.Count; i++)
                {
                    var position = mesh.Positions[i];
                    totalX += position.X;
                    totalY += position.Y;
                    totalZ += position.Z;
                    vertexCount++;

                    // Update min and max values
                    if (position.X < minX) minX = position.X;
                    if (position.Y < minY) minY = position.Y;
                    if (position.Z < minZ) minZ = position.Z;
                    if (position.X > maxX) maxX = position.X;
                    if (position.Y > maxY) maxY = position.Y;
                    if (position.Z > maxZ) maxZ = position.Z;
                }
            }

            // Calculate the center point
            Point3D center = new Point3D(totalX / vertexCount, totalY / vertexCount, totalZ / vertexCount);

            // Create Point3D for min and max values
            Point3D minPoint = new Point3D(minX, minY, minZ);
            Point3D maxPoint = new Point3D(maxX, maxY, maxZ);

            return (center, minPoint, maxPoint);
        }

        internal static bool FindCenterOfSelectedMeshes(Model3DGroup sceneGroup,
                                    Dictionary<int, Material> theSelectedMeshes,
                                    out Point3D center,
                                    out Point3D min,
                                    out Point3D max)
        {
            center = new Point3D(0, 0, 0);
            min = new Point3D(0, 0, 0);
            max = new Point3D(0, 0, 0);
            if (theSelectedMeshes.Count == 0) { return false; }

            List<MeshGeometry3D> lastSelectedMeshes = new List<MeshGeometry3D>();
            foreach (int key in theSelectedMeshes.Keys)
            {
                if (sceneGroup.Children[key] is GeometryModel3D theModel)
                {
                    MeshGeometry3D mesh = (MeshGeometry3D)theModel.Geometry;
                    if (mesh != null)
                    {
                        lastSelectedMeshes.Add(mesh);
                    }
                }
            }

            (center, min, max) = FindCenterOfMeshes(lastSelectedMeshes);
            return true;
        }

        internal static void RotateMeshesAroundCenter(IEnumerable<MeshGeometry3D> meshes, Vector3D axis, double angleInDegrees)
        {
            (Point3D center, Point3D min, Point3D max) = MeshUtils.FindCenterOfMeshes(meshes);

            // Create the rotation around the Y-axis
            AxisAngleRotation3D axisAngleRotationY = new AxisAngleRotation3D(axis, angleInDegrees);
            RotateTransform3D rotateTransformY = new RotateTransform3D(axisAngleRotationY);

            foreach (var mesh in meshes)
            {
                for (int i = 0; i < mesh.Positions.Count; i++)
                {
                    // Translate the point to the origin
                    Point3D point = mesh.Positions[i];
                    Point3D translated = new Point3D(point.X - center.X, point.Y - center.Y, point.Z - center.Z);

                    // Use RotateTransform3D to apply rotation
                    Point3D rotatedPoint = rotateTransformY.Transform(translated);

                    // Translate back to the original position
                    mesh.Positions[i] = new Point3D(rotatedPoint.X + center.X, rotatedPoint.Y + center.Y, rotatedPoint.Z + center.Z);
                }
            }
        }

        internal static void RotateSelectedMeshes(List<List<int>> rotationList, Model3DGroup sceneGroup, PerspectiveCamera camera, DirectionalLight cameraLight, Vector3D axis, double deltaX, double deltaY)
        {
            // Rotate all the saved meshes
            foreach (var curListToRotate in rotationList)
            {
                if (curListToRotate.Count == 0) { continue; }

                List<MeshGeometry3D> meshes = new List<MeshGeometry3D>();
                foreach (int key in curListToRotate)
                {
                    if (sceneGroup.Children[key] is GeometryModel3D theModel)
                    {
                        MeshGeometry3D mesh = (MeshGeometry3D)theModel.Geometry;
                        if (mesh != null)
                        {
                            meshes.Add(mesh);
                        }
                    }
                }
                // At this point we have all the meshes from the selection
                MeshUtils.RotateMeshesAroundCenter(meshes, axis, 10);
            }
        }

        internal static (double, double, Point3D, Point3D) CountVertsAndTriangles(Model3DGroup sceneGroup)
        {
            double trianglCount = 0;
            double vertCount = 0;

            double maxX = 0.0, maxY = 0.0, maxZ = 0.0;
            double minX = 0.0, minY = 0.0, minZ = 0.0;

            for (int i = sceneGroup.Children.Count - 1; i >= 0; i--)
            {
                if (sceneGroup.Children[i] is GeometryModel3D theModel)
                {
                    MeshGeometry3D mesh = (MeshGeometry3D)theModel.Geometry;
                    if (mesh != null)
                    {
                        trianglCount += (mesh.TriangleIndices.Count());
                        vertCount += mesh.Positions.Count();

                        IEnumerable<MeshGeometry3D> meshCollection = new List<MeshGeometry3D> { mesh };
                        (Point3D center, Point3D min, Point3D max) = MeshUtils.FindCenterOfMeshes(meshCollection);

                        if (max.X > maxX) { maxX = max.X; }
                        if (max.Y > maxY) { maxY = max.Y; }
                        if (max.Z > maxZ) { maxZ = max.Z; }

                        if (min.X < minX) { minX = min.X; }
                        if (min.Y < minY) { minY = min.Y; }
                        if (min.Z < minZ) { minZ = min.Z; }
                    }

                }
            }

            trianglCount = trianglCount / 3;
            Point3D maxValues = new Point3D(maxX, maxY, maxZ);
            Point3D minValues = new Point3D(minX, minY, minZ);
            return (vertCount, trianglCount, maxValues, minValues);
        }

        internal static void HideSelectedMeshes(Model3DGroup sceneGroup,
                                        Dictionary<int, Material> theSelectedMeshes,
                                        Dictionary<int, Material> theHiddenMeshes)
        {
            foreach (int key in theSelectedMeshes.Keys)
            {
                if (sceneGroup.Children[key] is GeometryModel3D theModel)
                {
                    MeshGeometry3D mesh = (MeshGeometry3D)theModel.Geometry;
                    if (mesh != null)
                    {
                        // Save the hidden mesh so we can restore it later
                        theHiddenMeshes[key] = (sceneGroup.Children[key] as GeometryModel3D).Material;

                        theModel.Material = null; //Hide by setting material to null
                    }
                }
            }
        }

        internal static void HideAllUnSelectedMeshes(Model3DGroup sceneGroup,
                                        Dictionary<int, Material> theSelectedMeshes,
                                        Dictionary<int, Material> theHiddenMeshes)
        {
            for (int i = sceneGroup.Children.Count - 1; i >= 0; i--)
            {
                if (sceneGroup.Children[i] is GeometryModel3D theModel)
                {
                    if (!theSelectedMeshes.ContainsKey(i))
                    {
                        MeshGeometry3D mesh = (MeshGeometry3D)theModel.Geometry;
                        if (mesh != null)
                        {
                            // Save the hidden mesh so we can restore it later
                            theHiddenMeshes[i] = (sceneGroup.Children[i] as GeometryModel3D).Material;
                            theModel.Material = null;
                        }
                    }
                }
            }
        }

        internal static void UnSelectAllVisibleMeshes(Model3DGroup sceneGroup,
                                        Dictionary<int, Material> theSelectedMeshes,
                                        Dictionary<int, Material> theHiddenMeshes,
                                        List<int> xAxisIndexList,
                                        List<int> yAxisIndexList,
                                        List<int> zAxisIndexList)
        {
            foreach (int key in theSelectedMeshes.Keys)
            {
                //Only restore selected meshes if not hidden
                if (!theHiddenMeshes.ContainsKey(key))
                {
                    Material material = theSelectedMeshes[key];
                    (sceneGroup.Children[key] as GeometryModel3D).Material = material;
                }
            }
            // Clear them now that we have restored them all
            theSelectedMeshes.Clear();

            // Clear so that if user selects something with 'move' tool, it will draw axis
            // Always clear the last to the first
            AxisUtils.ClearAxisFromMove(sceneGroup,
                            zAxisIndexList,
                            yAxisIndexList,
                            xAxisIndexList);
        }

    }
}
