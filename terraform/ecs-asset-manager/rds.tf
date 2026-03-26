# Master password must satisfy both PostgreSQL and SQL Server RDS rules (avoid / " @ ; etc.).
resource "random_password" "db_master" {
  length           = 32
  special          = true
  override_special = "!#%^*-_=+?"
}

resource "aws_db_instance" "postgres" {
  count = var.database_engine == "postgresql" ? 1 : 0

  identifier     = "${local.prefix}-pg"
  engine         = "postgres"
  engine_version = data.aws_rds_engine_version.postgres[0].version
  instance_class = var.db_instance_class

  allocated_storage     = var.db_allocated_storage_gb
  max_allocated_storage = var.db_allocated_storage_gb * 2
  storage_encrypted     = true

  db_name  = "assetmanager"
  username = "assetapp"
  password = random_password.db_master.result

  db_subnet_group_name   = aws_db_subnet_group.main.name
  vpc_security_group_ids  = [aws_security_group.rds.id]
  publicly_accessible     = false
  skip_final_snapshot     = true
  backup_retention_period = 1

  lifecycle {
    prevent_destroy = false
  }
}

resource "aws_db_instance" "sqlserver" {
  count = var.database_engine == "sqlserver" ? 1 : 0

  identifier     = "${local.prefix}-mssql"
  engine         = "sqlserver-ex"
  engine_version = var.sqlserver_engine_version
  license_model  = "license-included"

  instance_class = var.db_instance_class

  allocated_storage     = var.db_allocated_storage_gb
  max_allocated_storage = var.db_allocated_storage_gb * 2
  storage_encrypted     = true

  username = "assetapp"
  password = random_password.db_master.result
  db_name  = "assetmanager"

  db_subnet_group_name   = aws_db_subnet_group.main.name
  vpc_security_group_ids = [aws_security_group.rds.id]
  publicly_accessible    = false
  skip_final_snapshot    = true
  backup_retention_period = 1

  lifecycle {
    prevent_destroy = false
  }
}
