## Overview

The GoDaddy AnyCA Gateway REST plugin extends the capabilities of the [GoDaddy Certificate Authority (CA)](https://www.godaddy.com/web-security/ssl-certificate) to Keyfactor Command via the Keyfactor . The plugin represents a fully featured AnyCA REST Plugin with the following capabilies:
* CA Sync:
    * Download all certificates issued to the customer by the GoDaddy CA.
* Certificate enrollment for all published GoDaddy Certificate SKUs:
    * Support certificate enrollment (new keys/certificate). [see disclaimer]
    * Support certificate renewal (extend the life of a previously issued certificate with the same or different domain names).
    * Support certificate re-issuance (new public/private keys with the same or different domain names).
* Certificate revocation:
    * Request revocation of a previously issued certificate.

> **🚧 Disclaimer** 
>
> Prior to Keyfactor Command v12.3, the GoDaddy AnyCA Gateway REST plugin has limited Certificate Enrollment functionality.
>
> <details><summary>Notes</summary>
> The GoDaddy AnyCA Gateway REST plugin requires several custom enrollment parameters that are passed to GoDaddy upon the submission of a new PFX/CSR enrollment request. These custom enrollment parameters configure the domain/organization/extended validation procedure required to complete the certificate enrollment.
>
> Prior to Command v12.3, custom enrollment parameters are not supported on a per-request basis for PFX/CSR Enrollment. If your Keyfactor Command version is less than v12.3, the only way to configure custom enrollment parameters is to set default parameter values on the Certificate Template in the Keyfactor AnyCA Gateway REST. 
>
> Before continuing with installation prior to Command 12.3, users should consider the following:
>
> * Each combination of custom enrollment parameters will require the creation of a new Certificate Template and Certificate Profile in the Keyfactor AnyCA Gateway REST. 
> * If you have multiple combinations of custom enrollment parameters, consider the operational complexity of managing multiple Certificate Templates and Certificate Profiles.
> * If your certificate workflows mostly consist of certificate renewal, re-issuance, and revocation, the GoDaddy AnyCA Gateway REST plugin is fully supported.
> </details>

## Requirements

1. **GoDaddy Account**
   
    To use the GoDaddy AnyCA Gateway REST plugin, a production GoDaddy account must be created and configured fully. To create a new account, follow [GoDaddy's official documentation](https://www.godaddy.com/help/create-a-godaddy-account-16618). Make sure that your [account Profile is configured fully](https://www.godaddy.com/help/update-my-godaddy-account-profile-27250) with at least the following fields:
    * Full Name
    * Address
    * Organization
    * Email
    * Primary Phone

    Your GoDaddy account must also have at least one payment method. Follow [GoDaddy's official documentation](https://www.godaddy.com/help/add-a-payment-method-to-my-godaddy-account-20037) to add a payment method.

2. **GoDaddy Certificate**

    The GoDaddy AnyCA Gateway REST plugin does not purchase certificates from GoDaddy on its own. To enroll a certificate using the plugin, you must first [purchase a certificate from GoDaddy](https://www.godaddy.com/web-security/ssl-certificate). Once purchased, the AnyCA Gateway REST plugin enables enrollment, [renewal](https://www.godaddy.com/help/renewing-my-ssl-certificate-864), and [rekeying (re-issuing)](https://www.godaddy.com/help/ssl-certificates-1000006) your purchased certificate.

3. **GoDaddy API Key**

    The GoDaddy AnyCA Gateway REST plugin uses the [GoDaddy API](https://developer.godaddy.com/doc/endpoint/certificates) to perform all certificate operations. GoDaddy offers an environment for testing (OTE) and an environment for production use (Production). To configure the plugin, follow the [official GoDaddy documentation](https://developer.godaddy.com/getstarted) to create a [production API key](https://developer.godaddy.com/keys). To configure the CA, you'll need the following parameters handy:

    * API URL (https://api.godaddy.com or https://api.ote-godaddy.com)
    * API Key
    * API Secret

4. **GoDaddy Shopper ID**

    To synchronize certificates issued by the GoDaddy CA, the GoDaddy AnyCA Gateway REST plugin needs to know your Shopper ID (shown as Customer # on the GoDaddy website). The Shopper ID is a number with a max length of 10 (e.g., 1234567890). To find your Shopper ID, sign into [GoDaddy](https://www.godaddy.com/) and click on your name dropdown on the top right. The Shopper ID is shown as **Customer #** in this dropdown.

## Gateway Registration

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

Each defined Certificate Authority in the AnyCA Gateway REST can support one issuing certificate authority. Since GoDaddy has four available Certificate Authorities, if you require certificate enrollment from multiple GoDaddy Certificate Authorities, you must define multiple Certificate Authorities in the AnyCA Gateway REST. This will manifest in Command as one GoDaddy CA per defined Certificate Authority.

## Certificate Template Creation Step

alksdfjalksdjflkasdj

## Mechanics

asdlkfj
