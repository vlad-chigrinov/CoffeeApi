FROM node:24-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:1.29-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
# Writes /config.js from env vars (PRIMEUI_LICENSE) on every container start. CRs are stripped in case of a Windows checkout.
COPY docker-entrypoint.d/40-config.sh /docker-entrypoint.d/40-config.sh
RUN sed -i 's/\r$//' /docker-entrypoint.d/40-config.sh && chmod +x /docker-entrypoint.d/40-config.sh
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
