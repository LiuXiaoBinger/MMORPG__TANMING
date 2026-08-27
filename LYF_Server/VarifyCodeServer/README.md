nodejs可以去官网下载https://nodejs.org/en
我们新建GRpcServer文件夹，在文件夹内部初始化server要用到的nodejs库的配置文件

GRpcServer文件夹cmd 进入终端执行以下命令

npm init

npm cache clean --force

npm config set registry https://registry.npmmirror.com

 npm install @grpc/grpc-js

npm install @grpc/proto-loader

npm install @grpc/proto-loader

npm run server
