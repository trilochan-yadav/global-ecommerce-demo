# Stage 1 — build
FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
RUN npx ng build --configuration docker

# Stage 2 — serve with Nginx
FROM nginx:alpine
COPY --from=build /app/dist/client-app/browser /usr/share/nginx/html
RUN rm -f /usr/share/nginx/html/index.html \
  && mv /usr/share/nginx/html/index.csr.html /usr/share/nginx/html/index.html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80