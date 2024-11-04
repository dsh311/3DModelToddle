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

using System.Collections.Generic;

namespace ModelLoader
{
    public class GeometryLoaderResult : IGeometryLoaderResult
    {
        public List<MyGeometryModel3D> Models { get; set; }
        public List<string> Metadata { get; set; }
        public TreeNode Node { get; set; }

        public GeometryLoaderResult(List<MyGeometryModel3D> models, List<string> metadata, TreeNode node)
        {
            Models = models;
            Metadata = metadata;
            Node = node;
        }
    }
}
