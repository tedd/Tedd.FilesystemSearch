using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;

namespace Tedd
{
    public static class FilesystemSearch
    {
        public enum MatchType
        {
            /// <summary>
            /// Match exactly.
            /// </summary>
            Exact,
            /// <summary>
            /// Plain string match.
            /// </summary>
            Contains,
            /// <summary>
            /// Wildcard supports * and ?.
            /// </summary>
            Wildcards,
            /// <summary>
            /// Case insensitive regex search.
            /// </summary>
            Regex
        }

        public enum MatchTarget
        {
            /// <summary>
            /// Match only filename.
            /// </summary>
            File,
            /// <summary>
            /// Match each directory name separately.
            /// </summary>
            Directory,
            /// <summary>
            /// Match filename or separate directory name.
            /// </summary>
            FileOrDirectory,
            /// <summary>
            /// Match full path as a single string.
            /// (Match both directory and filename in one pattern.)
            /// </summary>
            FullPath
        }
        private const char DirectorySeparatorChar = '\\';
        private const char AltDirectorySeparatorChar = '/';
        private const char VolumeSeparatorChar = ':';
        private static readonly char[] DirectorySeparatorChars = { '\\', '/' };

        public static IEnumerable<string> FindFiles(string directory, string pattern, bool recursive, MatchType matchType = MatchType.Wildcards, MatchTarget matchTarget = MatchTarget.File)
        {
            Regex r = null;

            // Handle special case of null/empty
            if (string.IsNullOrEmpty(pattern))
                return FindFiles(directory, new Regex(""), recursive);

            // Crate correct regex object
            switch (matchType)
            {
                case MatchType.Exact:
                    r = new Regex("^" + Regex.Escape(pattern) + "$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    break;
                case MatchType.Contains:
                    r = new Regex(Regex.Escape(pattern), RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    break;
                case MatchType.Wildcards:
                    var tmp = Regex.Escape(pattern).Replace(@"\*", "$").Replace(@"\?", ".");
                    r = new Regex(tmp, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    break;
                case MatchType.Regex:
                    r = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    break;
                default:
                    throw new Exception("Unknown FileMatchType.");
            }

            return FindFiles(directory, r, recursive);

        }

        public static IEnumerable<string> FindFiles(string directory, Regex regex, bool recursive, MatchType matchType = MatchType.Wildcards, MatchTarget matchTarget = MatchTarget.File)
        {
            // We use IEnumerable so as to leave it up to caller if they want to build a full list. This way our string objects are potentially short lived and won't bother GC so much.

            // No directory provided = working dir
            if (string.IsNullOrEmpty(directory))
                directory = ".";

            // Directory queue - we only allocate one if this is a recursive search
            Queue<string> directories = null;
            if (recursive)
            {
                // We'll be using queue, so create one
                directories = new Queue<string>(1_024);
                // Add current directory
                directories.Enqueue(directory);
            }

            do
            {
                // If recursive we use queue, if not we only have one dir
                var dir = recursive ? directories.Dequeue() : directory;

                // If recursive enabled then queue 
                if (recursive)
                    foreach (var ndir in Directory.GetDirectories(directory))
                        directories.Enqueue(ndir);

                // Return any matches in current folder
                foreach (var file in Directory.GetFiles(dir))
                    if ((matchTarget == MatchTarget.File && regex.IsMatch(Path.GetFileName(file)))
                     || (matchTarget == MatchTarget.Directory && MatchDir(Path.GetDirectoryName(file), regex))
                     || (matchTarget == MatchTarget.FileOrDirectory && MatchDir(file, regex))
                     || (matchTarget == MatchTarget.FullPath && regex.IsMatch(file)))
                        yield return file;

            } while (directories != null && directories.Count > 0);
        }

        private static bool MatchDir(string dir, Regex regex)
        {
            foreach (var sd in dir.Split(DirectorySeparatorChars, StringSplitOptions.RemoveEmptyEntries))
            {
                if (sd.Contains(VolumeSeparatorChar))
                    continue;
                if (regex.IsMatch(sd))
                    return true;
            }

            return false;
        }

    }
}
