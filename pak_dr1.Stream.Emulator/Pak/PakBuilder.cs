using FileEmulationFramework.Lib.IO;
using FileEmulationFramework.Lib.IO.Struct;
using FileEmulationFramework.Lib.Utilities;
using Microsoft.Win32.SafeHandles;
using System.Text.RegularExpressions;

namespace PAK_DR1.Stream.Emulator.Pak
{
    internal class PakBuilder
    {
        private readonly Dictionary<int, FileSlice> _customFiles = new();

        /// <summary>
        /// Adds a file to the Virtual PAK builder.
        /// </summary>


        /// <summary>
        /// Adds a file to the Virtual PAK builder.
        /// </summary>
        /// <param name="filePath">Full path to the file.</param>
        /// 
        static Regex regex = new Regex(@"(\[.*?\])");
        public void AddOrReplaceFile(string filePath)
        {
            string[] filePathSplit = filePath.Split(Constants.PakExtension + Path.DirectorySeparatorChar);
            string x = filePathSplit[^1];
            x = Path.GetFileNameWithoutExtension(x);
            //y = Path.GetFileNameWithoutExtension(y);

            if (x.Contains("[") && x.Contains("]"))
                x = regex.Match(x).Value.Replace("[", "").Replace("]", "");
            int id = int.Parse(x);
            _customFiles[id] = new(filePath);
        }
        public unsafe MultiStream Build(IntPtr handle, string pakFilePath, Logger? logger = null)
        {
            logger?.Info($"[{nameof(PakBuilder)}] Building Pak File | {{0}}", pakFilePath);

            var stream = new FileStream(new SafeFileHandle(handle, false), FileAccess.Read);
            stream.Position = 0;
            DanganPAKLib.Pak pak = new DanganPAKLib.Pak(stream);


            System.IO.Stream headerStream = new MemoryStream();

            headerStream.Position = 0;

            headerStream.Write(BitConverter.GetBytes(pak.FileEntries.Count));

            var pairs = new List<StreamOffsetPair<System.IO.Stream>>()
            {
                // Add Header
                new (headerStream, OffsetRange.FromStartAndLength(0, 4 + ((pak.FileEntries.Count) * 4)))
            };
            int offset =  4 + ((pak.FileEntries.Count) * 4);
            for (int i = 0; i < pak.FileEntries.Count; i++)
            {
                headerStream.Write(BitConverter.GetBytes(offset));
                if (_customFiles.ContainsKey(i))
                {
                    //System.IO.Stream customFileStream = new FileStream(new SafeFileHandle(i, false), FileAccess.Read);
                    var fileSliceStream = new FileSliceStreamFs(_customFiles[i]); //new FileStream(new SafeFileHandle(_customFiles[i].Handle, false), FileAccess.Read); //new FileSliceStreamW32(_customFiles[i], logger);

                    pairs.Add(new (fileSliceStream, OffsetRange.FromStartAndLength(offset, _customFiles[i].Length)));
                    offset += _customFiles[i].Length;
                    logger.Debug("[PAK] Inserted new file at slot " + i.ToString() + "\n");
                }
                else
                {
                    var fileSlice = new FileSlice(pak.FileEntries[i].Offset, pak.FileEntries[i].Size, pakFilePath);
                    var fileSLiceStream = new FileSliceStreamW32(fileSlice, logger);
                    pairs.Add(new(fileSLiceStream, OffsetRange.FromStartAndLength(offset, pak.FileEntries[i].Size)));
                    offset += pak.FileEntries[i].Size;
                    logger.Debug("[PAK] normal slot " + i.ToString() + "\n");
                }
            }
           

            return new MultiStream(pairs, logger);

        }

    }
}
