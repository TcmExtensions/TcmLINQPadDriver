using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Tridion.ContentManager.CoreService.Client;

/// <summary>
/// A helper class to simplify creating new CoreServiceClients (without configuration files) and cleaning them up correctly.
/// </summary>
public static class TcmCoreService
{
    public enum BindingType { basicHttp, wsHttp, netTcp }

    public static ReadOptions DEFAULT_READ_OPTIONS = new ReadOptions();
    public static XNamespace R5_NAMESPACE = "http://www.tridion.com/ContentManager/5.0";
    public static XNamespace XLINK_NAMESPACE = "http://www.w3.org/1999/xlink";
    public static XNamespace XSD_NAMESPACE = "http://www.w3.org/2001/XMLSchema";

    /// <summary>
    /// Returns a new client based on the app.config (or web.config) of the application.
    /// </summary>
    /// <returns>A CoreServiceClient that can be used in a using block</returns>
    public static CoreServiceClient GetConfiguredClient()
    {
        return new SmarterCoreServiceClient();
    }

    /// <summary>
    /// Returns a new client based on values in the code; you don't need an app.config.
    /// </summary>
    /// <returns></returns>
    public static CoreServiceClient GetClient(BindingType type, bool secure)
    {
        var host = Environment.GetEnvironmentVariable("TRIDION_HOST") ?? "localhost";
        return new SmarterCoreServiceClient(GetBinding(type, secure), GetEndpoint(type, host, secure));
    }

    /// <summary>
    /// Returns a new client to the Core Service on the given host
    /// </summary>
    /// <returns></returns>
    public static CoreServiceClient GetClient(string hostName, bool secure)
    {
        hostName = hostName ?? "localhost";
        var bindingType = hostName == "localhost" ? BindingType.netTcp : BindingType.basicHttp;
        // client.ChannelFactory.Credentials.Windows.ClientCredential = CredentialCache.DefaultNetworkCredentials;
        // client.ChannelFactory.Credentials.Windows.ClientCredential = new System.Net.NetworkCredential(username, password);
        // client.Impersonate(userName)
        return new SmarterCoreServiceClient(GetBinding(bindingType, secure), GetEndpoint(bindingType, hostName, secure));
    }

    public static CoreServiceClient GetClient(CoreServiceInfo info)
    {
        var client = new SmarterCoreServiceClient(GetBinding(info.BindingType, info.Secure), GetEndpoint(info.BindingType, info.HostName, info.Secure));
        var credentials = CredentialCache.DefaultNetworkCredentials;
        if (!string.IsNullOrWhiteSpace(info.UserName) && !string.IsNullOrWhiteSpace(info.Password))
        {
            credentials = new NetworkCredential(info.UserName, info.Password);
        }
   
        client.ChannelFactory.Credentials.Windows.ClientCredential = credentials;

        return client;
    }

    public static Binding GetBinding(BindingType type, bool secure)
    {
        var readerQuotas = new XmlDictionaryReaderQuotas{ MaxStringContentLength = 2147483647, MaxArrayLength = 2147483647 };
        var basicHttpSecurity = new BasicHttpSecurity{ Mode = BasicHttpSecurityMode.TransportCredentialOnly, Transport = new HttpTransportSecurity { ClientCredentialType = HttpClientCredentialType.Windows } };

        if (type == BindingType.netTcp)
        {
            return new NetTcpBinding{ MaxReceivedMessageSize = 2147483647, ReaderQuotas = readerQuotas };
        }

        if (type == BindingType.wsHttp)
        {
            return new WSHttpBinding{ MaxReceivedMessageSize = 2147483647, ReaderQuotas = readerQuotas };
        }

        if (type == BindingType.basicHttp)
        {
            if (secure) {
                basicHttpSecurity.Mode = BasicHttpSecurityMode.Transport;
            }

            BasicHttpBinding binding = new BasicHttpBinding{ MaxReceivedMessageSize = 2147483647, ReaderQuotas = readerQuotas, Security = basicHttpSecurity  };
            binding.AllowCookies = false;

            return binding;
        }

        throw new ArgumentException("bindingType");
    }

    public static EndpointAddress GetEndpoint(BindingType bindingType, string hostName, bool secure)
    {
        string protocol = bindingType == BindingType.netTcp ? "net.tcp://" : "http://";

        if (secure && protocol == "http://")
            protocol = "https://";
   
        string port = bindingType == BindingType.netTcp ? ":2660" : "";
        string path = bindingType != BindingType.netTcp ? "/webservices/CoreService2013.svc" : "/CoreService/2013";
        string url = protocol + hostName + port + path + "/" + bindingType;
        return new EndpointAddress(url);
    }
}

public class CoreServiceInfo
{
    public String HostName { get; set; }
    public bool Secure { get; set; }
    public String UserName { get; set; }
    public String Password { get; set; }
    public TcmCoreService.BindingType BindingType { get; set; }

    // Don't allow creating instances using a constructor
    private CoreServiceInfo()
    {
    }

    /// <summary>
    /// Sample values
    /// "localhost"
    /// ":administrator:tridion@tcmserver"
    /// </summary>
    /// <param name="value">a string describing the hostname and optional username/password</param>
    /// <returns></returns>
    public static CoreServiceInfo ParseTridionHost(string value)
    {
        CoreServiceInfo result = new CoreServiceInfo();

        if (!string.IsNullOrWhiteSpace(value) && value.StartsWith(":"))
        {
            Match match = new Regex(":([^:]*):([^@]*)@(.*)").Match(value);
            result.UserName = match.Groups[1].Value;
            result.Password = match.Groups[2].Value;
            result.HostName = match.Groups[3].Value;
        }
        else
        {
            result.HostName = string.IsNullOrWhiteSpace(value) ? "localhost" : value;
        }
        result.BindingType = result.HostName == "localhost" ? TcmCoreService.BindingType.netTcp : TcmCoreService.BindingType.basicHttp;
        //Console.WriteLine(string.Format("CoreServiceInfo: {0}-{1}-{2}-{3}", result.BindingType, result.HostName, result.UserName, result.Password));
        return result;
    }

    public static CoreServiceInfo For(string hostName, bool secure, string userName = null, string password = null)
    {
        CoreServiceInfo result = new CoreServiceInfo {HostName = hostName, Secure = secure, UserName = userName, Password = password};
        result.BindingType = result.HostName == "localhost" ? TcmCoreService.BindingType.netTcp : TcmCoreService.BindingType.basicHttp;
        return result;
    }
}

/// <summary>
/// This is a simple wrapper around the normal CoreServiceClient that closes the client correctly when it is disposed.
/// </summary>
public class SmarterCoreServiceClient : CoreServiceClient, IDisposable
{
    private bool _isDisposed;

    public SmarterCoreServiceClient() : base()
    {
    }

    public SmarterCoreServiceClient(Binding binding, EndpointAddress endpoint): base(binding, endpoint)
    {
    }

    public void Dispose()
    {
        Dispose(true);
    }

    private void Dispose(bool disposing)
    {
        if (!_isDisposed && disposing)
        {
            if (State == CommunicationState.Faulted)
            {
                Abort();
            }
            else
            {
                Close();
            }
        }
        _isDisposed = true;
    }
}

/// <summary>
/// Extension methods for the CoreServiceClient
/// </summary>
public static class TcmCoreServiceClient
{
    public static ReadOptions DEFAULT_READ_OPTIONS = new ReadOptions();

    public static IEnumerable<XElement> ItemsInFolder(this CoreServiceClient client, string folderId, string schemaId = null)
    {
        var filter = new OrganizationalItemItemsFilterData { ItemTypes = new [] {ItemType.Component} };
        if (schemaId != null) filter.BasedOnSchemas = new[] { new LinkToSchemaData { IdRef = schemaId } };
        var comps = client.GetListXml(folderId, filter);
        return comps.Descendants();
    }

    public static IEnumerable<string> ComponentURIsInFolder(this CoreServiceClient client, string folderId, string schemaId = null)
    {
        var filter = new OrganizationalItemItemsFilterData { ItemTypes = new [] {ItemType.Component} };
        if (schemaId != null) filter.BasedOnSchemas = new[] { new LinkToSchemaData { IdRef = schemaId } };
        var comps = client.GetListXml(folderId, filter);
        return comps.Descendants().Select(itemElement => itemElement.Attribute("ID").Value);
    }

    public static IEnumerable<string> ItemURIsInOrganizationalItem(this CoreServiceClient client, string organizationalItemId)
    {
        var filter = new OrganizationalItemItemsFilterData();
        var comps = client.GetListXml(organizationalItemId, filter);
        return comps.Descendants().Select(itemElement => itemElement.Attribute("ID").Value);
    }

    public static IEnumerable<ComponentData> ComponentsInFolder(this CoreServiceClient client, string folderId, string schemaId = null)
    {
        return client.ComponentURIsInFolder(folderId, schemaId).Select(uri => (ComponentData)client.Read(uri, DEFAULT_READ_OPTIONS));
    }

    public static TcmFields ContentFields(this CoreServiceClient client, ComponentData component)
    {
        return TcmFields.ForContentOf(client.ReadSchemaFields(component.Schema.IdRef, true, TcmCoreService.DEFAULT_READ_OPTIONS), component);
    }

    public static TcmFields MetadataFields(this CoreServiceClient client, ComponentData component)
    {
        return TcmFields.ForMetadataOf(client.ReadSchemaFields(component.Schema.IdRef, true, TcmCoreService.DEFAULT_READ_OPTIONS), component);
    }
}

// ReSharper restore CheckNamespace
