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
using System.Windows.Media; //Colors

namespace ModelLoader
{
    public enum Axis
    {
        None,
        X,
        Y,
        Z
    }

    internal static class AxisUtils
    {
        internal static bool MeshIndexIsAnAxis(int checkThisIndex,
                                                List<int> xAxisIndexList,
                                                List<int> yAxisIndexList,
                                                List<int> zAxisIndexList)
        {
            bool hitXAxis = xAxisIndexList.Contains(checkThisIndex);
            bool hitYAxis = yAxisIndexList.Contains(checkThisIndex);
            bool hitZAxis = zAxisIndexList.Contains(checkThisIndex);
            return (hitXAxis || hitYAxis || hitZAxis);
        }

        internal static double CalculateAxisLength(PerspectiveCamera camera,
                                                    Point3D objectCenterPosition,
                                                    double scalingFactor)
        {
            // Get viewport width in 3D space
            double nearPlaneDistance = camera.NearPlaneDistance;
            double fovHorzRadians = camera.FieldOfView * (Math.PI / 180.0);
            double halfWidth = Math.Tan(fovHorzRadians / 2.0) * nearPlaneDistance;
            double wholeViewPortWidth = halfWidth * 2.0;

            Vector3D vectorTowardsObject = camera.Position - objectCenterPosition;
            double distanceToObjCenter = vectorTowardsObject.Length;
            double desiredViewPortPercentDist = (wholeViewPortWidth * scalingFactor);
            double axisLength = (distanceToObjCenter * desiredViewPortPercentDist) / nearPlaneDistance;
            return axisLength;
        }


        internal static void ClearAxisFromScene(Model3DGroup sceneGroup, List<int> clearMe)
        {
            for (int i = clearMe.Count - 1; i >= 0; i--)
            {
                int anAxisMeshIndex = clearMe[i];
                if (anAxisMeshIndex >= 0 && anAxisMeshIndex < sceneGroup.Children.Count)
                {
                    sceneGroup.Children.RemoveAt(anAxisMeshIndex);
                }
            }
            clearMe.Clear();
        }

        internal static void ClearAxisFromMove(Model3DGroup sceneGroup,
            List<int> clearnMeZAxis,
            List<int> clearnMeYAxis,
            List<int> clearnMeXAxis)
        {
            AxisUtils.ClearAxisFromScene(sceneGroup, clearnMeZAxis);
            AxisUtils.ClearAxisFromScene(sceneGroup, clearnMeYAxis);
            AxisUtils.ClearAxisFromScene(sceneGroup, clearnMeXAxis);

        }

        internal static void DrawAxisLinesFromSelectedMeshes(Model3DGroup sceneGroup,
                                                    PerspectiveCamera camera,
                                                    Dictionary<int, Material> theSelectedMeshes,
                                                    List<int> xAxisIndexList,
                                                    List<int> yAxisIndexList,
                                                    List<int> zAxisIndexList)
        {
            // Clear the old lines
            ClearAxisFromMove(sceneGroup,
                                zAxisIndexList,
                                yAxisIndexList,
                                xAxisIndexList);
            if (theSelectedMeshes.Count == 0) { return; }

            // Find the center of the selected meshes
            bool foundCenter = MeshUtils.FindCenterOfSelectedMeshes(sceneGroup,
                                                        theSelectedMeshes,
                                                        out Point3D center,
                                                        out Point3D min,
                                                        out Point3D max);

            if (!foundCenter) { return; }

            double percentageOfViewportForAxisLength = 0.20;
            double axisLength = AxisUtils.CalculateAxisLength(camera, center, percentageOfViewportForAxisLength);
            double axisTipLength = axisLength * 0.2;

            // Make the thickness a percentage of the length
            double lineThicknessFinal = axisLength * .03;
            double tipLineThicknessFinal = lineThicknessFinal * 2;

            // X axis
            GeometryModel3D xAxisLine = GeometryUtils.CreateLine(center,
                new Point3D(center.X + axisLength, center.Y, center.Z),
                lineThicknessFinal,
                Colors.Red);
            // Draw the tip of the line
            GeometryModel3D xAxisTipLine = GeometryUtils.CreateLine(new Point3D(center.X + axisLength, center.Y, center.Z),
                new Point3D(center.X + axisLength + axisTipLength, center.Y, center.Z),
                tipLineThicknessFinal,
                Colors.Red, true);

            // Y axis
            GeometryModel3D yAxisLine = GeometryUtils.CreateLine(center,
                new Point3D(center.X, center.Y + axisLength, center.Z),
                lineThicknessFinal,
                Colors.Green);
            // Draw the tip of the line
            GeometryModel3D yAxisTipLine = GeometryUtils.CreateLine(new Point3D(center.X, center.Y + axisLength, center.Z),
                new Point3D(center.X, center.Y + axisLength + axisTipLength, center.Z),
                tipLineThicknessFinal,
                Colors.Green, true);

            // Z axis
            GeometryModel3D zAxisLine = GeometryUtils.CreateLine(center,
                new Point3D(center.X, center.Y, center.Z + axisLength),
                lineThicknessFinal,
                Colors.Blue);
            // Draw the tip of the line
            GeometryModel3D zAxisTipLine = GeometryUtils.CreateLine(new Point3D(center.X, center.Y, center.Z + axisLength),
                new Point3D(center.X, center.Y, center.Z + axisLength + axisTipLength),
                tipLineThicknessFinal,
                Colors.Blue, true);

            // Add lines and line tips to scene groupt
            sceneGroup.Children.Add(xAxisLine);
            int xAxisMeshIndex = sceneGroup.Children.Count - 1;
            xAxisIndexList.Add(xAxisMeshIndex);

            sceneGroup.Children.Add(xAxisTipLine);
            int xAxisTipMeshIndex = sceneGroup.Children.Count - 1;
            xAxisIndexList.Add(xAxisTipMeshIndex);

            // Y Axis
            sceneGroup.Children.Add(yAxisLine);
            int yAxisMeshIndex = sceneGroup.Children.Count - 1;
            yAxisIndexList.Add(yAxisMeshIndex);

            sceneGroup.Children.Add(yAxisTipLine);
            int yAxisTipMeshIndex = sceneGroup.Children.Count - 1;
            yAxisIndexList.Add(yAxisTipMeshIndex);

            // Z Axis
            sceneGroup.Children.Add(zAxisLine);
            int zAxisMeshIndex = sceneGroup.Children.Count - 1;
            zAxisIndexList.Add(zAxisMeshIndex);

            sceneGroup.Children.Add(zAxisTipLine);
            int zAxisTipMeshIndex = sceneGroup.Children.Count - 1;
            zAxisIndexList.Add(zAxisTipMeshIndex);

        }


    }
}
