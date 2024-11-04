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
using System.IO;

namespace ModelLoader
{
    internal class ObjFileChunk
    {
        public string ObjectName { get; set; }
        public List<string> Lines { get; set; } = new List<string>();
    }
    internal class ObjFileReader
    {
        public static List<ObjFileChunk> ReadObjFile(string filePath, string[] delimiters)
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);
            return ReadObjFile(fileBytes, delimiters);
        }

        public static List<ObjFileChunk> ReadObjFile(byte[] fileBytes, string[] delimiters)
        {
            var chunks = new List<ObjFileChunk>();
            ObjFileChunk currentChunk = null;

            using (MemoryStream memoryStream = new MemoryStream(fileBytes))
            using (StreamReader reader = new StreamReader(memoryStream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    bool foundDelimiterOnLine = false;
                    string theFoundDelimiter = "";
                    foreach(string delimiter in delimiters)
                    {
                        if (line.StartsWith(delimiter))
                        {
                            theFoundDelimiter = delimiter;
                            foundDelimiterOnLine = true;
                            break;
                        }
                    }
                    if (foundDelimiterOnLine)
                    {
                        // If there is an existing chunk, add it to the list
                        if (currentChunk != null)
                        {
                            chunks.Add(currentChunk);
                        }

                        int index = line.IndexOf(theFoundDelimiter);
                        string parsedObjectName = "";
                        if (index != -1)
                        {
                            string modifiedString = line.Remove(index, theFoundDelimiter.Length);
                            parsedObjectName = modifiedString;
                        }
                        else
                        {
                            parsedObjectName = "Unknown";
                        }

                        // Start a new chunk
                        currentChunk = new ObjFileChunk
                        {
                            ObjectName = parsedObjectName
                        };
                    }

                    // Add line to the current chunk
                    if (currentChunk != null)
                    {
                        currentChunk.Lines.Add(line);
                    }
                }

                // Add the last chunk if it exists
                if (currentChunk != null)
                {
                    chunks.Add(currentChunk);
                }
            }

            // If there are zero chunks, assume the whole byte array is one object
            if (chunks.Count == 0)
            {
                var wholeFileChunk = new ObjFileChunk
                {
                    ObjectName = "UnnamedObject",
                    Lines = new List<string>()
                };

                using (MemoryStream memoryStream = new MemoryStream(fileBytes))
                using (StreamReader reader = new StreamReader(memoryStream))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        wholeFileChunk.Lines.Add(line);
                    }
                }

                chunks.Add(wholeFileChunk);
            }

            return chunks;
        }
    }
}
