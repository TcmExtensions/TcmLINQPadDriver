using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    public static CoreServiceClient GetClient(BindingType type)
    {
        var host = Environment.GetEnvironmentVariable("TRIDION_HOST") ?? "localhost";
        return new SmarterCoreServiceClient(GetBinding(type), GetEndpoint(type, host));
    }
    /// <summary>
    /// Returns a new client to the Core Service on the given host
    /// </summary>
    /// <returns></returns>
    public static CoreServiceClient GetClient(string hostName)
    {
        hostName = hostName ?? "localhost";
        var bindingType = hostName == "localhost" ? BindingType.netTcp : BindingType.basicHttp;
        // client.ChannelFactory.Credentials.Windows.ClientCredential = CredentialCache.DefaultNetworkCredentials;
        // client.ChannelFactory.Credentials.Windows.ClientCredential = new System.Net.NetworkCredential(username, password);
        // client.Impersonate(userName)
        return new SmarterCoreServiceClient(GetBinding(bindingType), GetEndpoint(bindingType, hostName));
    }

    public static CoreServiceClient GetClient(CoreServiceInfo info)
    {
        var client = new SmarterCoreServiceClient(GetBinding(info.BindingType), GetEndpoint(info.BindingType, info.HostName));
        var credentials = CredentialCache.DefaultNetworkCredentials;
        if (!string.IsNullOrWhiteSpace(info.UserName) && !string.IsNullOrWhiteSpace(info.Password))
        {
            credentials = new NetworkCredential(info.UserName, info.Password);
        }
        client.ChannelFactory.Credentials.Windows.ClientCredential = credentials;
        return client;
    }

    public static Binding GetBinding(BindingType type)
    {
        var readerQuotas = new XmlDictionaryReaderQuotas { MaxStringContentLength = 2147483647, MaxArrayLength = 2147483647 };
        var basicHttpSecurity = new BasicHttpSecurity { Mode = BasicHttpSecurityMode.TransportCredentialOnly, Transport = new HttpTransportSecurity { ClientCredentialType = HttpClientCredentialType.Windows } };

        if (type == BindingType.netTcp)
        {
            return new NetTcpBinding { MaxReceivedMessageSize = 2147483647, ReaderQuotas = readerQuotas };
        }
        if (type == BindingType.wsHttp)
        {
            return new WSHttpBinding { MaxReceivedMessageSize = 2147483647, ReaderQuotas = readerQuotas };
        }
        if (type == BindingType.basicHttp)
        {
            return new BasicHttpBinding { MaxReceivedMessageSize = 2147483647, ReaderQuotas = readerQuotas, Security = basicHttpSecurity };
        }
        throw new ArgumentException("bindingType");
    }
    public static EndpointAddress GetEndpoint(BindingType bindingType, string hostName)
    {
        string protocol = bindingType == BindingType.netTcp ? "net.tcp://" : "http://";
        string port = bindingType == BindingType.netTcp ? ":2660" : "";
        string path = bindingType != BindingType.netTcp ? "/webservices/CoreService2011.svc" : "/CoreService/2011";
        string url = protocol + hostName + port + path + "/" + bindingType;
        return new EndpointAddress(url);
    }
}

public class CoreServiceInfo
{
    public String HostName { get; set; }
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
    public static CoreServiceInfo For(string hostName, string userName = null, string password = null)
    {
        CoreServiceInfo result = new CoreServiceInfo { HostName = hostName, UserName = userName, Password = password };
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

    public SmarterCoreServiceClient()
        : base()
    {
    }
    public SmarterCoreServiceClient(Binding binding, EndpointAddress endpoint)
        : base(binding, endpoint)
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
        var filter = new OrganizationalItemItemsFilterData();
        filter.ItemTypes = new ItemType[] { ItemType.Component };
        if (schemaId != null) filter.BasedOnSchemas = new[] { new LinkToSchemaData { IdRef = schemaId } };
        var comps = client.GetListXml(folderId, filter);
        return comps.Descendants();
    }
    public static IEnumerable<string> ComponentURIsInFolder(this CoreServiceClient client, string folderId, string schemaId = null)
    {
        var filter = new OrganizationalItemItemsFilterData();
        filter.ItemTypes = new ItemType[] { ItemType.Component };
        if (schemaId != null) filter.BasedOnSchemas = new[] { new LinkToSchemaData { IdRef = schemaId } };
        var comps = client.GetListXml(folderId, filter);
        return comps.Descendants().Select(itemElement => itemElement.Attribute("ID").Value);
    }
    public static IEnumerable<ComponentData> ComponentsInFolder(this CoreServiceClient client, string folderId, string schemaId = null)
    {
        return client.ComponentURIsInFolder(folderId, schemaId).Select(uri => (ComponentData)client.Read(uri, DEFAULT_READ_OPTIONS));
    }
    public static ComponentData ReadComponent(this CoreServiceClient client, string componentId, ReadOptions readOptions = null)
    {
        return (ComponentData) client.Read(componentId, readOptions ?? TcmCoreServiceClient.DEFAULT_READ_OPTIONS);
    }

}

// ReSharper restore CheckNamespace
