# AWS Budget for cost monitoring
resource "aws_budgets_budget" "monthly" {
  name         = "${var.project_name}-monthly-budget"
  budget_type  = "COST"
  limit_amount = "100"
  limit_unit   = "USD"
  time_unit    = "MONTHLY"

  notification {
    comparison_operator        = "GREATER_THAN"
    threshold                  = 80
    threshold_type             = "PERCENTAGE"
    notification_type          = "ACTUAL"
    subscriber_email_addresses = [var.budget_email]
  }

  notification {
    comparison_operator        = "GREATER_THAN"
    threshold                  = 100
    threshold_type             = "PERCENTAGE"
    notification_type          = "FORECASTED"
    subscriber_email_addresses = [var.budget_email]
  }

  tags = {
    Name = "${var.project_name}-monthly-budget"
  }
}

# Cost anomaly monitor (COMMENTED OUT - AWS account limit reached, shared with sri-template)
# resource "aws_ce_anomaly_monitor" "main" {
#   name              = "${var.project_name}-anomaly-monitor"
#   monitor_type      = "DIMENSIONAL"
#   monitor_dimension = "SERVICE"
#
#   tags = {
#     Name = "${var.project_name}-anomaly-monitor"
#   }
# }

# Cost anomaly subscription (COMMENTED OUT - depends on monitor above)
# resource "aws_ce_anomaly_subscription" "main" {
#   name      = "${var.project_name}-anomaly-subscription"
#   frequency = "DAILY"
#
#   monitor_arn_list = [
#     aws_ce_anomaly_monitor.main.arn
#   ]
#
#   subscriber {
#     type    = "EMAIL"
#     address = var.budget_email
#   }
#
#   threshold_expression {
#     dimension {
#       key           = "ANOMALY_TOTAL_IMPACT_ABSOLUTE"
#       values        = ["10"]
#       match_options = ["GREATER_THAN_OR_EQUAL"]
#     }
#   }
#
#   tags = {
#     Name = "${var.project_name}-anomaly-subscription"
#   }
# }





