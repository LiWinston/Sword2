# 使用Nginx基础镜像
FROM nginx:alpine

# 将当前目录下的 WebGL 构建文件复制到 Nginx 的默认静态文件目录
COPY ./WebBuild/Build /usr/share/nginx/html/Build
COPY ./WebBuild/TemplateData /usr/share/nginx/html/TemplateData
COPY ./WebBuild/index.html /usr/share/nginx/html/

# 暴露端口80
EXPOSE 80

# 启动Nginx服务
CMD ["nginx", "-g", "daemon off;"]
