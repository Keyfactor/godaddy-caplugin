<h1 align="center" style="border-bottom: none">
    GoDaddy AnyCA Gateway Plugin
</h1>

<p align="center">
  <!-- Badges -->
<img src="https://img.shields.io/badge/integration_status-prototype-3D1973?style=flat-square" alt="Integration Status: prototype" />
<a href="https://github.com/Keyfactor/godaddy-anycagateway/releases"><img src="https://img.shields.io/github/v/release/Keyfactor/godaddy-anycagateway?style=flat-square" alt="Release" /></a>
<img src="https://img.shields.io/github/issues/Keyfactor/godaddy-anycagateway?style=flat-square" alt="Issues" />
<img src="https://img.shields.io/github/downloads/Keyfactor/godaddy-anycagateway/total?style=flat-square&label=downloads&color=28B905" alt="GitHub Downloads (all assets, all releases)" />
</p>

<p align="center">
  <!-- TOC -->
  <a href="#support">
    <b>Support</b>
  </a> 
  ·
  <a href="#installation">
    <b>Installation</b>
  </a>
  ·
  <a href="#license">
    <b>License</b>
  </a>
  ·
  <a href="https://github.com/orgs/Keyfactor/repositories?q=anycagateway">
    <b>Related Integrations</b>
  </a>
</p>


The GoDaddy AnyCA REST plugin extends the capabilities of the [GoDaddy Certificate Authority (CA)](https://www.godaddy.com/web-security/ssl-certificate) to Keyfactor Command via the Keyfactor . The plugin represents a fully featured AnyCA REST Plugin with the following capabilies:
* CA Sync:
    * Download all certificates issued to the customer by the GoDaddy CA.
* Certificate enrollment for all published GoDaddy Certificate SKUs:
    * Support certificate enrollment (new keys/certificate).
    * Support certificate renewal (extend the life of a previously issued certificate with the same or different domain names).
    * Support certificate re-issuance (new public/private keys with the same or different domain names).
* Certificate revocation:
    * Request revocation of a previously issued certificate.



## Compatibility

The GoDaddy AnyCA Gateway plugin is compatible with the Keyfactor AnyCA Gateway REST 24.2 and later.

## Support
The GoDaddy AnyCA Gateway plugin is open source and community supported, meaning that there is **no SLA** applicable. 

> To report a problem or suggest a new feature, use the **[Issues](../../issues)** tab. If you want to contribute actual bug fixes or proposed enhancements, use the **[Pull requests](../../pulls)** tab.

## Requirements

1. **GoDaddy Account**
   
    To use the GoDaddy AnyCA REST plugin, a production GoDaddy account must be created and configured fully. To create a new account, follow [GoDaddy's official documentation](https://www.godaddy.com/help/create-a-godaddy-account-16618). Make sure that your [account Profile is configured fully](https://www.godaddy.com/help/update-my-godaddy-account-profile-27250) with at least the following fields:
    * Full Name
    * Address
    * Organization
    * Email
    * Primary Phone

    Your GoDaddy account must also have at least one payment method. Follow [GoDaddy's official documentation](https://www.godaddy.com/help/add-a-payment-method-to-my-godaddy-account-20037) to add a payment method.

2. **GoDaddy Certificate**

    The GoDaddy AnyCA REST plugin does not purchase certificates from GoDaddy on its own. To enroll a certificate using the plugin, you must first [purchase a certificate from GoDaddy](https://www.godaddy.com/web-security/ssl-certificate). Once purchased, the AnyCA REST plugin enables enrollment, [renewal](https://www.godaddy.com/help/renewing-my-ssl-certificate-864), and [rekeying (re-issuing)](https://www.godaddy.com/help/ssl-certificates-1000006) your purchased certificate.

3. **GoDaddy API Key**

    The GoDaddy AnyCA REST plugin uses the [GoDaddy API](https://developer.godaddy.com/doc/endpoint/certificates) to perform all certificate operations. GoDaddy offers an environment for testing (OTE) and an environment for production use (Production). To configure the plugin, follow the [official GoDaddy documentation](https://developer.godaddy.com/getstarted) to create a [production API key](https://developer.godaddy.com/keys). To configure the , you'll need the following parameters handy:

    * API URL (https://api.godaddy.com or https://api.ote-godaddy.com)
    * API Key
    * API Secret

4. **GoDaddy Shopper ID**

    To synchronize certificates issued by the GoDaddy CA, the GoDaddy AnyCA REST plugin needs to know your Shopper ID (shown as Customer # on the GoDaddy website). The Shopper ID is a number with a max length of 10 (e.g., 1234567890). To find your Shopper ID, sign into [GoDaddy](https://www.godaddy.com/) and click on your name dropdown on the top right. The Shopper ID is shown as **Customer #** in this dropdown.



## Installation

1. Install the AnyCA Gateway REST per the [official Keyfactor documentation](https://software.keyfactor.com/Guides/AnyCAGatewayREST/Content/AnyCAGatewayREST/InstallIntroduction.htm).

2. On the server hosting the AnyCA Gateway REST, download and unzip the latest [GoDaddy AnyCA Gateway REST plugin](https://github.com/Keyfactor/godaddy-anycagateway/releases/latest) from GitHub.

3. Copy the unzipped directory (usually called `net6.0`) to the Extensions directory:

    ```shell
    Program Files\Keyfactor\AnyCA Gateway\AnyGatewayREST\net6.0\Extensions
    ```

    > The directory containing the GoDaddy AnyCA Gateway REST plugin DLLs (`net6.0`) can be named anything, as long as it is unique within the `Extensions` directory.

4. Restart the AnyCA Gateway REST service.

5. Navigate to the AnyCA Gateway REST portal and verify that the Gateway recognizes the GoDaddy plugin by hovering over the ⓘ symbol to the right of the Gateway on the top left of the portal.

## Configuration

1. Follow the [official AnyCA Gateway REST documentation](https://software.keyfactor.com/Guides/AnyCAGatewayREST/Content/AnyCAGatewayREST/AddCA-Gateway.htm) to define a new Certificate Authority, and use the notes below to configure the **Gateway Registration** and **CA Connection** tabs:

    * **Gateway Registration**


        GoDaddy has four available Certificate Authorities:

        - GoDaddy SHA-1 (GODADDY_SHA_1)
          - [Root Certificate](https://certs.godaddy.com/repository/gd-class2-root.crt) 
          - [Intermediate Certificate](https://certs.godaddy.com/repository/gd_intermediate.crt.pem)
        - GoDaddy SHA256 (GODADDY_SHA_2)
          - [Root Certificate](https://certs.godaddy.com/repository/gdroot-g2.crt) 
          - [Intermediate Certificate](https://certs.godaddy.com/repository/gdig2.crt.pem)
        - Starfield SHA-1 (STARFIELD_SHA_1)
          - [Root Certificate](https://certs.godaddy.com/repository/sf-class2-root.crt) 
          - [Intermediate Certificate](https://certs.godaddy.com/repository/sf_intermediate.crt.pem)
        - Starfield SHA256 (STARFIELD_SHA_2)
          - [Root Certificate](https://certs.godaddy.com/repository/sfroot-g2.crt) 
          - [Intermediate Certificate](https://certs.godaddy.com/repository/sfig2.crt.pem)

        Each defined Certificate Authority in the AnyCA REST can support one certificate authority. Since GoDaddy has four available Certificate Authorities, if you require certificate enrollment from multiple GoDaddy Certificate Authorities, you must define multiple Certificate Authorities in the AnyCA Gateway REST. This will manifest in Command as one GoDaddy CA per defined Certificate Authority.



    * **CA Connection**

        Populate using the configuration fields collected in the [requirements](#requirements) section.



        * **ApiKey** - The API Key for the GoDaddy API 
        * **ApiSecret** - The API Secret for the GoDaddy API 
        * **BaseUrl** - The Base URL for the GoDaddy API - Usually either https://api.godaddy.com or https://api.ote-godaddy.com 
        * **ShopperId** - The Shopper ID of the GoDaddy account to use for the API calls (ex: 1234567890) - has a max length of 10 digits 
        * **Enabled** - Flag to Enable or Disable gateway functionality. Disabling is primarily used to allow creation of the CA prior to configuration information being available. 

2. Define [Certificate Profiles](https://software.keyfactor.com/Guides/AnyCAGatewayREST/Content/AnyCAGatewayREST/AddCP-Gateway.htm) and [Certificate Templates](https://software.keyfactor.com/Guides/AnyCAGatewayREST/Content/AnyCAGatewayREST/AddCA-Gateway.htm) for the Certificate Authority as required. One Certificate Profile must be defined per Certificate Template. It's recommended that each Certificate Profile be named after the Product ID. The GoDaddy plugin supports the following product IDs:



    * **DV_SSL**
    * **DV_WILDCARD_SSL**
    * **EV_SSL**
    * **OV_CS**
    * **OV_DS**
    * **OV_SSL**
    * **OV_WILDCARD_SSL**
    * **UCC_DV_SSL**
    * **UCC_EV_SSL**
    * **UCC_OV_SSL**

3. Follow the [official Keyfactor documentation](https://software.keyfactor.com/Guides/AnyCAGatewayREST/Content/AnyCAGatewayREST/AddCA-Keyfactor.htm) to add each defined Certificate Authority to Keyfactor Command and import the newly defined Certificate Templates.

4. In Keyfactor Command, for each imported Certificate Template, follow the [official documentation](https://software.keyfactor.com/Core-OnPrem/Current/Content/ReferenceGuide/Configuring%20Template%20Options.htm) to define enrollment fields for each of the following parameters:



    * **JobTitle** - The job title of the certificate requestor 
    * **CertificateValidityInYears** - Number of years the certificate will be valid for 
    * **LastName** - Last name of the certificate requestor 
    * **FirstName** - First name of the certificate requestor 
    * **Email** - Email address of the requestor 
    * **Phone** - Phone number of the requestor 
    * **SlotSize** - Maximum number of SANs that a certificate may have - valid values are [FIVE, TEN, FIFTEEN, TWENTY, THIRTY, FOURTY, FIFTY, ONE_HUNDRED] 
    * **OrganizationName** - Name of the organization to be validated against 
    * **OrganizationAddress** - Address of the organization to be validated against 
    * **OrganizationCity** - City of the organization to be validated against 
    * **OrganizationState** - Full state name of the organization to be validated against 
    * **OrganizationCountry** - 2 character abbreviation of the country of the organization to be validated against 
    * **OrganizationPhone** - Phone number of the organization to be validated against 
    * **RegistrationAgent** - Registration agent name assigned to the organization when its documents were filed for registration 
    * **RegistrationNumber** - Registration number assigned to the organization when its documents were filed for registration 
    * **RootCAType** - The certificate's root CA - Depending on certificate expiration date, SHA_1 not be allowed. Will default to SHA_2 if expiration date exceeds sha1 allowed date. Options are GODADDY_SHA_1, GODADDY_SHA_2, STARFIELD_SHA_1, or STARFIELD_SHA_2. 

## License

Apache License 2.0, see [LICENSE](LICENSE).

## Related Integrations

See all [Keyfactor Any CA Gateways (REST)](https://github.com/orgs/Keyfactor/repositories?q=anycagateway).