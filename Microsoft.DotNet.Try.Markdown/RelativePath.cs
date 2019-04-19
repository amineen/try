﻿using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.Try.Markdown
{
    public abstract class RelativePath
    {
        public string Value { get; protected set; }

        public override int GetHashCode() =>
            Value.GetHashCode();

        public override string ToString() => Value;

        private static readonly HashSet<char> DisallowedPathChars = new HashSet<char>(
            new char[]
            {
                '|',
                '\0',
                '\u0001',
                '\u0002',
                '\u0003',
                '\u0004',
                '\u0005',
                '\u0006',
                '\a',
                '\b',
                '\t',
                '\n',
                '\v',
                '\f',
                '\r',
                '\u000e',
                '\u000f',
                '\u0010',
                '\u0011',
                '\u0012',
                '\u0013',
                '\u0014',
                '\u0015',
                '\u0016',
                '\u0017',
                '\u0018',
                '\u0019',
                '\u001a',
                '\u001b',
                '\u001c',
                '\u001d',
                '\u001e',
                '\u001f'
            });

        private static readonly HashSet<char> DisallowedFileNameChars = new HashSet<char>(
            new char[]
            {
                '"',
                '<',
                '>',
                '|',
                '\0',
                '\u0001',
                '\u0002',
                '\u0003',
                '\u0004',
                '\u0005',
                '\u0006',
                '\a',
                '\b',
                '\t',
                '\n',
                '\v',
                '\f',
                '\r',
                '\u000e',
                '\u000f',
                '\u0010',
                '\u0011',
                '\u0012',
                '\u0013',
                '\u0014',
                '\u0015',
                '\u0016',
                '\u0016',
                '\u0017',
                '\u0018',
                '\u0019',
                '\u001a',
                '\u001b',
                '\u001c',
                '\u001d',
                '\u001e',
                '\u001f',
                ':',
                '*',
                '?',
                '\\'
            });

        public static string NormalizeDirectory(string directoryPath)
        {
            directoryPath = directoryPath.Replace('\\', '/');

            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                directoryPath = "./";
            }
            else
            {
                directoryPath = directoryPath.TrimEnd('\\', '/') + '/';
            }

            ThrowIfContainsDisallowedDirectoryPathChars(directoryPath);

            return directoryPath;
        }

        protected static void ThrowIfContainsDisallowedDirectoryPathChars(string path)
        {
            for (var index = 0; index < path.Length; index++)
            {
                var ch = path[index];
                if (DisallowedPathChars.Contains(ch))
                {
                    throw new ArgumentException($"The character {ch} is not allowed in the path");
                }
            }
        }

        protected static void ThrowIfContainsDisallowedFilePathChars(string filename)
        {
            for (var index = 0; index < filename.Length; index++)
            {
                var ch = filename[index];
                if (DisallowedFileNameChars.Contains(ch))
                {
                    throw new ArgumentException($"The character {ch} is not allowed in the filename");
                }
            }
        }
    }
}