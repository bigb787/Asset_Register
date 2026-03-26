locals {
  prefix = "${var.project_name}-${var.environment}"

  name_prefix = substr(replace(local.prefix, "_", "-"), 0, 32)

  azs = slice(data.aws_availability_zones.available.names, 0, 2)

  public_subnet_cidrs  = [for i, z in local.azs : cidrsubnet(var.vpc_cidr, 4, i)]
  private_app_cidrs    = [for i, z in local.azs : cidrsubnet(var.vpc_cidr, 4, i + 4)]
  private_data_cidrs  = [for i, z in local.azs : cidrsubnet(var.vpc_cidr, 4, i + 8)]

  github_subject_filter = var.github_org != "" && var.github_repo != "" ? "repo:${var.github_org}/${var.github_repo}:ref:refs/heads/${var.github_branch}" : ""

  ecr_image = "${aws_ecr_repository.app.repository_url}:latest"

  # One branch references only the active RDS resource (count 0 on the other).
  app_secret_payload = var.database_engine == "postgresql" ? {
    ConnectionStrings__DefaultConnection = "Host=${aws_db_instance.postgres[0].address};Port=${aws_db_instance.postgres[0].port};Database=${aws_db_instance.postgres[0].db_name};Username=${aws_db_instance.postgres[0].username};Password=${random_password.db_master.result};SSL Mode=Require;Trust Server Certificate=true"
    Database__Provider                   = "PostgreSQL"
    ASPNETCORE_ENVIRONMENT               = "Production"
    Reports__BucketName                  = aws_s3_bucket.reports.id
  } : {
    ConnectionStrings__DefaultConnection = "Server=tcp:${aws_db_instance.sqlserver[0].address},${aws_db_instance.sqlserver[0].port};Database=${aws_db_instance.sqlserver[0].db_name};User Id=${aws_db_instance.sqlserver[0].username};Password=${random_password.db_master.result};Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
    Database__Provider                   = "SqlServer"
    ASPNETCORE_ENVIRONMENT               = "Production"
    Reports__BucketName                  = aws_s3_bucket.reports.id
  }
}

data "aws_availability_zones" "available" {
  state = "available"
}

data "aws_caller_identity" "current" {}
