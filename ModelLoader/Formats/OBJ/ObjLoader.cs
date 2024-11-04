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
using System.Windows.Media.Media3D; // Point3D stuff
using System.Windows; // Point()
using System.Windows.Media.Imaging; // BitmpaImage
using System.IO;

namespace ModelLoader
{

    public class ObjLoader: IModelLoader
    {
        public Dictionary<string, MyDiffuseMaterial> _materialsDictionary;
        public Dictionary<int, Material> selectedMeshes;

        TreeNode? modelAsTreeNodes;
        List<string>? modelMeshNames;

        public SelectedTool theTool;
        public ObjLoader()
        {
            _materialsDictionary = new Dictionary<string, MyDiffuseMaterial>();
            selectedMeshes = new Dictionary<int, Material>();

            modelAsTreeNodes = null;
            modelMeshNames = null;

            theTool = SelectedTool.select;
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
                return LoadOBJFromFile(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading OBJ file: {ex.Message}");
                return nullGeometry;
            }
        }

        private static (int[], int[], int[]) getAllFacesFromFaceLine(string theLine)
        {
            string theFaceLine = StringUtils.NormalizeSpaces(theLine);
            List<int> theListOfVerts = new List<int>();
            List<int> theListOfTextures = new List<int>();
            List<int> theListOfNormals = new List<int>();

            string[] tokens = theFaceLine.Split(' ');

            if (tokens[0].Trim() != "f")
            {
                return (theListOfVerts.ToArray(), theListOfTextures.ToArray(), theListOfNormals.ToArray());
            }

            // Skip the 'f' token
            for (int i = 1; i < tokens.Length; i++)
            {
                string curItem = tokens[i].Trim();
                if (curItem.Contains('/'))
                {
                    string[] theParts = curItem.Split('/');
                    if (int.TryParse(theParts[0], out int vertNum))
                    {
                        theListOfVerts.Add(vertNum - 1);
                    }
                    if (int.TryParse(theParts[1], out int textNum))
                    {
                        theListOfTextures.Add(textNum - 1);
                    }
                    if (theParts.Length > 2 && int.TryParse(theParts[2], out int vertNormNum))
                    {
                        theListOfNormals.Add(vertNormNum - 1);
                    }
                }
                else
                {
                    // Only a single number, no textures or normals
                    if (int.TryParse(curItem, out int vertNum))
                    {
                        theListOfVerts.Add(vertNum - 1);
                    }
                }
            }

            return (theListOfVerts.ToArray(), theListOfTextures.ToArray(), theListOfNormals.ToArray());
        }

        private static void AddTrianglesFromFan(
                                        MyMeshGeometry3D cubeMesh,
                                        List<Point3D> allVertsFromFile,
                                        List<Point> allTexturesFromFile,
                                        List<Vector3D> allNormalsFromFile,
                                        int[] vertIndices,
                                        int[] textureIndices,
                                        int[] normalIndices)
        {
            // The first vertex is used as the common vertex for all triangles
            int firstVertex = vertIndices[0];
            int firstTexture = textureIndices.Length > 0 ? textureIndices[0] : -1;
            int firstNormal = normalIndices.Length > 0 ? normalIndices[0] : -1;

            // Iterate over pairs of consecutive vertices, starting from the second one
            for (int i = 1; i < vertIndices.Length - 1; i++)
            {

                //--------------------------------------------
                // Add Vert
                if (firstVertex < allVertsFromFile.Count)
                {
                    Point3D addPoint = allVertsFromFile[firstVertex];
                    cubeMesh.Positions.Add(addPoint);
                }
                // Add the texture
                if (allTexturesFromFile != null && firstTexture >= 0 && firstTexture < allTexturesFromFile.Count)
                {
                    Point curPoint = allTexturesFromFile[firstTexture];
                    cubeMesh.TextureCoordinates.Add(curPoint);
                }
                // Add the normal
                if (allNormalsFromFile != null && firstNormal >= 0 && firstNormal < allNormalsFromFile.Count)
                {
                    Vector3D curPoint = allNormalsFromFile[firstNormal];
                    cubeMesh.Normals.Add(curPoint);
                }
                // Create triangles using the first vertex
                cubeMesh.TriangleIndices.Add(cubeMesh.Positions.Count - 1);
                //--------------------------------------------


                //--------------------------------------------
                int secondVertex = vertIndices[i];
                int secondTexture = textureIndices.Length > 0 ? textureIndices[i] : -1;
                int secondNormal = normalIndices.Length > 0 ? normalIndices[i] : -1;
                // Add 2nd point to Positions
                if (secondVertex < allVertsFromFile.Count)
                {
                    Point3D addPointTwo = allVertsFromFile[secondVertex];
                    cubeMesh.Positions.Add(addPointTwo);
                }
                // Add the texture
                if (allTexturesFromFile != null && secondTexture >= 0 && secondTexture < allTexturesFromFile.Count)
                {
                    Point curPoint = allTexturesFromFile[secondTexture];
                    cubeMesh.TextureCoordinates.Add(curPoint);
                }
                // Add the normal
                if (allNormalsFromFile != null && secondNormal >= 0 && secondNormal < allNormalsFromFile.Count)
                {
                    Vector3D curPoint = allNormalsFromFile[secondNormal];
                    cubeMesh.Normals.Add(curPoint);
                }
                // Add 2nd point to triangle
                cubeMesh.TriangleIndices.Add(cubeMesh.Positions.Count - 1);
                //--------------------------------------------


                //--------------------------------------------
                int thirdVertex = vertIndices[i + 1];
                int thirdTexture = textureIndices.Length > 0 ? textureIndices[i + 1] : -1;
                int thirdNormal = normalIndices.Length > 0 ? normalIndices[i + 1] : -1;
                // Add 2nd point to Positions
                if (thirdVertex < allVertsFromFile.Count)
                {
                    Point3D addPointThree = allVertsFromFile[thirdVertex];
                    cubeMesh.Positions.Add(addPointThree);
                }
                // Add the texture
                if (allTexturesFromFile != null && thirdTexture >= 0 && thirdTexture < allTexturesFromFile.Count)
                {
                    Point curPoint = allTexturesFromFile[thirdTexture];
                    cubeMesh.TextureCoordinates.Add(curPoint);
                }
                // Add the normal
                if (allNormalsFromFile != null && thirdNormal >= 0 && thirdNormal < allNormalsFromFile.Count)
                {
                    Vector3D curPoint = allNormalsFromFile[thirdNormal];
                    cubeMesh.Normals.Add(curPoint);
                }
                // Add 2nd point to triangle
                cubeMesh.TriangleIndices.Add(cubeMesh.Positions.Count - 1);
                //--------------------------------------------
            }
        }

        private static void loadMaterialsToDictionaryGivenMtlFile(string materialsFilePath,
                            Dictionary<string, MyDiffuseMaterial> fillThisMaterialsDictionary)
        {
            if (!File.Exists(materialsFilePath))
            {
                return;
            }
            string searchMapKdWord = "map_Kd";
            string directoryPath = Path.GetDirectoryName(materialsFilePath);
            string curMaterialName = "";
            string curMap_KdName = "";
            Boolean KdFound = false;
            Point3D Kd = new Point3D(0.4, 0.4, 0.4);
            double dissolve = 1.0;
            int lineCount = 0;

            using (StreamReader reader = new StreamReader(materialsFilePath))
            {
                string rawLine;
                while ((rawLine = reader.ReadLine()) != null)
                {
                    lineCount++;
                    string line = StringUtils.NormalizeSpaces(StringUtils.RemoveAfterHash(rawLine));
                    if (String.IsNullOrEmpty(line))
                    {
                        continue;
                    }
                    string[] tokens = line.Split(' ');
                    if (tokens.Length == 0)
                    {
                        continue;
                    }

                    string firstToken = tokens[0].Trim();

                    //Materials file tag found in the .obj file
                    if (firstToken == "newmtl" && tokens.Length == 2)
                    {
                        //Add the last material
                        if (!String.IsNullOrEmpty(curMap_KdName))
                        {
                            string mapKdFilePath = Path.Combine(directoryPath, curMap_KdName);
                            MyDiffuseMaterial aLoadedMaterial = new MyDiffuseMaterial();
                            aLoadedMaterial.mapKdFilePath = mapKdFilePath;

                            fillThisMaterialsDictionary.Add(curMaterialName, aLoadedMaterial);

                            curMap_KdName = "";
                            curMaterialName = "";

                            //We don't care if KdFound since map_Kd was found
                            KdFound = false;
                        }
                        else
                        {
                            if (KdFound)
                            {
                                string theMatName = String.IsNullOrEmpty(curMaterialName) ? tokens[1].Trim() : curMaterialName;

                                MyDiffuseMaterial aLoadedMaterial = new MyDiffuseMaterial();
                                aLoadedMaterial.Dissolve = dissolve;
                                aLoadedMaterial.Kd = Kd;


                                if (!fillThisMaterialsDictionary.ContainsKey(theMatName))
                                {
                                    fillThisMaterialsDictionary.Add(theMatName, aLoadedMaterial);
                                }
                                //Reset
                                dissolve = 1.0;
                                curMap_KdName = "";
                                curMaterialName = "";
                                KdFound = false;
                            }
                        }

                        //Get material name
                        curMaterialName = tokens[1].Trim();
                    }

                    if (firstToken == "Kd" && tokens.Length >= 4)
                    {
                        KdFound = true;
                        // Extract the coordinates
                        if (double.TryParse(tokens[1], out var x) &&
                            double.TryParse(tokens[2], out var y) &&
                            double.TryParse(tokens[3], out var z))
                        {
                            Kd = new Point3D(x, y, z);
                        }
                    }

                    if (firstToken == "d" && tokens.Length >= 4)
                    {
                        // Extract the coordinates
                        if (double.TryParse(tokens[1], out var theDissolve))
                        {
                            dissolve = theDissolve;
                        }
                    }

                    //Materials file tag found in the .obj file
                    if (firstToken == "map_Kd")
                    {
                        string mapKdName = "";
                        //Handle a space in the name
                        int index = line.IndexOf(searchMapKdWord);
                        if (index != -1)
                        {
                            mapKdName = line.Substring(index + searchMapKdWord.Length).Trim();
                        }

                        if (String.IsNullOrWhiteSpace(mapKdName))
                        {
                            continue;
                        }
                        curMap_KdName = mapKdName;
                    }


                }
            }

            //Add the last material
            //If we were reading a material then it should be completely read
            if (!String.IsNullOrEmpty(curMap_KdName))
            {
                string mapKdFilePath = Path.Combine(directoryPath, curMap_KdName);

                BitmapImage textureImage = new BitmapImage();
                textureImage.BeginInit();
                textureImage.UriSource = new Uri(mapKdFilePath, UriKind.Absolute);
                textureImage.CacheOption = BitmapCacheOption.OnLoad;
                textureImage.EndInit();
                MyDiffuseMaterial aLoadedMaterial = new MyDiffuseMaterial();
                aLoadedMaterial.mapKdFilePath = mapKdFilePath;
                fillThisMaterialsDictionary.Add(curMaterialName, aLoadedMaterial);
            }
            else
            {
                MyDiffuseMaterial modelMaterial = new MyDiffuseMaterial();
                modelMaterial.Dissolve = dissolve;
                modelMaterial.Kd = Kd;

                if (!fillThisMaterialsDictionary.ContainsKey(curMaterialName))
                {
                    fillThisMaterialsDictionary.Add(curMaterialName, modelMaterial);
                }
            }
        }

        private static void loadMaterialsFromOBJFileToDictionary(string filePath,
                                                Dictionary<string, MyDiffuseMaterial> loadThisDictionary,
                                                out int lineCount)
        {
            lineCount = 0;
            string directoryPath = Path.GetDirectoryName(filePath);

            if (filePath == null) { return; }

            HashSet<string> alreadyAddedMaterialFilePaths = new HashSet<string>();
            
            string searchWord = "mtllib";

            //Step 1, get full path to all the matrial files
            using (StreamReader reader = new StreamReader(filePath))
            {
                string rawLine;
                while ((rawLine = reader.ReadLine()) != null)
                {
                    lineCount++;

                    string line = StringUtils.NormalizeSpaces(StringUtils.RemoveAfterHash(rawLine));
                    if (String.IsNullOrEmpty(line))
                    {
                        continue;
                    }
                    string[] tokens = line.Split(' ');

                    //Materials file tag found in the .obj file
                    if (tokens[0].Trim() == "mtllib")
                    {
                        string materialsFileName = "";

                        int index = line.IndexOf(searchWord);
                        if (index != -1)
                        {
                            materialsFileName = line.Substring(index + searchWord.Length).Trim();
                        }

                        if (String.IsNullOrWhiteSpace(materialsFileName))
                        {
                            continue;
                        }

                        //string materialsFileName = tokens[1].Trim();
                        string materialsFilePath = Path.Combine(directoryPath, materialsFileName);
                        if (File.Exists(materialsFilePath))
                        {
                            //Only parse the .mtl file it we haven't parsed it already
                            if (!alreadyAddedMaterialFilePaths.Contains(materialsFilePath))
                            {
                                loadMaterialsToDictionaryGivenMtlFile(materialsFilePath, loadThisDictionary);
                                alreadyAddedMaterialFilePaths.Add(materialsFilePath);
                            }
                        }
                    }
                }
            }
        }


        private IGeometryLoaderResult LoadOBJFromFile(string filePath)
        {
            // Load all materials, count the lines of the model file
            int fileLineCountForProgress = 0;
            byte[] fileBytes = File.ReadAllBytes(filePath);
            loadMaterialsFromOBJFileToDictionary(filePath,
                                            _materialsDictionary,
                                            out fileLineCountForProgress);

            // Load global vertices, textures, and normals
            (List<Point3D> allVertsFromFile,
                List<Point> allTexturesFromFile,
                    List<Vector3D> allNormalsFromFile) = LoadOBJFromBytes(fileBytes, fileLineCountForProgress);

            var chunksOfObjects = ObjFileReader.ReadObjFile(filePath, new string[] { "o ", "# object " });

            // Lookup a mesh name from its index
            List<string> meshNames = new List<string> { };
            List<MyGeometryModel3D> allMeshes = new List<MyGeometryModel3D>();

            string fileNameOnly = Path.GetFileName(filePath);
            var root = new TreeNode { Name = fileNameOnly };

            int numChuncks = chunksOfObjects.Count;
            for (int i = 0; i < numChuncks; i++)
            {
                var chunkOfObject = chunksOfObjects[i];
                //Get the name,
                string objObjectName = String.IsNullOrWhiteSpace(chunkOfObject.ObjectName) ? "UnknownObject" : chunkOfObject.ObjectName;

                var curOBJObjectChild = new TreeNode { Name = objObjectName };

                string combinedString = string.Join(Environment.NewLine, chunkOfObject.Lines);
                byte[] byteArray = Encoding.UTF8.GetBytes(combinedString);

                //A group can use multiple materials
                var chunksOfGroups = ObjFileReader.ReadObjFile(byteArray, new string[] { "g " });
                foreach (var chunkOfGroup in chunksOfGroups)
                {
                    string objGroupName = String.IsNullOrWhiteSpace(chunkOfGroup.ObjectName) ? "UnknownGroup" : chunkOfGroup.ObjectName;

                    var curGroupChild = new TreeNode { Name = objGroupName };

                    string combinedGroupString = string.Join(Environment.NewLine, chunkOfGroup.Lines);
                    byte[] byteArrayOfGroup = Encoding.UTF8.GetBytes(combinedGroupString);

                    //A group can use multiple materials
                    var chunksOfMaterial = ObjFileReader.ReadObjFile(byteArrayOfGroup, new string[] { "usemtl " });
                    foreach (var chunkOfMaterial in chunksOfMaterial)
                    {
                        string materialName = String.IsNullOrWhiteSpace(chunkOfMaterial.ObjectName) ? "UnknownMaterial" : chunkOfMaterial.ObjectName;

                        var curMaterialChild = new TreeNode { Name = materialName };

                        string combinedStringTwo = string.Join(Environment.NewLine, chunkOfMaterial.Lines);
                        byte[] byteArrayTwo = Encoding.UTF8.GetBytes(combinedStringTwo);

                        //GeometryModel3D holds a MeshGeometry3D and a Material
                        MyGeometryModel3D theMesh = LoadOBJFacesFromBytes(allVertsFromFile,
                                                                        allTexturesFromFile,
                                                                        allNormalsFromFile,
                                                                        byteArrayTwo);

                        string meshName = chunkOfGroup.ObjectName;
                        meshNames.Add(meshName);
                        allMeshes.Add(theMesh);


                        //Since this current mesh has a material associated with it, save the index
                        curMaterialChild.GeometryModel3DIndex = allMeshes.Count - 1;
                        curGroupChild.Children.Add(curMaterialChild);

                    }// End Materials

                    curOBJObjectChild.Children.Add(curGroupChild);

                }// End Groups


                root.Children.Add(curOBJObjectChild);
            } // End Objects



            modelAsTreeNodes = root;
            modelMeshNames = meshNames;
            return new GeometryLoaderResult(allMeshes, meshNames, root);
        }


        //Take a single chunk of material named and faces below it
        private MyGeometryModel3D LoadOBJFacesFromBytes(
                                                List<Point3D> allVertsFromFile,
                                                List<Point> allTexturesFromFile,
                                                List<Vector3D> allNormalsFromFile,
                                                byte[] byteArray)
        {
            bool foundMTL = false;
            MyDiffuseMaterial modelMaterial = new MyDiffuseMaterial();
            MyMeshGeometry3D cubeMesh = new MyMeshGeometry3D();

            using (MemoryStream memoryStream = new MemoryStream(byteArray))
            using (StreamReader reader = new StreamReader(memoryStream))
            {
                string rawLine;
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
                    if (firstToken == "usemtl" && tokens.Length >= 2)
                    {
                        string materialsFileName = tokens[1].Trim();
                        if (_materialsDictionary.ContainsKey(materialsFileName))
                        {
                            foundMTL = true;
                            modelMaterial = _materialsDictionary[materialsFileName];
                        }
                    }

                    if (firstToken == "f")
                    {
                        (int[] faceLineFaceIndices,
                        int[] faceLineTextureIndices,
                        int[] faceLineNormalIndices) = getAllFacesFromFaceLine(line);

                        AddTrianglesFromFan(cubeMesh,
                                            allVertsFromFile,
                                            allTexturesFromFile,
                                            allNormalsFromFile,
                                            faceLineFaceIndices,
                                            faceLineTextureIndices,
                                            faceLineNormalIndices);
                    }
                }
            }


            if (!foundMTL)
            {
                //modelMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Blue));
                MyDiffuseMaterial defaultBlue = new MyDiffuseMaterial();
                defaultBlue.Kd = new Point3D(0, 0, 1);
                defaultBlue.Dissolve = 1.0;
                modelMaterial = defaultBlue;
            }

            return new MyGeometryModel3D(cubeMesh, modelMaterial);
        }

        private static (List<Point3D>, List<Point>, List<Vector3D>) LoadOBJFromBytes(byte[] byteArray,
                        int preCalculatedLineCount)
        {
            List<Point3D> vertexListLocal = new List<Point3D>();
            List<Point> texturesListLocal = new List<Point>();
            List<Vector3D> normalListLocal = new List<Vector3D>();

            using (MemoryStream memoryStream = new MemoryStream(byteArray))
            using (StreamReader reader = new StreamReader(memoryStream))
            {
                int lineCount = 0;
                string rawLine;
                while ((rawLine = reader.ReadLine()) != null)
                {
                    lineCount++;
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

                    if (firstToken == "v" && tokens.Length >= 4)
                    {
                        // Extract the coordinates
                        if (double.TryParse(tokens[1], out var x) &&
                            double.TryParse(tokens[2], out var y) &&
                            double.TryParse(tokens[3], out var z))
                        {
                            Point3D parsedPoint = new Point3D(x, y, z);
                            vertexListLocal.Add(new Point3D(x, y, z));
                        }
                    }

                    if (firstToken == "vn" && tokens.Length >= 4)
                    {
                        // Extract the coordinates
                        if (double.TryParse(tokens[1], out var x) &&
                            double.TryParse(tokens[2], out var y) &&
                            double.TryParse(tokens[3], out var z))
                        {
                            normalListLocal.Add(new Vector3D(x, y, z));
                        }
                    }

                    if (firstToken == "vt" && tokens.Length >= 3)
                    {
                        // Extract the coordinates
                        if (double.TryParse(tokens[1], out var x) &&
                            double.TryParse(tokens[2], out var y))
                        {
                            //NOTE, we do 1 - y because wpf uv coorts are flipped on the y
                            Point curPoint = new Point(x, 1 - y);
                            texturesListLocal.Add(curPoint);
                        }
                    }
                }
            }

            return (vertexListLocal, texturesListLocal, normalListLocal);
        }
    }
}
