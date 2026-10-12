FROM nginx:stable-alpine
COPY dist/ /usr/share/nginx/html/
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
# The delivered bundle uses a fixed local API URL. Use the same-origin proxy.
RUN find /usr/share/nginx/html -type f \( -name '*.js' -o -name '*.html' \) -exec sed -i -e 's|http://localhost:5042||g' -e 's|http://127.0.0.1:5042||g' {} +
EXPOSE 80
