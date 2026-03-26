resource "aws_ecs_cluster" "main" {
  name = "${local.prefix}-cluster"
}

resource "aws_cloudwatch_log_group" "ecs" {
  name              = "/ecs/${local.prefix}-app"
  retention_in_days = 30
}

resource "aws_ecs_task_definition" "app" {
  family                   = "${local.prefix}-app"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = tostring(var.container_cpu)
  memory                   = tostring(var.container_memory)
  execution_role_arn       = aws_iam_role.ecs_execution.arn
  task_role_arn            = aws_iam_role.ecs_task.arn

  container_definitions = jsonencode([{
    name  = "app"
    image = local.ecr_image
    essential = true
    portMappings = [{
      containerPort = 8080
      protocol      = "tcp"
    }]
    secrets = [
      {
        name      = "ConnectionStrings__DefaultConnection"
        valueFrom = "${aws_secretsmanager_secret.app.arn}:ConnectionStrings__DefaultConnection::"
      },
      {
        name      = "Database__Provider"
        valueFrom = "${aws_secretsmanager_secret.app.arn}:Database__Provider::"
      },
      {
        name      = "ASPNETCORE_ENVIRONMENT"
        valueFrom = "${aws_secretsmanager_secret.app.arn}:ASPNETCORE_ENVIRONMENT::"
      },
      {
        name      = "Reports__BucketName"
        valueFrom = "${aws_secretsmanager_secret.app.arn}:Reports__BucketName::"
      }
    ]
    environment = [
      { name = "ASPNETCORE_URLS", value = "http://+:8080" }
    ]
    logConfiguration = {
      logDriver = "awslogs"
      options = {
        "awslogs-group"         = aws_cloudwatch_log_group.ecs.name
        "awslogs-region"        = var.aws_region
        "awslogs-stream-prefix" = "app"
      }
    }
  }])
}

resource "aws_ecs_service" "app" {
  name            = "${local.prefix}-svc"
  cluster         = aws_ecs_cluster.main.id
  task_definition = aws_ecs_task_definition.app.arn
  desired_count   = var.ecs_desired_count
  launch_type     = "FARGATE"

  network_configuration {
    subnets          = aws_subnet.private_app[*].id
    security_groups  = [aws_security_group.ecs.id]
    assign_public_ip = false
  }

  load_balancer {
    target_group_arn = aws_lb_target_group.app.arn
    container_name   = "app"
    container_port   = 8080
  }

  depends_on = [aws_lb_listener.http]
}
