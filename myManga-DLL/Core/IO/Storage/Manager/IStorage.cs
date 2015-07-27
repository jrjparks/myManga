using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.IO.Storage.Manager
{
    /// <summary>
    /// Generic interface for storage
    /// </summary>
    public interface IStorage<T> where T: FileStorageInformationObject
    {
        /// <summary>
        /// Generic method to handel a write request
        /// </summary>
        /// <param name="filename">Target file name</param>
        /// <param name="stream">Stream to read from</param>
        /// <param name="args">Additional arguments</param>
        /// <returns></returns>
        Boolean Write(String filename, Stream stream, params Object[] args);

        /// <summary>
        /// Generic method to handel a read request
        /// </summary>
        /// <param name="filename">Target file name</param>
        /// <param name="args">Additional arguments</param>
        /// <returns></returns>
        Stream Read(String filename, params Object[] args);

        /// <summary>
        /// Generic method to try to handel a read request
        /// </summary>
        /// <param name="filename">Target file name</param>
        /// <param name="stream">Stream to write to</param>
        /// <param name="args">Additional arguments</param>
        /// <returns></returns>
        Boolean TryRead(String filename, out Stream stream, params Object[] args);

        /// <summary>
        /// Return information about file.
        /// </summary>
        /// <param name="filename">Target file name</param>
        /// <param name="args">Additional arguments</param>
        /// <returns></returns>
        T GetInformation(String filename, params Object[] args);
        
        /// <summary>
        /// Method to clean anything up
        /// </summary>
        void Destroy();
    }
}
