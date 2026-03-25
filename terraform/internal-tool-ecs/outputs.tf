output "alb_dns_name" {
  description = "ALB DNS name (accessible only from allowed_ingress_cidr)."
  value       = aws_lb.this.dns_name
}

output "rds_endpoint" {
  description = "RDS PostgreSQL endpoint/hostname."
  value       = aws_db_instance.postgres.address
}

