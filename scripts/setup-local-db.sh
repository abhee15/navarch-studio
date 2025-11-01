#!/usr/bin/env bash
# Setup local database for docker-compose development environment
# Applies Entity Framework migrations and seeds the database with test data

set -e

SKIP_MIGRATIONS=false
SKIP_SEEDS=false

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --skip-migrations)
            SKIP_MIGRATIONS=true
            shift
            ;;
        --skip-seeds)
            SKIP_SEEDS=true
            shift
            ;;
        *)
            echo "Unknown option: $1"
            echo "Usage: $0 [--skip-migrations] [--skip-seeds]"
            exit 1
            ;;
    esac
done

echo "🔄 Setting up local database..."

# Check if docker-compose is running
echo ""
echo "Checking if docker-compose services are running..."
if ! docker compose ps postgres --quiet > /dev/null 2>&1; then
    echo "❌ PostgreSQL container is not running. Please run 'docker compose up -d postgres' first."
    exit 1
fi
echo "✅ PostgreSQL is running"

# Wait for PostgreSQL to be ready
echo ""
echo "Waiting for PostgreSQL to be ready..."
MAX_RETRIES=30
RETRY=0
while [ $RETRY -lt $MAX_RETRIES ]; do
    if docker compose exec -T postgres pg_isready -U postgres > /dev/null 2>&1; then
        echo "✅ PostgreSQL is ready"
        break
    fi
    RETRY=$((RETRY + 1))
    echo "⏳ Waiting for PostgreSQL... ($RETRY/$MAX_RETRIES)"
    sleep 1
done

if [ $RETRY -eq $MAX_RETRIES ]; then
    echo "❌ PostgreSQL did not become ready in time"
    exit 1
fi

# Apply migrations
if [ "$SKIP_MIGRATIONS" = false ]; then
    echo ""
    echo "🔄 Applying Entity Framework migrations..."

    echo ""
    echo "  📦 IdentityService migrations..."
    (cd backend/IdentityService && \
        dotnet ef database update --connection "Host=localhost;Port=5433;Database=sri_template_dev;Username=postgres;Password=postgres")
    echo "  ✅ IdentityService migrations applied"

    echo ""
    echo "  📦 DataService migrations..."
    (cd backend/DataService && \
        dotnet ef database update --connection "Host=localhost;Port=5433;Database=sri_template_dev;Username=postgres;Password=postgres")
    echo "  ✅ DataService migrations applied"
else
    echo ""
    echo "⏭️  Skipping migrations (--skip-migrations flag set)"
fi

# Seed data
if [ "$SKIP_SEEDS" = false ]; then
    echo ""
    echo "🌱 Seeding database with test data..."

    SEED_FILE="database/seeds/dev-seed.sql"
    if [ -f "$SEED_FILE" ]; then
        cat "$SEED_FILE" | docker compose exec -T postgres psql -U postgres -d sri_template_dev
        echo "✅ Database seeded successfully"
    else
        echo "⚠️  Seed file not found: $SEED_FILE"
    fi
else
    echo ""
    echo "⏭️  Skipping seeds (--skip-seeds flag set)"
fi

echo ""
echo "✅ Local database setup complete!"
echo ""
echo "📝 Test users created:"
echo "   • admin@example.com (password: password)"
echo "   • user@example.com (password: password)"
echo ""
echo "🚀 You can now login to the application"
