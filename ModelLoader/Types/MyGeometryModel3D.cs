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

namespace ModelLoader
{
    public class MyGeometryModel3D
    {
        // Properties to hold the mesh and material
        public MyMeshGeometry3D CubeMesh { get; private set; }
        public MyDiffuseMaterial ModelMaterial { get; private set; }

        // Constructor with parameters
        public MyGeometryModel3D(MyMeshGeometry3D cubeMesh, MyDiffuseMaterial modelMaterial)
        {
            CubeMesh = cubeMesh;
            ModelMaterial = modelMaterial;
        }
    }
}
