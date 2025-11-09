using System;
using System.Collections.Generic;
using System.IO;
using DropTableEditor.Engine.Data;
using DropTableEditor.Misc;

namespace DropTableEditor.Engine
{
    /// <summary>
    /// FileManager class - Simplified for Drop Table Editor.
    /// Only loads STB/STL files needed for drop table editing.
    /// </summary>
    public static class FileManager
    {
        #region Member Declarations

        /// <summary>
        /// Gets or sets the STBs.
        /// </summary>
        /// <value>The STBs.</value>
        public static Dictionary<string, STB> STBs { get; set; }

        /// <summary>
        /// Gets or sets the STLs.
        /// </summary>
        /// <value>The STLs.</value>
        public static Dictionary<string, STL> STLs { get; set; }

        /// <summary>
        /// Gets or sets the data path.
        /// </summary>
        public static string DataPath { get; set; }

        #endregion

        /// <summary>
        /// Initializes the FileManager with the specified 3DDATA folder path.
        /// </summary>
        /// <param name="dataPath">Path to the 3DDATA folder</param>
        public static void Initialize(string dataPath)
        {
            DataPath = dataPath;
            STBs = new Dictionary<string, STB>();
            STLs = new Dictionary<string, STL>();

            Output.WriteLine(Output.MessageType.Event, "Loading STB files");

            string stbPath = Path.Combine(dataPath, "STB");

            // Load ITEM_DROP.STB (main drop table file)
            Add("ITEM_DROP", Path.Combine(stbPath, "ITEM_DROP.STB"));

            // Load LIST_NPC.STB (for NPC name lookup)
            Add("LIST_NPC", Path.Combine(stbPath, "LIST_NPC.STB"));

            // Load item category STBs (14 categories)
            Add("LIST_FACEITEM", Path.Combine(stbPath, "LIST_FACEITEM.STB"));
            Add("LIST_CAP", Path.Combine(stbPath, "LIST_CAP.STB"));
            Add("LIST_BODY", Path.Combine(stbPath, "LIST_BODY.STB"));
            Add("LIST_ARMS", Path.Combine(stbPath, "LIST_ARMS.STB"));
            Add("LIST_FOOT", Path.Combine(stbPath, "LIST_FOOT.STB"));
            Add("LIST_BACK", Path.Combine(stbPath, "LIST_BACK.STB"));
            Add("LIST_JEWEL", Path.Combine(stbPath, "LIST_JEWEL.STB"));
            Add("LIST_WEAPON", Path.Combine(stbPath, "LIST_WEAPON.STB"));
            Add("LIST_SUBWPN", Path.Combine(stbPath, "LIST_SUBWPN.STB"));
            Add("LIST_USEITEM", Path.Combine(stbPath, "LIST_USEITEM.STB"));
            Add("LIST_JEMITEM", Path.Combine(stbPath, "LIST_JEMITEM.STB"));
            Add("LIST_NATURAL", Path.Combine(stbPath, "LIST_NATURAL.STB"));
            Add("LIST_PAT", Path.Combine(stbPath, "LIST_PAT.STB"));

            Output.WriteLine(Output.MessageType.Event, "Loading STL files");

            // Load NPC name localization
            Add("LIST_NPC_S", Path.Combine(stbPath, "LIST_NPC_S.STL"));

            // Load item category STLs (14 categories)
            Add("LIST_FACEITEM_S", Path.Combine(stbPath, "LIST_FACEITEM_S.STL"));
            Add("LIST_CAP_S", Path.Combine(stbPath, "LIST_CAP_S.STL"));
            Add("LIST_BODY_S", Path.Combine(stbPath, "LIST_BODY_S.STL"));
            Add("LIST_ARMS_S", Path.Combine(stbPath, "LIST_ARMS_S.STL"));
            Add("LIST_FOOT_S", Path.Combine(stbPath, "LIST_FOOT_S.STL"));
            Add("LIST_BACK_S", Path.Combine(stbPath, "LIST_BACK_S.STL"));
            Add("LIST_JEWEL_S", Path.Combine(stbPath, "LIST_JEWEL_S.STL"));
            Add("LIST_WEAPON_S", Path.Combine(stbPath, "LIST_WEAPON_S.STL"));
            Add("LIST_SUBWPN_S", Path.Combine(stbPath, "LIST_SUBWPN_S.STL"));
            Add("LIST_USEITEM_S", Path.Combine(stbPath, "LIST_USEITEM_S.STL"));
            Add("LIST_JEMITEM_S", Path.Combine(stbPath, "LIST_JEMITEM_S.STL"));
            Add("LIST_NATURAL_S", Path.Combine(stbPath, "LIST_NATURAL_S.STL"));
            Add("LIST_PAT_S", Path.Combine(stbPath, "LIST_PAT_S.STL"));

            Output.WriteLine(Output.MessageType.Event, "File loading complete");
        }

        /// <summary>
        /// Adds a file to the appropriate dictionary based on extension.
        /// </summary>
        /// <param name="key">The key for the file</param>
        /// <param name="filePath">The file path</param>
        private static void Add(string key, string filePath)
        {
            if (!File.Exists(filePath))
            {
                Output.WriteLine(Output.MessageType.Error, string.Format("Missing file: {0}", filePath));
                throw new FileNotFoundException(string.Format("Required file not found: {0}", filePath), filePath);
            }

            string extension = Path.GetExtension(filePath).ToUpper();

            try
            {
                if (extension == ".STB")
                {
                    Output.WriteLine(Output.MessageType.Normal, string.Format("- Loading {0} [{1}]", filePath, key));
                    STB newFile = new STB();
                    newFile.Load(filePath);
                    STBs.Add(key, newFile);
                }
                else if (extension == ".STL")
                {
                    Output.WriteLine(Output.MessageType.Normal, string.Format("- Loading {0} [{1}]", filePath, key));
                    STL newFile = new STL();
                    newFile.Load(filePath);
                    STLs.Add(key, newFile);
                }
                else
                {
                    throw new ArgumentException(string.Format("Unsupported file type: {0}", extension));
                }
            }
            catch (Exception ex)
            {
                Output.WriteLine(Output.MessageType.Error, string.Format("Error loading {0}: {1}", filePath, ex.Message));
                throw;
            }
        }

        /// <summary>
        /// Clears all loaded files.
        /// </summary>
        public static void Reset()
        {
            if (STBs != null)
                STBs.Clear();
            if (STLs != null)
                STLs.Clear();

            STBs = new Dictionary<string, STB>();
            STLs = new Dictionary<string, STL>();
        }
    }
}
