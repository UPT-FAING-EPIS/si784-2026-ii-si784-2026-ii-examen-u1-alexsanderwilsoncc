terraform {
  required_version = ">= 1.9, < 2.0"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }
  backend "azurerm" {}
}
provider "azurerm" {
  features {}
}
resource "azurerm_resource_group" "wallet" {
  name     = "${var.name}-rg"
  location = var.location
}
resource "azurerm_container_registry" "wallet" {
  name                = var.registry_name
  resource_group_name = azurerm_resource_group.wallet.name
  location            = azurerm_resource_group.wallet.location
  sku                 = "Basic"
  admin_enabled       = false
}
resource "azurerm_service_plan" "wallet" {
  name                = "${var.name}-plan"
  resource_group_name = azurerm_resource_group.wallet.name
  location            = azurerm_resource_group.wallet.location
  os_type             = "Linux"
  sku_name            = "B1"
  worker_count        = 1
}
resource "azurerm_linux_web_app" "wallet" {
  name                = var.name
  resource_group_name = azurerm_resource_group.wallet.name
  location            = azurerm_resource_group.wallet.location
  service_plan_id     = azurerm_service_plan.wallet.id
  https_only          = true
  identity { type = "SystemAssigned" }
  site_config {
    always_on                               = true
    minimum_tls_version                     = "1.2"
    ftps_state                              = "Disabled"
    health_check_path                       = "/health"
    container_registry_use_managed_identity = true
    application_stack {
      docker_image_name   = var.image_tag
      docker_registry_url = "https://${azurerm_container_registry.wallet.login_server}"
    }
  }
  app_settings = {
    WEBSITES_PORT                       = "8080"
    WEBSITES_ENABLE_APP_SERVICE_STORAGE = "true"
    ConnectionStrings__Wallet           = "Data Source=/home/data/wallet.db;Default Timeout=10"
  }
  lifecycle {
    ignore_changes = [site_config[0].application_stack[0].docker_image_name]
  }
}
resource "azurerm_role_assignment" "pull" {
  scope                = azurerm_container_registry.wallet.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_linux_web_app.wallet.identity[0].principal_id
}
