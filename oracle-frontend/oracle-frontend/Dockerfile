# --- 阶段 1: 构建阶段 (Build Stage) ---
# 使用一个包含Node.js的官方镜像作为构建环境。
# '18-alpine' 是一个轻量级的版本，可以加快构建速度。
FROM node:18-alpine AS build

# 设置容器内的工作目录
WORKDIR /app

# 优化技巧：只拷贝package.json和lock文件，利用Docker的缓存机制。
# 只要这些文件不改变，下一步的 'npm install' 就不会重新执行。
COPY package*.json ./

# 安装项目所有依赖
RUN npm install

# 拷贝所有剩余的源代码到工作目录
COPY . .

# 执行构建命令，这将生成一个 'dist' 文件夹，包含所有生产环境的静态文件。
RUN npm run build

# --- 阶段 2: 生产阶段 (Production Stage) ---
# 使用一个非常轻量的官方Nginx镜像作为最终的运行环境。
FROM nginx:stable-alpine

# 将构建阶段生成的静态文件，从 'build' 阶段拷贝到Nginx的默认网站根目录。
COPY --from=build /app/dist /usr/share/nginx/html

# 将我们自定义的Nginx配置文件拷贝到Nginx的配置目录中，覆盖默认配置。
COPY nginx.conf /etc/nginx/conf.d/default.conf

# 声明容器将对外暴露80端口 (Nginx的默认HTTP端口)
EXPOSE 80

# 容器启动时运行的命令：在前台启动Nginx服务。
CMD ["nginx", "-g", "daemon off;"]