\# 同济大学23届数据库课设项目环境搭建说明

本文档用作部署项目时环境的搭建说明和一些已经创建的变量含义的说明

\## 项目搭建流程

1\. 打开windows虚拟机平台和WSL2功能

2\. 下载docker（最好也下载docker desktop）。在带有`docker-compose.yml`文件的目录下，运行下列指令可一键部署环境：

```

docker-compose up -d

```

第一次运行指令时，会自动下载环境所需的镜像。该环境包含后端、前端、数据库docker容器，在开发时，可以暂时不运行前、后端，只运行数据库容器：

```

docker-compose up -d oracle-db

```

\*\*就我体验来说，电脑上直接使用命令下载容器所需镜像会很慢，所以我建议在docker desktop上先下好镜像再运行命令

各位可在docker desktop的顶端搜索栏里搜索`gvenzl/oracle-xe:latest`、`nginx:latest`、`node:22.17.1`来下载环境所需的三个镜像，下载完毕后应该命令就能跑了\*\*

3\. 在项目的`db-init`文件夹里面有数据库的初始化脚本，在执行完部署命令后应该会自动初始化，大家可以在docker desktop上进对应的容器命令行使用sqlplus或是在电脑上安装像是\*sql developer\*这样的软件来看看有没有正确初始化数据库。

\*\*数据库的用户名：ORACLEDBA，密码23OracleServer，位于xepdb1容器数据库\*\*

\*\*数据库初始只有表，没有原始数据，可自行添加\*\*

\*\*容器内命令行可通过以下命令直接进入：\*\*

```sqlplus ORACLEDBA/23OracleServer@localhost:1521/XEPDB1```

\*\*使用sql developer建立连接的界面如下图所示：\*\*

!\[如何使用sql developer连接数据库](OracleConnection.png)



\## 项目文档结构说明

目前我只检查了后端部分的目录结构，前端部分暂时不用管它

\###1. Program.cs

存放main函数，不建议动

\###2. appsettings.json 和 appsettings.Development.json

目前作用：用来保存数据库的连接字符串，不建议动

\###3. Properties/launchSettings.json

大概是启动时的配置，不建议动

\###4. Models/一堆CS文件

是创建来和数据库的表进行一一对应的类，创建的对象表示关系模式的一个实例，目前仅规定了主键、外键约束，一般来讲通过操作这些类创建的对象就能实现数据库操作

\###5.DbContexts/其下的CS文件

是用来管理数据库操作的一系列“数据库上下文”类，用于在应用程序中配置和管理与数据库的连接、实体映射及数据访问操作的统一入口。各分组可各自创建本组的“数据库上下文”文件，尽量避免直接修改创建好的文件。

\###6. controllers/其下的CS文件

是一系列“控制器”类，用于接收客户端请求，调用相应的服务逻辑，并返回处理结果，用于前端与后端之间的交流，方便后续写前端时能够形成明确的接口。



\##项目基础运行效果说明

部署项目环境后，在VS中运行项目可以看到会蹦出一个swaggerUI网页，该网页可以方便调试controller，具体自行研究

