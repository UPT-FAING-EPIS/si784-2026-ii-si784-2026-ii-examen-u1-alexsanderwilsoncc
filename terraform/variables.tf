variable "name" {
  description = "Globally unique App Service name."
  type        = string
  validation {
    condition     = can(regex("^[a-z][a-z0-9-]{2,49}$", var.name))
    error_message = "Use 3-50 lowercase letters, numbers or hyphens."
  }
}
variable "registry_name" {
  description = "Globally unique Azure Container Registry name."
  type        = string
  validation {
    condition     = can(regex("^[a-z][a-z0-9]{4,49}$", var.registry_name))
    error_message = "Use 5-50 lowercase alphanumeric characters."
  }
}
variable "location" {
  type    = string
  default = "eastus"
}
variable "image_tag" {
  description = "Initial image; deploy.yml replaces it with an immutable commit tag."
  type        = string
  default     = "wallet:initial"
}
