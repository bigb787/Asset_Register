resource "aws_secretsmanager_secret" "app" {
  name                    = "${local.prefix}/app-config"
  recovery_window_in_days = var.environment == "prod" ? 30 : 0
}

resource "aws_secretsmanager_secret_version" "app" {
  secret_id     = aws_secretsmanager_secret.app.id
  secret_string = jsonencode(local.app_secret_payload)
}
