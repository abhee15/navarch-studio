# Create a NEW Cognito App Client for sri-subscription
# This shares the user pool but has its own client ID

resource "aws_cognito_user_pool_client" "sri_subscription" {
  name         = "${var.project_name}-client"
  user_pool_id = tolist(data.aws_cognito_user_pools.existing.ids)[0]

  explicit_auth_flows = [
    "ALLOW_USER_PASSWORD_AUTH",
    "ALLOW_USER_SRP_AUTH",
    "ALLOW_REFRESH_TOKEN_AUTH",
    "ALLOW_USER_PASSWORD_AUTH"
  ]

  prevent_user_existence_errors = "ENABLED"

  # Token validity
  refresh_token_validity = 30
  access_token_validity  = 60
  id_token_validity      = 60

  token_validity_units {
    refresh_token = "days"
    access_token  = "minutes"
    id_token      = "minutes"
  }

  # Allowed OAuth flows
  allowed_oauth_flows_user_pool_client = true
  allowed_oauth_flows                  = ["code", "implicit"]
  allowed_oauth_scopes                 = ["email", "openid", "profile"]

  # Callback URLs will be updated after frontend deployment
  callback_urls = ["http://localhost:3001"]
  logout_urls   = ["http://localhost:3001"]

  # Read and write attributes
  read_attributes = [
    "email",
    "email_verified",
    "name",
    "updated_at"
  ]

  write_attributes = [
    "email",
    "name",
    "updated_at"
  ]
}

output "sri_subscription_cognito_client_id" {
  description = "Cognito User Pool Client ID for sri-subscription"
  value       = aws_cognito_user_pool_client.sri_subscription.id
  sensitive   = true
}

output "sri_subscription_cognito_domain" {
  description = "Cognito Domain for sri-subscription (shared with sri-template)"
  value       = "sri-test-project-1-1zvox1e6"
}

