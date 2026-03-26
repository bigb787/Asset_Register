# Asset Manager — ECS Fargate + ALB + RDS PostgreSQL + Terraform

Deploys **`AssetManager`** (ASP.NET Core 9) as one **Fargate** service behind an **ALB (HTTP :80)**, with **RDS PostgreSQL**, **ECR**, **S3** (reports), and **Secrets Manager** for config.

Default **AWS region** for this stack: **`us-east-1`**. Default **profile** in examples: **`my-asset-project`** (override in `terraform.tfvars` if your profile uses another region).

## Prerequisites

- Terraform ≥ 1.5, AWS CLI with that profile
- Docker, for building the image
- GitHub (optional) for OIDC deploy role

## 1) Terraform

```powershell
cd terraform\ecs-asset-manager
copy terraform.tfvars.example terraform.tfvars
notepad terraform.tfvars
terraform init
terraform apply
```

**Database engine** (RDS):

- `database_engine = "postgresql"` (default) — Npgsql connection string + `Database__Provider=PostgreSQL` in Secrets Manager.
- `database_engine = "sqlserver"` — RDS **SQL Server Express** (`sqlserver-ex`), port **1433**, `Database__Provider=SqlServer`.

If SQL Server `apply` fails on **engine version**, adjust `sqlserver_engine_version` in `variables.tf` / tfvars to a value supported in your region (see AWS RDS SQL Server docs).

**PostgreSQL:** the stack uses `data.aws_rds_engine_version` so `postgresql_engine_version` defaults to **`16`** (latest **16.x** available in the region). Pin an exact version only if your org requires it (`postgresql_engine_version = "16.6"` etc.), using a value returned by `aws rds describe-db-engine-versions --engine postgres --region ...`.

Switching engines later usually means **replacing** the RDS instance (new `terraform apply` / state change) and migrating data separately.

Optional GitHub OIDC in `terraform.tfvars`:

```hcl
enable_github_oidc = true
github_org         = "YOUR_GITHUB_USER_OR_ORG"
github_repo        = "Asset_Management_Register"
github_branch      = "main"
```

Re-run `terraform apply`. Copy **`github_actions_role_arn`**, **`ecr_repository_url`**, **`ecs_cluster_name`**, **`ecs_service_name`** for CI.

## 2) First image push

From the **repo root** `Asset_Management_Register` (parent of `AssetManager`):

```powershell
$env:AWS_PROFILE = "my-asset-project"
$region = "us-east-1"
$ecr  = terraform -chdir=terraform/ecs-asset-manager output -raw ecr_repository_url
$registry = ($ecr -split '/')[0]
aws ecr get-login-password --region $region | docker login --username AWS --password-stdin $registry
docker build -f AssetManager/Dockerfile -t "${ecr}:latest" .
docker push "${ecr}:latest"
```

## 3) Start tasks

Either set in `terraform.tfvars`:

```hcl
ecs_desired_count = 1
```

and `terraform apply`, or: **ECS console → service → update desired count = 1**.

## 4) GitHub Actions

Repository **Secrets**:

- `AWS_ROLE_TO_ASSUME` — Terraform output `github_actions_role_arn`
- `ECR_REPOSITORY_URL` — output `ecr_repository_url` (no `:latest` suffix)

Repository **Variables**:

- `ECS_CLUSTER_NAME`
- `ECS_SERVICE_NAME`

Workflow: `.github/workflows/asset-manager-ecs.yml` (push to `main` under `AssetManager/**`).

## App URLs

- `terraform output -raw alb_url`
- Health: `http://<alb-dns>/health`
- API examples: `/api/laptops`, `/api/laptops/free`, `/api/free-assets`, `/api/reports/export/laptops.xlsx`

## Notes

- DB schema: app calls **`EnsureCreatedAsync()`** on PostgreSQL for a simple bootstrap. Switch to **EF migrations** for real production evolution.
- ALB is **HTTP only**; add **ACM + HTTPS** when you have a domain.
- **`terraform destroy`** removes billable resources (empty S3 if needed).
