- 1.0.0
    - First production release of the GoDaddy AnyCA Gateway REST plugin that implements:
        - CA Sync
            - Download all issued certificates
        - Certificate enrollment for all published GoDaddy Certificate SKUs
            - Support certificate enrollment (new keys/certificate)
            - Support certificate renewal (extend the life of a previously issued certificate with the same or different domain names)
            - Support certificate re-issuance (new public/private keys with the same or different domain names)
        - Certificate revocation
            - Request revocation of a previously issued certificate

- 1.1.0
  - chore(docs): Upgrade GitHub Actions to use Bootstrap Workflow v3 to support Doctool

