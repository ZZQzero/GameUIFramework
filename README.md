# GameUIFramework
一个简单,方便，易于使用的Unity UI框架

在网上看了看多大佬们写的UI框架，写的都很好，但是有的设计过于复杂，使用起来不够方便，不利于扩展和维护，有的功能比较单一，不满足一些需求。所以我决定自己写一个简单的UI框架，在参考许多大佬设计的基础上，加上自己多年的开发经验，分享出来供大家使用，学习和参考。

目前框架中使用到的插件：

1、[UniTask](https://github.com/Cysharp/UniTask) 

2、[YooAsset](https://github.com/tuyoogame/YooAsset)

# 框架中目前已经实现的功能
1、UI界面代码自动生成，UI界面的加载和卸载，支持异步加载,支持UI界面的缓存,UI界面反向切换（栈管理）。

2、无限循环列表[LoopScrollRect](https://github.com/qiankanglai/LoopScrollRect)，在这个基础上进行了一些修改和封装，使其更加方便使用。

3、红点系统，类似有向无环图的结构设计，支持多个父节点和多个子节点，自动检测红点中是否存在环，自动检测配置中是否有重复配置，可以自由的添加和删除红点，整个红点系统中，程序只需要简单的写几行代码，后面完全交给策划配置。

4、对象池，支持多个对象池，支持对象池的自动清理，支持定时清理超出限定范围的对象，支持对象池同步加载和异步加载。

5、功能持续更新中。。。想到什么功能，就加什么功能，欢迎大家提出建议和意见。

# 安装方法
1、在Unity中打开Package Manager窗口，点击左上角的+号，选择Add package from git URL，然后输入以下地址：
https://github.com/ZZQzero/GameUIFramework.git 点击Add

# 问题反馈
![9928cabc04e7a29f2a1579802dc4bae6](https://github.com/user-attachments/assets/6f6537e0-5bda-4da9-b189-377764113a0e)

![e00c532eafebc43116032c64684b3e4b](https://github.com/user-attachments/assets/07055802-abf6-4d8d-92ed-92ff439a6631)
点击提交
