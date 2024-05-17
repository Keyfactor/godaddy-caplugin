<h1 align="center" style="border-bottom: none">
    GoDaddy
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
  <a href="#license">
    <b>License</b>
  </a>
  ·
  <a href="https://github.com/orgs/Keyfactor/repositories?q=anycagateway">
    <b>Related Integrations</b>
  </a>
</p>


The GoDaddy AnyCA REST plugin extends the capabilities of the [GoDaddy Certificate Authority (CA)](https://www.godaddy.com/web-security/ssl-certificate) to Keyfactor Command via the Keyfactor AnyCA REST Gateway. The plugin represents a fully featured AnyCA REST Plugin with the following capabilies:
* CA Sync:
    * Download all certificates issued to the customer by the GoDaddy CA.
* Certificate enrollment for all published GoDaddy Certificate SKUs:
    * Support certificate enrollment (new keys/certificate).
    * Support certificate renewal (extend the life of a previously issued certificate with the same or different domain names).
    * Support certificate re-issuance (new public/private keys with the same or different domain names).
* Certificate revocation:
    * Request revocation of a previously issued certificate.



## Compatibility

The GoDaddy AnyCA REST plugin is compatible with the Keyfactor AnyCA REST Gateway 24.2 and later.

## Support
The GoDaddy AnyCA REST plugin is open source and community supported, meaning that there is **no SLA** applicable. 

> To report a problem or suggest a new feature, use the **[Issues](../../issues)** tab. If you want to contribute actual bug fixes or proposed enhancements, use the **[Pull requests](../../pulls)** tab.

## Requirements
1. **GoDaddy Account**
   
    To use the GoDaddy AnyCA REST plugin, a production GoDaddy account must be created and fully configured. To create a new account, follow [GoDaddy's official documentation](https://www.godaddy.com/help/create-a-godaddy-account-16618). Make sure that your [account Profile is fully configured](https://www.godaddy.com/help/update-my-godaddy-account-profile-27250) with at least the following fields:
    * Full Name
    * Address
    * Organization
    * Email
    * Primary Phone

    Your GoDaddy account must also have at least one payment method. Follow [GoDaddy's official documentation](https://www.godaddy.com/help/add-a-payment-method-to-my-godaddy-account-20037) to add a payment method.

2. **GoDaddy Certificate**

    The GoDaddy AnyCA REST plugin does not purchase certificates from GoDaddy on its own. To enroll a certificate using the plugin, you must first [purchase a certificate from GoDaddy](https://www.godaddy.com/web-security/ssl-certificate). Once purchased, the AnyCA REST plugin enables enrollment, [renewal](https://www.godaddy.com/help/renewing-my-ssl-certificate-864), and [rekeying (re-issuing)](https://www.godaddy.com/help/ssl-certificates-1000006) your purchased certificate.

3. **GoDaddy API Key**

    The GoDaddy AnyCA REST plugin uses the [GoDaddy API](https://developer.godaddy.com/doc/endpoint/certificates) to perform all certificate operations. GoDaddy offers an environment for testing (OTE) and an environment for production use (Production). To configure the plugin, follow the [official GoDaddy documentation](https://developer.godaddy.com/getstarted) to create a [production API key](https://developer.godaddy.com/keys). To configure the AnyCA REST Gateway, you'll need the following parameters handy:

    * API URL (https://api.godaddy.com or https://api.ote-godaddy.com)
    * API Key
    * API Secret

4. **GoDaddy Shopper ID**

    To synchronize certificates issued by the GoDaddy CA, the GoDaddy AnyCA REST plugin needs to know your Shopper ID (shown as Customer # on the GoDaddy website). The Shopper ID is a number with a max length of 10 (e.g., 1234567890). To find your Shopper ID, sign into [GoDaddy](https://www.godaddy.com/) and click on your name dropdown on the top right. The Shopper ID is shown as **Customer #** in this dropdown.



## Installation



## License

Apache License 2.0, see [LICENSE](LICENSE).

## Related Integrations

See all [Keyfactor Any CA Gateways (REST)](https://github.com/orgs/Keyfactor/repositories?q=anycagateway).