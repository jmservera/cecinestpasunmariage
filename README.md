# cecinestpasunmariage

Remeber to edit the [.env file](www/.env)

## Infra Deployment

Use the [main.bicep](infra/main.bicep) file to deploy the infra.

You need to have some resources in place before the deployment:

* A DNS Zone, that you will configure in the `dnszones_name` parameter
* A Log Analytics workspace, that you will configure in the `workspace_name` parameter
* Vision, Computer Vision and Azure OpenAI resources, that you will configure in the corresponding parameters

All the rest of resources will be created by the deployment.

// TODO:
//      * test that the function works in the prod environment (change of the storage account env variable)
//      * Add RG variable for the existing resources (DNS Zone, Log Analytics, etc)
//      * Create the script to validate domains and deploy swa and function app