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

namespace ModelLoader
{
    public enum SelectedTool
    {
        select,
        move
    }

    public class SceneCollection
    {
        public Dictionary<int, Material> _selectedMeshes;
        Dictionary<int, Material> _hiddenMeshes;

        List<List<int>> _listOfHorzMeshIndexLists;
        List<List<int>> _listOfVertMeshIndexLists;

        List<int> _xAxisMeshIndexList;
        List<int> _yAxisMeshIndexList;
        List<int> _zAxisMeshIndexList;

        public SelectedTool _theTool;

        public SceneCollection()
        {
            _selectedMeshes = new Dictionary<int, Material>();
            _hiddenMeshes = new Dictionary<int, Material>();

            _listOfHorzMeshIndexLists = new List<List<int>>();
            _listOfVertMeshIndexLists = new List<List<int>>();

            _xAxisMeshIndexList = new List<int>();
            _yAxisMeshIndexList = new List<int>();
            _zAxisMeshIndexList = new List<int>();

            _theTool = SelectedTool.select;
        }

        public IGeometryLoaderResult LoadFromFile(string filePath)
        {
            IModelLoader loader = null;

            if (filePath.EndsWith(".obj"))
            {
                loader = new ObjLoader();
            }
            else if (filePath.EndsWith(".stl"))
            {
                loader = new STLLoader();
            }
            else if (filePath.EndsWith(".x3d"))
            {
                loader = new X3DLoader();
            }

            return loader?.LoadFile(filePath) ?? new GeometryLoaderResult(null, null, null);
        }

        public void ChooseTool(SelectedTool chosenTool)
        {
            _theTool = chosenTool;
        }

        public void ClearHorzRotationLists()
        {
            _listOfHorzMeshIndexLists.Clear();
        }

        public void ClearVertRotationLists()
        {
            _listOfVertMeshIndexLists.Clear();
        }

        public bool MeshIndexIsAnAxis(int checkThisIndex)
        {
            return AxisUtils.MeshIndexIsAnAxis(checkThisIndex,
                                    _xAxisMeshIndexList,
                                    _yAxisMeshIndexList,
                                    _zAxisMeshIndexList);
        }

        public (double, double, Point3D, Point3D) CountVertsAndTriangles(Model3DGroup sceneGroup)
        {
            return MeshUtils.CountVertsAndTriangles(sceneGroup);
        }

        public void HideSelectedMeshes(Model3DGroup sceneGroup)
        {
            MeshUtils.HideSelectedMeshes(sceneGroup,
                                        _selectedMeshes,
                                        _hiddenMeshes);
        }


        // Isolate the selected meshes
        public void HideAllUnSelectedMeshes(Model3DGroup sceneGroup)
        {
            MeshUtils.HideAllUnSelectedMeshes(sceneGroup,
                                            _selectedMeshes,
                                            _hiddenMeshes);
        }

        public void ClearAxisFromMove(Model3DGroup sceneGroup)
        {
            AxisUtils.ClearAxisFromMove(sceneGroup,
                                        _zAxisMeshIndexList,
                                        _yAxisMeshIndexList,
                                        _xAxisMeshIndexList);
        }
        public void UnSelectAllVisibleMeshes(Model3DGroup sceneGroup)
        {
            MeshUtils.UnSelectAllVisibleMeshes(sceneGroup,
                                                _selectedMeshes,
                                                _hiddenMeshes,
                                                _xAxisMeshIndexList,
                                                _yAxisMeshIndexList,
                                                _zAxisMeshIndexList);

        }

        public bool FindCenterOfSelectedMeshes(Model3DGroup sceneGroup,
                                                out Point3D center,
                                                out Point3D min,
                                                out Point3D max)
        {
            return MeshUtils.FindCenterOfSelectedMeshes(sceneGroup,
                                                        _selectedMeshes,
                                                        out center,
                                                        out min,
                                                        out max);
        }

        public void SelectMesh(Model3DGroup sceneGroup,
                                PerspectiveCamera camera, 
                                int meshIndex,
                                bool keepPrevSelections = true)
        {
            MeshUtils.SelectMesh(sceneGroup,
                                camera,
                                _selectedMeshes,
                                _hiddenMeshes,
                                _xAxisMeshIndexList,
                                _yAxisMeshIndexList,
                                _zAxisMeshIndexList,
                                meshIndex,
                                (_theTool == SelectedTool.move),
                                keepPrevSelections);
        }

        public Axis GetHitAxisGivenRay(Model3DGroup sceneGroup,
                                Vector3D rayDirection,
                                PerspectiveCamera camera)
        {

            return CollisionUtils.GetHitAxisGivenRay(sceneGroup,
                   rayDirection,
                   camera,
                   _xAxisMeshIndexList,
                   _yAxisMeshIndexList,
                   _zAxisMeshIndexList);
        }

        public bool GetClosestVisibleHitGivenRay(Model3DGroup sceneGroup,
                                        Vector3D rayDirection,
                                        PerspectiveCamera camera,
                                        out Point3D intersectionPoint,
                                        out int childIndex)
        {

            return CollisionUtils.GetClosestVisibleHitGivenRay(sceneGroup,
                _hiddenMeshes,
                rayDirection,
                camera,
                out intersectionPoint,
                out childIndex);
        }

        public static (List<GeometryModel3D>, Vector3D) GetRayFromViewport(
                                            System.Windows.Point mousePosition,
                                            PerspectiveCamera camera,
                                            double viewportWidth,
                                            double viewportHeight)
        {
            return CollisionUtils.GetRayFromViewport(mousePosition,
                                                    camera,
                                                    viewportWidth,
                                                    viewportHeight);
        }

        public void DrawAxisLinesFromSelectedMeshes(Model3DGroup sceneGroup,
            PerspectiveCamera camera)
        {
            AxisUtils.DrawAxisLinesFromSelectedMeshes(sceneGroup,
                                         camera,
                                         _selectedMeshes,
                                        _xAxisMeshIndexList,
                                        _yAxisMeshIndexList,
                                        _zAxisMeshIndexList);
        }

        public static void MoveCameraLeft(PerspectiveCamera camera, double distance)
        {
            CameraUtils.MoveCameraLeft(camera, distance);
        }

        public static void MoveCameraUp(PerspectiveCamera camera, double distance)
        {
            CameraUtils.MoveCameraUp(camera, distance);
        }


        public void MoveCameraForward(Model3DGroup sceneGroup,
                                        PerspectiveCamera camera,
                                        int delta)
        {
            CameraUtils.MoveCameraForward(sceneGroup, camera, delta);

            if (_theTool == SelectedTool.move)
            {
                // Only add the axis if its not added already
                if (_xAxisMeshIndexList.Count != 0)
                {
                    DrawAxisLinesFromSelectedMeshes(sceneGroup, camera);
                }
            }
        }

        public static void RotateCamera(PerspectiveCamera camera,
                                        DirectionalLight cameraLight,
                                        double deltaX,
                                        double deltaY)
        {
            CameraUtils.RotateCamera(camera, cameraLight, deltaX, deltaY);
        }

        public void MoveSelectionToHorzRotationList(Model3DGroup sceneGroup)
        {
            MoveSelectionToRotationList(_listOfHorzMeshIndexLists, sceneGroup);
        }

        public void MoveSelectionToVertRotationList(Model3DGroup sceneGroup)
        {
            MoveSelectionToRotationList(_listOfVertMeshIndexLists, sceneGroup);
        }

        public void RotateSelectionOrCameraHorz(Model3DGroup sceneGroup,
                                                PerspectiveCamera camera,
                                                DirectionalLight cameraLight,
                                                double deltaX,
                                                double deltaY)
        {
            if (_listOfHorzMeshIndexLists.Count == 0 && _selectedMeshes.Count == 0)
            {
                CameraUtils.RotateCamera(camera, cameraLight, -1.0, 0.0);
            }
            else
            {
                // Rotate the selected meshes around their center axis
                Vector3D yAxis = new Vector3D(0, 1, 0);
                MeshUtils.RotateSelectedMeshes(_listOfHorzMeshIndexLists,
                                                sceneGroup,
                                                camera,
                                                cameraLight,
                                                yAxis,
                                                -1.0,
                                                0.0);
            }
        }

        public void RotateSelectionOrCameraVert(Model3DGroup sceneGroup,
                                                PerspectiveCamera camera,
                                                DirectionalLight cameraLight,
                                                double deltaX,
                                                double deltaY)
        {
            if (_listOfVertMeshIndexLists.Count == 0 && _selectedMeshes.Count == 0)
            {
                CameraUtils.RotateCamera(camera, cameraLight, 0.0, -1.0);
            }
            else
            {
                // Rotate the selected meshes around their center axis
                Vector3D xAxis = new Vector3D(1, 0, 0);
                MeshUtils.RotateSelectedMeshes(_listOfVertMeshIndexLists,
                                                sceneGroup,
                                                camera,
                                                cameraLight,
                                                xAxis,
                                                0.0,
                                                1.0);
            }
        }

        public void MoveSelectionOnAxis(Model3DGroup sceneGroup,
                                PerspectiveCamera camera,
                                Point3D center,
                                double deltaX,
                                double deltaY,
                                Axis draggingAxis)
        {

            MeshUtils.MoveSelectionOnAxis(sceneGroup,
                        camera,
                        center,
                        deltaX,
                        deltaY,
                        _selectedMeshes,
                        draggingAxis,
                        _xAxisMeshIndexList,
                        _yAxisMeshIndexList,
                        _zAxisMeshIndexList);
        }

        private void MoveSelectionToRotationList(List<List<int>> rotationList, Model3DGroup sceneGroup)
        {
            List<int> lastSelectedMeshesList = new List<int>();
            foreach (int key in _selectedMeshes.Keys)
            {
                if (sceneGroup.Children[key] is GeometryModel3D theModel)
                {
                    MeshGeometry3D mesh = (MeshGeometry3D)theModel.Geometry;
                    if (mesh != null)
                    {
                        lastSelectedMeshesList.Add(key);
                    }
                }
            }
            UnSelectAllVisibleMeshes(sceneGroup);
            if (lastSelectedMeshesList.Count > 0)
            {
                rotationList.Add(lastSelectedMeshesList);
            }
        }


    }
}

