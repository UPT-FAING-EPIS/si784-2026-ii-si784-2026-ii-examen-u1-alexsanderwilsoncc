output "application_url" {
  value = "https://${azurerm_linux_web_app.wallet.default_hostname}"
}
output "resource_group" {
  value = azurerm_resource_group.wallet.name
}
output "registry_name" {
  value = azurerm_container_registry.wallet.name
}
output "registry_server" {
  value = azurerm_container_registry.wallet.login_server
}
output "app_name" {
  value = azurerm_linux_web_app.wallet.name
}
