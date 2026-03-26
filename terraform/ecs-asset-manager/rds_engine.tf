# Resolves one PostgreSQL version in the current region.
# With version "16", default_only picks AWS's default 16.x build.
data "aws_rds_engine_version" "postgres" {
  count   = var.database_engine == "postgresql" ? 1 : 0
  engine  = "postgres"
  version = var.postgresql_engine_version
  default_only = true
}
