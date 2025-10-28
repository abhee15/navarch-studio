FROM node:20-alpine AS base
WORKDIR /app

FROM base AS deps
# Copy unit-conversion package first
COPY packages/unit-conversion/typescript /packages/unit-conversion/typescript
WORKDIR /packages/unit-conversion/typescript
RUN npm ci && npm run build

# Now build frontend
WORKDIR /app
COPY frontend/package*.json ./
RUN npm ci

FROM base AS dev
# Copy built unit-conversion package
COPY --from=deps /packages /packages
COPY --from=deps /app/node_modules ./node_modules
COPY frontend .
EXPOSE 3000
CMD ["npm", "run", "dev", "--", "--host"]
