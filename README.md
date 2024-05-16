
# godaddy-anycaplugin

GoDaddy plugin for the AnyCA Gateway framework

#### Integration status: Prototype - Demonstration quality. Not for use in customer environments.

## About the Keyfactor AnyCA Gateway DCOM Connector

This repository contains an AnyCA Gateway Connector, which is a plugin to the Keyfactor AnyGateway. AnyCA Gateway Connectors allow Keyfactor Command to be used for inventory, issuance, and revocation of certificates from a third-party certificate authority.

## Support for godaddy-anycaplugin

godaddy-anycaplugin is open source and community supported, meaning that there is no support guaranteed from Keyfactor Support for these tools.

###### To report a problem or suggest a new feature, use the **[Issues](../../issues)** tab. If you want to contribute actual bug fixes or proposed enhancements, use the **[Pull requests](../../pulls)** tab.

---


---





## Keyfactor AnyCA Gateway Framework Supported
The Keyfactor gateway framework implements common logic shared across various gateway implementations and handles communication with Keyfactor Command. The gateway framework hosts gateway implementations or plugins that understand how to communicate with specific CAs. This allows you to integrate your third-party CAs with Keyfactor Command such that they behave in a manner similar to the CAs natively supported by Keyfactor Command.




This gateway extension was compiled against version 1.0.0 of the AnyCA Gateway DCOM Framework.  You will need at least this version of the framework Installed. If you have a later AnyGateway Framework Installed you will probably need to add binding redirects in the CAProxyServer.exe.config file to make things work properly.


[Keyfactor CAGateway Install Guide](https://software.keyfactor.com/Guides/AnyGateway_Generic/Content/AnyGateway/Introduction.htm)



---





