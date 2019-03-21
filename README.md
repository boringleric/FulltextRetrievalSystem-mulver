# FulltextRetrievalSystem-mulver
毕业设计-全文检索系统-分布式版本

在单机版的基础上，使用Orleans 1.5.3框架和Redis 3.2.100 Windows ver实现分布式检索。  
**由于安全提示，packages.config的 <package id="Microsoft.Data.OData" version="5.8.4" targetFramework="net461" /> 原版本为5.6.4，如有问题可以改回原版本**  

**需要注意：VS调试要开启IIS64位版本！**  

LocalDB存放着本地检索数据库文件夹，有部分爬虫爬取的测试数据在里面。  

FulltextRetrievalSystem/packages/Xapian.1.2.23/的_XapianSharp.dll和zlib1.dll需手动复制到执行文件夹下，包括Orleans的服务器文件夹下，修改的xapian代码见本人的XapianModified项目；除此之外，jieba.net的resource文件夹也要复制到同一文件夹下，否则中文解析会出错。  

CrawlPart是爬虫操作相关文件夹，PushFunction是邮件推送文件夹，ScheduledTask是任务爬虫和任务推送文件夹，XapianPart是数据库文件夹，WebView是网站，WebCommon是数据交互层，GrainInterfaces和Grain是Orleans相关内容，Cache是redis和Orleans交互模块，OrleansHost是Orleans服务器。    

如果要配置路径之类的东西，请注意修改web.config或者app.config的对应内容！  

爬虫配置需要登录管理员账号进行配置，管理员账号admin@123.com，密码是Pa$$w0rd，随便玩，反正没啥权限……  

简单配置过程：  
1、Orleans的负载均衡等内容使用SqlServer配置，数据库创建脚本CreateOrleansTables_SqlServer.sql在nuget的Orleans的packages里面有添加；  
2、开启Redis;    
3、解压并复制本地检索数据库到指定位置；  
4、复制_XapianSharp.dll、zlib1.dll、resource文件夹到指定位置（Orleans服务器执行文件和网站的bin目录下）；  
5、修改host.xml和client.xml的内容，指定ip地址，数据库用户名和密码；  
6、cmd运行Orleans服务器，OrleansHostServ.exe，等到fin指示完成；  
7、运行网站执行查询。

想要运行彩蛋需要安装matlab运行库2017a版本，内容就是ADNHCS-code的dll版本……没啥好玩的，口令是：“我想玩旅行商问题的游戏上上下下左右左右BABA”，having fun！  


代码天长日久没动过了，要把它们拿出来通通风，如果有bug，这是很可能的，请见谅！  
