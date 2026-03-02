using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace core
{
    public abstract class ConfigFileLoader
    {
        public const string FileFormatBinary = "{0}.bytes";
        public const string FileFormatJson = "{0}.json";
        protected const string FileFormatEmpty = "{0}";
        
        public string PathRoot { get; protected set; }

        protected ConfigFileLoader(string pathRoot) { PathRoot = pathRoot; }

        /// <summary>
        /// This loading is used for versions file
        /// </summary>
        public virtual async Task<string> LoadJsonFile(string file)
        {
            return string.Empty;
        }
        
        /// <summary>
        /// This save is used for versions file
        /// </summary>
        public virtual async Task SaveJsonFile(string fileName, object serializeData) {}

        /// <summary>
        /// This loading is used for binary config files
        /// </summary>
        public abstract UniTask<List<T>> LoadBinaryFile<T>(string file);
        
        
        public virtual void RemoveFile(string fileName){}

        public virtual bool IsLoaderValid(string fileName) => IsExist(fileName, FileFormatBinary);
        
        protected bool IsExist(string file, string fileFormat)
        {
            return File.Exists(GetPath(file, fileFormat));
        }
        
        public string GetPath(string file, string fileFormat)
        {
            return Path.Combine(PathRoot, string.Format(fileFormat, file));
        }
    }
}