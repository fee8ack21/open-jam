FROM node:22-alpine AS builder
RUN corepack enable && corepack prepare pnpm@9.7.0 --activate
WORKDIR /app
COPY package.json pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile
COPY . .
RUN pnpm build

FROM nginx:alpine
COPY --from=builder /app/.vitepress/dist /usr/share/nginx/html
EXPOSE 80
