# Cost Optimization Guide

This template is designed to minimize AWS costs while providing a production-ready architecture pattern that can scale when needed.

## Monthly Cost Breakdown (Dev Environment)

### With AWS Free Tier (First 12 Months)
- **Total Estimated Cost: $0-10/month**

| Service | Free Tier | Usage | Cost |
|---------|-----------|-------|------|
| RDS db.t3.micro | 750 hours/month | 730 hours | **$0** |
| App Runner (3 services) | - | 256MB/0.25vCPU each | $15-25 |
| S3 Storage | 5GB | <500MB | **$0** |
| CloudFront | 50GB data transfer | <5GB | **$0** |
| ECR | 500MB storage | <300MB | **$0** |
| DynamoDB | 25GB storage | <100MB | **$0** |
| Cognito | 50,000 MAUs | <100 users | **$0** |
| Secrets Manager | - | 2 secrets | $0.80 |

**Total with Free Tier: $15-26/month**

### Without Free Tier (After 12 Months)
- **Total Estimated Cost: $30-40/month**

| Service | Monthly Cost |
|---------|--------------|
| RDS db.t3.micro (20GB) | $12-15 |
| App Runner (3 services @ 256MB) | $15-25 |
| S3 Storage + Requests | $0.50-1 |
| CloudFront Data Transfer | $0.50-1 |
| ECR Storage | $0.50 |
| DynamoDB | $0.25 |
| Cognito | $0 (up to 50K MAUs) |
| Secrets Manager | $0.80 |

**Total: $30-40/month**

## What We're NOT Using (and Why)

### 1. NAT Gateway - SAVES $32-45/month
**Traditional Approach:**
- Private subnets for RDS and App Runner
- NAT Gateway for internet access from private subnets
- Cost: $0.045/hour + $0.045/GB = ~$32-45/month

**Our Approach:**
- Public subnets for all resources
- RDS with `publicly_accessible = false`
- Security groups restrict all access
- **Same security, $0 cost**

### 2. Application Load Balancer - SAVES $16-20/month
**Traditional Approach:**
- ALB for routing and SSL termination
- Cost: $0.0225/hour + $0.008/LCU = ~$16-20/month

**Our Approach:**
- App Runner provides built-in load balancing
- CloudFront for SSL termination
- **Included in App Runner pricing**

### 3. Elastic Container Service (ECS/EKS) - SAVES $70-200/month
**Traditional Approach:**
- ECS Fargate or EKS cluster
- Cost: Multiple EC2 instances or Fargate tasks

**Our Approach:**
- App Runner (serverless containers)
- Pay only for what you use
- **65-80% cheaper than ECS/EKS**

### 4. ElastiCache - SAVES $12-50/month
**Traditional Approach:**
- Redis/Memcached for caching
- Cost: $0.017/hour = ~$12/month minimum

**Our Approach:**
- Application-level caching (MobX stores)
- CloudFront edge caching
- **$0 cost for dev/small deployments**

### 5. Multi-AZ Deployment (Dev) - SAVES $15-20/month
**Traditional Approach:**
- Multi-AZ for RDS
- Doubles RDS cost

**Our Approach:**
- Single AZ for dev/staging
- Multi-AZ only for production
- **50% savings on RDS for dev**

## Architecture Decisions for Cost Savings

### 1. Public Subnet Architecture
```
┌─────────────────────────────────────────────────┐
│                     VPC                         │
│                                                 │
│  ┌──────────────────────────────────────────┐  │
│  │         Public Subnet AZ-1               │  │
│  │                                          │  │
│  │  ┌──────────────┐  ┌──────────────┐    │  │
│  │  │ App Runner   │  │    RDS       │    │  │
│  │  │ (Services)   │──│ (private)    │    │  │
│  │  └──────────────┘  └──────────────┘    │  │
│  │         │                                │  │
│  └─────────┼────────────────────────────────┘  │
│            │                                    │
│  ┌─────────▼──────────────────────────────┐   │
│  │         Public Subnet AZ-2             │   │
│  │                                        │   │
│  │  ┌──────────────┐  ┌──────────────┐  │   │
│  │  │ App Runner   │  │    RDS       │  │   │
│  │  │ (Services)   │  │  (standby)   │  │   │
│  │  └──────────────┘  └──────────────┘  │   │
│  └────────────────────────────────────────┘   │
│                                                │
└─────────────────────────────────────────────────┘
         │
         ▼
    Internet Gateway
    (No NAT Gateway needed!)
```

**Security:**
- RDS has `publicly_accessible = false` (not accessible from internet)
- Security groups only allow App Runner → RDS communication
- App Runner services have public endpoints (protected by AWS)
- Same security as private subnets, but no NAT Gateway cost

### 2. Serverless First
- **App Runner**: Serverless containers (vs ECS/EKS)
- **CloudFront**: CDN with edge caching (vs ALB + EC2)
- **S3**: Static hosting (vs EC2 web server)
- **Cognito**: Managed auth (vs custom auth server)

### 3. Minimal Resource Sizes
- **RDS**: db.t3.micro (2 vCPU, 1GB RAM) - Free Tier eligible
- **App Runner**: 256MB RAM, 0.25 vCPU per service
- **Storage**: 20GB RDS, auto-scaling up to 100GB

### 4. Short Retention Periods (Dev)
- **CloudWatch Logs**: 7 days (vs 30-90 days)
- **RDS Backups**: 7 days (vs 30 days)
- **ECR Images**: Keep last 10 (vs unlimited)

## Scaling to Production

When you're ready to scale, here's what to change:

### Small Production (~100-1000 users)
**Additional Cost: +$30-50/month**
- Enable Multi-AZ for RDS (+$15/month)
- Increase App Runner to 512MB/0.5vCPU (+$15-20/month)
- Increase RDS to db.t3.small (+$10-15/month)
- Extend log retention to 30 days (+$2-5/month)

**Total: ~$70-90/month**

### Medium Production (~1000-10000 users)
**Additional Cost: +$100-200/month**
- RDS db.t3.medium (+$25/month from small)
- App Runner 1GB/1vCPU (+$50-70/month from small)
- Add private subnets + NAT Gateway (+$35/month)
- Add ElastiCache for Redis (+$12/month)
- Increase backup retention to 30 days (+$5/month)

**Total: ~$170-290/month**

### Large Production (>10000 users)
**Consider:**
- RDS db.m5.large or db.r5.large ($150-300/month)
- App Runner auto-scaling (3-10 instances)
- Multi-region deployment
- WAF for security ($5 + $1/rule/month)
- Aurora Serverless instead of RDS

**Total: $500-2000+/month (depends on traffic)**

## Free Tier Eligibility

### Always Free
- **Cognito**: 50,000 MAUs
- **Lambda**: 1M requests/month (if you add Lambda)
- **CloudWatch**: 5GB ingestion, 10 custom metrics

### Free for 12 Months
- **RDS**: 750 hours/month of db.t3.micro
- **S3**: 5GB storage, 20,000 GET requests, 2,000 PUT requests
- **CloudFront**: 50GB data transfer out, 2M HTTP/HTTPS requests
- **ECR**: 500MB storage/month

### Never Free (But Cheap)
- **App Runner**: ~$5-8/service/month for small sizes
- **Secrets Manager**: $0.40/secret/month
- **DynamoDB**: $0.25/GB storage (pay-per-request)

## Cost Monitoring and Alerts

### Budget Alerts
The template includes:
- AWS Budget set to $100/month
- Alerts at 80% ($80)
- Forecast alerts when projected to exceed 100%

### Cost Explorer
Enable Cost Explorer to:
- Track daily spending
- Identify cost trends
- Compare month-over-month
- Break down by service

### Tags for Cost Tracking
All resources are tagged:
- `Project`: Your project name
- `Environment`: dev/staging/prod
- `ManagedBy`: Terraform
- `CostCenter`: engineering

Use these tags in AWS Cost Explorer to filter costs.

## Best Practices for Keeping Costs Low

### 1. Turn Off What You Don't Need
```powershell
# Stop dev environment when not in use
terraform destroy -target=module.app_runner -var-file=environments/dev.tfvars

# Keep RDS (has data) but stop App Runner services
# Saves ~$15-20/month
```

### 2. Use Spot Instances (Advanced)
For non-critical workloads, consider:
- Spot instances for batch jobs
- Fargate Spot for non-production (if switching from App Runner)

### 3. Optimize Images
- Use multi-stage Docker builds
- Minimize layer sizes
- Use Alpine Linux base images
- Smaller images = faster deploys = lower costs

### 4. Monitor and Alert
- Set up billing alarms
- Review Cost Explorer monthly
- Delete unused resources
- Archive old logs to S3 Glacier

### 5. Dev/Staging Automation
```powershell
# Auto-shutdown script for nights/weekends
# Run via GitHub Actions scheduled workflow
# Can save 60-70% on dev/staging costs
```

## Comparison: Traditional vs Cost-Optimized

| Component | Traditional | Cost-Optimized | Savings |
|-----------|------------|----------------|---------|
| Networking | Private subnets + NAT Gateway | Public subnets only | $35/month |
| Compute | ECS Fargate (2 tasks) | App Runner (serverless) | $50/month |
| Load Balancer | ALB | Built into App Runner | $18/month |
| Database | db.t3.small | db.t3.micro | $15/month |
| Caching | ElastiCache | Application-level | $12/month |
| Monitoring | CloudWatch + 3rd party | CloudWatch only | $20/month |
| **Total Traditional** | **~$180/month** | **~$30/month** | **$150/month** |

## When to Spend More

Don't optimize prematurely! Consider spending more when:

1. **You have paying customers** - Revenue > AWS costs
2. **Performance matters** - User experience impacts revenue
3. **High availability needed** - Downtime costs more than Multi-AZ
4. **Compliance requirements** - May need private subnets, encryption, etc.
5. **Team productivity** - Don't waste engineer time to save $10/month

## Conclusion

This template is designed for:
- ✅ Learning and experimentation
- ✅ Side projects and MVPs
- ✅ Small production apps (<1000 users)
- ✅ Budget-conscious development

**Start cheap, scale when needed!**

For most developers, this architecture will cost **$30-40/month** (or **$0-10 with Free Tier**) and can easily scale to handle thousands of users before requiring changes.

---

*Last updated: October 2025*
*Based on AWS us-east-1 pricing*






