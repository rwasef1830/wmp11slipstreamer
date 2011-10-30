using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Epsilon.IO
{
    public class FileSystemEntry
    {
        string _name;
        string _parentPath;
        string _fullPath;
        string _shortName;
        FileAttributes _attributes;
        DateTime? _created;
        DateTime? _accessed;
        DateTime? _modified;
        long _fileSize;

        public string Name
        {
            get { return this._name; }
        }

        public string ParentPath
        {
            get { return this._parentPath; }
        }

        public string FullPath
        {
            get
            {
                if (this._fullPath == null)
                {
                    this._fullPath
                        = this._parentPath + Path.DirectorySeparatorChar + this._name;
                }
                return this._fullPath;
            }
        }

        public FileAttributes Attributes
        {
            get { return this._attributes; }
        }

        public DateTime? Created
        {
            get { return this._created; }
        }

        public DateTime? Accessed
        {
            get { return this._accessed; }
        }

        public DateTime? Modified
        {
            get { return this._modified; }
        }

        public string ShortName
        {
            get { return this._shortName; }
        }

        public long FileSize
        {
            get
            {
                if (this.IsDirectory)
                {
                    throw new InvalidOperationException(
                        "This property is only valid for files.");
                }
                else
                {
                    return this._fileSize;
                }
            }
        }

        public bool IsDirectory
        {
            get { return (this._attributes & FileAttributes.Directory) != 0; }
        }

        public bool Exists()
        {
            return File.Exists(this._fullPath);
        }

        public void Delete()
        {
            File.Delete(this._fullPath);
        }

        public IEnumerable<FileSystemEntry> WalkChildren()
        {
            if (!this.IsDirectory)
                throw new InvalidOperationException(
                    "This operation is only valid for directories.");

            return FileSystem.WalkTree(this._fullPath);
        }

        internal FileSystemEntry(
            string name, 
            string parentPath, 
            FileAttributes attributes,
            DateTime? created,
            DateTime? accessed,
            DateTime? modified,
            string shortName,
            long fileSize)
        {
            this._name = name;
            this._parentPath = parentPath;
            this._attributes = attributes;
            this._created = created;
            this._accessed = accessed;
            this._modified = modified;
            this._shortName = shortName;
            this._fileSize = fileSize;
        }
    }
}
