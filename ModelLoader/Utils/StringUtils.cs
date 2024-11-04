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
using System.Text.RegularExpressions;

namespace ModelLoader
{
    internal static class StringUtils
    {
        internal static string RemoveAfterHash(string input)
        {
            int index = input.IndexOf('#');
            if (index >= 0)
            {
                return input.Substring(0, index);
            }
            return input; // Return the original string if no '#' is found
        }

        internal static string NormalizeSpaces(string input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            // Trim leading and trailing spaces
            string trimmed = input.Trim();

            // Replace multiple spaces with a single space
            string normalized = Regex.Replace(trimmed, @"\s+", " ");

            return normalized;
        }
    }
}
