variable "aws_region" {
  type        = string
  description = "AWS region for all resources in this stack (VPC, ECR, ECS, RDS, etc.)."
  default     = "us-east-1"
}

variable "aws_profile" {
  type        = string
  description = "AWS CLI profile (e.g. my-asset-project)."
  default     = "my-asset-project"
}

variable "project_name" {
  type    = string
  default = "asset-manager"
}

variable "environment" {
  type    = string
  default = "prod"
}

variable "vpc_cidr" {
  type    = string
  default = "10.40.0.0/16"
}

variable "github_org" {
  type        = string
  description = "GitHub org or user name for OIDC trust (e.g. octocat)."
  default     = ""
}

variable "github_repo" {
  type        = string
  description = "Repository name under the org (e.g. Asset_Management_Register)."
  default     = ""
}

variable "github_branch" {
  type    = string
  default = "main"
}

variable "enable_github_oidc" {
  type        = bool
  description = "Create IAM OIDC provider + role for GitHub Actions (needs org + repo set)."
  default     = false
}

variable "ecs_desired_count" {
  type        = number
  description = "Use 0 until the first image exists in ECR; then set to 1 (or scale as needed)."
  default     = 0
}

variable "database_engine" {
  type        = string
  description = "RDS engine for the app: postgresql or sqlserver (SQL Server Express)."
  default     = "postgresql"

  validation {
    condition     = contains(["postgresql", "sqlserver"], var.database_engine)
    error_message = "database_engine must be postgresql or sqlserver."
  }
}

variable "postgresql_engine_version" {
  type        = string
  description = "PostgreSQL version selector for aws_rds_engine_version. Major value (e.g. 16) is recommended; the data source uses default_only=true to choose one regional default build."
  default     = "16"
}

variable "sqlserver_engine_version" {
  type        = string
  description = "SQL Server Express version string for RDS (adjust if apply fails in your region)."
  default     = "15.00.4355.150.v1"
}

variable "db_allocated_storage_gb" {
  type    = number
  default = 20
}

variable "db_instance_class" {
  type    = string
  default = "db.t3.micro"
}

variable "container_cpu" {
  type    = number
  default = 512
}

variable "container_memory" {
  type    = number
  default = 1024
}
