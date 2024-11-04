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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace _3dTests
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TabItem _draggedTab; //Hold currently dragged tab
        private Point _startPoint;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void TabItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TabItem tabItem = sender as TabItem;
            if (tabItem != null && tabItem.Name != "AddTabItem")
            {
                _draggedTab = sender as TabItem;
                _startPoint = e.GetPosition(null);
            }
            else
            {
                _draggedTab = null;
            }
        }

        private void TabItem_MouseMove(object sender, MouseEventArgs e)
        {
            if (_draggedTab != null && e.LeftButton == MouseButtonState.Pressed)
            {
                Point mousePos = e.GetPosition(null);
                Vector diff = _startPoint - mousePos;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    // Start the drag-and-drop operation
                    DragDrop.DoDragDrop(_draggedTab, _draggedTab, DragDropEffects.Move);
                }
            }
        }
        private void TabItem_Drop(object sender, DragEventArgs e)
        {
            TabItem targetTab = sender as TabItem;
            if (targetTab == null || _draggedTab == null) return;

            int targetIndex = MyTabControl.Items.IndexOf(targetTab);
            int draggedIndex = MyTabControl.Items.IndexOf(_draggedTab);

            // Handle the reorder
            if (draggedIndex != targetIndex && _draggedTab.Name != "AddTabItem")
            {
                MyTabControl.SelectedItem = null;
                // Unsubscribe from the SelectionChanged event to prevent it from firing
                MyTabControl.SelectionChanged -= MyTabControl_SelectionChanged;
                
                MyTabControl.Items.Remove(_draggedTab);
                MyTabControl.Items.Insert(targetIndex, _draggedTab);

                TabItem tabItem = MyTabControl.Items[targetIndex] as TabItem;
                if (tabItem != null)
                {
                    tabItem.IsSelected = true;
                }

                // Re-subscribe to the SelectionChanged event
                MyTabControl.SelectionChanged += MyTabControl_SelectionChanged;
            }
            _draggedTab = null; // Reset dragged tab
        }

        private void TabItem_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(typeof(TabItem)) ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        }


        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            // Mark the event as handled
            e.Handled = true;

            Button closeButton = sender as Button;
            if (closeButton != null)
            {
                // Get the TabItem associated with the button
                // Given the button is an ancestor
                TabItem tabItem = closeButton.Tag as TabItem;
                if (tabItem != null)
                {
                    // Unsubscribe from the SelectionChanged event to prevent it from firing
                    MyTabControl.SelectionChanged -= MyTabControl_SelectionChanged;

                    // Only remove if more than 2 tabs left
                    if (MyTabControl.Items.Count > 2)
                    {
                        int index = MyTabControl.Items.IndexOf(tabItem);
                        // Remove the tab from the TabControl
                        MyTabControl.Items.Remove(tabItem);

                        // Select the previous tab if it exists
                        if (index > 0)
                        {
                            MyTabControl.SelectedIndex = index - 1; // Select the tab before the removed one
                        }
                        else if (MyTabControl.Items.Count > 0)
                        {
                            MyTabControl.SelectedIndex = 0; // Select the first tab if it's the only one
                        }
                    }

                    // Re-subscribe to the SelectionChanged event
                    MyTabControl.SelectionChanged += MyTabControl_SelectionChanged;
                }
            }
        }

        private void MyTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Check if the selected item is the "Add Tab+" tab
            if (MyTabControl.SelectedItem is TabItem selectedTab && selectedTab.Name == "AddTabItem")
            {
                // Remove the selection of the "Add Tab+" tab temporarily
                // Otherwize this might et called again
                MyTabControl.SelectedItem = null;

                // Add a new tab
                AddNewTab();

                // Set to null since sometimes the drag and drop inserts a duplicate
                // If the user moves the mouse when clicing
                _draggedTab = null;
            }
        }

        private void AddNewTab()
        {
            TabItem newTab = new TabItem();
            newTab.Header = "New Tab";
            newTab.HeaderTemplate = (DataTemplate)FindResource("TabHeaderTemplate");

            // Create a new instance of the UserControl for your DockPanel
            ModelUIControl dockPanelInstance = new ModelUIControl();

            newTab.Header = "Untitled" + MyTabControl.Items.Count;
            newTab.Content = dockPanelInstance;

            // Allow for dragging
            newTab.AllowDrop = true;
            newTab.PreviewMouseLeftButtonDown += TabItem_PreviewMouseLeftButtonDown;

            newTab.Drop += TabItem_Drop;
            newTab.DragEnter += TabItem_DragEnter;

            // Add the new TabItem to the TabControl before the last tab
            MyTabControl.Items.Insert(MyTabControl.Items.Count - 1, newTab);

            // Select the newly added tab
            newTab.IsSelected = true;

            // Show the open file dialog right away
            dockPanelInstance.openFileDialogChooseModelFile();
        }

    }
}
