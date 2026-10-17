output "resource_group_name" {
  description = "Name of the resource group"
  value       = azurerm_resource_group.main.name
}

output "iothub_name" {
  description = "Name of the IoT Hub, used to register devices"
  value       = azurerm_iothub.main.name
}

output "iothub_hostname" {
  description = "Hostname the devices connect to"
  value       = azurerm_iothub.main.hostname
}