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
using System.Windows.Media.Media3D;
using System.Windows;

namespace ModelLoader
{
    public class MyMeshGeometry3D
    {
        public List<Point3D> Positions { get; private set; }
        public List<Point> TextureCoordinates { get; private set; }
        public List<Vector3D> Normals { get; private set; }
        public List<Int32> TriangleIndices { get; private set; }

        public MyMeshGeometry3D()
        {
            Positions = new List<Point3D>();
            TextureCoordinates = new List<Point>();
            Normals = new List<Vector3D>();
            TriangleIndices = new List<Int32>();
        }
    }
}
