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
using System.Windows.Media.Media3D; //Point3D stuff
using System.Windows.Media; //SolidColorBrush

namespace ModelLoader
{
    internal static class GeometryUtils
    {
        internal static GeometryModel3D CreateLine(Point3D start,
            Point3D end,
            double thickness,
            Color theColor,
            bool hasPointyEnd = false)
        {
            // Calculate the direction vector and normalize it
            Vector3D direction = end - start;
            direction.Normalize();

            // Calculate two perpendicular vectors for thickness (forming a rectangular cross-section)
            Vector3D perpendicular1 = Vector3D.CrossProduct(direction, new Vector3D(0, 0, 1));
            // The cross product of two identical vectors results in a new vector with length 0.
            if (perpendicular1.Length == 0)
            {
                perpendicular1 = Vector3D.CrossProduct(direction, new Vector3D(0, 1, 0));
            }
            perpendicular1.Normalize();

            Vector3D perpendicular2 = Vector3D.CrossProduct(direction, perpendicular1);
            perpendicular2.Normalize();

            // Scale the perpendicular vectors by thickness
            perpendicular1 *= thickness / 2;
            perpendicular2 *= thickness / 2;

            // Create the mesh
            MeshGeometry3D mesh = new MeshGeometry3D();

            // Define the vertices for the rectangular line (8 vertices for the box)
            Point3DCollection positions = new Point3DCollection
                {
                    // Four corners of one end of the line
                    start + perpendicular1 + perpendicular2,  // 0
                    start + perpendicular1 - perpendicular2,  // 1
                    start - perpendicular1 + perpendicular2,  // 2
                    start - perpendicular1 - perpendicular2,  // 3

                    // Four corners of the other end of the line
                    end + perpendicular1 + perpendicular2,    // 4
                    end + perpendicular1 - perpendicular2,    // 5
                    end - perpendicular1 + perpendicular2,    // 6
                    end - perpendicular1 - perpendicular2     // 7
                };

            if (hasPointyEnd)
            {
                positions = new Point3DCollection
                {
                    // Four corners of one end of the line
                    start + perpendicular1 + perpendicular2,  // 0
                    start + perpendicular1 - perpendicular2,  // 1
                    start - perpendicular1 + perpendicular2,  // 2
                    start - perpendicular1 - perpendicular2,  // 3

                    // Four corners of the other end of the line
                    end,    // 4
                    end,    // 5
                    end,    // 6
                    end     // 7
                };
            }


            mesh.Positions = positions;

            // Define the triangles for the 6 sides of the box (12 triangles total)
            Int32Collection indices = new Int32Collection
            {
                // Front face
                0, 1, 2, 2, 1, 3,
                // Back face
                4, 6, 5, 5, 6, 7,
                // Left face
                0, 2, 4, 4, 2, 6,
                // Right face
                1, 5, 3, 3, 5, 7,
                // Top face
                0, 4, 1, 1, 4, 5,
                // Bottom face
                2, 3, 6, 6, 3, 7
            };
            mesh.TriangleIndices = indices;
            // Create a material for the line
            Material material = new DiffuseMaterial(new SolidColorBrush(theColor));
            // Create the geometry model
            GeometryModel3D lineModel = new GeometryModel3D(mesh, material);
            return lineModel;
        }
    }
}
