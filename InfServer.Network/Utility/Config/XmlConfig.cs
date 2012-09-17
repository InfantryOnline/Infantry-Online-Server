using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace InfServer
{
    /// <summary>
    /// The class which represents a configuration xml file
    /// </summary>
    public class Xmlconfig : IDisposable
    {
        private XmlDocument xmldoc;
        private string originalFile;
        private bool commitonunload = true;
        private bool cleanuponsave = false;


        /// <summary>
        /// Create an XmlConfig from an empty xml file 
        /// containing only the rootelement named as 'xml'
        /// </summary>
        public Xmlconfig()
        {
            xmldoc = new XmlDocument();
            LoadXmlFromString("<xml></xml>");
        }

        /// <summary>
        /// Create an XmlConfig from an existing file, or create a new one
        /// </summary>
        /// <param name="loadfromfile">
        /// Path and filename from where to load the xml file
        /// </param>
        /// <param name="create">
        /// If file does not exist, create it, or throw an exception?
        /// </param>
        public Xmlconfig(string loadfromfile, bool create)
        {
            xmldoc = new XmlDocument();
            LoadXmlFromFile(loadfromfile,create);
        }

        /// <summary>
        /// Check XML file if it conforms the config xml restrictions
        /// 1. No nodes with two children of the same name
        /// 2. Only alphanumerical names
        /// </summary>
        /// <param name="silent">
        /// Whether to return a true/false value, or throw an exception on failiure
        /// </param>
        /// <returns>
        /// True on success and in case of silent mode false on failiure
        /// </returns>
        public bool ValidateXML(bool silent)
        {
            if (!Settings.Validate())
            {
                if (silent)
                    return false;
                else
                    throw new Exception("This is not a valid configuration xml file! Probably duplicate children with the same names, or non-alphanumerical tagnames!");
            }
            else
                return true;
        }

        /// <summary>
        /// Strip empty nodes from the whole tree.
        /// </summary>
        public void Clean()
        {
            Settings.Clean();
        }

        /// <summary>
        /// Whether to clean the tree by stripping out
        /// empty nodes before saving the XML config
        /// file
        /// </summary>
        public bool CleanUpOnSave
        {
            get { return cleanuponsave; }
            set { cleanuponsave = value; }            
        }

        
        /// <summary>
        /// When unloading the current XML config file
        /// shold any changes be saved back to the file?
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Only applies if it was loaded from a local file</item>
        /// <item>True by default</item>
        /// </list>
        /// </remarks>
        public bool CommitOnUnload
        {
            get { return commitonunload; }
            set { commitonunload = value; }
        }

        /// <summary>
        /// Save any modifications to the XML file before destruction
        /// if CommitOnUnload is true
        /// </summary>
        public void Dispose()
        {
            if (CommitOnUnload) Commit();
        }

        /// <summary>
        /// Load a new XmlDocument from a file
        /// </summary>
        /// <param name="filename">
        /// Path and filename from where to load the xml file
        /// </param>
        /// <param name="create">
        /// If file does not exist, create it, or throw an exception?
        /// </param>
        public void LoadXmlFromFile(string filename, bool create)
        {
            if (CommitOnUnload) Commit();
            try
            {
                xmldoc.Load(filename);
            }
            catch (Exception e)
            {
                Log.write(e.Message);
                if (!create)
                    throw new Exception(String.Format("xmldoc.Load() failed! Probably file does NOT exist!\nMissing: {0}", filename));
                else
                {
                    xmldoc.LoadXml("<xml></xml>");
                    Save(filename);
                }
            }
            ValidateXML(false);
            originalFile = filename;

        }

        /// <summary>
        /// Load a new XmlDocument from a file
        /// </summary>
        /// <param name="filename">
        /// Path and filename from where to load the xml file
        /// </param>
        /// <remarks>
        /// Throws an exception if file does not exist
        /// </remarks>
        public void LoadXmlFromFile(string filename)
        {
            LoadXmlFromFile(filename, false);
        }

        /// <summary>
        /// Load a new XmlDocument from a string
        /// </summary>
        /// <param name="xml">
        /// XML string
        /// </param>
        public void LoadXmlFromString(string xml)
        {
            if (CommitOnUnload) Commit();
            xmldoc.LoadXml(xml);
            originalFile = null;
            ValidateXML(false);
        }

        /// <summary>
        /// Load an empty XmlDocument
        /// </summary>
        /// <param name="rootelement">
        /// Name of root element
        /// </param>
        public void NewXml(string rootelement)
        {
            if (CommitOnUnload) Commit();
            LoadXmlFromString(String.Format("<{0}></{0}>",rootelement));
        }

        /// <summary>
        /// Save configuration to an xml file
        /// </summary>
        /// <param name="filename">
        /// Path and filname where to save
        /// </param>
        public void Save(string filename)
        {
            ValidateXML(false);
            if (CleanUpOnSave)
                Clean();
            xmldoc.Save(filename);
            originalFile = filename;
        }

        /// <summary>
        /// Save configuration to a stream
        /// </summary>
        /// <param name="stream">
        /// Stream where to save
        /// </param>
        public void Save(System.IO.Stream stream)
        {
            ValidateXML(false);
            if (CleanUpOnSave)
                Clean();
            xmldoc.Save(stream);
        }

        /// <summary>
        /// If loaded from a file, commit any changes, by overwriting the file
        /// </summary>
        /// <returns>
        /// True on success
        /// False on failiure, probably due to the file was not loaded from a file
        /// </returns>

        public bool Commit()
        {
            if (originalFile != null) { Save(originalFile); return true; } else { return false; }
        }

        /// <summary>
        /// If loaded from a file, trash any changes, and reload the file
        /// </summary>
        /// <returns>
        /// True on success
        /// False on failiure, probably due to file was not loaded from a file
        /// </returns>
        public bool Reload()
        {
            if (originalFile != null) { LoadXmlFromFile(originalFile); return true; } else { return false; }
        }

        /// <summary>
        /// Gets the root ConfigSetting
        /// </summary>
        public ConfigSetting Settings
        {
            get { return new ConfigSetting(xmldoc.DocumentElement); }           
        }

    }
}
