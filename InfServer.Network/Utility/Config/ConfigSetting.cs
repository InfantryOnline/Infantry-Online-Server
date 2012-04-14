using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace InfServer
{
    /// <summary>
    /// Represents a Configuration Node in the XML file
    /// </summary>
    public class ConfigSetting
    {
        /// <summary>
        /// The node from the XMLDocument, which it describes
        /// </summary>
        private XmlNode node;

        /// <summary>
        /// This class cannot be constructed directly. You will need to give a node to describe
        /// </summary>
        private ConfigSetting()
        {
            throw new Exception("Cannot be created directly. Needs a node parameter");
        }

        /// <summary>
        /// Creates an instance of the class
        /// </summary>
        /// <param name="node">
        /// the XmlNode to describe
        /// </param>
        public ConfigSetting(XmlNode node)
        {
            if (node == null)
                throw new Exception("Node parameter can NOT be null!");
            this.node = node;
        }

		/// <summary>
		/// Creates an instance of the class
		/// </summary>
		/// <param name="node">
		/// the XmlNode to describe
		/// </param>
		public static ConfigSetting Blank
		{
			get
			{	//Create an XmlDocument to house ourself
				XmlDocument doc = new XmlDocument();
				XmlElement rootNode = doc.CreateElement("xml");

				return new ConfigSetting(rootNode);
			}
		}

        /// <summary>
        /// The Name of the element it describes
        /// </summary>
        /// <remarks>Read only property</remarks>        
        public string Name
        {
            get
            {
                return node.Name;
            }
        }

        /// <summary>
        /// Gets the number of children of the specific node
        /// </summary>
        /// <param name="unique">
        /// If true, get only the number of children with distinct names.
        /// So if it has two nodes with "foo" name, and three nodes
        /// named "bar", the return value will be 2. In the same case, if unique
        /// was false, the return value would have been 2 + 3 = 5
        /// </param>
        /// <returns>
        /// The number of (uniquely named) children
        /// </returns>
        public int ChildCount(bool unique)
        {
            IList<string> names = ChildrenNames(unique);
            if (names != null)
                return names.Count;
            else
                return 0;
        }

        /// <summary>
        /// Gets the names of children of the specific node
        /// </summary>
        /// <param name="unique">
        /// If true, get only distinct names.
        /// So if it has two nodes with "foo" name, and three nodes
        /// named "bar", the return value will be {"bar","foo"} . 
        /// In the same case, if unique was false, the return value 
        /// would have been {"bar","bar","bar","foo","foo"}
        /// </param>
        /// <returns>
        /// An IList object with the names of (uniquely named) children
        /// </returns>

        public IList<String> ChildrenNames(bool unique)
        {
            if (node.ChildNodes.Count == 0)
                return null;
            List<String> stringlist = new List<string>();

            foreach (XmlNode achild in node.ChildNodes)
            {
                string name = achild.Name;
                if ((!unique) || (!stringlist.Contains(name)))
                    stringlist.Add(name);
            }
            
            stringlist.Sort();
            return stringlist;
        }



        /// <summary>
        /// An IList compatible object describing each and every child node
        /// </summary>
        /// <remarks>Read only property</remarks>
        public IList<ConfigSetting> Children()
        {
            if (ChildCount(false) == 0)
                return null;
            List<ConfigSetting> list = new List<ConfigSetting>();

            foreach (XmlNode achild in node.ChildNodes)
            {
                list.Add(new ConfigSetting(achild));
            }
            return list;
        }
        /// <summary>
        /// Get all children with the same name, specified in the name parameter
        /// </summary>
        /// <param name="name">
        /// An alphanumerical string, containing the name of the child nodes to return
        /// </param>
        /// <returns>
        /// An array with the child nodes with the specified name, or null 
        /// if no childs with the specified name exist
        /// </returns>
        public IList<ConfigSetting> GetNamedChildren(String name)
        {
            foreach (Char c in name)
                if (!Char.IsLetterOrDigit(c))
                    throw new Exception("Name MUST be alphanumerical!");
            XmlNodeList xmlnl = node.SelectNodes(name);
            // int NodeCount = xmlnl.Count;
            List<ConfigSetting> css = new List<ConfigSetting>();
            foreach (XmlNode achild in xmlnl)
            {
                css.Add(new ConfigSetting(achild));
            }
            return css;
        }

        /// <summary>
        /// Gets the number of childs with the specified name
        /// </summary>
        /// <param name="name">
        /// An alphanumerical string with the name of the nodes to look for
        /// </param>
        /// <returns>
        /// An integer with the count of the nodes
        /// </returns>
        public int GetNamedChildrenCount(String name)
        {
            foreach (Char c in name)
                if (!Char.IsLetterOrDigit(c))
                    throw new Exception("Name MUST be alphanumerical!");
            return node.SelectNodes(name).Count;
        }

		/// <summary>
		/// Determines whether the given path exists
		/// </summary>
		/// <param name="name">
		/// An alphanumerical string with the name of the nodes to look for
		/// </param>
		public bool Exists(String name)
		{
			foreach (Char c in name)
				if (!Char.IsLetterOrDigit(c))
					throw new Exception("Name MUST be alphanumerical!");
			return (node.SelectNodes(name).Count > 0);
		}

		/// <summary>
		/// Adds a child node to the xml tree
		/// </summary>
		public void AddChild(ConfigSetting child)
		{	//Add it!
			node.AppendChild(node.OwnerDocument.ImportNode(child.node, true));
		} 

        /// <summary>
        /// String value of the specific Configuration Node
        /// </summary>
        public string Value
        {
            get
            {
                XmlNode xmlattrib = node.Attributes.GetNamedItem("value");
                if (xmlattrib != null) return xmlattrib.Value; else return "";
            }

            set
            {
                XmlNode xmlattrib = node.Attributes.GetNamedItem("value");
                if (value != "")
                {
                    if (xmlattrib == null) xmlattrib = node.Attributes.Append(node.OwnerDocument.CreateAttribute("value"));
                    xmlattrib.Value = value;
                }
                else if (xmlattrib != null) node.Attributes.RemoveNamedItem("value");
            }
        }

		 /// <summary>
        /// int64 value of the specific Configuration Node
        /// </summary>
		public Int64 int64Value
		{
			get { Int64 i; Int64.TryParse(Value, out i); return i; }
			set { Value = value.ToString(); }
		}


        /// <summary>
        /// int value of the specific Configuration Node
        /// </summary>
        public int intValue
        {
            get { int i; int.TryParse(Value, out i); return i; }
            set { Value = value.ToString(); }
            
        }

        /// <summary>
        /// bool value of the specific Configuration Node
        /// </summary>
        public bool boolValue
        {
            get { bool b; bool.TryParse(Value, out b); return b; }
            set { Value = value.ToString(); }
        }

        /// <summary>
        /// float value of the specific Configuration Node
        /// </summary>
        public float floatValue
        {
            get { float f; float.TryParse(Value, out f); return f; }
            set { Value = value.ToString(); }

        }

        /// <summary>
        /// Get a specific child node
        /// </summary>
        /// <param name="path">
        /// The path to the specific node. Can be either only a name, or a full path separated by '/' or '\'
        /// </param>
        /// <example>
        /// <code>
        /// XmlConfig conf = new XmlConfig("configuration.xml");
        /// screenname = conf.Settings["screen"].Value;
        /// height = conf.Settings["screen/height"].IntValue;
        ///  // OR
        /// height = conf.Settings["screen"]["height"].IntValue;
        /// </code>
        /// </example>
        /// <returns>
        /// The specific child node
        /// </returns>
        public ConfigSetting this[string path]
        {
            get
            {
                char[] separators = { '/', '\\' };
                path.Trim(separators);
                String[] pathsection = path.Split(separators);
                                
                XmlNode selectednode = node;
                XmlNode newnode;

                foreach (string asection in pathsection)
                {
                    String nodename, nodeposstr;
                    int nodeposition;
                    int indexofdiez = asection.IndexOf('#');

                    if (indexofdiez == -1) // No position defined, take the first one by default
                    {
                        nodename = asection;
                        nodeposition = 1;
                    }
                    else
                    {
                        nodename = asection.Substring(0, indexofdiez); // Node name is before the diez character
                        nodeposstr = asection.Substring(indexofdiez + 1);
                        if (nodeposstr == "#") // Double diez means he wants to create a new node
                            nodeposition = GetNamedChildrenCount(nodename) + 1;
                        else
                            nodeposition = int.Parse(nodeposstr);
                    }

                    // Verify name
                    foreach (char c in nodename)
                    { if ((!Char.IsLetterOrDigit(c))) return null; }

                    String transformedpath = String.Format("{0}[{1}]", nodename, nodeposition);
                    newnode = selectednode.SelectSingleNode(transformedpath);

                    while (newnode == null)
                    {
                        XmlElement newelement = selectednode.OwnerDocument.CreateElement(nodename);
                        selectednode.AppendChild(newelement);
                        newnode = selectednode.SelectSingleNode(transformedpath);
                    }
                    selectednode = newnode;
                }

                return new ConfigSetting(selectednode);
            }
        }

        /// <summary>
        /// Check if the node conforms with the config xml restrictions
        /// 1. No nodes with two children of the same name
        /// 2. Only alphanumerical names
        /// </summary>
        /// <returns>
        /// True on success and false on failiure
        /// </returns>        
        public bool Validate()
        {
            // Check this node's name for validity
            foreach (Char c in this.Name)
                if (!Char.IsLetterOrDigit(c))
                    return false;

            // If there are no children, the node is valid.
            // If there the node has other children, check all of them for validity
            if (ChildCount(false) == 0)
                return true;
            else
            {
                foreach (ConfigSetting cs in this.Children())
                {
                    if (!cs.Validate())
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Removes any empty nodes from the tree, 
        /// that is it removes a node, if it hasn't got any
        /// children, or neither of its children have got a value.
        /// </summary>
        public void Clean()
        {
            if (ChildCount(false) != 0)
                foreach (ConfigSetting cs in this.Children())
                {
                    cs.Clean();
                }
            if ((ChildCount(false) == 0) && (this.Value == ""))
                this.Remove();            
        }
        
        /// <summary>
        /// Remove the specific node from the tree
        /// </summary>
        public void Remove()
        {
            if (node.ParentNode == null) return;
            node.ParentNode.RemoveChild(node);        
        }

        /// <summary>
        /// Remove all children of the node, but keep the node itself
        /// </summary>
        public void RemoveChildren()
        {
            node.RemoveAll();
        }


    }
}
