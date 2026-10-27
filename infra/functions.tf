# Dedicated consumer group so the Function doesn't share $Default with anything else
resource "azurerm_iothub_consumer_group" "functions" {
  name                   = "functions"
  iothub_name            = azurerm_iothub.main.name
  eventhub_endpoint_name = "events"
  resource_group_name    = azurerm_resource_group.main.name
}

locals {
  # Least-privilege "service" policy, NOT iothubowner (last lesson)
  service_policy = one([
    for p in azurerm_iothub.main.shared_access_policy : p if p.key_name == "service"
  ])

  iothub_events_connection = "Endpoint=${azurerm_iothub.main.event_hub_events_endpoint};SharedAccessKeyName=${local.service_policy.key_name};SharedAccessKey=${local.service_policy.primary_key};EntityPath=${azurerm_iothub.main.event_hub_events_path}"
}

# Accepted risk: the Consumption plan (Y1) can't run with a network-restricted
# storage account (Microsoft docs). Will revisit when moving to Flex Consumption or Premium.
#trivy:ignore:AZU-0012:exp:2027-01-31
resource "azurerm_storage_account" "func" {
  name                     = "stfn${var.project_name}${random_string.suffix.result}"
  resource_group_name      = azurerm_resource_group.main.name
  location                 = azurerm_resource_group.main.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

resource "azurerm_service_plan" "func" {
  name                = "asp-${var.project_name}-${var.environment}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  os_type             = "Linux"
  sku_name            = "Y1" # Consumption: pay per execution
}

resource "azurerm_linux_function_app" "main" {
  name                       = "func-${var.project_name}-${random_string.suffix.result}"
  resource_group_name        = azurerm_resource_group.main.name
  location                   = azurerm_resource_group.main.location
  service_plan_id            = azurerm_service_plan.func.id
  storage_account_name       = azurerm_storage_account.func.name
  storage_account_access_key = azurerm_storage_account.func.primary_access_key
  https_only                 = true

  site_config {
    application_stack {
      dotnet_version              = "8.0"
      use_dotnet_isolated_runtime = true
    }
  }

  app_settings = {
    IoTHubConnection = local.iothub_events_connection
    IoTHubEventPath  = azurerm_iothub.main.event_hub_events_path
    ConsumerGroup    = azurerm_iothub_consumer_group.functions.name
    CosmosEndpoint   = azurerm_cosmosdb_account.main.endpoint
    CosmosKey        = azurerm_cosmosdb_account.main.primary_key
    APPLICATIONINSIGHTS_CONNECTION_STRING = azurerm_application_insights.main.connection_string
  }
}
