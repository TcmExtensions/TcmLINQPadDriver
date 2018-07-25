using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace TcmLINQPadDriver.MFA
{
    public class CookieManagerMessageInspector : IClientMessageInspector, IContractBehavior
    {
        private string userAgent;
        private string cookieData;
        
        public CookieManagerMessageInspector(String userAgent, String cookieData)
        {
            this.userAgent = userAgent;
            this.cookieData = cookieData;
        }

        public void AddBindingParameters(ContractDescription contractDescription, ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(ContractDescription contractDescription, ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            // Do not add a message inspector if it already exists
            foreach (IClientMessageInspector messageInspector in clientRuntime.MessageInspectors) {
                if (messageInspector is CookieManagerMessageInspector) {
                    return;
                }
            }

            clientRuntime.MessageInspectors.Add(new CookieManagerMessageInspector(this.userAgent, this.cookieData));
        }

        public void ApplyDispatchBehavior(ContractDescription contractDescription, ServiceEndpoint endpoint, DispatchRuntime dispatchRuntime)
        {
        }

        public void Validate(ContractDescription contractDescription, ServiceEndpoint endpoint)
        {
        }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            HttpRequestMessageProperty httpRequest;

            // The HTTP request object is made available in the outgoing message only
            // when the Visual Studio Debugger is attached to the running process
            if (!request.Properties.ContainsKey(HttpRequestMessageProperty.Name)) {
                request.Properties.Add(HttpRequestMessageProperty.Name, new HttpRequestMessageProperty());
            }

            httpRequest = (HttpRequestMessageProperty)request.Properties[HttpRequestMessageProperty.Name];

            httpRequest.Headers[HttpRequestHeader.UserAgent] = this.userAgent;
            httpRequest.Headers[HttpRequestHeader.Cookie] = this.cookieData;

            return null;
        }

        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
            HttpResponseMessageProperty httpResponse =
                reply.Properties[HttpResponseMessageProperty.Name]
                as HttpResponseMessageProperty;

            if (httpResponse != null) {
                /*
                string cookie = httpResponse.Headers[HttpResponseHeader.SetCookie];

                if (!string.IsNullOrEmpty(cookie)) {
                    this.sharedCookie = cookie;
                }
                */
            }
        }
    }
}
