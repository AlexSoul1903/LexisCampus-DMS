# ==============================================================================
# Build Stage: Compile Vite React Application
# ==============================================================================
FROM node:20-alpine AS build
WORKDIR /app

# Copy dependency definitions and install cleanly
COPY package*.json ./
RUN npm ci

# Copy source code and build production distribution
COPY . .
RUN npm run build

# ==============================================================================
# Runtime Stage: Serve SPA & Reverse Proxy with Nginx Alpine
# ==============================================================================
FROM nginx:alpine AS final

# Copy built distribution assets to default Nginx root
COPY --from=build /app/dist /usr/share/nginx/html

# Replace default Nginx configuration with custom SPA & API proxy configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
