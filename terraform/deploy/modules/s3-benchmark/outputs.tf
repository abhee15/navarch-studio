output "raw_bucket_name" {
  description = "Raw benchmark bucket name"
  value       = aws_s3_bucket.raw.bucket
}

output "curated_bucket_name" {
  description = "Curated benchmark bucket name"
  value       = aws_s3_bucket.curated.bucket
}
