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
using System.Text;
using System.Windows.Media.Media3D;
using System.IO;

namespace ModelLoader
{
    public class STLLoader: IModelLoader
    {
        public IGeometryLoaderResult LoadFile(string filePath)
        {
            GeometryLoaderResult nullGeometry = new GeometryLoaderResult(null, null, null);
            if (!File.Exists(filePath))
            {
                return nullGeometry;
            }

            try
            {
                return LoadSTLFromFile(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading STL file: {ex.Message}");
                return nullGeometry;
            }
        }

        private IGeometryLoaderResult LoadSTLFromFile(string filePath)
        {
            if (isAsciiSTLFile(filePath))
            {
                return LoadSTLFromAsciiFile(filePath);
            }
            else
            {
                return LoadSTLFromBinaryFile(filePath);
            }

            return new GeometryLoaderResult(new List<MyGeometryModel3D>(), new List<string>(), new TreeNode());
        }

        private bool isAsciiSTLFile(string filePath)
        {
            // 'solid' is 5 bytes long in ASCII encoding
            byte[] buffer = new byte[5];

            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                // Read the first 5 bytes of the file
                int bytesRead = fileStream.Read(buffer, 0, buffer.Length);

                // Check if we've read 5 bytes (the file might be too short)
                if (bytesRead < 5)
                {
                    return false;
                }

                // Convert the bytes to a string using ASCII encoding
                string fileStart = Encoding.ASCII.GetString(buffer);

                // Compare the result with 'solid'
                return fileStart.Equals("solid", StringComparison.Ordinal);
            }
        }

        private IGeometryLoaderResult LoadSTLFromBinaryFile(string filePath)
        {
            List<MyGeometryModel3D> allMeshes = new List<MyGeometryModel3D>();
            MyMeshGeometry3D cubeMesh = new MyMeshGeometry3D();

            string fileNameOnly = Path.GetFileName(filePath);
            List<string> meshNames = new List<string> { };
            meshNames.Add(fileNameOnly);
            var root = new TreeNode { Name = fileNameOnly };

            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                // Skip the first 80 bytes
                fileStream.Seek(80, SeekOrigin.Begin);

                // Read the number of triangles (UINT32)
                byte[] uint32Buffer = new byte[4];
                fileStream.Read(uint32Buffer, 0, 4);
                uint numTriangles = BitConverter.ToUInt32(uint32Buffer, 0);

                // Loop through each triangle and read the 50 bytes per triangle
                for (uint i = 0; i < numTriangles; i++)
                {
                    byte[] triangleBuffer = new byte[50];
                    int bytesRead = fileStream.Read(triangleBuffer, 0, 50);

                    if (bytesRead == 50)
                    {
                        // Normal vector (12 bytes)
                        float normalX = BitConverter.ToSingle(triangleBuffer, 0);
                        float normalY = BitConverter.ToSingle(triangleBuffer, 4);
                        float normalZ = BitConverter.ToSingle(triangleBuffer, 8);

                        cubeMesh.Normals.Add(new Vector3D(normalX, normalY, normalZ));
                        cubeMesh.Normals.Add(new Vector3D(normalX, normalY, normalZ));
                        cubeMesh.Normals.Add(new Vector3D(normalX, normalY, normalZ));

                        // Vertex 1 (12 bytes)
                        float vertex1X = BitConverter.ToSingle(triangleBuffer, 12);
                        float vertex1Y = BitConverter.ToSingle(triangleBuffer, 16);
                        float vertex1Z = BitConverter.ToSingle(triangleBuffer, 20);

                        cubeMesh.Positions.Add(new Point3D(vertex1X, vertex1Y, vertex1Z));
                        cubeMesh.TriangleIndices.Add(cubeMesh.Positions.Count - 1);

                        // Vertex 2 (12 bytes)
                        float vertex2X = BitConverter.ToSingle(triangleBuffer, 24);
                        float vertex2Y = BitConverter.ToSingle(triangleBuffer, 28);
                        float vertex2Z = BitConverter.ToSingle(triangleBuffer, 32);

                        cubeMesh.Positions.Add(new Point3D(vertex2X, vertex2Y, vertex2Z));
                        cubeMesh.TriangleIndices.Add(cubeMesh.Positions.Count - 1);

                        // Vertex 3 (12 bytes)
                        float vertex3X = BitConverter.ToSingle(triangleBuffer, 36);
                        float vertex3Y = BitConverter.ToSingle(triangleBuffer, 40);
                        float vertex3Z = BitConverter.ToSingle(triangleBuffer, 44);

                        cubeMesh.Positions.Add(new Point3D(vertex3X, vertex3Y, vertex3Z));
                        cubeMesh.TriangleIndices.Add(cubeMesh.Positions.Count - 1);


                        // Attribute byte count (2 bytes)
                        ushort attributeByteCount = BitConverter.ToUInt16(triangleBuffer, 48);
                    }
                    else
                    {
                        Console.WriteLine("Error reading triangle data.");
                        break;
                    }
                }
            }

            MyDiffuseMaterial material = new MyDiffuseMaterial
            {
                Dissolve = 1.0,
                KdName = "White",
                Kd = new Point3D(1.0, 1.0, 1.0),
                MaterialName = "White",
                mapKdFilePath = ""
            };
            MyGeometryModel3D finalModel = new MyGeometryModel3D(cubeMesh, material);
            allMeshes.Add(finalModel);
            root.GeometryModel3DIndex = 1;

            return new GeometryLoaderResult(allMeshes, meshNames, root);
        }

        private IGeometryLoaderResult LoadSTLFromAsciiFile(string filePath)
        {
            List<MyGeometryModel3D> allMeshes = new List<MyGeometryModel3D>();
            MyMeshGeometry3D cubeMesh = new MyMeshGeometry3D();

            string fileNameOnly = Path.GetFileName(filePath);
            List<string> meshNames = new List<string> { };
            meshNames.Add(fileNameOnly);
            var root = new TreeNode { Name = fileNameOnly };

            byte[] fileBytes = File.ReadAllBytes(filePath);

            using (MemoryStream memoryStream = new MemoryStream(fileBytes))
            using (StreamReader reader = new StreamReader(memoryStream))
            {
                string rawLine;
                string curObjectName = "";

                while ((rawLine = reader.ReadLine()) != null)
                {
                    string line = StringUtils.NormalizeSpaces(StringUtils.RemoveAfterHash(rawLine));
                    if (String.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    // Split the string into tokens
                    string[] tokens = line.Split(' ');
                    if (tokens.Length == 0)
                    {
                        continue;
                    }

                    string firstToken = tokens[0].Trim();

                    //Use a material
                    if (firstToken == "solid" && tokens.Length >= 2)
                    {
                        curObjectName = tokens[1].Trim();
                    }

                    if (firstToken == "facet" && tokens.Length == 5)
                    {
                        if (tokens[1] == "normal")
                        {
                            if (double.TryParse(tokens[2], out var x) &&
                            double.TryParse(tokens[3], out var y) &&
                            double.TryParse(tokens[4], out var z))
                            {
                                cubeMesh.Normals.Add(new Vector3D(x, y, z));
                                cubeMesh.Normals.Add(new Vector3D(x, y, z));
                                cubeMesh.Normals.Add(new Vector3D(x, y, z));
                            }
                        }
                    }

                    if (firstToken == "vertex" && tokens.Length == 4)
                    {
                        if (double.TryParse(tokens[1], out var x) &&
                        double.TryParse(tokens[2], out var y) &&
                        double.TryParse(tokens[3], out var z))
                        {
                            cubeMesh.Positions.Add(new Point3D(x, y, z));
                            cubeMesh.TriangleIndices.Add(cubeMesh.Positions.Count - 1);
                        }
                    }
                }
            }
            
            MyDiffuseMaterial material = new MyDiffuseMaterial
            {
                Dissolve = 1.0,
                KdName = "White",
                Kd = new Point3D(1.0, 1.0, 1.0),
                MaterialName = "White",
                mapKdFilePath = ""
            };
            MyGeometryModel3D finalModel = new MyGeometryModel3D(cubeMesh, material);
            allMeshes.Add(finalModel);
            root.GeometryModel3DIndex = 1;

            return new GeometryLoaderResult(allMeshes, meshNames, root);
        }
    }
}
