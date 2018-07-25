using System;
using System.Collections.Generic;
using System.Linq;
using LINQPad;
using LINQPad.Extensibility.DataContext;
using Tridion.ContentManager.CoreService.Client;

namespace TcmLINQPadDriver
{
    /// <summary>
    /// This static driver let users query any Tridion system through its Core Service - in other words,
    /// that exposes properties of type IEnumerable of T.
    /// </summary>
    public class TridionStaticDriver : StaticDataContextDriver
	{
		public override string Name { get { return "Tridion Core Service Driver"; } }

		public override string Author { get { return "Frank van Puffelen"; } }

		public override string GetConnectionDescription (IConnectionInfo cxInfo)
		{
			// For static drivers, we can use the description of the custom type & its assembly:)
		    var data = cxInfo.DriverData;
		    var username = data.Attribute("Username");
		    var result = "";

            if (data.Attribute("Context") != null && !string.IsNullOrEmpty(data.Attribute("Context").Value))
            {
                result += data.Attribute("Context").Value + " ";
            }

            if (Boolean.Parse(data.Attribute("MFA").Value)) {
                result += "<MFA>@";
            } else {
                result += username != null && !string.IsNullOrEmpty(username.Value) ? username.Value + "@" : "";
            }

            result += data.Attribute("Hostname").Value + " (Tridion)";

            return result;
		}

		public override bool ShowConnectionDialog (IConnectionInfo cxInfo, bool isNewConnection)
		{
			// Prompt the user for a host name, user name and password
            cxInfo.CustomTypeInfo.CustomAssemblyPath = this.GetDriverFolder() + "\\TcmLINQPadDriver.dll";
            cxInfo.CustomTypeInfo.CustomTypeName = "TcmLINQPadDriver.LINQPadCoreServiceClient";
			return new ConnectionDialog (cxInfo).ShowDialog() == true;
		}

	    public override IEnumerable<string> GetNamespacesToAdd(IConnectionInfo cxInfo)
	    {
	        // TODO: add all common core service namespaces
	        return new[]
	                   {
                           "Tridion.ContentManager.CoreService.Client"
	                   };
	    }

	    public override IEnumerable<string> GetAssembliesToAdd(IConnectionInfo cxInfo)
	    {
	        return new[]
	                   {
	                       "System.ServiceModel, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
	                       "System.Runtime.Serialization, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
	                   };
	    }

	    public override ParameterDescriptor[] GetContextConstructorParameters(IConnectionInfo cxInfo)
        {
            return new [] {
                new ParameterDescriptor("hostname", "System.String"),
                new ParameterDescriptor("secure", "System.Boolean"),
                new ParameterDescriptor("MFA", "System.Boolean"),
                new ParameterDescriptor("username", "System.String"),
                new ParameterDescriptor("password", "System.String"),
                new ParameterDescriptor("context", "System.String")
            };
        }
        public override object[] GetContextConstructorArguments(IConnectionInfo cxInfo)
        {
            var data = cxInfo.DriverData;
            return new object[]
                       {
                           data.Attribute("Hostname").Value,
                           Boolean.Parse(data.Attribute("Secure").Value),
                           Boolean.Parse(data.Attribute("MFA").Value),
                           data.Attribute("Username").Value,
                           data.Attribute("Password").Value,
                           data.Attribute("Context") != null ? data.Attribute("Context").Value : null
                       };
        }

		public override void InitializeContext (IConnectionInfo cxInfo, object context, QueryExecutionManager executionManager)
		{
			// If the data context happens to be a LINQ to SQL DataContext, we can look up the SQL translation window.
			//var l2s = context as System.Data.Linq.DataContext;
			//if (l2s != null) l2s.Log = executionManager.SqlTranslationWriter;
		}

		public override List<ExplorerItem> GetSchema (IConnectionInfo cxInfo, Type customType)
		{
			// Return the objects with which to populate the Schema Explorer by reflecting over customType.

            var itemTypes = new [] {
                    //new ExplorerItem("Client", ExplorerItemKind.QueryableObject, ExplorerIcon.ScalarFunction),
				    new ExplorerItem("Publications", ExplorerItemKind.QueryableObject, ExplorerIcon.Table)
				    {
					    IsEnumerable = true,
					    ToolTipText = "Publication"
                        /*,
                        Children = new List<ExplorerItem>
                        {
                            new ExplorerItem("Id", ExplorerItemKind.Property, ExplorerIcon.Parameter),
                            new ExplorerItem("Title", ExplorerItemKind.Property, ExplorerIcon.Parameter),
                            new ExplorerItem("PublicationType", ExplorerItemKind.Property, ExplorerIcon.Parameter),
                            new ExplorerItem("Key", ExplorerItemKind.Property, ExplorerIcon.Parameter)
                        }
                        */
				    },
                    GetExplorerItem("Users", "Users"),
                    GetExplorerItem("Groups", "Groups"),
                    GetExplorerItem("PublishTransactions", "PublishTransactions")
                }.ToList();

		    var context = cxInfo.DriverData.Attribute("Context") != null ? cxInfo.DriverData.Attribute("Context").Value : null;

            if (!string.IsNullOrEmpty(context))
            {
                ItemType type = GetItemType(context);
                bool isRecursive = type == ItemType.Publication;
                var suffix = string.Format(isRecursive ? "in {0}" : "under {0} (recursively)", context);
                itemTypes.Add(GetExplorerItem("Items", "All items " + suffix));
                itemTypes.Add(GetExplorerItem("ItemElements", "All items (as XElement) " + suffix));
                if (type == ItemType.Publication || type == ItemType.Folder) itemTypes.Add(GetExplorerItem("Components", "All Components " + suffix));
                if (type == ItemType.Publication || type == ItemType.StructureGroup) itemTypes.Add(GetExplorerItem("Pages", "All Pages "+suffix));
                if (type == ItemType.Publication) itemTypes.Add(GetExplorerItem("Categories", "All Categories " + suffix));
            }

		    return itemTypes;
		}

	    public static ItemType GetItemType(string context)
	    {
	        var split = context.Substring(context.IndexOf(':') + 1).Split('-');
            if (split.Length < 3) return ItemType.Component;
	        return (ItemType) int.Parse(split[2]);
	    }

        public static ExplorerItem GetExplorerItem(string name, string description)
        {
            return new ExplorerItem(name, ExplorerItemKind.QueryableObject, ExplorerIcon.Table)
                       {IsEnumerable = true, ToolTipText = description};
        }

	    public override ICustomMemberProvider GetCustomDisplayMemberProvider(Object item)
        {
            if (item is PublicationData)
            {
            }
            return null;
        }
    }


}
