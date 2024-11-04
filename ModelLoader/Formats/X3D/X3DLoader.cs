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
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;
using System.Xml;
using System.Windows;

namespace ModelLoader
{
    public class X3DLoader: IModelLoader
    {
        public class IndexedFaceSetData
        {
            public List<int[]> CoordIndexes { get; set; }
            public List<double[]> Points { get; set; }
        }

        public IGeometryLoaderResult LoadFile(string filePath)
        {
            GeometryLoaderResult nullGeometry = new GeometryLoaderResult(null, null, null);
            if (!File.Exists(filePath))
            {
                return nullGeometry;
            }

            try
            {
                return LoadX3DFromFile(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading X3D file: {ex.Message}");
                return nullGeometry;
            }
        }

        private IGeometryLoaderResult LoadX3DFromFile(string filePath)
        {
            return ParseX3D(filePath);
        }

        private static GeometryLoaderResult ParseX3D(string xmlFilePath)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlFilePath);
            XmlNodeList shapeNodes = xmlDoc.GetElementsByTagName("Shape");

            List<string> meshNames = new List<string> { };

            string directoryPath = Path.GetDirectoryName(xmlFilePath);
            string fileNameOnly = Path.GetFileName(xmlFilePath);
            meshNames.Add(fileNameOnly);
            var root = new TreeNode { Name = fileNameOnly };
            List<MyGeometryModel3D> allMeshes = new List<MyGeometryModel3D>();

            foreach (XmlNode shapeNode in shapeNodes)
            {
                string materialFileName = "";
                // Get texture image file
                XmlNode appearanceNode = shapeNode.SelectSingleNode("Appearance");
                if (appearanceNode != null)
                {
                    XmlNode imageTextureNode = appearanceNode.SelectSingleNode("ImageTexture");
                    if (imageTextureNode != null)
                    {
                        string urlString = imageTextureNode.Attributes["url"]?.Value;
                        if (!string.IsNullOrEmpty(urlString))
                        {
                            materialFileName = urlString;
                        }
                    }
                }

                XmlNode indexedFaceSetNode = shapeNode.SelectSingleNode("IndexedFaceSet");

                if (indexedFaceSetNode != null)
                {
                    XmlNode texCoordIndexNode = indexedFaceSetNode.SelectSingleNode("texCoordIndex");
                    if (texCoordIndexNode != null)
                    {
                        string texCoordIndexFoundString = texCoordIndexNode.InnerText;
                        Console.WriteLine($"texCoordIndex: {texCoordIndexFoundString}");
                    }

                    foreach (XmlAttribute attribute in indexedFaceSetNode.Attributes)
                    {
                        Console.WriteLine($"{attribute.Name}: {attribute.Value}");
                    }

                    MyMeshGeometry3D cubeMesh = new MyMeshGeometry3D();
                    List<int[]> coordIndexes = new List<int[]>();
                    List<int[]> textureIndexCoords = new List<int[]>();
                    List<float[]> textureCoords = new List<float[]>();
                    List<float[]> points = new List<float[]>();


                    // Read the texCoordIndex attribute
                    string texCoordIndexString = indexedFaceSetNode.Attributes["texCoordIndex"]?.Value;

                    if (!string.IsNullOrEmpty(texCoordIndexString))
                    {
                        // Parse texture coords into a list of arrays, each representing a 3D point (x, y, z)
                        textureIndexCoords = texCoordIndexString.Split(new[] { "-1" }, StringSplitOptions.RemoveEmptyEntries)
                                                                .Select(face => face.Trim().Split(' ')
                                                                .Select(int.Parse)
                                                                .ToArray())
                                                                .ToList();
                    }

                    // Read the coordIndex attribute
                    string coordIndexString = indexedFaceSetNode.Attributes["coordIndex"]?.Value;
                    if (string.IsNullOrEmpty(coordIndexString))
                    {
                        throw new Exception("coordIndex attribute not found or empty.");
                    }
                    if (!string.IsNullOrEmpty(coordIndexString))
                    {
                        // Split coordIndex by spaces, handling -1 as separator between face definitions
                        coordIndexes = coordIndexString.Split(new[] { "-1" }, StringSplitOptions.RemoveEmptyEntries)
                                                                    .Select(face => face.Trim().Split(' ')
                                                                    .Select(int.Parse)
                                                                    .ToArray())
                                                                    .ToList();
                    }

                    // Read the points from the Coordinate element
                    XmlNode textureCoordinateNode = indexedFaceSetNode.SelectSingleNode("TextureCoordinate");

                    if (textureCoordinateNode != null)
                    {
                        string textureCoordinatePointString = textureCoordinateNode.Attributes["point"]?.Value;
                        if (!string.IsNullOrEmpty(textureCoordinatePointString))
                        {
                            // Parse points into a list of arrays, each representing a 3D point (x, y, z)
                            textureCoords = textureCoordinatePointString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                                            .Select((value, index) => new { value, index })
                                                            .GroupBy(x => x.index / 2)
                                                            .Select(g => g.Select(x => float.Parse(x.value)).ToArray())
                                                            .ToList();
                        }
                    }


                    // Read the points from the Coordinate element
                    XmlNode coordinateNode = indexedFaceSetNode.SelectSingleNode("Coordinate");
                    if (coordinateNode == null) { continue; }

                    string pointsString = coordinateNode.Attributes["point"]?.Value;
                    if (!string.IsNullOrEmpty(pointsString))
                    {

                        // Parse points into a list of arrays, each representing a 3D point (x, y, z)
                        points = pointsString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                                        .Select((value, index) => new { value, index })
                                                        .GroupBy(x => x.index / 3)
                                                        .Select(g => g.Select(x => float.Parse(x.value)).ToArray())
                                                        .ToList();
                    }



                    // Fill the mesh with all the information
                    foreach (float[] curPointArray in points)
                    {
                        float vertexX = curPointArray[0];
                        float vertexY = curPointArray[1];
                        float vertexZ = curPointArray[2];
                        cubeMesh.Positions.Add(new Point3D(vertexX, vertexY, vertexZ));
                    }

                    foreach (int[] curTextureIndexCoordArray in textureIndexCoords)
                    {
                        //Each coordinate index has 3 values associated with it
                        int index1 = curTextureIndexCoordArray[0];
                        int index2 = curTextureIndexCoordArray[1];
                        int index3 = curTextureIndexCoordArray[2];

                        float[] textureCoordIndexes1 = textureCoords[index1];
                        //NOTE, we do 1 - y because wpf uv coorts are flipped on the y
                        Point curPointFromIndex1 = new Point(textureCoordIndexes1[0], 1.0 - textureCoordIndexes1[1]);
                        cubeMesh.TextureCoordinates.Add(curPointFromIndex1);

                        float[] textureCoordIndexes2 = textureCoords[index2];
                        Point curPointFromIndex2 = new Point(textureCoordIndexes2[0], 1.0 - textureCoordIndexes2[1]);
                        cubeMesh.TextureCoordinates.Add(curPointFromIndex2);

                        float[] textureCoordIndexes3 = textureCoords[index3];
                        Point curPointFromIndex3 = new Point(textureCoordIndexes3[0], 1.0 - textureCoordIndexes3[1]);
                        cubeMesh.TextureCoordinates.Add(curPointFromIndex3);
                    }

                    //Assumes faces are only 3 points and not more
                    foreach (int[] curIntArray in coordIndexes)
                    {
                        int index1 = curIntArray[0];
                        int index2 = curIntArray[1];
                        int index3 = curIntArray[2];

                        cubeMesh.TriangleIndices.Add(index1);
                        cubeMesh.TriangleIndices.Add(index2);
                        cubeMesh.TriangleIndices.Add(index3);
                    }


                    MyDiffuseMaterial texturedMaterial = new MyDiffuseMaterial();
                    if (!String.IsNullOrEmpty(materialFileName))
                    {
                        string imageTextureFilePath = Path.Combine(directoryPath, materialFileName);
                        texturedMaterial.mapKdFilePath = imageTextureFilePath;
                    }

                    MyDiffuseMaterial materialWhite = new MyDiffuseMaterial
                    {
                        Dissolve = 1.0,
                        KdName = "White",
                        Kd = new Point3D(1.0, 1.0, 1.0),
                        MaterialName = "White",
                        mapKdFilePath = ""
                    };

                    MyDiffuseMaterial finalMaterial = String.IsNullOrEmpty(materialFileName) ? materialWhite : texturedMaterial;
                    MyGeometryModel3D finalModel = new MyGeometryModel3D(cubeMesh, finalMaterial);
                    allMeshes.Add(finalModel);
                }
            }

            return new GeometryLoaderResult(allMeshes, meshNames, root);
        }

    }
}
