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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D; //Point3D stuff
using System.IO;
using System.Windows.Forms; // For ColorDialog
using MessageBox = System.Windows.MessageBox;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using ModelLoader;

namespace _3dTests
{
    /// <summary>
    /// Interaction logic for _3DInteractivePane.xaml
    /// </summary>
    public partial class ModelUIControl : System.Windows.Controls.UserControl
    {
        SceneCollection _sceneCollection = new SceneCollection();

        private System.Windows.Point _lastMousePosition, _originalMousePosition;
        private bool _isMouseDragging = false;
        private bool _isMouseWheelDragging = false;
        private bool _isMouseRightBtnDragging = false;
        private ModelLoader.Axis _draggingAxis = ModelLoader.Axis.None;
        Point3D _dragginSelectedCenter;
        private bool _modelLoaded = false;

        private readonly DispatcherTimer _rotationHorzTimer;
        private readonly DispatcherTimer _rotationVertTimer;
        private bool _isAutoRotatingHorz = false;
        private bool _isAutoRotatingVert = false;
        private bool _showLeftPanel = false;
        private double _vertCount = 0;
        private double _triangleCount = 0;

        private ModelLoader.SelectedTool _selectedTool = ModelLoader.SelectedTool.select;
        ObservableCollection<string> _meshNames = new ObservableCollection<string>();

        // Help find the TreeViewItem associated with a mesh index
        Dictionary<int, TreeViewItem> _meshTreeViewMapping = new Dictionary<int, TreeViewItem>();

        public ModelUIControl()
        {
            InitializeComponent();

            // Initialize the DispatcherTimer
            _rotationHorzTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(30)
            };
            _rotationHorzTimer.Tick += OnRotationHorzTimerTick;

            _rotationVertTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(30)
            };
            _rotationVertTimer.Tick += OnRotationVertTimerTick;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!_modelLoaded) { return; }
            
            System.Windows.Point curPos = e.GetPosition(this);

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // Start dragging
                _lastMousePosition = e.GetPosition(this);
                _originalMousePosition = e.GetPosition(this);
                _isMouseDragging = true;
                _isMouseRightBtnDragging = false;
                _isMouseWheelDragging = false;
            }

            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                // Start dragging
                _lastMousePosition = e.GetPosition(this);
                _originalMousePosition = e.GetPosition(this);
                _isMouseDragging = false;
                _isMouseRightBtnDragging = false;
                _isMouseWheelDragging = true;
            }

            if (e.RightButton == MouseButtonState.Pressed)
            {
                // Start dragging
                _lastMousePosition = e.GetPosition(this);
                _originalMousePosition = e.GetPosition(this);
                _isMouseDragging = false;
                _isMouseWheelDragging = false;
                _isMouseRightBtnDragging = true;
            }
            
            if (_selectedTool == ModelLoader.SelectedTool.move)
            {
                PerspectiveCamera camera = (PerspectiveCamera)myViewport.Camera;
                //Check if hit an axis
                System.Windows.Point mousePosition = e.GetPosition(this);
                bool leftPanelIsVisible = leftPanel.Visibility == Visibility.Visible;
                bool gridSplitterVisible = theGridSplitter.Visibility == Visibility.Visible;

                double leftPanelWidth = leftPanelIsVisible ? leftPanel.ActualWidth : 0.0;
                double gridSplitterWidth = gridSplitterVisible ? theGridSplitter.ActualWidth : 0.0;

                double theX = mousePosition.X - leftPanelWidth - gridSplitterWidth;
                double topBarHeight = topBar.ActualHeight;
                double theY = mousePosition.Y - topBarHeight;
                System.Windows.Point adjustedMousePosition = new System.Windows.Point(theX, theY);

                //Check if hit axis while dragging
                (List<GeometryModel3D> lineList, Vector3D rayDirection) =
                    SceneCollection.GetRayFromViewport(
                                            adjustedMousePosition,
                                            camera,
                                            myViewport.ActualWidth,
                                            myViewport.ActualHeight);

                //Get the closest hit triangle and index
                _draggingAxis = _sceneCollection.GetHitAxisGivenRay(
                                        sceneGroup,
                                        rayDirection,
                                        camera);
                if (_draggingAxis != ModelLoader.Axis.None)
                {
                    bool foundCenter = _sceneCollection.FindCenterOfSelectedMeshes(sceneGroup, out Point3D center, out Point3D min, out Point3D max);
                    if (foundCenter) {
                        _dragginSelectedCenter = center;
                    }
                }

            }
            
        }


        private void UpdateUIStats()
        {
            if (!_modelLoaded) { return; }

            PerspectiveCamera camera = (PerspectiveCamera)myViewport.Camera;
            double roundX = Math.Round(camera.Position.X, 4);
            double roundY = Math.Round(camera.Position.Y, 4);
            double roundZ = Math.Round(camera.Position.Z, 4);
            camLocX.Text = "X:" + roundX.ToString();
            camLocY.Text = "Y:" + roundY.ToString();
            camLocZ.Text = "Z:" + roundZ.ToString();

            viewportInfo.Text = "Width: " + myViewport.ActualWidth + ", " +
                " Height: " + Math.Truncate(myViewport.ActualHeight);

            // Calculate the angle
            double angleRadiansYaw = Math.Atan2(camera.Position.Z, camera.Position.X);
            double angleDegreesYaw = angleRadiansYaw * (180.0 / Math.PI);
            if (angleDegreesYaw < 0)
            {
                angleDegreesYaw += 360.0;
            }
            angleDegreesYaw = Math.Truncate(angleDegreesYaw);

            double angleRadiansRoll = Math.Atan2(camera.Position.Y, camera.Position.X);
            double angleDegreesRoll = angleRadiansRoll * (180.0 / Math.PI);
            if (angleDegreesRoll < 0)
            {
                angleDegreesRoll += 360.0;
            }

            angleDegreesRoll = Math.Truncate(angleDegreesRoll);
            camAngle.Text = "XZ: " + angleDegreesYaw.ToString() + "° " +
                "XY: " + angleDegreesRoll.ToString() + "°";
            meshCount.Text = sceneGroup.Children.Count().ToString("N0");
            vertCount.Text = _vertCount.ToString("N0");
            triangleCount.Text = _triangleCount.ToString("N0");
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateUIStats();
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_modelLoaded) { return; }

            System.Windows.Point curPos = e.GetPosition(this);
            camInfo.Text = "X:" + curPos.X + ", Y:" + curPos.Y;
            
            PerspectiveCamera camera = (PerspectiveCamera)myViewport.Camera;

            if (_isMouseDragging)
            {
                // Get the current mouse position
                System.Windows.Point currentPosition = e.GetPosition(this);
                // Calculate the delta of the mouse movement
                double scaleUp = 1.0;
                double deltaX = scaleUp * (_lastMousePosition.X - currentPosition.X);
                double deltaY = scaleUp * (_lastMousePosition.Y - currentPosition.Y);


                // If tool is Move, then check if they clicked an axis

                if (_selectedTool == ModelLoader.SelectedTool.select)
                {
                    // Adjust the camera's rotation based on the delta
                    SceneCollection.RotateCamera(camera,
                                            cameraLight,
                                            deltaX,
                                            deltaY);

                    // Update the last mouse position
                    _lastMousePosition = currentPosition;
                }

                if (_selectedTool == ModelLoader.SelectedTool.move)
                {

                    if (_draggingAxis == ModelLoader.Axis.None)
                    {
                        // Adjust the camera's rotation based on the delta
                        SceneCollection.RotateCamera(camera,
                                                cameraLight,
                                                deltaX,
                                                deltaY);

                        // Update the last mouse position
                        _lastMousePosition = currentPosition;
                    }
                    else
                    {
                        _sceneCollection.MoveSelectionOnAxis(sceneGroup, camera, _dragginSelectedCenter, deltaX, deltaY, _draggingAxis);
                    }
                }
            }

            if (_isMouseWheelDragging || _isMouseRightBtnDragging)
            {
                // Get the current mouse position
                System.Windows.Point currentPosition = e.GetPosition(this);
                // Calculate the delta of the mouse movement
                double scaleUp = 1.0;
                double deltaX = scaleUp * (_lastMousePosition.X - currentPosition.X);
                double deltaY = scaleUp * (_lastMousePosition.Y - currentPosition.Y);
                SceneCollection.MoveCameraLeft(camera, deltaX);
                SceneCollection.MoveCameraUp(camera, deltaY);

                // Update the last mouse position
                _lastMousePosition = currentPosition;
            }
            
            UpdateUIStats();
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // Stop dragging
            _isMouseDragging = false;
            _isMouseWheelDragging = false;
            _isMouseRightBtnDragging = false;

            _draggingAxis = ModelLoader.Axis.None;

            if (!_modelLoaded)
            {
                return;
            }

            bool shiftIsDown = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
            bool ctrlIsDown = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
            bool keepPrevSelections = shiftIsDown || ctrlIsDown || e.ChangedButton == MouseButton.Right;

            System.Windows.Point mousePosition = e.GetPosition(this);

            // Only case the ray if the mousedown location is the same as mouseup
            if (_originalMousePosition == mousePosition)
            {
                PerspectiveCamera camera = (PerspectiveCamera)myViewport.Camera;
                bool leftPanelIsVisible = leftPanel.Visibility == Visibility.Visible;
                bool gridSplitterVisible = theGridSplitter.Visibility == Visibility.Visible;

                double leftPanelWidth = leftPanelIsVisible ? leftPanel.ActualWidth : 0.0;
                double gridSplitterWidth = gridSplitterVisible ? theGridSplitter.ActualWidth : 0.0;
                
                double theX = mousePosition.X - leftPanelWidth - gridSplitterWidth;
                double topBarHeight = topBar.ActualHeight;
                double theY = mousePosition.Y - topBarHeight;
                System.Windows.Point adjustedMousePosition = new System.Windows.Point(theX, theY);

                // Get the ray from the click
                (List<GeometryModel3D> lineList, Vector3D rayDirection) =
                    SceneCollection.GetRayFromViewport(
                                            adjustedMousePosition,
                                            camera,
                                            myViewport.ActualWidth,
                                            myViewport.ActualHeight);

                // Get the closest hit triangle and index
                bool hitSomethingVisible = _sceneCollection.GetClosestVisibleHitGivenRay(sceneGroup,
                                        rayDirection,
                                        camera,
                                        out Point3D intersectionPoint,
                                        out int childHitIndex);

                // Colorize just the geometry that was hit
                if (hitSomethingVisible)
                {
                    // If they mouseup on an axis, do nothing
                    if (_sceneCollection.MeshIndexIsAnAxis(childHitIndex))
                    {
                        return;
                    }

                    // Note, SelectMesh will draw the axis when the move tool is selected
                    _sceneCollection.SelectMesh(sceneGroup, camera, childHitIndex, keepPrevSelections);

                    // Find the start of the GeometryModel3D
                    int adjustmentAmount = getStartIndexOfGM3D();
                    int adjustedIndex = adjustmentAmount >= 0 ? childHitIndex - adjustmentAmount : 0;

                    SelectTreeViewItemFromTag(adjustedIndex, theTreeView);
                }
                else
                {
                    if (!keepPrevSelections)
                    {
                        _sceneCollection.UnSelectAllVisibleMeshes(sceneGroup);
                    }
                }
            }
        }

        private void Viewport3D_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_modelLoaded && myViewport != null && myViewport.Camera != null)
            {
                _sceneCollection.MoveCameraForward(sceneGroup, (PerspectiveCamera)myViewport.Camera, e.Delta);
            }

            UpdateUIStats();
        }

        // This is needed because there can be lights and other objects in the scene
        private int getStartIndexOfGM3D()
        {
            int startIndex = -1;

            // Clean the sceneGroup incase there are left over items
            for (int i = 0; i < sceneGroup.Children.Count; i++)
            {
                if (sceneGroup.Children[i] is GeometryModel3D theModel)
                {
                    startIndex = i;
                    break;
                }
            }

            return startIndex;
        }


        // Helper method to get all child TreeViewItems of a given TreeViewItem
        private IEnumerable<TreeViewItem> GetChildTreeViewItems(TreeViewItem parent)
        {
            // Make sure the parent is expanded to generate its child items
            if (!parent.IsExpanded)
            {
                parent.IsExpanded = true;
                parent.UpdateLayout(); // Forces the layout system to create child items
                parent.IsExpanded = false;
                parent.UpdateLayout();
            }

            List<TreeViewItem> result = new List<TreeViewItem>();

            // Loop through the parent TreeViewItem's items
            foreach (object item in parent.Items)
            {
                // Check if the item is a TreeViewItem
                if (parent.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem child)
                {
                    result.Add(child);
                }
            }

            return result;
        }
        private void SelectTreeViewItemFromTag(int tagIndex, System.Windows.Controls.TreeView primeTree)
        {
            if (primeTree == null) return;

            // Detach the event handler temporarily
            RoutedPropertyChangedEventHandler<object> handler = theTreeView_SelectedItemChanged;
            primeTree.SelectedItemChanged -= handler;


            if (_meshTreeViewMapping.ContainsKey(tagIndex))
            {
                TreeViewItem showThisItem = _meshTreeViewMapping[tagIndex];
                showThisItem.IsSelected = true;
                showThisItem.BringIntoView();

                DependencyObject parent = showThisItem.Parent;
                while (parent != null)
                {
                    // If the parent is a TreeViewItem, expand it
                    if (parent is TreeViewItem parentTreeViewItem)
                    {
                        parentTreeViewItem.IsExpanded = true; // Expand the parent
                        parent = parentTreeViewItem.Parent;
                    }
                    else
                    {
                        break;
                    }
                }

            }

            // Reattach the event handler
            primeTree.SelectedItemChanged += handler;
        }

        private bool SelectChildItemByTag(TreeViewItem parentItem, int tagIndex)
        {
            foreach (TreeViewItem childItem in GetChildTreeViewItems(parentItem))
            {
                if (childItem.Tag is int selectionIndex && selectionIndex == tagIndex)
                {
                    // Select the item and bring it into view
                    childItem.IsSelected = true;

                    var container = (TreeViewItem)parentItem.ItemContainerGenerator.ContainerFromItem(childItem);
                    container?.BringIntoView(); // Bring the item into view if the container is found

                    return true; // Found and selected the item
                }

                // Recursively search in child items
                if (SelectChildItemByTag(childItem, tagIndex))
                {
                    return true; // Found in descendants
                }
            }

            return false; // Item not found
        }

        private void selectedTreeViewDescendants(TreeViewItem treeViewItem)
        {
            PerspectiveCamera camera = (PerspectiveCamera)myViewport.Camera;

            // Base case: If the TreeViewItem is null, just return
            if (treeViewItem == null) return;

            // Loop through each child of the TreeViewItem
            foreach (TreeViewItem childItem in GetChildTreeViewItems(treeViewItem))
            {
                if (childItem.Tag is int selectionIndex) {
                    //If they clicked a material
                    if (selectionIndex >= 0)
                    {
                        // Find the start of the GeometryModel3D
                        int adjustmentAmount = getStartIndexOfGM3D();
                        int adjustedIndex = adjustmentAmount >= 0 ? adjustmentAmount + selectionIndex : 0;

                        _sceneCollection.SelectMesh(sceneGroup, camera, adjustedIndex, true);
                    }
                }

                // Recursively visit descendants of the child item
                selectedTreeViewDescendants(childItem);
            }
        }

        private void theTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            PerspectiveCamera camera = (PerspectiveCamera)myViewport.Camera;

            // Check if the selected item is a TreeViewItem
            if (e.NewValue is TreeViewItem selectedItem)
            {
                if (selectedItem.Tag is int selectionIndex)
                {
                    bool isCtrlDown = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
                    bool isShiftDown = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                    bool keepPreviousSelections = isCtrlDown || isShiftDown;
                    if (!keepPreviousSelections)
                    {
                        // Deselect previious meshes, unless a key is down
                        _sceneCollection.UnSelectAllVisibleMeshes(sceneGroup);
                    }

                    //If they clicked a material
                    if (selectionIndex >= 0)
                    {
                        // Find the start of the GeometryModel3D
                        int adjustmentAmount = getStartIndexOfGM3D();
                        int adjustedIndex = adjustmentAmount >= 0 ? adjustmentAmount + selectionIndex : 0;
                        _sceneCollection.SelectMesh(sceneGroup, camera, adjustedIndex, keepPreviousSelections);
                    }
                    else
                    {
                        // For non-material, select everything material under the click
                        selectedTreeViewDescendants(selectedItem);
                    }
                }
            }
        }

        private void FillTreeView(System.Windows.Controls.TreeView treeView, List<ModelLoader.TreeNode> data)
        {
            foreach (var node in data)
            {
                if (node != null)
                {
                    int geoIndex = node.GeometryModel3DIndex;
                    var treeViewItem = new TreeViewItem { Header = node.Name, Tag = geoIndex };
                    AddChildren(treeViewItem, node.Children);
                    treeView.Items.Add(treeViewItem);
                    _meshTreeViewMapping[geoIndex] = treeViewItem;
                }
            }
        }

        private void AddChildren(TreeViewItem parentItem, List<ModelLoader.TreeNode> children)
        {
            foreach (var child in children)
            {
                int geoIndex = child.GeometryModel3DIndex;
                var treeViewItem = new TreeViewItem { Header = child.Name, Tag = geoIndex };
                AddChildren(treeViewItem, child.Children);
                parentItem.Items.Add(treeViewItem);
                _meshTreeViewMapping[geoIndex] = treeViewItem;
            }
        }

        private void populateUISceneFromModelData(Model3DGroup populateMe, List<ModelLoader.MyGeometryModel3D> tempAllModels)
        {
            if (tempAllModels == null) { return; }

            foreach (var aMyGeometryModel3D in tempAllModels)
            {
                var cubeMesh = aMyGeometryModel3D.CubeMesh;

                var positions = cubeMesh.Positions;
                var texturecords = cubeMesh.TextureCoordinates;
                var normals = cubeMesh.Normals;
                var triangleIndices = cubeMesh.TriangleIndices;
                var meshGeometry3D = new MeshGeometry3D
                {
                    Positions = new Point3DCollection(positions),
                    TextureCoordinates = new PointCollection(texturecords),
                    TriangleIndices = new Int32Collection(triangleIndices),
                    Normals = new Vector3DCollection(normals)
                };

                // Load a real Mataerial given the paramenters
                DiffuseMaterial finalMaterial;
                var theMaterial = aMyGeometryModel3D.ModelMaterial;

                if (!String.IsNullOrEmpty(theMaterial.mapKdFilePath))
                {
                    BitmapImage textureImage = new BitmapImage();
                    textureImage.BeginInit();
                    textureImage.UriSource = new Uri(theMaterial.mapKdFilePath, UriKind.Absolute);
                    textureImage.CacheOption = BitmapCacheOption.OnLoad;
                    textureImage.EndInit();

                    // Create an ImageBrush from the texture
                    ImageBrush textureBrush = new ImageBrush(textureImage);
                    // Create a DiffuseMaterial using the texture
                    finalMaterial = new DiffuseMaterial(textureBrush);
                }
                else
                {
                    float clampedDissolve = Math.Clamp((float)theMaterial.Dissolve, 0f, 1f);
                    System.Windows.Media.Color diffuseColor = System.Windows.Media.Color.FromScRgb(clampedDissolve, (float)theMaterial.Kd.X, (float)theMaterial.Kd.Y, (float)theMaterial.Kd.Z);
                    finalMaterial = new DiffuseMaterial(new SolidColorBrush(diffuseColor));
                }

                var geometry = new GeometryModel3D(meshGeometry3D, finalMaterial);
                populateMe.Children.Add(geometry);
            }
        }

        private async Task LoadModelFromPath(string pathToModel)
        {
            // Stop any rotating if occuring
            _isAutoRotatingHorz = false;
            _isAutoRotatingVert = false;
            _rotationHorzTimer.Stop();
            _rotationVertTimer.Stop();

            // LOAD -------------------
            List<GeometryModel3D> allModels = new List<GeometryModel3D>();
            List<string> newMeshNames = new List<string>();
            ModelLoader.TreeNode root = null;

            await Task.Run(() =>
            {
                List<ModelLoader.MyGeometryModel3D> tempAllModels = new List<MyGeometryModel3D>();
                List<string> tempNewMeshNames = new List<string>();
                ModelLoader.TreeNode tempRoot = new ModelLoader.TreeNode();
                string fileExtension = System.IO.Path.GetExtension(pathToModel);

                GeometryLoaderResult theResult = (GeometryLoaderResult)_sceneCollection.LoadFromFile(pathToModel);
                tempAllModels = theResult.Models;
                tempNewMeshNames = theResult.Metadata;
                tempRoot = theResult.Node;

                //On the main thread, create all the needed objects
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    populateUISceneFromModelData(sceneGroup, tempAllModels);
                });

                newMeshNames = tempNewMeshNames;
                root = tempRoot;
            });


            _meshNames.Clear();
            if (newMeshNames != null)
            {
                foreach (var mesh in newMeshNames)
                {
                    _meshNames.Add(mesh);
                }
            }
            // Clear the Tree view
            theTreeView.Items.Clear();
            theTreeView.ItemsSource = null;
            // File the theTreeView TreeView
            FillTreeView(theTreeView, new List<ModelLoader.TreeNode> { root });

            (double vertCount, double triangleCount, Point3D maxXYZ, Point3D minXYZ) = _sceneCollection.CountVertsAndTriangles(sceneGroup);
            _vertCount = vertCount;
            _triangleCount = triangleCount;

            double XMaxDist = Math.Abs(minXYZ.X) > Math.Abs(maxXYZ.X) ? Math.Abs(minXYZ.X) : maxXYZ.X;
            double YMaxDist = Math.Abs(minXYZ.Y) > Math.Abs(maxXYZ.Y) ? Math.Abs(minXYZ.Y) : maxXYZ.Y;
            double ZMaxDist = Math.Abs(minXYZ.Z) > Math.Abs(maxXYZ.Z) ? Math.Abs(minXYZ.Z) : maxXYZ.Z;

            // Get the distance to origin using the largest found value
            double modelMaxRadius = Math.Sqrt(Math.Pow(XMaxDist, 2) +
                            Math.Pow(YMaxDist, 2) +
                            Math.Pow(ZMaxDist, 2));

            // Create a PerspectiveCamera
            PerspectiveCamera camera = new PerspectiveCamera();
            camera.Position = new Point3D(0, 0, modelMaxRadius * 2);
            camera.LookDirection = new Vector3D(0, 0, -1);
            camera.UpDirection = new Vector3D(0, 1, 0); // Y-axis as up
            camera.FieldOfView = 60;
            myViewport.Camera = camera;

            // Light
            cameraLight.Direction = camera.LookDirection;

            _modelLoaded = true;

        }

        private void ChangeParentTabName(string newHeaderName)
        {
            TabItem parentTabItem = FindParentTabItem(this);
            if (parentTabItem != null)
            {
                parentTabItem.Header = newHeaderName;
            }
        }

        private TabItem? FindParentTabItem(DependencyObject child)
        {
            if (child == null) return null;
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is System.Windows.Controls.TabControl tabControl)
                {
                    TabItem? activeTab = tabControl.SelectedItem as TabItem;
                    return activeTab;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        public async void openFileDialogChooseModelFile()
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            //openFileDialog.Filter = "OBJ and STL Files (*.obj;*.stl)|*.obj;*.stl|OBJ Files (*.obj)|*.obj|STL Files (*.stl)|*.stl";
            openFileDialog.Filter = "All Supported Files (*.obj;*.stl;*.x3d)|*.obj;*.stl;*.x3d|OBJ Files (*.obj)|*.obj|STL Files (*.stl)|*.stl|X3D Files (*.x3d)|*.x3d";
            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                // Show progress bar
                progBar.IsIndeterminate = true;
                progBar.IsEnabled = true;

                string filePath = openFileDialog.FileName;
                await LoadModelFromPath(filePath);

                string fileNameOnly = System.IO.Path.GetFileName(filePath);
                ChangeParentTabName(fileNameOnly);

                // Hide proress bar
                progBar.IsIndeterminate = false;
                progBar.IsEnabled = false;
            }
        }
        private void LoadModel_Click(object sender, RoutedEventArgs e)
        {
            openFileDialogChooseModelFile();
        }

        private void Recenter_Click(object sender, RoutedEventArgs e)
        {
            double maxX = 0, maxY = 0, maxZ = 0;

            foreach (Model3D child in sceneGroup.Children)
            {
                // Check if the child is of type GeometryModel3D
                if (child is GeometryModel3D geometryModel)
                {
                    MeshGeometry3D mesh = geometryModel.Geometry as MeshGeometry3D;
                    if (mesh != null)
                    {
                        Point3DCollection positions = mesh.Positions;
                        foreach (Point3D point in positions)
                        {
                            maxX = Math.Max(maxX, Math.Abs(point.X));
                            maxY = Math.Max(maxY, Math.Abs(point.Y));
                            maxZ = Math.Max(maxZ, Math.Abs(point.Z));
                        }
                    }
                }
            }

            //Get the distance to origin using the largest found value
            double modelMaxRadius = Math.Sqrt(Math.Pow(maxX, 2) +
                Math.Pow(maxY, 2) +
                Math.Pow(maxZ, 2));
            PerspectiveCamera camera = new PerspectiveCamera();
            camera.Position = new Point3D(0, 0, modelMaxRadius * 2);
            camera.LookDirection = new Vector3D(0, 0, -1); // Looking towards the origin
            camera.UpDirection = new Vector3D(0, 1, 0); // Y-axis as up
            camera.FieldOfView = 60;
            myViewport.Camera = camera;
            cameraLight.Direction = camera.LookDirection;
        }

        private static void promptToSaveViewportImage(Viewport3D viewPort3D)
        {
            // Create and configure the SaveFileDialog
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PNG Files (*.png)|*.png|All Files (*.*)|*.*",
                DefaultExt = "png",
                FileName = "Untitled.png"
            };
            bool? result = saveFileDialog.ShowDialog();
            if (result == true)
            {
                string filePath = saveFileDialog.FileName;
                SaveViewportImageToFile(viewPort3D, filePath);
            }
        }

        private static void SaveViewportImageToFile(Viewport3D viewPort3D, string filePath)
        {
            int width = (int)viewPort3D.ActualWidth;
            int height = (int)viewPort3D.ActualHeight;
            RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);

            renderTargetBitmap.Render(viewPort3D);

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(fileStream);
            }
        }

        private void SaveViewPort_Click(object sender, RoutedEventArgs e)
        {
            promptToSaveViewportImage(myViewport);
        }

        private void BackgroundColor_Click(object sender, RoutedEventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog
            {
                Color = System.Drawing.Color.FromArgb(255, 255, 255)
            };
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                System.Drawing.Color selectedColor = colorDialog.Color;
                System.Windows.Media.Color wpfColor = System.Windows.Media.Color.FromArgb(
                    selectedColor.A, selectedColor.R, selectedColor.G, selectedColor.B);
                mainPanel.Background = new SolidColorBrush(wpfColor);
            }
        }

        private void LightsToggle_Click(object sender, RoutedEventArgs e)
        {
            var lightsToRemove = new List<AmbientLight>();
            bool foundLightToRemove = false;

            foreach (var model in sceneGroup.Children)
            {
                // Check if the model is an AmbientLight
                if (model is AmbientLight ambientLight)
                {
                    foundLightToRemove = true;
                    lightsToRemove.Add(ambientLight);
                }
            }


            if (foundLightToRemove)
            {
                // Remove the abient light
                foreach (var light in lightsToRemove)
                {
                    sceneGroup.Children.Remove(light);
                }
                var lightsOffUri = new Uri("pack://application:,,,/Images/lightsoff.png", UriKind.Absolute);
                lightImg.Source = new BitmapImage(lightsOffUri);
            }
            else
            {
                // Add an ambient light since none was found
                AmbientLight ambientLight = new AmbientLight(Colors.White);
                sceneGroup.Children.Add(ambientLight);

                // Update icon
                var lightsOnUri = new Uri("pack://application:,,,/Images/lightson.png", UriKind.Absolute);
                lightImg.Source = new BitmapImage(lightsOnUri);
            }
        }

        private void OnRotationHorzTimerTick(object sender, EventArgs e)
        {
            _sceneCollection.RotateSelectionOrCameraHorz(sceneGroup, (PerspectiveCamera)myViewport.Camera,
                                        cameraLight,
                                        -1.0,
                                        0.0);
            UpdateUIStats();
        }

        private void OnRotationVertTimerTick(object sender, EventArgs e)
        {
            _sceneCollection.RotateSelectionOrCameraVert(sceneGroup, (PerspectiveCamera)myViewport.Camera,
                                        cameraLight,
                                        -1.0,
                                        0.0);
            UpdateUIStats();
        }

        

        private void HideMesh_Click(object sender, RoutedEventArgs e)
        {
            _sceneCollection.HideSelectedMeshes(sceneGroup);
        }

        private void IsolateMesh_Click(object sender, RoutedEventArgs e)
        {
            _sceneCollection.HideAllUnSelectedMeshes(sceneGroup);
        }

        private void LeftPanelToggle_Click(object sender, RoutedEventArgs e)
        {
            if (_showLeftPanel)
            {
                _showLeftPanel = false;
                var showPanelUri = new Uri("pack://application:,,,/Images/panelshow.png", UriKind.Absolute);
                panelshowImg.Source = new BitmapImage(showPanelUri);
                leftPanel.Visibility = Visibility.Collapsed;
                theGridSplitter.Visibility = Visibility.Collapsed;
            }
            else
            {
                _showLeftPanel = true;
                var hidePanelUri = new Uri("pack://application:,,,/Images/panelhide.png", UriKind.Absolute);
                panelshowImg.Source = new BitmapImage(hidePanelUri);
                leftPanel.Visibility = Visibility.Visible;
                theGridSplitter.Visibility = Visibility.Visible;
            }
        }


        private void GridSplitter_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            double newWidth = leftPanel.ActualWidth + e.HorizontalChange;
            if (newWidth > 0)
            {
                leftPanel.Width = newWidth;
            }
        }

        private void RotateHorzToggle_Click(object sender, RoutedEventArgs e)
        {
            if (_isAutoRotatingHorz)
            {
                _isAutoRotatingHorz = false;
                _rotationHorzTimer.Stop();
                _sceneCollection.ClearHorzRotationLists();

                var lightsOnUri = new Uri("pack://application:,,,/Images/rotaterightoff.png", UriKind.Absolute);
                rotateHorzImg.Source = new BitmapImage(lightsOnUri);
            }
            else
            {
                // On first click, move the selection over
                _sceneCollection.MoveSelectionToHorzRotationList(sceneGroup);

                _isAutoRotatingHorz = true;
                _rotationHorzTimer.Start();
                var lightsOnUri = new Uri("pack://application:,,,/Images/rotaterighton.png", UriKind.Absolute);
                rotateHorzImg.Source = new BitmapImage(lightsOnUri);
            }
        }

        private void RotateHorz_Click(object sender, RoutedEventArgs e)
        {
            // On first click, move the selection over
            _sceneCollection.MoveSelectionToHorzRotationList(sceneGroup);

            if (!_rotationHorzTimer.IsEnabled)
            {
                _isAutoRotatingHorz = true;
                _rotationHorzTimer.Start();
                var lightsOnUri = new Uri("pack://application:,,,/Images/rotaterighton.png", UriKind.Absolute);
                rotateHorzImg.Source = new BitmapImage(lightsOnUri);
            }
        }

        private void RotateVert_Click(object sender, RoutedEventArgs e)
        {
            // On first click, move the selection over
            _sceneCollection.MoveSelectionToVertRotationList(sceneGroup);

            if (!_rotationVertTimer.IsEnabled)
            {
                _isAutoRotatingVert = true;
                _rotationVertTimer.Start();
                var lightsOnUri = new Uri("pack://application:,,,/Images/rotateverticallyon.png", UriKind.Absolute);
                rotateVertImg.Source = new BitmapImage(lightsOnUri);
            }
        }

        private void SelectToggle_Click(object sender, RoutedEventArgs e)
        {
            _selectedTool = ModelLoader.SelectedTool.select;
            _sceneCollection.ChooseTool(_selectedTool);

            var moveOffURI = new Uri("pack://application:,,,/Images/moveoff.png", UriKind.Absolute);
            moveImg.Source = new BitmapImage(moveOffURI);

            var selectOnURI = new Uri("pack://application:,,,/Images/selecton.png", UriKind.Absolute);
            selectImg.Source = new BitmapImage(selectOnURI);

            //Clear the movable axis
            _sceneCollection.ClearAxisFromMove(sceneGroup);
        }

        private void MoveToggle_Click(object sender, RoutedEventArgs e)
        {
            _selectedTool = ModelLoader.SelectedTool.move;
            _sceneCollection.ChooseTool(_selectedTool);

            var selectOffURI = new Uri("pack://application:,,,/Images/selectoff.png", UriKind.Absolute);
            selectImg.Source = new BitmapImage(selectOffURI);

            var moveOnURI = new Uri("pack://application:,,,/Images/moveon.png", UriKind.Absolute);
            moveImg.Source = new BitmapImage(moveOnURI);

            //Draw axis over currently selected meshes
            PerspectiveCamera camera = (PerspectiveCamera)myViewport.Camera;
            _sceneCollection.DrawAxisLinesFromSelectedMeshes(sceneGroup, camera);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Create and configure the SaveFileDialog
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PNG Files (*.obj)|*.obj|All Files (*.*)|*.*",
                DefaultExt = "obj",
                FileName = "Untitled.obj"
            };
            bool? result = saveFileDialog.ShowDialog();

            if (result != true)
            {
                return;
            }

            string filePath = saveFileDialog.FileName;
            var finalLines = new List<string>();

            foreach (Model3D child in sceneGroup.Children)
            {
                // Check if the child is of type GeometryModel3D
                if (child is GeometryModel3D geometryModel)
                {
                    //TODO, get the name of the material
                    Material curMaterial = geometryModel.Material;

                    //finalLines.Add(curMaterial);

                    MeshGeometry3D mesh = geometryModel.Geometry as MeshGeometry3D;
                    if (mesh != null)
                    {
                        List<string> listPositions = new List<string>();
                        List<string> listNormals = new List<string>();
                        List<string> listTextures = new List<string>();
                        List<string> listFaces = new List<string>();

                        Point3DCollection positions = mesh.Positions;
                        Int32Collection triangles = mesh.TriangleIndices;
                        PointCollection textures = mesh.TextureCoordinates;
                        Vector3DCollection normals = mesh.Normals;

                        for (int j = 0; j < positions.Count; j += 3)
                        {
                            Point3D curPoint = positions[j];
                            string vLine = "v" + " " + curPoint.X + " " + curPoint.Y + " " + curPoint.Z;
                            listPositions.Add(vLine);
                        }

                        for (int j = 0; j < normals.Count; j += 3)
                        {
                            Vector3D curNormal = normals[j];
                            string vLine = "vn" + " " + curNormal.X + " " + curNormal.Y + " " + curNormal.Z;
                            listNormals.Add(vLine);
                        }

                        for (int j = 0; j < textures.Count; j += 3)
                        {
                            System.Windows.Point curTextureCoord = textures[j];
                            string vLine = "vt" + " " + curTextureCoord.X + " " + curTextureCoord.Y;
                            listTextures.Add(vLine);
                        }

                        //TODO, maybe add the material to the first item of listFaces
                        for (int j = 0; j < triangles.Count; j += 3)
                        {
                            // Get the three vertices of the triangle
                            //Remember to add 1 since there is no 0
                            int vertexIndex1 = (1 + triangles[j]);
                            int vertexIndex2 = (1 + triangles[j + 1]);
                            int vertexIndex3 = (1 + triangles[j + 2]);

                            string vLine = "f" + " " + vertexIndex1 + " " + vertexIndex2 + " " + vertexIndex3;
                            listFaces.Add(vLine);
                        }

                        finalLines.Add("#Positions:");
                        finalLines.AddRange(listPositions);
                        finalLines.Add("");  // Adds a blank line for separation

                        finalLines.Add("#Normals:");
                        finalLines.AddRange(listNormals);
                        finalLines.Add("");  // Adds a blank line for separation

                        finalLines.Add("#Textures:");
                        finalLines.AddRange(listTextures);
                        finalLines.Add("");  // Adds a blank line for separation

                        finalLines.Add("#Faces:");
                        finalLines.AddRange(listFaces);
                        finalLines.Add("");

                    }
                }
            }

            // Write file to disk
            File.WriteAllLines(filePath, finalLines);

        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            string theMsg = "By David S. Shelley - (2024)" + Environment.NewLine + Environment.NewLine;
            

            (double numVerts, double numTriangles, Point3D maxValues, Point3D minValues) = _sceneCollection.CountVertsAndTriangles(sceneGroup);
            Point3D finalMaxValues = new Point3D(maxValues.X - minValues.X, maxValues.Y - minValues.Y, maxValues.Z - minValues.Z);


            theMsg += "Vertices: " + numVerts.ToString("N0") + Environment.NewLine;
            theMsg += "Triangles: " + numTriangles.ToString("N0") + Environment.NewLine + Environment.NewLine;

            theMsg += "Size X: " + finalMaxValues.X + Environment.NewLine;
            theMsg += "Size Y: " + finalMaxValues.Y + Environment.NewLine;
            theMsg += "Size Z: " + finalMaxValues.Z + Environment.NewLine;

            MessageBox.Show(theMsg);
        }

        private void RotateVertToggle_Click(object sender, RoutedEventArgs e)
        {
            if (_isAutoRotatingVert)
            {
                _isAutoRotatingVert = false;
                _rotationVertTimer.Stop();
                _sceneCollection.ClearVertRotationLists();

                var lightsOnUri = new Uri("pack://application:,,,/Images/rotateverticallyoff.png", UriKind.Absolute);
                rotateVertImg.Source = new BitmapImage(lightsOnUri);
            }
            else
            {
                // On first click, move the selection over
                _sceneCollection.MoveSelectionToVertRotationList(sceneGroup);

                _isAutoRotatingVert = true;
                _rotationVertTimer.Start();
                var lightsOnUri = new Uri("pack://application:,,,/Images/rotateverticallyon.png", UriKind.Absolute);
                rotateVertImg.Source = new BitmapImage(lightsOnUri);
            }
        }
    }
}
