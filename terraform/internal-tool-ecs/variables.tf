variable "aws_region" {
  type    = string
  default = "us-east-1"
}

variable "aws_profile" {
  type        = string
  description = "AWS CLI named profile used by Terraform."
  default     = "my-asset-project"
}

variable "project_name" {
  type    = string
  default = "internal-tool"
}

variable "vpc_cidr" {
  type    = string
  default = "10.40.0.0/16"
}

variable "subnet_newbits" {
  type        = number
  description = "If VPC is /16 and newbits=4, subnets are /20."
  default     = 4
}

variable "allowed_ingress_cidr" {
  type        = string
  description = "Your public IP CIDR allowed to reach the ALB, e.g. 203.0.113.10/32"
}

# App runtime
variable "app_choice" {
  type        = string
  description = "Which internal tool to run."
  default     = "appsmith"
  validation {
    condition     = contains(["appsmith", "budibase"], var.app_choice)
    error_message = "app_choice must be 'appsmith' or 'budibase'."
  }
}

variable "container_image" {
  type        = string
  description = "Docker image to run. Leave empty to use a sensible default based on app_choice."
  default     = ""
}

variable "container_port" {
  type        = number
  description = "Port the container listens on."
  default     = 80
}

variable "desired_count" {
  type    = number
  default = 1
}

variable "task_cpu" {
  type    = number
  default = 1024
}

variable "task_memory" {
  type    = number
  default = 2048
}

variable "healthcheck_path" {
  type    = string
  default = "/"
}

# RDS PostgreSQL
variable "db_instance_class" {
  type    = string
  default = "db.t4g.micro"
}

variable "db_allocated_storage_gb" {
  type    = number
  default = 20
}

variable "postgres_engine_version" {
  type    = string
  default = "16.3"
}

variable "db_name" {
  type    = string
  default = "appdb"
}

variable "db_username" {
  type    = string
  default = "appadmin"
}

