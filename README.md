# TcmLINQPadDriver
Tridion Core Service driver for LINQPad

This TcmLINQPadDriver extends on the existing concept posted by Frank van Puffelen on the SDL Tridion community website:
https://community.sdl.com/product-groups/sdl-tridion-dx/tridion-sites/tridion-developer/b/weblog/posts/using-linqpad-with-sdl-tridion

## Use-case scenario

With the move to cloud-based services, organisations tend to have their Tridion CME instances hosted in the cloud.
Often this makes use of SSO (Single Sign-On) solutions such as Akamai Enterprise Application Access.

When MFA (Multi-factor authentication) is enabled, this prohibts the CoreService WCF client from authenticating to the CoreService over HTTP(S).

## Functionality Changes

This repository contains an enhanced Tridion Core Service driver which supports MFA authentication scenarios.
It does this by implementing a browser window during connection, allowing the user to authenticate through the authentication portal.

Once the authorization is complete, the browsers cookies for the destination domain (Tridion CME instance) are exported and injected on WCF client requests.

Through this approach the WCF client requests are leveraging the browser authenticated session (as well as the user agent string)

## Minor Updates

This version of TcmLINQPadDriver has been updated against the Tridion 2013 CoreService client to allow some newly introduced functionalities on the SDL CoreService

## Installation

The following setup is expected (and tested):

    (client) --- HTTPS ---> (authentication gateway) --- HTTP ---> (Tridion CME instance)

Due to the SSL off-loading happing before the request reaches the Tridion CME instance a number of configuration changes are needed in `/webservices/Web.config`.

*Note: These steps only modify the `basicHTTPBinding`, other bindings such as `netTCP` can still be used directly within the cloud infrastructure.*

- Ensure SSO is enabled as per Tridion documentation
- In the case of Tridion 2013, do not enable the `SsoAgentHttpModule`, as it is already added from the main Web.config
- `CoreService_basicHttpBinding` configuration for `Security/@Mode` should be set as `None`
- The Tridion CME instance should be listening on SSL, a self-signed certificate is sufficient. This allows WCF to listen for `https://` URIs

## Notes & Problems

- If login in the Multi-factor authentication Window does not succeed, execute a login through the system installed Internet Explorer first.
- The TcmLINQPadDriver is currently configured to accept untrusted / self-signed SSL certificates for secure connections