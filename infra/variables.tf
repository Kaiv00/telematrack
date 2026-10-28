variable "project_name" {
  type    = string
  default = "telematrack"
}

variable "location" {
  type    = string
  default = "Japan East"
}

variable "environment" {
  type    = string
  default = "dev"
}

variable "alert_email" {
  description = "Email address that receives critical fault alerts"
  type        = string
  # No default on purpose: set it in a git-ignored terraform.tfvars file
}