//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
//using System.Data;
// using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security;
using System.Xml;
// using Microsoft.AnalysisServices;
// using Microsoft.SqlServer.Common;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
// using Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer;
using Assembly = System.Reflection.Assembly;
using System.Xml.Linq;
using Microsoft.Data.Tools.DataSets;
//This is used only for non-express sku


namespace Microsoft.SqlTools.ServiceLayer.Common
{
    /// <summary>
    /// CDataContainer
    /// </summary>
    public class CDataContainer : IDisposable
    {
        #region Nested types

        public enum ServerType
        {
            SQL,
            OLAP, //This type is used only for non-express sku
			SQLCE,
            UNKNOWN
        }

        #endregion

        #region Fields

        private ServerConnection serverConnection;
        private Server m_server = null;
 
        //This member is used for non-express sku only
        // private AnalysisServices.Server m_amoServer = null;
        private ISandboxLoader sandboxLoader;

        //protected XDocument m_doc = null;
        protected XmlDocument m_doc = null;
        private XmlDocument originalDocument = null;
        private SqlOlapConnectionInfoBase connectionInfo = null;
        private SqlConnectionInfoWithConnection sqlCiWithConnection;
        private bool ownConnection = true;
        private IManagedConnection managedConnection;
        protected string serverName;

        //This member is used for non-express sku only
        protected string olapServerName;

		protected string sqlceFilename;

        private ServerType serverType = ServerType.UNKNOWN;

        private Hashtable m_hashTable = null;

        private string objectNameKey = "object-name-9524b5c1-e996-4119-a433-b5b947985566";
        private string objectSchemaKey = "object-schema-ccaf2efe-8fa3-4f62-be79-62ef3cbe7390";

        private SqlSmoObject sqlDialogSubject = null;

        private int sqlServerVersion = 0;
        private int sqlServerEffectiveVersion = 0;


        #endregion

        #region Public properties

        /// <summary>
        /// gets/sets XmlDocument with parameters
        /// </summary>
        public XmlDocument Document
        {
            get
            {
                return this.m_doc;
            }
            set
            {
                this.m_doc = value;

                if (value != null)
                {
                    //this.originalDocument = (XmlDocument) value.Clone();
                    this.originalDocument = value;
                }
                else
                {
                    this.originalDocument = null;
                }
            }
        }


        /// <summary>
        /// returns the Hashtable that can be used to store generic information
        /// </summary>
        public Hashtable HashTable
        {
            get
            {
                if (m_hashTable == null)
                {
                    m_hashTable = new Hashtable();
                }

                return m_hashTable;
            }
        }


        /// <summary>
        /// gets/sets SMO server object
        /// </summary>
        public Server Server
        {
            get
            {
                return m_server;
            }
            set
            {
                m_server = value;
            }
        }

        /// <summary>
        /// gets/sets AMO server object
        /// This member is used for non-express sku only
        /// </summary>
        //public AnalysisServices.Server OlapServer
        //{
        //    get
        //    {
        //        return m_amoServer;
        //    }
        //    set
        //    {
        //        m_amoServer = value;
        //    }
        //}

        public ISandboxLoader SandboxLoader
        {
            get { return this.sandboxLoader; }
        }

        /// <summary>
        /// connection info that should be used by the dialogs
        /// </summary>
        public SqlOlapConnectionInfoBase ConnectionInfo
        {
            get
            {
                //// update the database name in the serverconnection object to set the correct database context when connected to Azure
                //var conn = this.connectionInfo as SqlConnectionInfoWithConnection;

                //if (conn != null && conn.ServerConnection.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
                //{
                //    if (this.RelevantDatabaseName != null)
                //    {
                //        IComparer<string> dbNamesComparer = ServerConnection.ConnectionFactory.GetInstance(conn.ServerConnection).ServerComparer as IComparer<string>;
                //        if (dbNamesComparer.Compare(this.RelevantDatabaseName, conn.DatabaseName) != 0)
                //        {                            
                //            ServerConnection serverConnection = conn.ServerConnection.GetDatabaseConnection(this.RelevantDatabaseName, true, conn.AccessToken);
                //            ((SqlConnectionInfoWithConnection)this.connectionInfo).ServerConnection = serverConnection;
                //        }
                //    }
                //}
                
                return this.connectionInfo;
            }
        }

        /// <summary>
        /// returns SMO server connection object constructed off the connectionInfo.
        /// This method cannot work until ConnectionInfo property has been set
        /// </summary>
        public ServerConnection ServerConnection
        {
            get
            {
                if (this.serverConnection == null)
                {
                    if (this.serverType != ServerType.SQL)
                    {
                        throw new InvalidOperationException();
                    }

                    if (this.connectionInfo == null)
                    {
                        throw new InvalidOperationException();
                    }


                    if (this.sqlCiWithConnection != null)
                    {
                        this.serverConnection = this.sqlCiWithConnection.ServerConnection;
                    }
                    else
                    {
                        SqlConnectionInfo sci = this.connectionInfo as SqlConnectionInfo;
                        this.serverConnection = new ServerConnection(sci);
                    }
                }


                return this.serverConnection;
            }
        }

        /// <summary>
        /// returns SMO server connection object constructed off the connectionInfo.
        /// This method cannot work until ConnectionInfo property has been set
        /// </summary>
        public SqlConnectionInfoWithConnection SqlInfoWithConnection
        {
            get
            {
                if (this.serverConnection == null)
                {
                    if (this.serverType != ServerType.SQL)
                    {
                        throw new InvalidOperationException();
                    }

                    if (this.connectionInfo == null)
                    {
                        throw new InvalidOperationException();
                    }


                    if (this.sqlCiWithConnection != null)
                    {
                        this.serverConnection = this.sqlCiWithConnection.ServerConnection;
                    }
                    else
                    {
                        SqlConnectionInfo sci = this.connectionInfo as SqlConnectionInfo;
                        this.serverConnection = new ServerConnection(sci);
                    }
                }

                return this.sqlCiWithConnection;
            }
        }

        public string ServerName
        {
            get
            {
                return this.serverName;
            }
            set
            {
                this.serverName = value;
            }
        }

        public ServerType ContainerServerType
        {
            get
            {
                return this.serverType;
            }
            set
            {
                this.serverType = value;
            }
        }

        public string SqlCeFileName
        {
            get
            {
                return this.sqlceFilename;
            }
            set
            {
                this.sqlceFilename = value;
            }
        }

        //This member is used for non-express sku only
        public string OlapServerName
        {
            get
            {
                return this.olapServerName;
            }
            set
            {
                this.olapServerName = value;
            }
        }

        /// <summary>
        /// Whether we are creating a new object
        /// </summary>
        public bool IsNewObject
        {
            get
            {
                string itemType = this.GetDocumentPropertyString("itemtype");
                return (itemType.Length != 0);
            }
        }

        /// <summary>
        /// The URN to the parent of the object we are creating/modifying
        /// </summary>
        public string ParentUrn
        {
            get
            {
                string result = String.Empty;
                string documentUrn = this.GetDocumentPropertyString("urn");

                if (this.IsNewObject)
                {
                    result = documentUrn;
                }
                else
                {
                    Urn urn = new Urn(documentUrn);
                    result = urn.Parent.ToString();
                }

                return result;
            }
        }

        /// <summary>
        /// The URN to the object we are creating/modifying
        /// </summary>
        public string ObjectUrn
        {
            get
            {
                string result = String.Empty;

                if (this.IsNewObject)
                {
                    string objectName = this.ObjectName;
                    string objectSchema = this.ObjectSchema;

                    if (0 == objectName.Length)
                    {
                        throw new InvalidOperationException("object name is not known, so URN for object can't be formed");
                    }

                    if (0 == objectSchema.Length)
                    {
                        result = String.Format(CultureInfo.InvariantCulture,
                            "{0}/{1}[@Name='{2}']",
                            this.ParentUrn,
                            this.ObjectType,
                            Urn.EscapeString(objectName));
                    }
                    else
                    {
                        result = String.Format(CultureInfo.InvariantCulture,
                            "{0}/{1}[@Schema='{2}' and @Name='{3}']",
                            this.ParentUrn,
                            this.ObjectType,
                            Urn.EscapeString(objectSchema),
                            Urn.EscapeString(objectName));
                    }
                }
                else
                {
                    result = this.GetDocumentPropertyString("urn");
                }

                return result;
            }
        }

        /// <summary>
        /// The name of the object we are modifying
        /// </summary>
        public string ObjectName
        {
            get
            {
                return this.GetDocumentPropertyString(objectNameKey);
            }

            set
            {
                this.SetDocumentPropertyValue(objectNameKey, value);
            }
        }

        /// <summary>
        /// The schema of the object we are modifying
        /// </summary>
        public string ObjectSchema
        {
            get
            {
                return this.GetDocumentPropertyString(objectSchemaKey);
            }

            set
            {
                this.SetDocumentPropertyValue(objectSchemaKey, value);
            }
        }

        /// <summary>
        /// The type of the object we are creating (as it appears in URNs)
        /// </summary>
        public string ObjectType
        {
            get
            {
                // note that the itemtype property is only set for new objects

                string result = String.Empty;
                string itemtype = this.GetDocumentPropertyString("itemtype");

                // if this is not a new object
                if (0 == itemtype.Length)
                {
                    string documentUrn = this.GetDocumentPropertyString("urn");
                    Urn urn = new Urn(documentUrn);

                    result = urn.Type;
                }
                else
                {
                    result = itemtype;
                }

                return result;
            }


        }

        /// <summary>
        /// The SQL SMO object that is the subject of the dialog.
        /// </summary>
        public SqlSmoObject SqlDialogSubject
        {
            get
            {
                SqlSmoObject result = null;

                if (this.sqlDialogSubject != null)
                {
                    result = this.sqlDialogSubject;
                }
                else
                {
                    result = this.Server.GetSmoObject(this.ObjectUrn);
                }

                return result;
            }

            set
            {
                this.sqlDialogSubject = value;
            }
        }

        /// <summary>
        /// Whether the logged in user is a system administrator
        /// </summary>
        public bool LoggedInUserIsSysadmin
        {
            get
            {
                bool result = false;

                if (this.Server != null && this.Server.ConnectionContext != null)
                {
                    result = this.Server.ConnectionContext.IsInFixedServerRole(FixedServerRoles.SysAdmin);
                }

                return result;
            }
        }

        /// <summary>
        /// Get the name of the Database that contains (or is) the subject of the dialog.
        /// If no there is no relevant database, then an empty string is returned.
        /// </summary>
        public string RelevantDatabaseName
        {
            get
            {
                string result = String.Empty;
                string urnText = this.GetDocumentPropertyString("urn");

                if (urnText.Length != 0)
                {
                    Urn urn = new Urn(urnText);

                    while ((urn != null) && (urn.Type != "Database"))
                    {
                        urn = urn.Parent;
                    }

                    if ((urn != null) && (urn.Type == "Database"))
                    {
                        result = urn.GetAttribute("Name");
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// The server major version number
        /// </summary>
        public int SqlServerVersion
        {
            get
            {
                if (this.sqlServerVersion == 0)
                {
                    this.sqlServerVersion = 9;

                    if ((this.ConnectionInfo != null) && (ServerType.SQL == this.ContainerServerType))
                    {
                        Enumerator enumerator = new Enumerator();
                        Urn urn = "Server/Information";
                        string[] fields = new string[] { "VersionMajor" };
                        DataTable dataTable = enumerator.Process(this.ConnectionInfo, new Request(urn, fields));

                        if (dataTable.Rows.Count != 0)
                        {
                            this.sqlServerVersion = (int)dataTable.Rows[0][0];
                        }
                    }
                }

                return this.sqlServerVersion;
            }

        }

        /// <summary>
        /// The server version the database is emulating.  If database compatibility level is
        /// not relevant to the subject, then this just returns the actual server version.
        /// </summary>
        public int EffectiveSqlServerVersion
        {
            get
            {
                if (this.sqlServerEffectiveVersion == 0)
                {
                    this.sqlServerEffectiveVersion = 9;

                    if ((this.ConnectionInfo != null) && (ServerType.SQL == this.ContainerServerType))
                    {
                        string databaseName = this.RelevantDatabaseName;

                        if (databaseName.Length != 0)
                        {
                            Enumerator enumerator = new Enumerator();
                            Urn urn = String.Format("Server/Database[@Name='{0}']", Urn.EscapeString(databaseName));
                            string[] fields = new string[] { "CompatibilityLevel" };
                            DataTable dataTable = enumerator.Process(this.ConnectionInfo, new Request(urn, fields));

                            if (dataTable.Rows.Count != 0)
                            {

                                CompatibilityLevel level = (CompatibilityLevel)dataTable.Rows[0][0];

                                switch (level)
                                {
                                    case CompatibilityLevel.Version60:
                                    case CompatibilityLevel.Version65:

                                        this.sqlServerEffectiveVersion = 6;
                                        break;

                                    case CompatibilityLevel.Version70:

                                        this.sqlServerEffectiveVersion = 7;
                                        break;

                                    case CompatibilityLevel.Version80:

                                        this.sqlServerEffectiveVersion = 8;
                                        break;

                                    case CompatibilityLevel.Version90:

                                        this.sqlServerEffectiveVersion = 9;
                                        break;
                                    case CompatibilityLevel.Version100:

                                        this.sqlServerEffectiveVersion = 10;
                                        break;
                                    case CompatibilityLevel.Version110:

                                        this.sqlServerEffectiveVersion = 11;
                                        break;
                                    case CompatibilityLevel.Version120:

                                        this.sqlServerEffectiveVersion = 12;
                                        break;

                                    case CompatibilityLevel.Version130:
                                        this.sqlServerEffectiveVersion = 13;
                                        break;

                                    case CompatibilityLevel.Version140:
                                        this.sqlServerEffectiveVersion = 14;
                                        break;

                                    default:

                                        this.sqlServerEffectiveVersion = 14;
                                        break;
                                }
                            }
                            else
                            {
                                this.sqlServerEffectiveVersion = this.SqlServerVersion;
                            }
                        }
                        else
                        {
                            this.sqlServerEffectiveVersion = this.SqlServerVersion;
                        }
                    }
                }

                return this.sqlServerEffectiveVersion;
            }
        }

        #endregion

        #region Constructors, finalizer

        public CDataContainer()
        {
        }

        /// <summary>
        /// contructs the object and initializes its SQL ConnectionInfo and ServerConnection properties
        /// using the specified connection info containing live connection. 
        /// </summary>
        /// <param name="ciObj">connection info containing live connection</param>
        public CDataContainer(object ciObj, bool ownConnection)
        {
            SqlConnectionInfoWithConnection ci = (SqlConnectionInfoWithConnection)ciObj;          
            if (ci == null)
            {
                throw new ArgumentNullException("ci");
            }
            ApplyConnectionInfo(ci, ownConnection);
        }

        /// <summary>
        /// contructs the object and initializes its SQL ConnectionInfo and ServerConnection properties
        /// using the specified connection info containing live connection. 
        /// 
        /// in addition creates a server of the given server type
        /// </summary>
        /// <param name="ci">connection info containing live connection</param>
        public CDataContainer(ServerType serverType, object ciObj, bool ownConnection)
        {
            SqlConnectionInfoWithConnection ci = (SqlConnectionInfoWithConnection)ciObj;            
            if (ci == null)
            {
                throw new ArgumentNullException("ci");
            }

            this.serverType = serverType;
            ApplyConnectionInfo(ci, ownConnection);

            if (serverType == ServerType.SQL)
            {
                    //NOTE: ServerConnection property will constuct the object if needed
                    m_server = new Server(ServerConnection);
            }           
            else
            {
                    throw new ArgumentException("SRError.UnknownServerType(serverType.ToString()), serverType");
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="serverType">Server type</param>
        /// <param name="serverName">Server name</param>
        /// <param name="trusted">true if connection is trused. If true user name and password are ignored</param>
        /// <param name="userName">User name for not trusted connections</param>
        /// <param name="password">Password for not trusted connections</param>
        /// <param name="xmlParameters">XML string with parameters</param>
        public CDataContainer(ServerType serverType, string serverName, bool trusted, string userName, SecureString password, string xmlParameters)
        {
            this.serverType = serverType;
            this.serverName = serverName;

            if (serverType == ServerType.SQL)
            {
                    //does some extra initialization
                    ApplyConnectionInfo(GetTempSqlConnectionInfoWithConnection(serverName, trusted, userName, password), true);

                    //NOTE: ServerConnection property will constuct the object if needed
                    m_server = new Server(ServerConnection);
            }         
            else
            {
                    throw new ArgumentException("SRError.UnknownServerType(serverType.ToString()), serverType");
            }

            if (xmlParameters != null)
            {
                this.Document = GenerateXmlDocumentFromString(xmlParameters);
            }

            if (ServerType.SQL == serverType)
            {
                this.InitializeObjectNameAndSchema();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataContainer">Data container</param>
        /// <param name="xmlParameters">XML string with parameters</param>
        public CDataContainer(CDataContainer dataContainer, string xmlParameters)
        {
            Server = dataContainer.Server;
            this.serverName = dataContainer.serverName;
            this.serverType = dataContainer.serverType;
            this.connectionInfo = dataContainer.connectionInfo;
            this.ownConnection = dataContainer.ownConnection;

            this.sqlCiWithConnection = dataContainer.connectionInfo as SqlConnectionInfoWithConnection;
            if (this.sqlCiWithConnection != null)
            {              
                //we want to be notified if it is closed
                this.sqlCiWithConnection.ConnectionClosed += new EventHandler(OnSqlConnectionClosed);
            }

            if (xmlParameters != null)
            {
                XmlDocument doc = GenerateXmlDocumentFromString(xmlParameters);
                this.Init(doc);
            }
        }

        ~CDataContainer()
        {
            Dispose(false);
        }

        #endregion

        #region Public virtual methods

        /// <summary>
        /// Initialization routine that is a convience fuction for clients with the data in a string
        /// </summary>
        /// <param name="xmlText">The string that contains the xml data</param>
        public virtual void Init(string xmlText)
        {
            XmlDocument xmlDoc = GenerateXmlDocumentFromString(xmlText);

            this.Init(xmlDoc);
        }

        /// <summary>
        /// Overload of basic Init which takes a IServiceProvider and initializes 
        /// what it can of the container with elements provided by IServcieProvider
        /// 
        /// Today this is only the IManagedProvider if available but this function
        /// could be modified to init other things provided by the ServiceProvider
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="site"></param>
        public virtual void Init(XmlDocument doc, IServiceProvider site)
        {

            if (site != null)
            {
                //see if service provider supports INodeInformation interface from the object explorer
                try
                {
                    //NOTE: we're trying to forcefully set connection information on the data container.
                    //If this code doesn't execute, then dc.Init call below will result in CDataContainer
                    //initializing its ConnectionInfo member with a new object contructed off the parameters
                    //in the XML doc [server name, user name etc]
                    IManagedConnection managedConnection = site.GetService(typeof(IManagedConnection)) as IManagedConnection;
                    if (managedConnection != null)
                    {
                        this.SetManagedConnection(managedConnection);
                    }
                }
                catch (Exception ex)
                {
                    // keep the exception flowing
                    throw ex;
                }
            }

            this.Document = doc;
            LoadData();           

            // finish the initialization
            this.Init(doc);
        }


        /// <summary>
        /// main initialization method - the object is unusable until this method is called
        /// NOTE: it will ensure the ConnectionInfo and ServerConnetion objects are constructed
        /// for the appropriate server types
        /// </summary>
        /// <param name="doc"></param>
        public virtual void Init(XmlDocument doc)
        {
            //First, we read the data from XML by calling LoadData
            this.Document = doc;

            LoadData();

            //Second, create the rignt server and connection objects
            if (this.serverType == ServerType.SQL)
            {
                //ensure that we have a valid ConnectionInfo
                if (ConnectionInfo == null)
                {
                    throw new InvalidOperationException();
                }

                if (m_server == null)
                {
                    //NOTE: ServerConnection property will constuct the object if needed
                    m_server = new Server(ServerConnection);
                }
            }           
            else if (this.serverType == ServerType.SQLCE)
            {
                // do nothing; originally we were only distinguishing between two
                // types of servers (OLAP/SQL); as a result for SQLCE we were 
                // executing the same codepath as for OLAP server which was
                // resulting in an exception;
            }
        }

        /// <summary>
        /// loads data into internal members from the XML document and detects the server type
        /// [SQL, OLAP etc] based on the info in the XML doc
        /// </summary>
        public virtual void LoadData()
        {
            STParameters param;
            bool bStatus;

            param = new STParameters();

            param.SetDocument(m_doc);

            // DEVNOTE: chrisze 02/25/03
            // This is an ugly way to distinguish between different server types
            // Maybe we should pass server type as one of the parameters?
            //
            bStatus = param.GetParam("servername", ref this.serverName);

            if (!bStatus || this.serverName.Length == 0)
            {
               
                {
                    bStatus = param.GetParam("database", ref this.sqlceFilename);
				
				    if (bStatus && !String.IsNullOrEmpty(this.sqlceFilename))
				    {
					    this.serverType = ServerType.SQLCE;
				    }
				    else if (this.sqlCiWithConnection != null)
				    {	
					    this.serverType = ServerType.SQL;
				    }
				    else
				    {
					    this.serverType = ServerType.UNKNOWN;
				    }
                }              
            }
            else
            {
                //OK, let's see if <servertype> was specified in the parameters. It it was, use
                //it to double check that it is SQL
                string specifiedServerType = "";
                bStatus = param.GetParam("servertype", ref specifiedServerType);
                if (bStatus)
                {
                    if (specifiedServerType != null && "sql" != specifiedServerType.ToLowerInvariant())
                    {
                        this.serverType = ServerType.UNKNOWN;//we know only about 3 types, and 2 of them were excluded by if branch above
                    }
                    else
                    {
                        this.serverType = ServerType.SQL;
                    }
                }
                else
                {
                    this.serverType = ServerType.SQL;
                }
            }

            // Ensure there is no password in the XML document
            string temp = String.Empty;
            if (param.GetParam("password", ref temp))
            {
                temp = null;               
                throw new SecurityException();
            }

            if (ServerType.SQL == this.serverType)
            {
                this.InitializeObjectNameAndSchema();
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// we need to store it as context from the OE
        /// </summary>
        /// <param name="managedConnection"></param>
        internal void SetManagedConnection(IManagedConnection managedConnection)
        {          
            this.managedConnection = managedConnection;

            ApplyConnectionInfo(managedConnection.Connection, true);//it will do some extra initialization
        }

        /// <summary>
        /// Get the named property value from the XML document
        /// </summary>
        /// <param name="propertyName">The name of the property to get</param>
        /// <returns>The property value</returns>
        public object GetDocumentPropertyValue(string propertyName)
        {
            object result = null;
            STParameters param = new STParameters(this.Document);

            param.GetBaseParam(propertyName, ref result);

            return result;
        }

        /// <summary>
        /// Get the named property value from the XML document
        /// </summary>
        /// <param name="propertyName">The name of the property to get</param>
        /// <returns>The property value</returns>
        public string GetDocumentPropertyString(string propertyName)
        {
            object result = GetDocumentPropertyValue(propertyName);
            if (result == null)
            {
                result = String.Empty;
            }

            return (string)result;
        }

        /// <summary>
        /// Set the named property value in the XML document
        /// </summary>
        /// <param name="propertyName">The name of the property to set</param>
        /// <param name="propertyValue">The property value</param>
        public void SetDocumentPropertyValue(string propertyName, string propertyValue)
        {
            STParameters param = new STParameters(this.Document);

            param.SetParam(propertyName, propertyValue);
        }

        #endregion

        #region internal methods

        /// <summary>
        /// Reset the data container to its state from just after it was last initialized or reset
        /// </summary>
        internal void Reset()
        {
            if (this.originalDocument != null)
            {
                this.Init(this.originalDocument);
            }

            if (this.m_hashTable != null)
            {
                this.m_hashTable = new Hashtable();
            }
        }


        #endregion

        #region Private helpers

        /// <summary>
        /// Get the font size specified in the STParameters structure
        /// </summary>
        /// <param name="param">The structure from which to extract font data</param>
        //private float GetFontSize(STParameters param)
        //{
        //    float result = 8.0f;
        //    string fontSize = String.Empty;

        //    if (param.GetParam("fontsize", ref fontSize) && (0 != fontSize.Length))
        //    {
        //        result = Convert.ToSingle(fontSize, CultureInfo.InvariantCulture);
        //    }

        //    return result;
        //}

        /// <summary>
        /// Get the font style specified in the STParameters structure
        /// </summary>
        /// <param name="param">The structure from which to extract font data</param>
        //private FontStyle GetFontStyle(STParameters param)
        //{
        //    FontStyle style = FontStyle.Regular;
        //    string fontStyle = String.Empty;

        //    if (param.GetParam("fontstyle", ref fontStyle) && (0 != fontStyle.Length))
        //    {
        //        bool styleIsInitialized = false;
        //        string fontStyleUpper = fontStyle.ToUpperInvariant();

        //        if (-1 != fontStyleUpper.IndexOf("BOLD",StringComparison.Ordinal))
        //        {
        //            style = FontStyle.Bold;
        //            styleIsInitialized = true;
        //        }

        //        if (-1 != fontStyleUpper.IndexOf("ITALIC",StringComparison.Ordinal))
        //        {
        //            if (styleIsInitialized)
        //            {
        //                style |= FontStyle.Italic;
        //            }
        //            else
        //            {
        //                style = FontStyle.Italic;
        //                styleIsInitialized = true;
        //            }
        //        }

        //        if (-1 != fontStyleUpper.IndexOf("REGULAR",StringComparison.Ordinal))
        //        {
        //            if (styleIsInitialized)
        //            {
        //                style |= FontStyle.Regular;
        //            }
        //            else
        //            {
        //                style = FontStyle.Regular;
        //                styleIsInitialized = true;
        //            }
        //        }

        //        if (-1 != fontStyleUpper.IndexOf("STRIKEOUT",StringComparison.Ordinal))
        //        {
        //            if (styleIsInitialized)
        //            {
        //                style |= FontStyle.Strikeout;
        //            }
        //            else
        //            {
        //                style = FontStyle.Strikeout;
        //                styleIsInitialized = true;
        //            }
        //        }

        //        if (-1 != fontStyleUpper.IndexOf("UNDERLINE",StringComparison.Ordinal))
        //        {
        //            if (styleIsInitialized)
        //            {
        //                style |= FontStyle.Underline;
        //            }
        //            else
        //            {
        //                style = FontStyle.Underline;
        //                styleIsInitialized = true;
        //            }
        //        }
        //    }

        //    return style;
        //}

        /// <summary>
        /// Get the name and schema (if applicable) for the object we are referring to
        /// </summary>
        private void InitializeObjectNameAndSchema()
        {
            string documentUrn = this.GetDocumentPropertyString("urn");

            if (documentUrn.Length != 0)
            {
                Urn urn = new Urn(documentUrn);
                string name = urn.GetAttribute("Name");
                string schema = urn.GetAttribute("Schema");

                if ((name != null) && (name.Length != 0))
                {
                    this.ObjectName = name;
                }

                if ((schema != null) && (schema.Length != 0))
                {
                    this.ObjectSchema = schema;
                }
            }
        }

        /// <summary>
        /// returns SqlConnectionInfoWithConnection object constructed using our internal vars
        /// </summary>
        /// <returns></returns>
        private SqlConnectionInfoWithConnection GetTempSqlConnectionInfoWithConnection(
            string serverName,
            bool trusted,
            string userName,
            SecureString password)
        {         
            SqlConnectionInfoWithConnection tempCI = new SqlConnectionInfoWithConnection(serverName);
            tempCI.SingleConnection = false;
            tempCI.Pooled = false;
            //BUGBUG - set the right application name?
            if (trusted)
            {
                tempCI.UseIntegratedSecurity = true;
            }
            else
            {
                tempCI.UseIntegratedSecurity = false;
                tempCI.UserName = userName;
                tempCI.SecurePassword = password;
            }

            return tempCI;
        }


        /// <summary>
        /// our handler of sqlCiWithConnection.ConnectionClosed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSqlConnectionClosed(object sender, EventArgs e)
        {          
            //nothing - per MRaheem we'll let user deal with this situation
        }

        /// <summary>
        /// stores specified connection info and performs some extra initialization steps
        /// that can only be done after we have the connection information
        /// </summary>
        /// <param name="ci"></param>
        private void ApplyConnectionInfo(SqlOlapConnectionInfoBase ci, bool ownConnection)
        {
          
            this.connectionInfo = ci;
            this.ownConnection = ownConnection;

            //cache the cast value. It is OK that it is null for non SQL types
            this.sqlCiWithConnection = ci as SqlConnectionInfoWithConnection;

            if (this.sqlCiWithConnection != null)
            {               
                //we want to be notified if it is closed
                this.sqlCiWithConnection.ConnectionClosed += new EventHandler(OnSqlConnectionClosed);
            }
        }

        ///// <summary>
        ///// returns string that should be specified to AMO Server.Connect method
        ///// This member is used for non-express sku only
        ///// </summary>
        ///// <value></value>
        //internal string OlapConnectionString
        //{
        //    get
        //    {
        //        OlapConnectionInfo olapCi = this.ConnectionInfo as OlapConnectionInfo;
        //        if (olapCi != null)
        //        {
        //            return olapCi.ConnectionString;
        //        }
        //        else
        //        {
        //            STrace.Assert(this.olapServerName != null);
        //            return string.Format(CultureInfo.InvariantCulture, "Data Source={0}", this.olapServerName);
        //        }

        //    }
        //}
        
        private static bool MustRethrow(Exception exception)
        {
            bool result = false;

            switch (exception.GetType().Name)
            {
                case "ExecutionEngineException":
                case "OutOfMemoryException":
                case "AccessViolationException":
                case "BadImageFormatException":
                case "InvalidProgramException":

                    result = true;
                    break;
            }

            return result;
        }

        /// <summary>
        /// Generates an XmlDocument from a string, avoiding exploits available through
        /// DTDs
        /// </summary>
        /// <param name="sourceXml"></param>
        /// <returns></returns>
        private XmlDocument GenerateXmlDocumentFromString(string sourceXml)
        {
            if (sourceXml == null)
            {
                throw new ArgumentNullException("sourceXml");
            }
            if (sourceXml.Length == 0)
            {
                throw new ArgumentException("sourceXml");
            }

            MemoryStream memoryStream = new MemoryStream();
            StreamWriter streamWriter = new StreamWriter(memoryStream);

            //  Writes the xml to the memory stream
            streamWriter.Write(sourceXml);
            streamWriter.Flush();

            //  Resets the stream to the beginning
            memoryStream.Seek(0, SeekOrigin.Begin);

            //  Creates the XML reader from the stream 
            //  and moves it to the correct node
            XmlReader xmlReader = XmlReader.Create(memoryStream);
            xmlReader.MoveToContent();

            // generate the xml document
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.PreserveWhitespace = true;
            xmlDocument.LoadXml(xmlReader.ReadOuterXml());

            return xmlDocument;
        }

        #endregion

        #region ICustomTypeDescriptor

        //AttributeCollection ICustomTypeDescriptor.GetAttributes()
        //{
        //    return AttributeCollection.Empty;
        //}

        //string ICustomTypeDescriptor.GetClassName()
        //{
        //    return TypeDescriptor.GetClassName(this, true);
        //}

        //string ICustomTypeDescriptor.GetComponentName()
        //{
        //    return TypeDescriptor.GetComponentName(this, true);
        //}

        //TypeConverter ICustomTypeDescriptor.GetConverter()
        //{
        //    return TypeDescriptor.GetConverter(GetType());
        //}

        //EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
        //{
        //    return TypeDescriptor.GetDefaultEvent(GetType());
        //}

        //PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
        //{
        //    return TypeDescriptor.GetDefaultProperty(GetType());
        //}

        //object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
        //{
        //    return TypeDescriptor.GetEditor(GetType(), editorBaseType);
        //}

        //EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
        //{
        //    return EventDescriptorCollection.Empty;
        //}

        //EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes)
        //{
        //    return EventDescriptorCollection.Empty;
        //}

        //PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
        //{
        //    return PropertyDescriptorCollection.Empty;
        //}

        //PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
        //{
        //    PropertyDescriptorCollection propertyDescriptorCollection = new PropertyDescriptorCollection(null);

        //    if (this.serverType == ServerType.SQL)
        //    {
        //        bool serverIsAvailable = false;
        //        int queryTimeout = 30;

        //        try
        //        {
        //            // five seconds should be plenty to determine the network name of the server
        //            queryTimeout = this.m_server.ConnectionContext.StatementTimeout;
        //            this.m_server.ConnectionContext.StatementTimeout = 5;
        //            serverIsAvailable = this.m_server.Information.NetName != null;
        //        }
        //        catch (Exception e)
        //        {
        //            if (MustRethrow(e))
        //            {
        //                throw;
        //            }
        //        }
        //        finally
        //        {
        //            this.m_server.ConnectionContext.StatementTimeout = queryTimeout;
        //        }

        //        PropertyDescriptor propertyDescriptor;
        //        /////////////////////////////ServerEnvironment////////////////////////////////
        //        // Computer Name
        //        try
        //        {
        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                 serverIsAvailable ? this.m_server.Information.NetName : SR.ServerIsUnavailable,
        //                 SR.ComputerName,
        //                 SR.ServerEnvironment,
        //                 SR.ComputerNameDescription);
        //        }
        //        catch (Exception e)
        //        {
        //            if (MustRethrow(e))
        //            {
        //                throw;
        //            }

        //            serverIsAvailable = false;

        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                (e.Message != null) ? e.Message : SR.ServerIsUnavailable,
        //                SR.ComputerName,
        //                SR.ServerEnvironment,
        //                SR.ComputerNameDescription);
        //        }

        //        propertyDescriptorCollection.Add(propertyDescriptor);

        //        // Platform
        //        try
        //        {
        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                serverIsAvailable ?
        //                    ((this.m_server.Information.Platform == null) ?
        //                        String.Empty :
        //                        this.m_server.Information.Platform) :
        //                    SR.ServerIsUnavailable,
        //                SR.Platform,
        //                SR.ServerEnvironment,
        //                SR.PlatformDescription);
        //        }
        //        catch (Exception e)
        //        {
        //            if (MustRethrow(e))
        //            {
        //                throw;
        //            }

        //            serverIsAvailable = false;

        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                (e.Message != null) ? e.Message : SR.ServerIsUnavailable,
        //                SR.Platform,
        //                SR.ServerEnvironment,
        //                SR.PlatformDescription);
        //        }

        //        propertyDescriptorCollection.Add(propertyDescriptor);

        //        // Operating System
        //        try
        //        {
        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                serverIsAvailable ? this.m_server.Information.OSVersion : SR.ServerIsUnavailable,
        //                SR.OperatingSystem,
        //                SR.ServerEnvironment,
        //                SR.OperatingSystemDescription);
        //        }
        //        catch (Exception e)
        //        {
        //            if (MustRethrow(e))
        //            {
        //                throw;
        //            }

        //            serverIsAvailable = false;

        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                (e.Message != null) ? e.Message : SR.ServerIsUnavailable,
        //                SR.OperatingSystem,
        //                SR.ServerEnvironment,
        //                SR.OperatingSystemDescription);
        //        }

        //        propertyDescriptorCollection.Add(propertyDescriptor);

        //        // Processors
        //        try
        //        {
        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                serverIsAvailable ?
        //                    this.m_server.Information.Processors.ToString() :
        //                    SR.ServerIsUnavailable,
        //                SR.Processors,
        //                SR.ServerEnvironment,
        //                SR.ProcessorsDescription);
        //        }
        //        catch (Exception e)
        //        {
        //            if (MustRethrow(e))
        //            {
        //                throw;
        //            }

        //            serverIsAvailable = false;

        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                (e.Message != null) ? e.Message : SR.ServerIsUnavailable,
        //                SR.Processors,
        //                SR.ServerEnvironment,
        //                SR.ProcessorsDescription);
        //        }

        //        propertyDescriptorCollection.Add(propertyDescriptor);

        //        // Operating System Memory
        //        try
        //        {
        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                serverIsAvailable ?
        //                    this.m_server.Information.PhysicalMemory.ToString() :
        //                    SR.ServerIsUnavailable,
        //                SR.OperatingSystemMemory,
        //                SR.ServerEnvironment,
        //                SR.OperatingSystemMemoryDescription);
        //        }
        //        catch (Exception e)
        //        {
        //            if (MustRethrow(e))
        //            {
        //                throw;
        //            }

        //            serverIsAvailable = false;

        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                (e.Message != null) ? e.Message : SR.ServerIsUnavailable,
        //                SR.OperatingSystemMemory,
        //                SR.ServerEnvironment,
        //                SR.OperatingSystemMemoryDescription);
        //        }

        //        propertyDescriptorCollection.Add(propertyDescriptor);

        //        /////////////////////////////ProductCategory////////////////////////////////
        //        // Product Name
        //        try
        //        {
        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                serverIsAvailable ?
        //                    (this.m_server.Information.Product + " " + this.m_server.Information.Edition) :
        //                    SR.ServerIsUnavailable,
        //                SR.ProductName,
        //                SR.ProductCategory,
        //                SR.ProductNameDescription);
        //        }
        //        catch (Exception e)
        //        {
        //            if (MustRethrow(e))
        //            {
        //                throw;
        //            }

        //            serverIsAvailable = false;

        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                (e.Message != null) ? e.Message : SR.ServerIsUnavailable,
        //                SR.ProductName,
        //                SR.ProductCategory,
        //                SR.ProductNameDescription);
        //        }

        //        propertyDescriptorCollection.Add(propertyDescriptor);

        //        // Product Version
        //        try
        //        {
        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                serverIsAvailable ?
        //                    (this.m_server.Information.Version + " " + this.m_server.Information.ProductLevel) :
        //                    SR.ServerIsUnavailable,
        //                SR.ProductVersion,
        //                SR.ProductCategory,
        //                SR.ProductVersionDescription);
        //        }
        //        catch (Exception e)
        //        {
        //            if (MustRethrow(e))
        //            {
        //                throw;
        //            }

        //            serverIsAvailable = false;

        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                (e.Message != null) ? e.Message : SR.ServerIsUnavailable,
        //                SR.ProductVersion,
        //                SR.ProductCategory,
        //                SR.ProductVersionDescription);
        //        }

        //        propertyDescriptorCollection.Add(propertyDescriptor);

        //        // Server Name
        //        try
        //        {
        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                serverIsAvailable ? this.m_server.Name : SR.ServerIsUnavailable,
        //                SR.ServerName,
        //                SR.ProductCategory,
        //                SR.ServerNameDescription);
        //        }
        //        catch (Exception e)
        //        {
        //            if (MustRethrow(e))
        //            {
        //                throw;
        //            }

        //            serverIsAvailable = false;

        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                (e.Message != null) ? e.Message : SR.ServerIsUnavailable,
        //                SR.ServerName,
        //                SR.ProductCategory,
        //                SR.ServerNameDescription);
        //        }

        //        propertyDescriptorCollection.Add(propertyDescriptor);

        //        // Instance Name
        //        try
        //        {
        //            string instanceName = serverIsAvailable ? this.m_server.InstanceName : SR.ServerIsUnavailable;

        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                (instanceName != null) ? instanceName : String.Empty,
        //                SR.InstanceName,
        //                SR.ProductCategory,
        //                SR.InstanceNameDescription);
        //        }
        //        catch (Exception e)
        //        {
        //            if (MustRethrow(e))
        //            {
        //                throw;
        //            }

        //            serverIsAvailable = false;

        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                (e.Message != null) ? e.Message : SR.ServerIsUnavailable,
        //                SR.InstanceName,
        //                SR.ProductCategory,
        //                SR.InstanceNameDescription);
        //        }

        //        propertyDescriptorCollection.Add(propertyDescriptor);

        //        // Language
        //        try
        //        {
        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                serverIsAvailable ? this.m_server.Information.Language : SR.ServerIsUnavailable,
        //                SR.Language,
        //                SR.ProductCategory,
        //                SR.LanguageDescription);
        //        }
        //        catch (Exception e)
        //        {
        //            if (MustRethrow(e))
        //            {
        //                throw;
        //            }

        //            serverIsAvailable = false;

        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                (e.Message != null) ? e.Message : SR.ServerIsUnavailable,
        //                SR.Language,
        //                SR.ProductCategory,
        //                SR.LanguageDescription);
        //        }

        //        propertyDescriptorCollection.Add(propertyDescriptor);

        //        // Collation
        //        try
        //        {
        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                serverIsAvailable ? this.m_server.Information.Collation : SR.ServerIsUnavailable,
        //                SR.Collation,
        //                SR.ProductCategory,
        //                SR.CollationDescription);
        //        }
        //        catch (Exception e)
        //        {
        //            if (MustRethrow(e))
        //            {
        //                throw;
        //            }

        //            serverIsAvailable = false;

        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                (e.Message != null) ? e.Message : SR.ServerIsUnavailable,
        //                SR.Collation,
        //                SR.ProductCategory,
        //                SR.CollationDescription);
        //        }

        //        propertyDescriptorCollection.Add(propertyDescriptor);

        //        /////////////////////////////ConnectionCategory////////////////////////////////
        //        // Database
        //        try
        //        {
        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                serverIsAvailable ?
        //                    this.m_server.ConnectionContext.SqlConnectionObject.Database :
        //                    SR.ServerIsUnavailable,
        //                SR.Database,
        //                SR.ConnectionCategory,
        //                SR.DatabaseDescription);
        //        }
        //        catch (Exception e)
        //        {
        //            if (MustRethrow(e))
        //            {
        //                throw;
        //            }

        //            serverIsAvailable = false;

        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                (e.Message != null) ? e.Message : SR.ServerIsUnavailable,
        //                SR.Database,
        //                SR.ConnectionCategory,
        //                SR.DatabaseDescription);
        //        }

        //        propertyDescriptorCollection.Add(propertyDescriptor);

        //        // SPID
        //        try
        //        {
        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                serverIsAvailable ?
        //                    this.m_server.ConnectionContext.ProcessID.ToString() :
        //                    SR.ServerIsUnavailable,
        //                SR.Spid,
        //                SR.ConnectionCategory,
        //                SR.SpidDescription);
        //        }
        //        catch (InvalidCastException)
        //        {
        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                String.Empty,
        //                SR.Spid,
        //                SR.ConnectionCategory,
        //                SR.SpidDescription);
        //        }
        //        catch (Exception e)
        //        {
        //            if (MustRethrow(e))
        //            {
        //                throw;
        //            }

        //            serverIsAvailable = false;

        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                (e.Message != null) ? e.Message : SR.ServerIsUnavailable,
        //                SR.Spid,
        //                SR.ConnectionCategory,
        //                SR.SpidDescription);
        //        }

        //        propertyDescriptorCollection.Add(propertyDescriptor);

        //        // Network Protocol
        //        try
        //        {
        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                serverIsAvailable ?
        //                    (this.m_server.ConnectionContext.NetworkProtocol == NetworkProtocol.NotSpecified ?
        //                        SR.Default :
        //                        this.m_server.ConnectionContext.NetworkProtocol.ToString()) :
        //                    SR.ServerIsUnavailable,
        //                SR.NetworkProtocol,
        //                SR.ConnectionCategory,
        //                SR.NetworkProtocolDescription);
        //        }
        //        catch (Exception e)
        //        {
        //            if (MustRethrow(e))
        //            {
        //                throw;
        //            }

        //            serverIsAvailable = false;

        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                (e.Message != null) ? e.Message : SR.ServerIsUnavailable,
        //                SR.NetworkProtocol,
        //                SR.ConnectionCategory,
        //                SR.NetworkProtocolDescription);
        //        }

        //        propertyDescriptorCollection.Add(propertyDescriptor);

        //        // Network Packet Size
        //        try
        //        {
        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                serverIsAvailable ?
        //                    this.m_server.ConnectionContext.PacketSize.ToString() :
        //                    SR.ServerIsUnavailable,
        //                SR.NetworkPacketSize,
        //                SR.ConnectionCategory,
        //                SR.NetworkPacketSizeDescription);
        //        }
        //        catch (Exception e)
        //        {
        //            if (MustRethrow(e))
        //            {
        //                throw;
        //            }

        //            serverIsAvailable = false;

        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                (e.Message != null) ? e.Message : SR.ServerIsUnavailable,
        //                SR.NetworkPacketSize,
        //                SR.ConnectionCategory,
        //                SR.NetworkPacketSizeDescription);
        //        }

        //        propertyDescriptorCollection.Add(propertyDescriptor);

        //        // Connect Timeout
        //        try
        //        {
        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                serverIsAvailable ?
        //                    this.m_server.ConnectionContext.ConnectTimeout.ToString() :
        //                    SR.ServerIsUnavailable,
        //                SR.ConnectTimeout,
        //                SR.ConnectionCategory,
        //                SR.ConnectTimeoutDescription);
        //        }
        //        catch (Exception e)
        //        {
        //            if (MustRethrow(e))
        //            {
        //                throw;
        //            }

        //            serverIsAvailable = false;

        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                (e.Message != null) ? e.Message : SR.ServerIsUnavailable,
        //                SR.ConnectTimeout,
        //                SR.ConnectionCategory,
        //                SR.ConnectTimeoutDescription);
        //        }

        //        propertyDescriptorCollection.Add(propertyDescriptor);

        //        // Statement Timeout
        //        try
        //        {
        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                serverIsAvailable ?
        //                    this.m_server.ConnectionContext.StatementTimeout.ToString() :
        //                    SR.ServerIsUnavailable,
        //                SR.StatementTimeout,
        //                SR.ConnectionCategory,
        //                SR.StatementTimeoutDescription);
        //        }
        //        catch (Exception e)
        //        {
        //            if (MustRethrow(e))
        //            {
        //                throw;
        //            }

        //            serverIsAvailable = false;

        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                (e.Message != null) ? e.Message : SR.ServerIsUnavailable,
        //                SR.StatementTimeout,
        //                SR.ConnectionCategory,
        //                SR.StatementTimeoutDescription);
        //        }

        //        propertyDescriptorCollection.Add(propertyDescriptor);

        //        // Encryption
        //        try
        //        {
        //            string encryptAsString = SR.NoString;

        //            if (serverIsAvailable)
        //            {
        //                if (this.m_server.ConnectionContext.EncryptConnection)
        //                {
        //                    encryptAsString = SR.YesString;
        //                }
        //            }
        //            else
        //            {
        //                encryptAsString = SR.ServerIsUnavailable;
        //            }

        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                encryptAsString,
        //                SR.EncryptedConnection,
        //                SR.ConnectionCategory,
        //                SR.EncryptedConnectionDescription);
        //        }
        //        catch (InvalidCastException)
        //        {
        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                String.Empty,
        //                SR.EncryptedConnection,
        //                SR.ConnectionCategory,
        //                SR.EncryptedConnectionDescription);
        //        }
        //        catch (Exception e)
        //        {
        //            if (MustRethrow(e))
        //            {
        //                throw;
        //            }

        //            serverIsAvailable = false;

        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                (e.Message != null) ? e.Message : SR.ServerIsUnavailable,
        //                SR.EncryptedConnection,
        //                SR.ConnectionCategory,
        //                SR.EncryptedConnectionDescription);
        //        }

        //        propertyDescriptorCollection.Add(propertyDescriptor);

        //        /////////////////////////////Authentication////////////////////////////////
        //        // Authentication
        //        try
        //        {
        //            string authenticationType;

        //            if (!serverIsAvailable)
        //            {
        //                authenticationType = SR.ServerIsUnavailable;
        //            }
        //            else if (this.m_server.ConnectionContext.LoginSecure)
        //            {
        //                authenticationType = SR.WindowsAuthentication;
        //            }
        //            else
        //            {
        //                authenticationType = SR.SqlServerAuthentication;
        //            }

        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                authenticationType,
        //                SR.AuthenticationMethod,
        //                SR.AuthenticationCategory,
        //                SR.AuthenticationMethodDescription);

        //        }
        //        catch (PropertyNotAvailableException e)
        //        {
        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                (e.Message == null) ? String.Empty : e.Message,
        //                SR.AuthenticationMethod,
        //                SR.AuthenticationCategory,
        //                SR.AuthenticationMethodDescription);

        //        }
        //        catch (Exception exception)
        //        {
        //            if (MustRethrow(exception))
        //            {
        //                throw;
        //            }

        //            serverIsAvailable = false;

        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                (exception.Message != null) ? exception.Message : SR.ServerIsUnavailable,
        //                SR.EncryptedConnection,
        //                SR.ConnectionCategory,
        //                SR.EncryptedConnectionDescription);
        //        }

        //        propertyDescriptorCollection.Add(propertyDescriptor);

        //        // TrueLogin
        //        try
        //        {
        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                serverIsAvailable ? this.m_server.ConnectionContext.TrueLogin : SR.ServerIsUnavailable,
        //                SR.UserName,
        //                SR.AuthenticationCategory,
        //                SR.UserNameDescription);
        //        }
        //        catch (Exception e)
        //        {
        //            if (MustRethrow(e))
        //            {
        //                throw;
        //            }

        //            serverIsAvailable = false;

        //            propertyDescriptor = new PropertyDescriptorWrapper(
        //                (e.Message != null) ? e.Message : SR.ServerIsUnavailable,
        //                SR.UserName,
        //                SR.AuthenticationCategory,
        //                SR.UserNameDescription);
        //        }

        //        propertyDescriptorCollection.Add(propertyDescriptor);
        //    }
        //    else if (!Utils.IsSsmsMinimalSet())
        //    {
        //        if (this.serverType == ServerType.OLAP)
        //    {
        //        PropertyDescriptor propertyDescriptor;

        //        /////////////////////////////ServerEnvironment////////////////////////////////

        //        /////////////////////////////ProductCategory////////////////////////////////
        //        // Product Version
        //        propertyDescriptor = new PropertyDescriptorWrapper(this.m_amoServer.Version, SR.ProductVersion, SR.ProductCategory, SR.ProductVersionDescription);
        //        propertyDescriptorCollection.Add(propertyDescriptor);
        //        // Server Name
        //        propertyDescriptor = new PropertyDescriptorWrapper(this.m_amoServer.Name, SR.ServerName, SR.ProductCategory, SR.ServerNameDescription);
        //        propertyDescriptorCollection.Add(propertyDescriptor);
        //        // Language
        //        int langAsInt = Convert.ToInt32(this.m_amoServer.ServerProperties[@"Language"].Value, CultureInfo.InvariantCulture);
        //        if (langAsInt > 0)
        //        {
        //            try
        //            {
        //                propertyDescriptor = new PropertyDescriptorWrapper(new CultureInfo(langAsInt).ToString(), SR.Language, SR.ProductCategory, SR.LanguageDescription);
        //                propertyDescriptorCollection.Add(propertyDescriptor);
        //            }
        //            catch //eat it - CultureInfo might not be creatable from this ID
        //            { }
        //        }
        //        // Collation
        //        propertyDescriptor = new PropertyDescriptorWrapper(this.m_amoServer.ServerProperties[@"CollationName"].Value, SR.Collation, SR.ProductCategory, SR.CollationDescription);
        //        propertyDescriptorCollection.Add(propertyDescriptor);

        //        /////////////////////////////ConnectionCategory////////////////////////////////
        //        // Database
        //        propertyDescriptor = new PropertyDescriptorWrapper(this.ConnectionInfo.DatabaseName, SR.Database, SR.ConnectionCategory, SR.DatabaseDescription);
        //        propertyDescriptorCollection.Add(propertyDescriptor);
        //        // Connect Timeout
        //        propertyDescriptor = new PropertyDescriptorWrapper(this.ConnectionInfo.ConnectionTimeout, SR.ConnectTimeout, SR.ConnectionCategory, SR.ConnectTimeoutDescription);
        //        propertyDescriptorCollection.Add(propertyDescriptor);
        //        // Execution Timeout
        //        propertyDescriptor = new PropertyDescriptorWrapper(this.ConnectionInfo.QueryTimeout, SR.StatementTimeout, SR.ConnectionCategory, SR.StatementTimeoutDescription);
        //        propertyDescriptorCollection.Add(propertyDescriptor);

        //        //whether connection is encrypted or not
        //        OlapConnectionInfo olapCi = this.ConnectionInfo as OlapConnectionInfo;
        //        if (olapCi != null)
        //        {
        //            string encryptAsString = SR.NoString;
        //            if (olapCi.EncryptConnection)
        //            {
        //                encryptAsString = SR.YesString;
        //            }
        //            propertyDescriptor = new PropertyDescriptorWrapper(encryptAsString, SR.EncryptedConnection, SR.ConnectionCategory, SR.EncryptedConnectionDescription);
        //            propertyDescriptorCollection.Add(propertyDescriptor);
        //        }

        //        // Authentication
        //        if (this.ConnectionInfo.UseIntegratedSecurity)
        //            propertyDescriptor = new PropertyDescriptorWrapper(SR.WindowsAuthentication, SR.AuthenticationMethod, SR.AuthenticationCategory, SR.AuthenticationMethodDescription);
        //        else
        //            propertyDescriptor = new PropertyDescriptorWrapper(SR.SqlServerAuthentication, SR.AuthenticationMethod, SR.AuthenticationCategory, SR.AuthenticationMethodDescription);
        //        propertyDescriptorCollection.Add(propertyDescriptor);
        //        // TrueLogin
        //        propertyDescriptor = new PropertyDescriptorWrapper(this.ConnectionInfo.UserName, SR.UserName, SR.AuthenticationCategory, SR.UserNameDescription);
        //        propertyDescriptorCollection.Add(propertyDescriptor);
        //    }
        //    else if (this.serverType == ServerType.SQLCE)
        //    {
        //        PropertyDescriptor propertyDescriptor;

        //        // Create an instance of SQLCE engine through reflection
        //        //
        //        Assembly asm = SfcUtility.LoadSqlCeAssembly(
        //            "Microsoft.SqlServerCe.Client.dll");
        //        Type type = asm.GetType("Microsoft.SqlServerCe.Client.SqlCeEngine", true);

        //        // Create SqlCeEngine instance
        //        //
        //        ConstructorInfo constructor = type.GetConstructor(new Type[] { });
        //        object instance = constructor.Invoke(new object[] { });

        //        // Call GetDatabaseProperties
        //        //
        //        Type[] types = { typeof(String) };
        //        MethodInfo mi = type.GetMethod("GetDatabaseProperties", types);

        //        instance = mi.Invoke(instance, new object[] { SqlCeFileName });

        //        // Call the accessors on the DatabaseProperties class
        //        //
        //        type = asm.GetType("Microsoft.SqlServerCe.Client.DatabaseProperties", true);

        //        PropertyInfo pi = type.GetProperty("Platform");
        //        string platform = (string)pi.GetValue(instance, new object[] { });

        //        pi = type.GetProperty("OSVersion");
        //        string osVersion = (string)pi.GetValue(instance, new object[] { });

        //        pi = type.GetProperty("Version");
        //        string sqlCeVersion = (string)pi.GetValue(instance, new object[] { });

        //        pi = type.GetProperty("ClrVersion");
        //        string clrVersion = (string)pi.GetValue(instance, new object[] { });

        //        // Platform
        //        propertyDescriptor = new PropertyDescriptorWrapper(platform, SR.Platform, SR.ServerEnvironment, SR.PlatformDescription);
        //        propertyDescriptorCollection.Add(propertyDescriptor);

        //        // Operating System
        //        propertyDescriptor = new PropertyDescriptorWrapper(osVersion, SR.OperatingSystem, SR.ServerEnvironment, SR.OperatingSystemDescription);
        //        propertyDescriptorCollection.Add(propertyDescriptor);

        //        // CLR version
        //        propertyDescriptor = new PropertyDescriptorWrapper(clrVersion, SR.ClrVersion, SR.ServerEnvironment, SR.ClrVersionDescription);
        //        propertyDescriptorCollection.Add(propertyDescriptor);

        //        // Product Name
        //        propertyDescriptor = new PropertyDescriptorWrapper(SR.SqlServerCeName, SR.ProductName, SR.ProductCategory, SR.ProductNameDescriptionCE);
        //        propertyDescriptorCollection.Add(propertyDescriptor);

        //        // Product Version
        //        propertyDescriptor = new PropertyDescriptorWrapper(sqlCeVersion, SR.ProductVersion, SR.ProductCategory, SR.ProductVersionDescriptionCE);
        //        propertyDescriptorCollection.Add(propertyDescriptor);
        //    }
        //    }
        //    return propertyDescriptorCollection;
        //}

        //object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
        //{
        //    return this;
        //}

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// MUST be called, as we'll be closing SQL connection inside this call
        /// </summary>
        private void Dispose(bool disposing)
        {           
            try
            {
                //take care of live SQL connection
                if (this.sqlCiWithConnection != null)
                {
                    this.sqlCiWithConnection.ConnectionClosed -= new EventHandler(OnSqlConnectionClosed);

                    if (disposing)
                    {
                        //if we have the managed connection interface, then use it to disconnect.
                        //Otherwise, Dispose on SqlConnectionInfoWithConnection should disconnect
                        if (this.managedConnection != null)
                        {
                            //in this case we have gotten sqlCiWithConnection as this.managedConnection.Connection
                            if (this.ownConnection)
                            {
                                this.managedConnection.Close();
                            }
                            this.managedConnection = null;
                        }
                        else
                        {
                            if (this.ownConnection)
                            {
                                this.sqlCiWithConnection.Dispose();//internally will decide whether to disconnect or not
                            }
                        }
                        this.sqlCiWithConnection = null;
                    }
                    else
                    {
                        this.managedConnection = null;
                        this.sqlCiWithConnection = null;
                    }
                }
                else if (this.managedConnection != null)
                {
                    if (disposing && this.ownConnection)
                    {
                        this.managedConnection.Close();
                    }
                    this.managedConnection = null;
                }               
            }
            catch (Exception exToEat)
            {               
            }
        }

        #endregion
    }
}