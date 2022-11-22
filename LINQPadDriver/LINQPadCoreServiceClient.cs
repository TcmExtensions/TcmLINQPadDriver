using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Xml.Linq;
using MFA;
using Tridion.ContentManager.CoreService.Client;

namespace TcmLINQPadDriver
{
    /// <summary>
    /// A wrapper class around the Core Service Client that exposes some extra properties that make the class more
    /// visual for working with it in LINQPad.
    /// </summary>
    public class LINQPadCoreServiceClient: SmarterCoreServiceClient
    {
        private static MFAData mfaData = null;

        public string ContextId { get; private set; }

        public LINQPadCoreServiceClient(string hostname, bool secure, bool MFA, string username, string password, string context = null)
            : base(TcmCoreService.GetBinding(GetBindingTypeFor(hostname), secure), TcmCoreService.GetEndpoint(GetBindingTypeFor(hostname), GetHostnameFor(hostname), secure))
        {
            NetworkCredential credentials = CredentialCache.DefaultNetworkCredentials;
            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                credentials = new NetworkCredential(username, password);

            ChannelFactory.Credentials.Windows.ClientCredential = credentials;

            if (context != null) ContextId = context;

            if (secure) {
                ServicePointManager.ServerCertificateValidationCallback =
                    delegate (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return true; };

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            }

            if (MFA) {
                if (mfaData == null)
                    mfaData = new MFAData(new Uri("https://" + hostname));

                ChannelFactory.Endpoint.Contract.ContractBehaviors.Add(mfaData.GetContractBehavior());
            }
        }

        private static TcmCoreService.BindingType GetBindingTypeFor(string hostname)
        {
            return hostname == "localhost" || hostname.StartsWith("net.tcp://") ? TcmCoreService.BindingType.netTcp : TcmCoreService.BindingType.basicHttp;
        }

        private static string GetHostnameFor(string hostname)
        {
            if (hostname.StartsWith("net.tcp://")) hostname = hostname.Substring("net.tcp://".Length);
            if (hostname.StartsWith("http://")) hostname = hostname.Substring("http://".Length);
            if (hostname.StartsWith("https://")) hostname = hostname.Substring("https://".Length);

            return hostname;
        }

        // Expose this client through a few aliases, but it will also be available to queries as `this`
        public LINQPadCoreServiceClient Tcm { get { return this; } }
        public LINQPadCoreServiceClient Tridion { get { return this; } }
        public LINQPadCoreServiceClient Client { get { return this; } }

        // Expose some system wide lists as properties, so that they can be used as collections (instead of function calls)
        public IEnumerable<PublicationData> Publications
        {
            get
            {
                return GetSystemWideList(new PublicationsFilterData()).Cast<PublicationData>();
            }
        }

        public IEnumerable<UserData> Users
        {
            get
            {
                return GetSystemWideList(new UsersFilterData()).Cast<UserData>();
            }
        }

        public IEnumerable<XElement> Groups
        {
            get
            {
                return GetSystemWideListXml(new GroupsFilterData()).Elements();
            }
        }

        public IEnumerable<PublishTransactionData> PublishTransactions
        {
            get { return GetSystemWideList(new PublishTransactionsFilterData()).Cast<PublishTransactionData>(); }
        }

        public IEnumerable<XElement> ItemElements
        {
            get
            {
                if (string.IsNullOrEmpty(ContextId)) throw new Exception("Can only get Items when a context is specified for the connection");
                //return GetListXml(ContextId, new OrganizationalItemItemsFilterData()).Elements();
                //var filter = GetFilterFor(ContextId);
                //if (GetItemType(ContextId) == ItemType.Publication) filter.Recursive = true;
                //return GetItemElements(ContextId)
                return GetItemElements(ContextId);
            }
        }

        public IEnumerable<IdentifiableObjectData> Items
        {
            get { return GetItems(ContextId); }
        }

        public IEnumerable<ComponentData> Components
        {
            get { return GetItems(ContextId, ItemType.Component).Cast<ComponentData>(); }
        }

        public IEnumerable<PageData> Pages
        {
            get { return GetItems(ContextId, ItemType.Page).Cast<PageData>(); }
        }

        public IEnumerable<CategoryData> Categories
        {
            get { return GetItems(ContextId, ItemType.Category).Cast<CategoryData>(); }
        }

        public IEnumerable<XElement> GetItemElements(string context, ItemType type = ItemType.None)
        {
            var filter = GetFilterFor(context);
            if (type != ItemType.None) filter.ItemTypes = new[] { type };
            if (GetItemType(context) == ItemType.Publication) filter.Recursive = true;
            var xml = GetListXml(context, filter);
            return xml.Descendants();
        }

        public IEnumerable<string> GetItemUris(string context, ItemType type = ItemType.None)
        {
            return GetItemElements(context, type).Select(element => element.Attribute("ID").Value);
        }

        public IEnumerable<IdentifiableObjectData> GetItems(string context, ItemType type = ItemType.None)
        {
            return GetItemUris(context, type).Select(uri => this.Read(uri, TcmCoreService.DEFAULT_READ_OPTIONS));
        }

        public static ItemsFilterData GetFilterFor(string context)
        {
            return GetItemType(context) == ItemType.Publication 
                ? (ItemsFilterData)new RepositoryItemsFilterData() 
                : new OrganizationalItemItemsFilterData();
        }

        public static ItemType GetItemType(string context)
        {
            var split = context.Substring(context.IndexOf(':') + 1).Split('-');
            if (split.Length < 3) return ItemType.Component;
            return (ItemType)int.Parse(split[2]);
        }
    }
}
