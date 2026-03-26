output "alb_url" {
  description = "Open in browser (HTTP). Point a DNS name here or use this hostname."
  value       = "http://${aws_lb.main.dns_name}"
}

output "ecr_repository_url" {
  value = aws_ecr_repository.app.repository_url
}

output "ecs_cluster_name" {
  value = aws_ecs_cluster.main.name
}

output "ecs_service_name" {
  value = aws_ecs_service.app.name
}

output "github_actions_role_arn" {
  description = "Set as GitHub secret AWS_ROLE_TO_ASSUME after enabling OIDC in tfvars."
  value       = try(aws_iam_role.github_actions[0].arn, null)
}

output "secrets_manager_secret_arn" {
  value     = aws_secretsmanager_secret.app.arn
  sensitive = false
}

output "reports_bucket" {
  value = aws_s3_bucket.reports.id
}

output "database_engine" {
  value = var.database_engine
}

output "rds_endpoint" {
  value     = var.database_engine == "postgresql" ? aws_db_instance.postgres[0].address : aws_db_instance.sqlserver[0].address
  sensitive = false
}

output "rds_port" {
  value = var.database_engine == "postgresql" ? aws_db_instance.postgres[0].port : aws_db_instance.sqlserver[0].port
}
